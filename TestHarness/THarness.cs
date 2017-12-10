/////////////////////////////////////////////////////////////////////
// THarness.cs                                                     //
//                                                                 //
// Author: Quanfeng Du,  qdu101@syr.edu                            //
// Application: BuildServer                                        //
// Environment: C# console                                         //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * this package is used to accept the dll files send from the repository, build the dll files and generate testresult files and send it back to the repository
 * 
 * public INterface:
 * -----------------
 *    loadAndExerciseTestes--------------------start load all the dll and call runSimulatedTest to test
 * 
 * other functions:
 *    doneTestMsgToBuildMother----------------------send the Done test message to buils mother
 *    sendTestResultToRepo-------------------send test result to repo
 *    runSimulatedTest-----------------------run test for each DLL
 * 
 * Required Files:
 * ---------------
 * Commu.cs
 * CsBlockingQueue
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 02 Oct 2017
 * - first release
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;

namespace ProcessPool 
{
    
    class THarness
    {
        //XDocument to generate test result xml
        public XDocument doc { get; set; } = new XDocument();

        //Start to load all dll in the folder and test 
        public string loadAndExerciseTestes()
        {
            XElement testResulrElem = new XElement("TestResult");
            doc.Add(testResulrElem);
            Console.Write("\n\nTestHarness start to load and execute DLL.");
            Console.Write("\n=============================================");
            string path_ = "..\\..\\..\\TestHarness\\DLL";
            string[] files = Directory.GetFiles(path_, "*.dll");
            foreach (string file in files)
            {
                string file_=Path.GetFullPath(file);
                Assembly asm = Assembly.LoadFile(file_);
                string fileName = Path.GetFileName(file_);
                Console.Write("\n\n  Loaded {0}", fileName);
                Console.Write("\n  ====================================");
                bool flag = true;
                Type[] types = asm.GetTypes();
                foreach (Type t in types)
                {
                    if (t.Name != "ITest")
                        if (!runSimulatedTest(t, asm))
                            flag = false;
                }
                string sub = Path.GetFileName(file);
                sub = sub.Substring(0, sub.LastIndexOf('.'));
                XElement TestFile = new XElement(sub);
                if (flag == false)//test failed
                {
                    Console.Write("\n test {0} failed", file);
                    TestFile.Add("failed");
                    testResulrElem.Add(TestFile);
                }
                else // test passed
                {
                    Console.Write("\n test {0} passed", file);
                    TestFile.Add("pass");
                    testResulrElem.Add(TestFile);
                } 
            }
            doc.Save("..\\..\\..\\TestHarness\\TestResult\\testResult.xml");
            // after done test, send donetest message to build mother.
            doneTestMsgToBuildMother();           
            return "Simulated Testing completed";
        }

        //send done test message to build mother 
        private void doneTestMsgToBuildMother()
        {
            Sender s = new Sender("http://localhost", 8079);
            CommMessage cm = new CommMessage(CommMessage.MessageType.reply);
            cm.command = "DoneTest";
            cm.author = "Quanfeng Du";
            cm.to = "http://localhost:" + 8079.ToString() + "/Ibuilder";
            cm.from = "http://localhost:" + 8079.ToString() + "/Ibuilder";
            s.postMessage(cm);
        }
        
        //start send test result and test result back to repo 
        private void sendTestResultToRepo(Sender sndr)
        {
            
            string[] file_ = Directory.GetFiles("..\\..\\..\\TestHarness\\TestResult", "*.xml");
            Console.Write("\n\n testHarness send test result {0} to repository ..\\..\\..\\Repository\\TestResult", Path.GetFileName(file_[0]));
            sndr.WritePath_ = "..\\..\\..\\Repository\\TestResult";
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "sendTestResultToRepo";
            sndMsg.author = "Quanfeng Du";
            sndMsg.to = "http://localhost:" + 8068.ToString() + "/Ibuilder";
            sndMsg.from = "http://localhost:" + 8068.ToString() + "/Ibuilder";
            sndMsg.filename = file_[0];
            sndr.postMessage(sndMsg);
            sndr.postFile(file_[0]);
        }

        //start test each dll 
        private bool runSimulatedTest(Type t, Assembly asm)
        {
            try
            {
                Console.Write("\n  Attempting to create instance of {0}", t.ToString());
                object obj = asm.CreateInstance(t.ToString());
                MethodInfo method = t.GetMethod("say");
                bool status = false;
                if (method != null)
                    status = (bool)method.Invoke(obj, new object[0]);
                Func<bool, string> act = (bool pass) =>
                {
                    if (pass)
                        return "pass";
                    return "fail";
                };
                string tem = act(status);
                Console.Write("\n  test {0}.", tem);
                if (tem == "pass")
                    return true;
                else return false;
            }
            catch (Exception ex)
            {
                Console.Write("\n  test failed with message \"{0}\"", ex.Message);
                return false;
            }
        }


        static void Main(string[] args)
        {
            THarness th = new THarness();
            Receiver receive= new ProcessPool.Receiver();
            receive.start("http://localhost", 8050);   //test harness receiver 
            Console.Write("\n  TestHarness Server Started!");
            CommMessage rcvMsg;
            Boolean flag = true; 
            Sender sndr = new Sender("http://localhost", 8068);
            while (true)
            {
                rcvMsg = receive.getMessage();
                if (rcvMsg.command == "StartTest" && flag==true)
                {
                    rcvMsg.show();
                    Thread.Sleep(5000);
                    th.loadAndExerciseTestes();
                    flag = false;
                }
                if(rcvMsg.command== "SendTestResult")
                {
                    rcvMsg.show();
                    th.sendTestResultToRepo(sndr);
                }
            }
        }
    }
}
