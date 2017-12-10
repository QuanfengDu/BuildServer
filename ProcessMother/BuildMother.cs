/////////////////////////////////////////////////////////////////////////////
//  BuildMother.cs - demonstrate build mmother build child process         //    
//                   send receive message from child                       //
//  ver 1.0                                                                //
//  Language:     C#, VS 2017                                              //
//  Author:       Quanfeng Du,  Syracuse University                        //
//                (315) 418-9913,qdu101@syr.edu                            //
/////////////////////////////////////////////////////////////////////////////
/*
 *   Package Operations
 *   ------------------
 *   This package implements the mother process to build several child process
 *   and can send receive message to from the child process. Also can receive 
 *   start message from the GUI
 * 
 *  Public Interface
 * ----------------
 * startChildProcess - start a new child process
*  dirict - get the directory choosed in the list



 * All other functions:
 * ------------------
 * FileList - get all the request files in the request 
 * generaterequest - generate the request file
 * exitButton_Click - Close the selected window
 * startsendOpen - open a sender  
 * startsend - start a send source file and request files by each sender 
 * sendSourceToChild - build mother sends the sources conresponding the request file to child builder
 * startReceiver - After receive the number of process, the mother start several receiver
 * quit - quit the child process 
 * createDir - will create directory for diferent group of source files, and send created message back to repo
 * sendFileToChild - the build mother starts to send files to child process
 * askChildSendLog - send send log request message to child, child will send build log to repository
 * requestTestHarnessSendResu - mother send message to testharness to request testharness send test result to repository
 * RequestTestHarnessTest - build mother send start test message to request testharness start test dll files 
 * 
 * 
 /*
 * This package provides:
 * ----------------------
 * - BuildMother : used for creat different port and child processes
 * 
 * Required Files:
 * ---------------
 * - BuildMother.cs   
 * - Ibuild.cs 
 * - XmlParser.cs
 * 
 *  Maintenance History:
     ver 2.0 : 12 Dec 2017
     - second release
     function added:
        send message to repo, testharness
        
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ServiceModel;
using System.Threading;
using System.Diagnostics;


namespace ProcessPool
{

    using CM = ProcessPool.CommMessage;
    using sender = ProcessPool.Sender;
    class BuildMother
    {

        /*----< start a new child process >----------*/
        public bool startChildProcess(int numOfProcess,int numofProcess)
        {
            Process p = new Process();
            string FileName = "..\\..\\..\\ProcessPool\\bin\\Debug\\ProcessPool.exe";
            int first = numOfProcess + 8080;
            int second = numOfProcess+ numofProcess + 8080;
            string commandline = first.ToString() + " " + second.ToString()+" "+ numOfProcess.ToString();
            try
            {
                Process.Start(FileName, commandline);
            }
            catch(Exception e)
            {
                Console.Write("\n  {0}", e.Message);
                return false;
            }
            return true;
        }

        /*----< get all the request files in the request >----------*/
        private List<string> FileList()
        {
            string path = "..\\..\\..\\ProcessMother\\BuildRequests";
            string[] filelist = Directory.GetFiles(path, "*.xml");
            List<string> fileall = new List<string>();
            //transfer array to list
            foreach (string it in filelist)
            {
                fileall.Add(it);
            }
            return fileall;
        }

        /*----< open a sender >----------*/
        private void startsendOpen(List<Sender> sendlist,int i)
        {
            Sender sndr = new Sender("http://localhost", 8080 + i);
            sendlist.Add(sndr);
            
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "open";
            sndMsg.author = "Quanfeng Du";
            sndMsg.to = "http://localhost:" + (8080 + i).ToString() + "/Ibuilder";
            sndMsg.from = "http://localhost:" + (8080 + i).ToString() + "/Ibuilder";
            sndr.postMessage(sndMsg);
        }

        /*----< start a send source file and request files by each sender >----------*/
        private void startsend(List<string> requestFiles,int port,int numofProcess,Sender send)
        {
            Sender sndr = send;
            sndr.WritePath_ = "..\\..\\..\\ProcessPool\\Build"+(port-numofProcess);
            sndr.postFile(requestFiles[requestFiles.Count() - 1]);
            sendSourceToChild(sndr, requestFiles[requestFiles.Count() - 1]);
            string fileSendToChild= requestFiles[requestFiles.Count() - 1];
            requestFiles.RemoveAt(requestFiles.Count() - 1);
            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "sendfile";
            sndMsg.author = "Quanfeng Du";
            sndMsg.to = "http://localhost:" + (port - numofProcess).ToString() + "/Ibuilder";
            sndMsg.from = "http://localhost:" + (port - numofProcess).ToString() + "/Ibuilder";
            sndMsg.filename = fileSendToChild;
            sndr.postMessage(sndMsg);
        }

        //build mother sends the sources conresponding the request file to child builder
        private void sendSourceToChild(Sender snd,string XmlFile)
        {
            Sender s = snd;
            ParseXml.Parse parse = new ParseXml.Parse();
            if (parse.loadXml(XmlFile) == false)
                Console.Write("\n Unable parse the Xml file!");
            string dir = parse.parseDir("test");
            string SourceFilePath = "..\\..\\..\\ProcessMother\\" + dir;
            string[] sourceFiles = Directory.GetFiles(SourceFilePath, "*.*");
            foreach (string file in sourceFiles)
                s.postFile(file);
        }

        //After receive the number of process, the mother start several receiver
        private void startReceiver(int numofProcess, Receiver rcvr,List<Sender> sendlist)
        {
            for (int i = 1; i <= numofProcess; i++)
            {
                rcvr.start("http://localhost", 8080 + numofProcess + i);
                startChildProcess(i, numofProcess);
                startsendOpen(sendlist, i);
            }
        }

        /*----< quit the child process >----------*/
        private void quit(List<Sender> sendlist)
        {
           Console.Write("\n\n");
            for (int i= 0;i < sendlist.Count();i++)
           {
                CommMessage sndMsg = new CommMessage(CommMessage.MessageType.closeReceiver);
                sndMsg.type = CommMessage.MessageType.closeReceiver;
                sndMsg.from = "http://localhost:" + (8080 + i+1).ToString() + "/Ibuilder";
                sndMsg.to = "http://localhost:" + (8080 + i+1).ToString() + "/Ibuilder";
                sendlist[i].postMessage(sndMsg);
                Console.Write("  Closing port {0} : ", 8081 + i);
                Console.Write("    Please wait.\n");
            }
        }

        //will create directory for diferent group of source files, and send created message back to repo
        private void createDir(CM msg)
        {
            System.IO.Directory.CreateDirectory("..\\..\\..\\ProcessMother\\" + msg.replyto);
            ProcessPool.Sender sdReplyToMotherBuild = new ProcessPool.Sender("http://localhost", 8068);
            CM reply = new CM(CM.MessageType.reply);
            reply.to = "http://localhost:" + (8068).ToString() + "/Ibuilder";
            reply.from = "http://localhost:" + (8068).ToString() + "/Ibuilder";
            reply.command = "DirCreated";
            reply.filename = msg.filename;
            sdReplyToMotherBuild.postMessage(reply);
        }

        //the build mother starts to send files to child process
        private void sendFileToChild(int numofProcess, CommMessage rcvMsg_, List<Sender> sendlist,List<string> requestFiles)
        {
            int lastindex = rcvMsg_.from.LastIndexOf("localhost");
            int port = Int32.Parse(rcvMsg_.from.Substring(lastindex + 10, 4));
            string add = "http://localhost:" + (port - numofProcess).ToString() + "/Ibuilder";
            if (requestFiles.Count() != 0)
            {
                rcvMsg_.show();
                for (int i = 0; i < sendlist.Count(); i++)
                {
                    if (sendlist[i].getAdree() == add)
                        startsend(requestFiles, port, numofProcess, sendlist[i]);
                }
            }
            // if all requests are send, should send message to request child send log file to repo 
            else
                askChildSendLog(rcvMsg_, numofProcess, sendlist);
               
        }
        //send send log request message to child, child will send build log to repository
        private void askChildSendLog(CommMessage rcvMsg_, int numofProcess,List<Sender> sendlist)
        {
            int lastindex = rcvMsg_.from.LastIndexOf("localhost");
            int port = Int32.Parse(rcvMsg_.from.Substring(lastindex + 10, 4));
            foreach (sender s in sendlist)
            {
                CommMessage cm = new CommMessage(CommMessage.MessageType.request);
                cm.command = "SendLog";
                cm.to = "http://localhost:" + (port - numofProcess).ToString() + "/Ibuilder";
                cm.from = "http://localhost:" + (port - numofProcess).ToString() + "/Ibuilder";
                cm.author = "Quanfeng Du";
                s.postMessage(cm);
            }
        }
        //mother send message to testharness to request testharness send test result to repository
        private void requestTestHarnessSendResu()
        {
            Sender s = new sender("http://localhost",8050);
            CommMessage cm = new CommMessage(CommMessage.MessageType.request);
            cm.command = "SendTestResult";
            cm.to = "http://localhost:" + 8050 + "/Ibuilder";
            cm.from = "http://localhost:" + 8050 + "/Ibuilder";
            cm.author = "Quanfeng Du";
            s.postMessage(cm);
        }
        //build mother send start test message to request testharness start test dll files 
        private void RequestTestHarnessTest()
        {
            Sender s = new sender("http://localhost", 8050);
            CommMessage cm = new CommMessage(CommMessage.MessageType.request);
            cm.command = "StartTest";
            cm.to = "http://localhost:" + 8050 + "/Ibuilder";
            cm.from = "http://localhost:" + 8050 + "/Ibuilder";
            cm.author = "Quanfeng Du";
            s.postMessage(cm);
        }




        static void Main(string[] args)
        {
            BuildMother bm = new BuildMother();
            Console.Write("  Build Server Started");
            List<Sender> sendlist = new List<Sender>();// sender list for each child process
            List<string> requestFiles = new List<string>();//a list store all request files
            Receiver rcvr = new Receiver();
            rcvr.start("http://localhost",8079);//start receiver for build mother 
            CommMessage rcvMsg_;
            int numofProcess = 0;//the number of process to be started, will get its true value from command argument
            int numOfDoneDllSend = 0;// the number of DllSendDone message, used to judge when all child process finish send log when it equals to number of process
            while (true)
            {
                rcvMsg_ = rcvr.getMessage();
                if (rcvMsg_.command == "CreateDir")
                {
                    rcvMsg_.show();
                    bm.createDir(rcvMsg_);
                }
                if (rcvMsg_.type == CommMessage.MessageType.startBuild)
                {
                    rcvMsg_.show();
                    numofProcess = Int32.Parse(rcvMsg_.processNum);
                    Console.Write(" The number of child process is {0}: \n", numofProcess);
                    numofProcess = Int32.Parse(rcvMsg_.processNum);//give numofProcess the true value
                    bm.startReceiver(numofProcess, rcvr, sendlist);
                    requestFiles =bm.FileList();
                }
                if (rcvMsg_.command == "doneSendDll")
                {
                    numOfDoneDllSend++;
                    if (numOfDoneDllSend == numofProcess)//compare the value of numOfDoneDllSend with numofProcess
                        bm.RequestTestHarnessTest();
                }
                if (rcvMsg_.command == "ready")
                    bm.sendFileToChild(numofProcess, rcvMsg_, sendlist,requestFiles);
                if(rcvMsg_.command== "DoneTest")
                {
                    rcvMsg_.show();
                    bm.requestTestHarnessSendResu();
                }
                if (rcvMsg_.command == "close")
                {
                    rcvMsg_.show();
                    bm.quit(sendlist);
                    break;
                }
            }
        }
     }
}

