using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.XPath;

namespace Wumpus
{
    //
    // ported to C# from the original 1972 BASIC program listing:
    //  
    //   http://www.atariarchives.org/bcc1/showpage.php?page=250 
    //
    // David Bautista
    // davidb@vertigosoftware.com
    //
    public class WumpusLogic
    {

        private const int DEFAULT_BATS = 2;
        private const int DEFAULT_PITS = 2;
        private const int MAX_SHOTS = 5;
        private static readonly Random RANDOM = new Random();
        private const int START_ARROWS = 5;

        public enum GameStatus 
        {
            InPlay = 0,
            Lost,
            Won
        }

        public class Node
        {

            private readonly SortedList links;
            private readonly string name;
            
            public Node(string name) 
            {
                links = new SortedList();
                this.name = name;
            }
            

            public ICollection LinkedNodes 
            {
                get 
                {
                    return links.Values;
                }
            }

            public string Name 
            {
                get 
                {
                    return name;
                }
            }

            public void AddLinkedNode(Node node) 
            {
                links.Add(node.Name, node);
            }

            public bool IsAdjacentNode(Node node) 
            {
                bool result;
            
                if (node == null) 
                {
                    result = false;
                } 
                else 
                {            
                    result = IsAdjacentNode(node.Name);
                }
            
                return result;
            }

            public bool IsAdjacentNode(string nodeName) 
            {
                return links.ContainsKey(nodeName);
            }        

            public void ClearNodeLinks() 
            {
                links.Clear();
            }

        }

        public enum MovementResult 
        {
            IllegalMove = 0,
            BumpedAWumpus,
            FellInAPit,
            GotByWumpus,            
            SuccessfulMove,
            SuperBatSnatch            
        }

        public enum ShotResult 
        {
            Missed = 0,
            GotByWumpus,                   
            NoMoreArrows,
            ShotSelf,
            ShotWumpus
        }

        public enum SurroundingHazards 
        {
            BatNearby = 1,
            None = 0,
            PitNearby = 2, 
            WumpusNearby = 4
        }

        private int arrows;
        private Hashtable batNodes;
        private GameStatus gameStatus;
        private Hashtable initialBatNodes;
        private Hashtable initialPitNodes;
        private Node initialPlayerNode;
        private Node initialWumpusNode;
        private Hashtable nodes;
        private int numBats;
        private int numPits;
        private Hashtable pitNodes;
        private Node playerNode;
        private Node wumpusNode;


        public WumpusLogic(string setupXmlString) 
        { 
            StringReader stringReader;

            stringReader = null;
            try 
            {
                initialBatNodes = new Hashtable();
                initialPitNodes = new Hashtable();
                nodes = new Hashtable();            
                
                stringReader = new StringReader(setupXmlString);
                SetupNodes(stringReader);
                PlaceItems();
                RestartGame();                
            } 
            finally 
            {
                if (stringReader != null) 
                {
                    stringReader.Close();
                }
            }            
        }

        public WumpusLogic(TextReader setupXmlReader) 
        {            
            initialBatNodes = new Hashtable();
            initialPitNodes = new Hashtable();
            nodes = new Hashtable();

            SetupNodes(setupXmlReader);
            PlaceItems();

            RestartGame();
        }


        public int ArrowsRemaining 
        {
            get 
            {
                return arrows;
            }
        }

        public ICollection BatNodes 
        {
            get 
            {
                if (gameStatus == GameStatus.InPlay) 
                {
                    return null;
                } 
                else 
                {
                    return batNodes.Values;
                }
            }
        }

        public GameStatus Status 
        {
            get 
            {
                return gameStatus;
            }
        }

        public ICollection Nodes 
        {
            get 
            {
                return nodes.Values;
            }
        }

        public Node PlayerNode 
        {
            get 
            {
                return playerNode;
            }
        }

        public ICollection PitNodes 
        {
            get  
            {
                if (gameStatus == GameStatus.InPlay) 
                {
                    return null;
                } 
                else 
                {
                    return pitNodes.Values;
                }
            }
        }

        public Node WumpusNode 
        {
            get 
            {
                if (gameStatus == GameStatus.InPlay) 
                {
                    return null;
                } 
                else 
                {
                    return wumpusNode;
                }
            }
        }
        
        public Node GetNodeByName(string nodeName) 
        {
            return nodes[nodeName] as Node;
        }

