using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Xml;

namespace Wumpus
{
    //
    // This is a console application that converts DATA statements 
    // in the original Creative Computing Wumpus program listing 
    //  
    //   http://www.atariarchives.org/bcc1/showpage.php?page=250 
    //
    // such as
    //
    //   DATA 2,5,8,1,3,10,2,4,12,3,5,14,1,4,6
    //
    // to a friendlier, modern XML file representation of same.
    //
    // David Bautista
    // davidb@vertigosoftware.com
    //
    public class CaveGenerator
    {
        
        private static readonly string APP_NAME = Process.GetCurrentProcess().ProcessName;

        private const int USAGE_ERROR = 1;
        private const int INPUT_FILE_ERROR = 2;
        private const int OUTPUT_FILE_ERROR = 3;

        private static readonly char[] SPLIT_CHARS = {' ', '\t'};
        private static readonly string USAGE_STRING =
            string.Format("{0}:\n" +
            "Usage:  {0} <source file> <output file>\n" +
            "<source file>:  The file containing data statements.\n" +
            "<output file>:  The file to recieve the output xml.\n",
            APP_NAME);

        [STAThread]
        public static int Main(string[] argv) 
        {
                       
            if (argv.Length != 2) 
            {
                Console.Write(USAGE_STRING);
                return USAGE_ERROR;
            }

            StreamReader streamReader;
            try 
            {
                streamReader = File.OpenText(argv[0]);
            } 
            catch 
            {
                return INPUT_FILE_ERROR;
            }

            Queue linkQueue = new Queue();
            int i;

            try 
            {
                StringBuilder buffer = new StringBuilder();
                string line;
                bool readyForLinks;

                while (streamReader.Peek() >= 0) 
                {
                    readyForLinks = false;
                    buffer.Length = 0;
                    line = streamReader.ReadLine();
                    for (i = 0 ; i < line.Length ; i++) 
                    {
                        if (readyForLinks) 
                        {
                            if (line[i] == ',') 
                            {
                                linkQueue.Enqueue(buffer.ToString().TrimStart());
                                buffer.Length = 0;
                            } 
                            else 
                            {
                                buffer.Append(line[i]);
                            }
                        } 
                        else 
                        {
                            if (Char.IsWhiteSpace(line[i])) 
                            {
                                if (buffer.ToString().ToUpper() == "DATA") 
                                {
                                    readyForLinks = true;
                                }
                                buffer.Length = 0;
                            } 
                            else 
                            {
                                buffer.Append(line[i]);
                            }
                        }
                    }
                }
            } 
            catch 
            {
                return INPUT_FILE_ERROR;
            }
            finally
            {
                if (streamReader != null) streamReader.Close();
            }
            
            if ((linkQueue.Count == 0) || ((linkQueue.Count % 3) != 0)) 
            {
                return INPUT_FILE_ERROR;
            }

            XmlTextWriter xtw;
            try 
            {
                xtw = new XmlTextWriter(argv[1], Encoding.UTF8);
            } 
            catch 
            {
                return OUTPUT_FILE_ERROR;
            }
            
            try 
            {
                xtw.WriteStartDocument();
                xtw.WriteStartElement("Wumpus");
                xtw.WriteStartElement("Nodes");

                int nodeCount = 1;
                while (linkQueue.Count > 0) 
                {
                    xtw.WriteStartElement("Node");
                    xtw.WriteStartAttribute("Name", string.Empty);
                    xtw.WriteString(
                        nodeCount.ToString(CultureInfo.InvariantCulture));
                    xtw.WriteEndAttribute();
                    xtw.WriteStartElement("Links");
                    for (i = 0 ; i < 3 ; i++) 
                    {
                        xtw.WriteStartElement("Link");
                        xtw.WriteStartAttribute("Name", string.Empty);
                        xtw.WriteString((string)linkQueue.Dequeue());
                        xtw.WriteEndElement();
                    }
                    xtw.WriteEndElement();
                    
                    xtw.WriteEndElement();
                }
                
                xtw.WriteEndElement();
                xtw.WriteEndElement();
                xtw.WriteEndDocument();
            } 
            catch 
            {
                return OUTPUT_FILE_ERROR;
            }
            finally
            {
                if (xtw != null) xtw.Close();
            }
                       
            return 0;
        }
        
    }
}