/////////////////////////////////////////////////////////////////////
// RequestGenerator.cs - Generate build Request                    //
//                                                                 //
// Author: Quanfeng Du,  qdu101@syr.edu                            //
// Application: BuildServer                                        //
// Environment: C# console                                         //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * this package used to generate the request files selected in the selected window.
 * 
 * public Inerface:
 * ---------------
 *     makeRequest-----generate request
 *     saveXml---------save the request
 * 
 * Required Files:
 * ---------------

 * 
 * Maintenance History:
 * --------------------
 *  Maintenance History:
     ver 2.0 : 12 Dec 2017
     - second release
     -add the directory tag
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ClientGUI
{
    class xmlgenerator
    {
        
            public String author { get; set; } = "";
            public String dateTime { get; set; } = "";
            public String toolChain { get; set; } = "";
            public String testDriver { get; set; } = "";
            public String directory { get; set; } = "";
            public List<String> testFiles { get; set; } = new List<String>();
            public Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();
            public XDocument doc { get; set; } = new XDocument();
            //used to generate the request 
            public void makeRequest()
            {
                XElement testRequestElem = new XElement("testRequest");
                doc.Add(testRequestElem);

                XElement authorElem = new XElement("author");
                authorElem.Add(author);
                testRequestElem.Add(authorElem);

                XElement dateTimeElem = new XElement("dateTime");
                dateTimeElem.Add(DateTime.Now.ToString());
                testRequestElem.Add(dateTimeElem);

                XElement toolChainElem = new XElement("toolChain");
                toolChainElem.Add(toolChain);
                testRequestElem.Add(toolChainElem);
 
                foreach (KeyValuePair<string, List<string>> item in dict)
                {
                    XElement testElem = new XElement("test");
                    testRequestElem.Add(testElem);
                    XElement driverElem = new XElement("testDriver");
                    driverElem.Add(item.Key);
                    testElem.Add(driverElem);
                   XElement Direc = new XElement("directory");
                   Direc.Add(directory);
                   testElem.Add(Direc);

                foreach (string file in item.Value)
                    {
                        XElement testedElem = new XElement("tested");
                        testedElem.Add(file);
                        testElem.Add(testedElem);
                    }

                }
            }

            //save the xml file after the request generated
            public bool saveXml(string path)
            {
                try
                {
                    doc.Save(path);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.Write("\n--{0}--\n", ex.Message);
                    return false;
                }
            }
        
    }


#if (TEST_Xmlgenerator)
    class generate
    {
        static void Main(string[] args)
        {
            Console.Write("Test Xmlgenerate.");
            Console.Write("\n==========\n");
             string[] list = Directory.GetFiles("..\\..\\..\\Repository\\test1");
            
            xmlgenerator xg = new xmlgenerator();
            xg.author = "QuanfengDu";
            xg.toolChain = "MSBuild";
            xg.directory = this.dirrc;
            foreach(string it in list) //insert element to the request structure
            {
                if (it.Contains("Driver"))
                {
                    xg.dict.Add(it, new List<string>());
                    foreach(string iter in list)
                    {
                        if (iter.Contains("Driver"))
                            continue;
                        else
                        {
                            xg.dict[it].Add(iter);
                        }
                    }
                    break;
                }
            }
            xg.makeRequest();
            xg.saveXml("../../../request/"+xg.author+".xml");

        }
    }
#endif
}