        public SurroundingHazards GetSurroundingHazards() 
        {
            IEnumerator enumerator;
            Node node;
            SurroundingHazards results;

            if (playerNode == wumpusNode) 
            {
                results = SurroundingHazards.WumpusNearby;
            } 
            else 
            {
                results = SurroundingHazards.None;
            }
            enumerator = playerNode.LinkedNodes.GetEnumerator();
            while (enumerator.MoveNext()) 
            {
                node = enumerator.Current as Node;
                if (node != null) 
                {
                    if (node == wumpusNode) 
                    {
                        results |= SurroundingHazards.WumpusNearby;
                    }
                    if (pitNodes.ContainsKey(node.Name)) 
                    {
                        results |= SurroundingHazards.PitNearby;
                    }
                    if (batNodes.ContainsKey(node.Name)) 
                    {
                        results |= SurroundingHazards.BatNearby;
                    }
                }
            }

            return results;
        }

        public ICollection Move(string nodeName) 
        {
            return Move(nodeName, false);
        }

        private ICollection Move(string nodeName, bool allowNonAdjacentMove) 
        {
                  
            IEnumerator enumerator;
            int i;
            Node node;
            MovementResult result;
            Queue results;
            IEnumerator subEnumerator;

            results = new Queue();
            node = nodes[nodeName] as Node;
            if ((node == null) ||
                (!allowNonAdjacentMove && !playerNode.IsAdjacentNode(node)) ||
                (gameStatus != GameStatus.InPlay)) 
            {
                results.Enqueue(MovementResult.IllegalMove);
            } 
            else 
            {
                result = MovePlayer(node);
                results.Enqueue(result);
                switch (result) 
                {
                    case MovementResult.BumpedAWumpus:
                        MoveWumpus();       
                        results.Enqueue(MovementResult.GotByWumpus);
                        gameStatus = GameStatus.Lost;
                        break;
                    case MovementResult.FellInAPit:
                        gameStatus = GameStatus.Lost;
                        break;
                    case MovementResult.SuperBatSnatch:
                        i = RANDOM.Next(nodes.Count);
                        enumerator = nodes.Keys.GetEnumerator();
                        while (i >= 0) 
                        {
                            enumerator.MoveNext();
                            i--;
                        }
                        nodeName = enumerator.Current as string;
                        if (nodeName != null) 
                        {
                            subEnumerator = Move(nodeName, true).GetEnumerator();
                            while (subEnumerator.MoveNext()) 
                            {
                                results.Enqueue(subEnumerator.Current);
                            }
                        }
                        break;
                }                
            }

            return results;
        }

        private MovementResult MovePlayer(Node node) 
        {
            MovementResult result;
            
            playerNode = node;

            if (pitNodes.ContainsKey(node.Name)) 
            {
                result = MovementResult.FellInAPit;
            } 
            else if (batNodes.ContainsKey(node.Name)) 
            {
                result = MovementResult.SuperBatSnatch;
            } 
            else if (playerNode == wumpusNode) 
            {
                result = MovementResult.BumpedAWumpus;
            } 
            else 
            {
                result = MovementResult.SuccessfulMove;
            }            

            return result;
        }
        
        private void MoveWumpus() 
        {            
            int direction;
            IEnumerator enumerator;
            Node node;
            
            direction = RANDOM.Next(wumpusNode.LinkedNodes.Count + 1);
            if (direction < wumpusNode.LinkedNodes.Count) 
            {
                enumerator = wumpusNode.LinkedNodes.GetEnumerator();
                while (direction >= 0) 
                {
                    enumerator.MoveNext();
                    direction--;
                }                
                node = enumerator.Current as Node;
                if (node != null) 
                {
                    wumpusNode = node;
                }
            }
        }

        private void PlaceItems() 
        {            
            int i;
            object key;
            ArrayList keys;
            Node node;
            
            if (nodes.Count <= (numBats + numPits + 2)) 
            {
                throw new Exception("Insufficient nodes for game.");
            }

            keys = new ArrayList(nodes.Keys);
            
            // Place player.
            key = keys[RANDOM.Next(keys.Count)];
            initialPlayerNode = (Node)nodes[key];
            keys.Remove(key);

            // Place wumpus.
            key = keys[RANDOM.Next(keys.Count)];
            initialWumpusNode = (Node)nodes[key];
            keys.Remove(key);

            // Place pits.
            for (i = 0 ; i < numPits ; i++) 
            {
                key = keys[RANDOM.Next(keys.Count)];
                node = (Node)nodes[key];
                initialPitNodes[node.Name] = node;
                keys.Remove(key);
            }                              

            // Place bats.    
            for (i = 0 ; i < numPits ; i++) 
            {
                key = keys[RANDOM.Next(keys.Count)];
                node = (Node)nodes[key];
                initialBatNodes[node.Name] = node;
                keys.Remove(key);
            }                              
        }

