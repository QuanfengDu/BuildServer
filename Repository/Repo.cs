/////////////////////////////////////////////////////////////////////
// Repo.cs                                                         //
//                                                                 //
// Author: Quanfeng Du,  qdu101@syr.edu                            //
// Application: BuildServer                                        //
// Environment: C# console                                         //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * this package is the repository, which accept request files from client, send file and source files to build server, accept the result files 
 * 
 * public Interface:
 * -------------------
 * initializeDispatcher--------------------initialize the diapather dictionary
 * 
 * other functions:
 * 
 * sendRequesttoBuild - send the request files and the conresponding scource files to the build mother 
 * sendCreateDirtoBuild - send request message which request the build mother to create directory
 * parseDir - used to parse the xml to get the directory information
 * 
 * 
 * 
 * Required Files:
 * ---------------
 * Commu.cs
 * CsBlockingQueue
 * Xmlparser
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


namespace Repository
{
    using CM = ProcessPool.CommMessage;
    public class Repo
    {
       //the sender to MotherBuild
        ProcessPool.Sender sdToMotherBuild = new ProcessPool.Sender("http://localhost", 8079);
        //A dictionary store the command the action
        Dictionary<string, Func<CM, CM>> messageDispatcher = new Dictionary<string, Func<CM, CM>>();

        //initialize the dispather dictionary with commmand and action
        public void initializeDispatcher()
        {
            Func<CM, CM> viewSourceFiles = (CM msg) =>
               { 
                   string path = "..\\..\\..\\Repository\\sourceFIles\\" + msg.filename;
                   CM reply = new CM(CM.MessageType.reply);
                   reply.to = "http://localhost:" + (8069).ToString() + "/Ibuilder";
                   reply.from = "http://localhost:" + (8069).ToString() + "/Ibuilder";
                   reply.command = "viewSourceFiles";
                   reply.arguments = Directory.GetFiles(path,"*.cs").ToList<string>();
                   return reply;
               };
            messageDispatcher["viewSourceFiles"] = viewSourceFiles;
            Func<CM, CM> RequestDirs = (CM msg) =>
            {
                string path = "..\\..\\..\\Repository\\sourceFIles";
                List<string> dirs = new List<string>();
                dirs = Directory.GetDirectories(path).ToList<string>();
                CM reply = new CM(CM.MessageType.reply);
                reply.to = "http://localhost:" + (8069).ToString() + "/Ibuilder";
                reply.from = "http://localhost:" + (8069).ToString() + "/Ibuilder";
                reply.command = "RequestDirs";
                reply.arguments = dirs;
                return reply;
            };
            messageDispatcher["RequestDirs"] = RequestDirs;
            Func<CM, CM> repoRequestfile = (CM msg) =>
            {
                string path = "..\\..\\..\\Repository\\BuildRequests";
                CM reply = new CM(CM.MessageType.reply);
                reply.to = "http://localhost:" + (8069).ToString() + "/Ibuilder";
                reply.from = "http://localhost:" + (8069).ToString() + "/Ibuilder";
                reply.command = "repoRequestfile";
                reply.arguments = Directory.GetFiles(path,"*.xml").ToList<string>();
                return reply;
            };
            messageDispatcher["repoRequestfile"] = repoRequestfile;
        }

        //send the request files and the conresponding scource files to the build mother 
        private void sendRequesttoBuild(CM msg)
        {
            string path = Path.Combine("..\\..\\..\\Repository\\BuildRequests", msg.filename);
            string direc= parseDir(path);
            CM reply = new CM(CM.MessageType.request);
            reply.to = "http://localhost:" + (8079).ToString() + "/Ibuilder";
            reply.from = "http://localhost:" + (8079).ToString() + "/Ibuilder";
            reply.command = "sendRequestFileToBuild";
            reply.filename = path;
            sdToMotherBuild.postMessage(reply);
            sdToMotherBuild.WritePath_ = "..\\..\\..\\ProcessMother\\BuildRequests";
            Console.Write("\n\n repository send file {0} to build mother ..\\..\\..\\ProcessMother\\BuildRequests",msg.filename);
            sdToMotherBuild.postFile(path);  //post the requet files
            List<string> sourceFiles = Directory.GetFiles("..\\..\\..\\Repository\\sourceFIles\\" + direc,"*.*").ToList();
            sdToMotherBuild.WritePath_ = "..\\..\\..\\ProcessMother\\" + direc;
            foreach (string file in sourceFiles) // post the scource fils associated with the request files
            {
                Console.Write("\n\n repository send source file {0} to build mother ..\\..\\..\\ProcessMother\\{1}", Path.GetFileName(file),direc);
                sdToMotherBuild.postFile(file);
            }
        }

        //send request message which request the build mother to create directory
        private void sendCreateDirtoBuild(CM msg)
        {
            string path = Path.Combine("..\\..\\..\\Repository\\BuildRequests", msg.filename);
            CM reply = new CM(CM.MessageType.request);
            reply.to = "http://localhost:" + (8079).ToString() + "/Ibuilder";
            reply.from = "http://localhost:" + (8079).ToString() + "/Ibuilder";
            reply.command = "CreateDir";
            reply.filename = msg.filename;
            reply.replyto= parseDir(path);
            sdToMotherBuild.postMessage(reply);
        }

        //used to parse the xml to get the directory information
        private string parseDir(string path)
        {
            ParseXml.Parse parse = new ParseXml.Parse();
            if(parse.loadXml(path)==false)
                Console.Write("\n Unable parse the Xml file!");
            return parse.parseDir("test");
        }



        static void Main(string[] args)
        {
            Console.Write("\n  Repository Server Started!");
            try
            {
                Repo repo = new Repo();
                repo.initializeDispatcher();
                ProcessPool.Receiver re = new ProcessPool.Receiver();
                re.start("http://localhost", 8068);//start the receiver for repo
                ProcessPool.Sender sd = new ProcessPool.Sender("http://localhost", 8069);// start sender for repo
                while (true)
                {
                    CM msg = re.getMessage();
                    if (msg.type == CM.MessageType.closeReceiver)  
                        break;
                    if (msg.command == "SendRequest")
                    {
                        msg.show();
                        continue;
                    }
                    if (msg.command == null)   continue;
                    if(msg.command== "SendRequestToBuildServer")
                    {
                        msg.show();
                        repo.sendCreateDirtoBuild(msg);
                        continue;
                    }
                    if (msg.command == "DirCreated")
                    {
                        msg.show();
                        repo.sendRequesttoBuild(msg);
                        continue;
                    }
                    if (msg.command == "log")
                        continue;
                    if(msg.command== "sendTestResultToRepo") {
                        msg.show();
                        continue;
                    }
                    CM reply = repo.messageDispatcher[msg.command](msg);
                    reply.show();
                    sd.postMessage(reply);
                }
            }
            catch(Exception ex)
            {
                Console.Write("\n exception thrown:\n{0}\n\n", ex.Message);
            }
        }
    }
}
