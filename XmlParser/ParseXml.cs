/////////////////////////////////////////////////////////////////////
// Parse.cs - package will help to parse the xml file              //
//                                                                 //
// Author: Quanfeng Du,  qdu101@syr.edu                            //
// Application: BuildServer                                        //
// Environment: C# console                                         //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * this package is used to parse the request files and get the corresponding content
 * 
 * Public Interface:
 * ---------------
 * loadXml--------------------load the xml file
 * parse----------------------parse xml element
 * parsedic-------------------parse test drive and tested case
 * parseDir ------------------start parse the directory tag     
 * 
 * Required Files:
 * ---------------

 * 
 *  Maintenance History:
     ver 2.0 : 12 Dec 2017
     - second release
     - add parse the directory parts
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.IO;

namespace ParseXml
{
    public class Parse
    {
        public XDocument doc { get; set; } = new XDocument();

        //start to load the xml file
        public bool loadXml(string path)
        {
            try
            {
                doc = XDocument.Load(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n--{0}--\n", ex.Message);
                return false;
            }
        }

        //start to parse the xml element 
        public string parse(string propertyname)
        {
            string parseStr = doc.Descendants(propertyname).First().Value;
            if (parseStr.Length > 0)
            {
                return parseStr;
            }
            return "";
        }

        //start to parse the test driver and tested cs files 
        public Dictionary<string, List<string>> parsedic(string propertyname)
        {
            Dictionary<string, List<string>> di = new Dictionary<string, List<string>>();
            IEnumerable<XElement> parseElems = doc.Descendants(propertyname);
            if (parseElems.Count() > 0 && propertyname == "test")
            {
                foreach (XElement elem in parseElems)
                {
                    IEnumerable<XElement> parseElems_ = elem.Descendants("tested");
                    List<string> listfile = new List<string>();

                    foreach (XElement file in parseElems_)
                    {
                        listfile.Add(file.Value);
                    }
                    IEnumerable<XElement> parseElemsD = elem.Descendants("testDriver");
                    foreach (XElement file in parseElemsD)
                    {
                        di.Add(file.Value, listfile);
                    }

                }
            }
            return di;
        }
        // start parse the directory tag
        public string parseDir(string propername)
        {
            string directory = "";
            IEnumerable<XElement> parseElems = doc.Descendants(propername).Descendants("directory");
            foreach (XElement file in parseElems)
            {
               directory = file.Value;
            }
            return directory;
        }
    }

#if(TEST_PARSE)
    class parse
    {
        static void Main(string[] args)
        {
            Console.Write("Test Parse.");
            Console.Write("\n==========\n");
            Parse parse = new Parse();
            string path_ = Directory.GetCurrentDirectory();

            int index = path_.LastIndexOf("Project2");
            string requestpath = path_.Substring(0, index);
            requestpath = requestpath + "\\Project2\\Repo\\request";

            string[] allRequest = Directory.GetFiles(requestpath, "*.xml");
            if (!parse.loadXml(allRequest[0]))
                Console.Write("cannot load request.");
            else
            {
                Console.Write("\nThe content of request: ");
                Console.Write("\n=========================");
                Console.Write("\n{0}", parse.doc.ToString());
                string author_ = parse.parse("author");
                string timeDate_ = parse.parse("dateTime");
                Dictionary<string, List<string>> dict = parse.parsedic("test");
            }

        }
    }
#endif
}