        public void RestartGame() 
        {
            arrows = START_ARROWS;
            batNodes = (Hashtable)initialBatNodes.Clone();
            gameStatus = GameStatus.InPlay;
            pitNodes = (Hashtable)initialPitNodes.Clone();
            playerNode = initialPlayerNode;
            wumpusNode = initialWumpusNode;
        }

        private void SetupNodes(TextReader setupXmlReader) 
        {
            XPathDocument document;
            XPathNodeIterator linkIterator;
            Node linkNode;
            string linkNodeName;
            XPathNavigator navigator;
            Node node;
            XPathNodeIterator nodeIterator;
            string nodeName;
            XPathExpression linkNameExpression;
          
            document = new XPathDocument(setupXmlReader);
            navigator = document.CreateNavigator();
            numBats = Convert.ToInt32(navigator.Evaluate("sum(/Wumpus/Bats/@number)"));
            if (numBats < 1) 
            {
                numBats = DEFAULT_BATS;
            }
            numPits = 
                Convert.ToInt32(navigator.Evaluate("sum(/Wumpus/Pits/@number)"));
            if (numPits < 1) 
            {
                numPits = DEFAULT_PITS;
            }

            linkNameExpression = navigator.Compile("Links/Link/@name");
            
            nodeIterator = navigator.Select("/Wumpus/Nodes/Node");
            while (nodeIterator.MoveNext()) 
            {
                nodeName = nodeIterator.Current.GetAttribute(
                    "name", string.Empty);
                if (nodeName != string.Empty) 
                {
                    //Console.Write("{0}\n", nodeName);
                    node = nodes[nodeName] as Node;
                    if (node == null) 
                    {
                        node = new Node(nodeName);
                        nodes[nodeName] = node;
                    }
                    linkIterator = 
                        nodeIterator.Current.Select(linkNameExpression);
                    while (linkIterator.MoveNext()) 
                    {
                        linkNodeName = linkIterator.Current.Value;
                        if (linkNodeName != string.Empty) 
                        {
                            //Console.Write("\t{0}\n", linkNodeName);
                            linkNode = nodes[linkNodeName] as Node;
                            if (linkNode == null) 
                            {
                                linkNode = new Node(linkNodeName);
                                nodes[linkNodeName] = linkNode;
                            }
                            node.AddLinkedNode(linkNode);
                        }
                    }
                }
            }
        }

        public ShotResult Shoot(ICollection targetNodes) 
        {
            IEnumerator enumerator;
            int i;
            int j;
            Node node;
            IEnumerator randomEnumerator;
            Node tempNode;
            
            if ((arrows < 1) || (gameStatus != GameStatus.InPlay)) 
            {
                return ShotResult.NoMoreArrows;    
            } 
           
            i = 0;
            arrows -= targetNodes.Count;
            tempNode = playerNode;
            enumerator = targetNodes.GetEnumerator();

            while ((i < MAX_SHOTS) && enumerator.MoveNext()) 
            {
                node = enumerator.Current as Node;

                if (tempNode.IsAdjacentNode(node)) 
                {
                    tempNode = node;
                } 
                else 
                {
                    j = RANDOM.Next(tempNode.LinkedNodes.Count);
                    randomEnumerator = tempNode.LinkedNodes.GetEnumerator();
                    while (j >= 0) 
                    {
                        randomEnumerator.MoveNext();
                        j--;
                    }
                    node = randomEnumerator.Current as Node;
                    i = MAX_SHOTS;
                }

                if (node == wumpusNode) 
                {                    
                    gameStatus = GameStatus.Won;
                    return ShotResult.ShotWumpus;
                } 
                if (node == playerNode) 
                {                    
                    gameStatus = GameStatus.Lost;
                    return ShotResult.ShotSelf;
                }                 
                i++;   
            }

            if (arrows < 1) 
            {                
                gameStatus = GameStatus.Lost;
                return ShotResult.NoMoreArrows;
            } 
            else 
            {
                MoveWumpus();
                if (wumpusNode == playerNode) 
                {                    
                    gameStatus = GameStatus.Lost;
                    return ShotResult.GotByWumpus;
                }
            }
            return ShotResult.Missed;
        }

        public static int ArrowsAtStart 
        {
            get 
            {
                return START_ARROWS;
            }
        }

        public static int MaxShots 
        {
            get 
            {
                return MAX_SHOTS;
            }
        }

    } 
}
