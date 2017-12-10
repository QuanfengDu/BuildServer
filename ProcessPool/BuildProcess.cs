/////////////////////////////////////////////////////////////////////
// BuildProcess.cs                                                 //
// ver 2.1                                                         //
// Quanfeng Du, CSE681, Fall 2017                                  //
/////////////////////////////////////////////////////////////////////
/*
 * - Added references to:
 *   - System.ServiceModel
 *   - System.Runtime.Serialization
 *   - System.Threading;
 *   - System.IO;
 *   
 * Package Operations:
 * -------------------
 * This package defines one class:
 * - BuildProcess which implement to start a new process and continuelly to retrive message from mather build
 *   - replyMess        : Create reply message and send to mother process
 *   - buildFolder      : Build forder for each process
 *   
 *   
 *  Public Interface:
 *    ----------------
 *    startBuild - Child Process start to build the source files for each request
 

 *    All other functions:
 *   ------------------
*     replyMess - generate the reply message and send to the mother process 
 *    buildFolder - build a new folder 
 *   exitButton_Click - Close the selected window
 *    sendDLL child process starts to send DLL to testharness
 *    sendDoneDllSendNotification - When send all dll to test, send done dll notification to morther
 *    SendDLLToTS - send dll files to testharness
 *    SendLog - send log files to repository
 *    SendLogToRepo - send log files to repository
 *
 *   
 *   
 *   
 *   
 *   
 * Required Files:
 * ---------------
 * buildProcess.cs
 * 
  Maintenance History:
     ver 2.0 : 12 Dec 2017
     - second release
     function added:
         send dll to testharness
         send log to repoo
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using System.IO;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using Microsoft.Build.Execution;


namespace ProcessPool
{


    ///////////////////////////////////////////////////////////////////
    // BuildProcess class - build a new process and create a new folder for current process
    class BuildProcess
    {
        static private Sender sd = new Sender("http://localhost", 8068);//sender to repository
        static private Sender sdToTS = new Sender("http://localhost", 8050);//sender to testharness

        // Child Process start to build the source files for each request
        public void startBuild(string port)
        {
            string path = "..\\..\\..\\ProcessPool\\Build" + port;
            string[] files = Directory.GetFiles(path, "*.csproj");//get all csproj files
            foreach (string file in files)
            {
                try
                {
                    Console.Write("\n  Child {0} start to build request {1}\n", port, Path.GetFileName(file));
                    FileLogger logger = new FileLogger();
                    string logPath = "..\\..\\..\\ProcessPool\\Build" + port;
                    StringBuilder logFileSpec = new StringBuilder(logPath);
                    string filename = Path.GetFileName(file);
                    logFileSpec.Append("\\" + filename.Substring(0, filename.LastIndexOf('.')) + ".txt");
                    StringBuilder loggerParam = new StringBuilder(logFileSpec.ToString());
                    loggerParam.Insert(0, "logfile=");
                    logger.Parameters = loggerParam.ToString();
                    Dictionary<string, string> GlobalProperty = new Dictionary<string, string>();
                    BuildRequestData BuildRequest = new BuildRequestData(file, GlobalProperty, null, new string[] { "Rebuild" }, null);
                    BuildParameters bp = new BuildParameters();
                    bp.Loggers = new List<ILogger> { logger };
                    BuildResult buildResult = BuildManager.DefaultBuildManager.Build(bp, BuildRequest);
                    string time = DateTime.Now.ToString();
                    string[] csproj = Directory.GetFiles(logPath, "*.csproj");
                    string[] cs = Directory.GetFiles(logPath, "*.cs");
                    foreach (string file_ in csproj)
                        File.Delete(file_);
                    foreach (string file_ in cs)
                        File.Delete(file_);
                    if (buildResult.OverallResult == BuildResultCode.Success)
                    {
                        Thread.Sleep(100);
                        Console.WriteLine("\n\n Build successfully! Build time is: {0}", time);
                    }
                    else if(buildResult.OverallResult != BuildResultCode.Success)
                    {
                        Thread.Sleep(100);
                        Console.WriteLine("\n\n Build failed! Build time is: {0}", time);
                    }
                        
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /*----< generate the reply message and send to the mother process >-------------*/
        private void replyMess(Sender sndr, string port)
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "ready";
            sndMsg.author = "QuanfengDu";
            sndMsg.to = "http://localhost:" + port + "/Ibuilder";
            sndMsg.from = "http://localhost:" + port + "/Ibuilder";
            sndr.postMessage(sndMsg);
        }

        /*----< build a new folder >-------------*/
        private void buildFolder(string port)
        {
            string path = "..\\..\\..\\ProcessPool\\Build" + port;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        
        //child process starts to send DLL to testharness
        private void sendDLL(Sender sndr, string port,string port2)
        {
            string path = "..\\..\\..\\ProcessPool\\Build" + port+ "\\csproj_debug";
            if (!Directory.Exists(path))//if the Directory does not exit, just do not send 
            {
                sendDoneDllSendNotification(sndr,port2);
                return;
            }
            string[] csprojlist = Directory.GetFiles(path, "*.dll");
            string[] requestFiles = Directory.GetFiles("..\\..\\..\\ProcessPool\\Build" + port, "*.xml");
            string[] combined = csprojlist.Concat(requestFiles).ToArray();
            foreach (string file in combined)
            {
                SendDLLToTS(file);
            }
            sendDoneDllSendNotification(sndr, port2);
        }

        //When send all dll to test, send done dll notification to morther
        private void sendDoneDllSendNotification(Sender sndr,string port2)
        {
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "doneSendDll";
            sndMsg.author = "QuanfengDu";
            sndMsg.to = "http://localhost:" + port2 + "/Ibuilder";
            sndMsg.from = "http://localhost:" + port2 + "/Ibuilder";
            sndr.postMessage(sndMsg);
        }
        //send dll files to testharness
        private void SendDLLToTS(string file)
        {
            Console.Write("\n\n build child send dll and request file {0} to test harness ..\\..\\..\\TestHarness\\DLL", file);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "dll";
            sndMsg.author = "QuanfengDu";
            sndMsg.to = "http://localhost:" + 8050.ToString() + "/Ibuilder";
            sndMsg.from = "http://localhost:" + 8050.ToString() + "/Ibuilder";
            sdToTS.WritePath_ = "..\\..\\..\\TestHarness\\DLL";
            sndMsg.filename = file;
            sdToTS.postMessage(sndMsg);
            sdToTS.postFile(file);
        }

        //send log files to repository
        private void SendLog(Sender sndr,string port,string port2)
        {
            string path= "..\\..\\..\\ProcessPool\\Build" + port;
            string[] csprojlist = Directory.GetFiles(path, "*.txt");
            foreach (string file in csprojlist)
            {
                SendLogToRepo(file,port);
            }
            sendDLL(sndr, port,port2);//after send the log files, start send the dll to test harness 
        }

        //send log files to repository
        private void SendLogToRepo(string file,string port)
        {
            Console.Write("\n\n build child send build log {0} to repository ..\\..\\..\\Repository\\LogFilesAfterBuild", Path.GetFileName(file));
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "log";
            sndMsg.author = "QuanfengDu";
            sndMsg.to = "http://localhost:" + 8068.ToString() + "/Ibuilder";
            sndMsg.from = "http://localhost:" + 8068.ToString() + "/Ibuilder";
            sd.WritePath_ = "..\\..\\..\\Repository\\LogFilesAfterBuild";
            sndMsg.filename = file;
            sd.postMessage(sndMsg);
            sd.postFile(file);
            
        }

        

        static void Main(string[] args)
        {
            BuildProcess bp=new BuildProcess();
            Receiver rcvr = new Receiver();
            rcvr.start("http://localhost", Int32.Parse(args[0]));//start child receiver
            Sender sndr = new Sender("http://localhost", Int32.Parse(args[1]));//start child sender
            CommMessage rcvMsg;
            while (true)
            {
                rcvMsg = rcvr.getMessage();
                if (rcvMsg.command == "open")
                {
                    rcvMsg.show();
                    bp.buildFolder(args[0]);
                    bp.replyMess(sndr, args[1]);
                    continue;
                }                
                if(rcvMsg.command == "sendfile")
                {
                    rcvMsg.show();
                    Thread.Sleep(100);
                    bp.startBuild(args[0]);
                    bp.replyMess(sndr, args[1]);
                }
                if(rcvMsg.command== "SendLog")
                {
                    bp.SendLog(sndr,args[0],args[1]);
                }
                if (rcvMsg.type == CommMessage.MessageType.closeReceiver)
                {
                    rcvMsg.show();
                    rcvMsg.type = CommMessage.MessageType.closeSender;
                    sndr.postMessage(rcvMsg);
                    sdToTS.postMessage(rcvMsg);
                    sd.postMessage(rcvMsg);
                    break;
                }
            }
        }
    }
}
