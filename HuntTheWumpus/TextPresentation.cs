using System;
using System.Collections;
using System.Diagnostics;
using System.IO;

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
    public class TextPresentation
    {

        private static readonly string APP_NAME = Process.GetCurrentProcess().ProcessName;
        
        private const string BEADS_FILENAME = "StringOfBeads.xml";
        private const string DENDRITE_FILENAME = "DendriteWithDegeneracies.xml";
        private const string DODECAHEDRON_FILENAME = "Dodecahedron.xml";
        private const string LATTICE_FILENAME = "OneWayLattice.xml";
        private const string MOBIUS_FILENAME = "MobiusStrip.xml";                
        private const string TORUS_FILENAME = "HexNetOnTorus.xml";

        private const int INPUT_FILE_ERROR = 1;
        private const int GAME_ERROR = 2;
        
        private const string GOT_BY_WUMPUS_STRING =
            "TSK TSK TSK- WUMPUS GOT YOU!\n";
        private const string INSTRUCTIONS =
            "WELCOME TO WUMPUS II\n" +
            "THIS VERSION HAS THE SAME RULES AS 'HUNT THE WUMPUS'.\n" +
            "HOWEVER, YOU NOW HAVE A CHOICE OF CAVES TO PLAY IN.\n" +
            "SOME CAVES ARE EASIER THAN OTHERS. ALL [default] CAVES HAVE 20\n" +
            "ROOMS AND 3 TUNNELS LEADING FROM ONE ROOM TO OTHER ROOMS.\n" +
            "THE CAVES ARE:\n" +
            "  0  -  DODECAHEDRON   THE ROOMS OF THIS CAVE ARE ON A\n" +
            "        12-SIDED OBJECT, EACH FORMING A PENTAGON.\n" +
            "        THE ROOMS ARE AT THE CORNERS OF THE PENTAGONS.\n" +
            "        EACH ROOM HAVING TUNNELS THAT LEAD TO 3 OTHER ROOMS.\n" +
            "\n" +
            "  1  -  MOBIUS STRIP   THIS CAVE IS TWO ROOMS\n" +
            "        WIDE AND 10 ROOMS AROUND (LIKE A BELT)\n" +
            "        YOU WILL NOTICE THERE IS A HALF TWIST\n" +
            "        SOMEWHERE.\n" +
            "\n" +
            "  2  -  STRING OF BEADS    FIVE BEADS IN A CIRCLE.\n" +
            "        EACH BEAD IS A DIAMOND WITH A VERTICAL\n" +
            "        CROSS-BAR. THE RIGHT & LEFT CORNERS LEAD\n" +
            "        TO NEIGHBORING BEADS. (THIS ONE IS DIFFICULT\n" +
            "        TO PLAY.\n" +
            "\n" +
            "  3  -  HEX NETWORK        IMAGINE A HEX TILE FLORE.\n" +
            "        TAKE A RECTANGLE WITH 20 POINTS (INTERSECTIONS)\n" +
            "        INSIDE (4X4). JOIN RIGHT & LEFT SIDES TO MAKE A\n" +
            "        CYLINDER. THEN JOIN TOP & BOTTOM TO FORM A \n" +
            "        TORUS (DOUGHNUT).\n" +
            "        HAVE FUN IMAGINING THIS ONE!!\n" +
            "\n" +
            " CAVES 1-3 ARE REGULAR IN A SENSE THAT EACH ROOM\n" +
            "GOES TO THREE OTHER ROOMS & TUNNELS ALLOW TWO-\n" +
            "WAY TRAFFIC. HERE ARE SOME 'IRREGULAR' CAVES:\n" +
            "\n" +
            "  4  -  DENDRITE WITH DEGENERACIES   PULL A PLANT FROM\n" +
            "        THE GROUND. THE ROOTS & BRANCHES FORM A \n" +
            "        DENDRITE. IE., THERE ARE NO LOOPING PATHS\n" +
            "        DEGENERACIES MEANS A) SOME ROOMS CONNECT TO\n" +
            "        THEMSELVES AND B) SOME ROOMS HAVE MORE THAN ONE\n" +
            "        TUNNEL TO THE SAME ROOM IE, 12 HAS \n" +
            "        TWO TUNNELS TO 13.\n" +
            "\n" +
            "  5  -  ONE WAY LATTICE     HERE ALL TUNNELS GO ONE\n" +
            "        WAY ONLY. TO RETURN, YOU MUST GO AROUND THE CAVE\n" +
            "        (AROUND 5 MOVES).\n" +
            "\n" +
            "  6  -  ENTER YOUR OWN CAVE    The computer will ask you \n" +
            "        for a filename.  It should point to an XML file with \n" +
            "        a schema, identical to that, used by the included \n" +
            "        Wumpus XML files.  The nodes can have non-numeric \n" +
            "        names and zero or more linked nodes.  The only \n" +
            "        requirement is that you include sufficient nodes for \n" +
            "        the player, wumpus, pits, and bats.\n" +
            "  HAPPY HUNTING!\n";
           

        private static void HandleMove(WumpusLogic wumpusLogic) 
        {            
            IEnumerator enumerator;
            string input;
            ICollection movementResults;

            input = string.Empty;
            do 
            {
                Console.Write("WHERE TO ");
                input = Console.ReadLine().Trim();
                Console.Write("\n");
                if (!wumpusLogic.PlayerNode.IsAdjacentNode(input)) 
                {                                   
                    Console.Write("NOT POSSIBLE - {0}\n", input);
                    Console.ReadLine();
                    input = string.Empty;
                }                
            } while (input == string.Empty);

            movementResults = wumpusLogic.Move(input);
            enumerator = movementResults.GetEnumerator();
        
            while (enumerator.MoveNext()) 
            {
                switch ((WumpusLogic.MovementResult)enumerator.Current) 
                {
                    case WumpusLogic.MovementResult.BumpedAWumpus:
                        Console.Write("... OOPS! BUMPED A WUMPUS!\n");
                        break;
                    case WumpusLogic.MovementResult.FellInAPit:
                        Console.Write("YYYIIIEEEE . . . FELL IN A PIT\n");
                        break;
                    case WumpusLogic.MovementResult.GotByWumpus:
                        Console.Write(GOT_BY_WUMPUS_STRING);
                        break;
                    case WumpusLogic.MovementResult.SuperBatSnatch:
                        Console.Write(
                            "ZAP--SUPER BAT SNATCH! ELSEWHERESVILLE FOR YOU!\n");
                        break;
                }
            }           
        }

        public static void HandleShoot(WumpusLogic wumpusLogic) 
        {
            string input;
            WumpusLogic.Node node;
            int numShots;
            Queue shots;

            numShots = -1;
            do 
            {
                Console.Write("NO. OF ROOMS ");
                input = Console.ReadLine().Trim();
                try 
                {
                    numShots = Convert.ToInt32(input);
                } 
                catch 
                {
                    numShots = -1;
                }

                if ((numShots < 1) ||
                    (numShots > WumpusLogic.MaxShots)) 
                {
                    numShots = -1;
                    Console.Write("ERROR   ");
                    Console.ReadLine();
                }
                
            } while (numShots < 1);

            shots = new Queue();
            while (numShots > 0) 
            {
                Console.Write("ROOM #");
                input = Console.ReadLine().Trim();
                Console.Write("\n");
                node = wumpusLogic.GetNodeByName(input);                
                
                if (node == null) 
                {
                    Console.Write("ERROR   ");
                    Console.ReadLine();        
                } 
                else 
                {
                    shots.Enqueue(node);
                    numShots--;
                }
            }

            switch (wumpusLogic.Shoot(shots)) 
            {
                case WumpusLogic.ShotResult.GotByWumpus:
                    Console.Write(GOT_BY_WUMPUS_STRING);
                    break;
                case WumpusLogic.ShotResult.Missed:
                    Console.Write("MISSED\n");
                    break;
                case WumpusLogic.ShotResult.NoMoreArrows:
                    Console.Write("YOU HAVE USED ALL YOUR ARROWS.\n");
                    break;
                case WumpusLogic.ShotResult.ShotSelf:
                    Console.Write("OUCH! ARROW GOT YOU!\n");
                    break;
                case WumpusLogic.ShotResult.ShotWumpus:
                    Console.Write(
                        "AHA! YOU GOT THE WUMPUS! HE WAS IN ROOM {0}\n",
                        wumpusLogic.WumpusNode.Name);
                    break;            
            }
            
        }

        [STAThread]
        public static int Main(string[] argv) 
        {

            string input;
            bool playAgain;
            int result;
            string setupFileName;
            StreamReader streamReader;	   	                
            WumpusLogic wumpusLogic;

            result = 0;

            Console.Write(
                "*** Wumpus .NET ***\n\nBased on:\n\n\n{0}\n{1}\n{2}\n\n\n\n",
                "WUMPUS 2".PadLeft(33), "CREATIVE COMPUTING".PadLeft(38),
                "MORRISTOWN  NEW JERSEY".PadLeft(40));

            Console.Write("INSTRUCTIONS");
            input = Console.ReadLine().ToUpper();
            if (input.StartsWith("Y")) 
            {
                Console.Write(INSTRUCTIONS);
            }

            playAgain = false;     
            wumpusLogic = null;
            do 
            {
                if (wumpusLogic == null) 
                {
                    setupFileName = string.Empty;
                    do 
                    {
                        Console.Write("CAVE #(0-6) ");
                        input = Console.ReadLine();
                        Console.Write("\n");
                        input = input.Trim();
                        if (input == string.Empty) 
                        {
                            input = "X";
                        }
                        switch (input[0]) 
                        {
                            case '0':
                                setupFileName = DODECAHEDRON_FILENAME;     
                                break;
                            case '1':
                                setupFileName = MOBIUS_FILENAME;
                                break;
                            case '2':
                                setupFileName = BEADS_FILENAME;
                                break;
                            case '3':
                                setupFileName = TORUS_FILENAME;
                                break;
                            case '4':
                                setupFileName = DENDRITE_FILENAME;
                                break;
                            case '5':
                                setupFileName = LATTICE_FILENAME;
                                break;
                            case '6':
                                Console.Write("Wumpus XML Filename: ");
                                setupFileName = Console.ReadLine().Trim();
                                break;                            
                            default:                            
                                Console.Write("ERROR  \n");
                                Console.ReadLine();
                                Console.Write("\n");                            
                                break;                            
                        }
                        
                    } while (setupFileName == string.Empty);  
                    
                    if (!File.Exists(setupFileName))
                    {
                        setupFileName = Path.Combine(Environment.CurrentDirectory, "..\\..\\" + setupFileName);
                    }

                    streamReader = null;
                    try 
                    {
                        streamReader = File.OpenText(setupFileName);
                        wumpusLogic = new WumpusLogic(streamReader);
                    } 
                    catch 
                    {
                        Console.Write(
                            "{0}:\nError: Unable to read Wumpus XML file, {1}.\n",
                            APP_NAME, setupFileName);
                        result = INPUT_FILE_ERROR;
                    } 
                    finally 
                    {
                        if (streamReader != null) 
                        {
                            streamReader.Close();
                        }
                    }                    
                }

                if ((result == 0) && (wumpusLogic != null)) 
                {
                    Console.Write("HUNT THE WUMPUS\n");
                    try 
                    {
                        while (wumpusLogic.Status == 
                            WumpusLogic.GameStatus.InPlay) 
                        {
                            PrintHazardWarningsAndLocation(wumpusLogic);
                            Console.Write("SHOOT OR MOVE ");
                            input = Console.ReadLine().TrimStart().ToUpper();
                            if (input.StartsWith("M")) 
                            {
                                HandleMove(wumpusLogic);
                            } 
                            else if (input.StartsWith("S")) 
                            {
                                HandleShoot(wumpusLogic);
                            } 
                            else 
                            {
                                Console.Write("ERROR   ");
                                Console.ReadLine();
                            }
                        }
                        if (wumpusLogic.Status ==
                            WumpusLogic.GameStatus.Lost) 
                        {
                            Console.Write(
                                "HA HA HA - YOU LOOSE!\n");
                        } 
                        else 
                        {
                            Console.Write(
                                "HEE HEE HEE - THE WUMPUS'LL GET YOU NEXT TIME!!\n");
                        }
                        Console.Write("PLAY AGAIN");
                        input = Console.ReadLine().TrimStart().ToUpper();
                        playAgain = input.StartsWith("Y");
                        Console.Write("\n\n");
                        if (playAgain) 
                        {
                            Console.Write("SAME SET-UP ");                            
                            input = Console.ReadLine().TrimStart().ToUpper();                            
                            if (input.StartsWith("Y")) 
                            {
                                wumpusLogic.RestartGame();
                            } 
                            else 
                            {
                                wumpusLogic = null;
                            }
                        } 

                    } 
                    catch 
                    {
                        Console.Write(
                            "{0}:\nError: Game error.\n");
                        result = GAME_ERROR;                        
                    }                    
                }                                                                   
            } while (playAgain && (result == 0));
                                   
            return result;
        }

        private static void PrintHazardWarningsAndLocation(WumpusLogic wumpusLogic) 
        {
            IEnumerator enumerator;
            WumpusLogic.SurroundingHazards hazards;
            WumpusLogic.Node node;

            Console.Write("\n");

            hazards = wumpusLogic.GetSurroundingHazards();
            if ((hazards & WumpusLogic.SurroundingHazards.WumpusNearby) != 0) 
            {
                Console.Write("I SMELL A WUMPUS!\n");
            }
            if ((hazards & WumpusLogic.SurroundingHazards.PitNearby) != 0) 
            {
                Console.Write("I FEEL A DRAFT!\n");
            }
            if ((hazards & WumpusLogic.SurroundingHazards.BatNearby) != 0) 
            {
                Console.Write("BATS NEARBY!\n");
            }

            Console.Write("YOU ARE IN ROOM {0}\n", wumpusLogic.PlayerNode.Name);

            Console.Write("TUNNELS LEAD TO");
            enumerator = wumpusLogic.PlayerNode.LinkedNodes.GetEnumerator();
            while (enumerator.MoveNext()) 
            {
                node = enumerator.Current as WumpusLogic.Node;
                if (node != null) 
                {
                    Console.Write(" {0}", node.Name);
                }
            }
            Console.Write("\n\n");
        }

    }
}
