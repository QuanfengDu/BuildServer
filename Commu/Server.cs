/////////////////////////////////////////////////////////////////////
// Program.cs - service interface for buildprocess                //
// ver 2.1                                                         //
//Quanfeng Du, CSE681, Fall 2017                                   //
/////////////////////////////////////////////////////////////////////
/*
 * package operation:
 * this package include the send and receiver, used as communication channel 
 * 
 * Added references to:
 * - System.ServiceModel
 * - System.Runtime.Serialization
 */
/*
 * This package provides three classes:
 * - Sender which implements the public methods:
 *   -------------------------------------------
 *   - connect          : opens channel and attempts to connect to an endpoint, 
 *                        trying multiple times to send a connect message
 *   - close            : closes channel
 *   - postMessage      : posts to an internal thread-safe blocking queue, which
 *                        a sendThread then dequeues msg, inspects for destination,
 *                        and calls connect(address, port)
 *   - postFile         : attempts to upload a file in blocks
 *   - getLastError     : returns exception messages on method failure
 * - Receiver which implements the public methods:
 *   ---------------------------------------------
 *   - start            : creates instance of ServiceHost which services incoming messages
 *   - postMessage      : Sender proxies call this message to enqueue for processing
 *   - getMessage       : called by Receiver application to retrieve incoming messages
 *   - close            : closes ServiceHost
 *   - openFileForWrite : opens a file for storing incoming file blocks
 *   - writeFileBlock   : writes an incoming file block to storage
 *   - closeFile        : closes newly uploaded file
 *   
  * - Program which implements the test method
  * 
 * Required Files:
 * ---------------
 * - Ibuilder.cs         : Service interface and Message definition
 * - Program.cs          : Sender and Receiver
 * Maintenance History:
 * --------------------
 * ver 1.0 :  Oct 2017
 * - first release
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
    ///////////////////////////////////////////////////////////////////
    // Sender class - sends messages and files to Receiver
    public class Sender
    {
        private Ibuilder channel;
        private ChannelFactory<Ibuilder> factory = null;
        private SWTools.BlockingQueue<CommMessage> sndQ = null;
        private int port = 0;
        private string fromAddress = "";
        private string toAddress = "";
        private Thread sndThread = null;
        private int tryCount = 0, maxCount = 10;
        private string lastError = "";
        private string lastUrl = "";
        public String WritePath_ { get; set; } = "";

        /*----< get current address >------------------------------------------*/
        public string getAdree()
        {
            return lastUrl;
        }


        /*----< constructor >------------------------------------------*/

        public Sender(string baseAddress, int listenPort)
        {
            port = listenPort;
            fromAddress = baseAddress + listenPort.ToString() + "/Ibuilder";
            sndQ = new SWTools.BlockingQueue<CommMessage>();
            sndThread = new Thread(threadProc);
            sndThread.Start();
        }

        /*----< creates proxy with interface of remote instance >------*/
        public void createSendChannel(string address)
        {
            EndpointAddress baseAddress = new EndpointAddress(address);
            WSHttpBinding binding = new WSHttpBinding();
            factory = new ChannelFactory<Ibuilder>(binding, address);
            channel = factory.CreateChannel();
        }

        /*----< attempts to connect to Receiver instance >-------------*/
        public bool connect(string baseAddress, int port)
        {
            toAddress = baseAddress + ":" + port.ToString() + "/Ibuilder";
            return connect(toAddress);
        }

        /*----< attempts to connect to Receiver instance >-------------*/
        /*
         * - attempts a finite number of times to connect to a Receiver
         * - first attempt to send will throw exception of no listener
         *   at the specified endpoint
         * - to test that we attempt to send a connect message
         */
        public bool connect(string toAddress)
        {
            int timeToSleep = 500;
            createSendChannel(toAddress);
            CommMessage connectMsg = new CommMessage(CommMessage.MessageType.connect);
            while (true)
            {
                try
                {
                    channel.postMessage(connectMsg);
                    tryCount = 0;
                    return true;
                }
                catch (Exception ex)
                {
                    if (++tryCount < maxCount)
                    {
                        Thread.Sleep(timeToSleep);
                    }
                    else
                    {
                        lastError = ex.Message;
                        return false;
                    }
                }
            }
        }

        /*----< processing for receive thread >------------------------*/
        /*
         * - send thread dequeues send message and posts to channel proxy
         * - thread inspects message and routes to appropriate specified endpoint
         */
        private void threadProc()
        {
            while (true)
            {
                CommMessage msg = sndQ.deQ();
                if (msg.type == CommMessage.MessageType.closeSender)
                {                   
                    break;
                }
                if (msg.to == lastUrl)
                {
                    channel.postMessage(msg);
                }
                else
                {
                    close();
                    if (!connect(msg.to))
                        continue;
                    lastUrl = msg.to;
                    channel.postMessage(msg);
                }
            }
        }

        /*----< closes Sender's proxy >--------------------------------*/

        public void close()
        {
            while (sndQ.size() > 0)
            {
                CommMessage msg = sndQ.deQ();
                channel.postMessage(msg);
            }

            try
            {
                if (factory != null)
                    factory.Close();
            }
            catch (Exception ex)
            {
                Console.Write("\n  already closed {0}",ex.Message);
            }
        }

        /*----< main thread enqueues message for sending >-------------*/

        public void postMessage(CommMessage msg)
        {
            sndQ.enQ(msg);
        }
        /*----< uploads file to Receiver instance >--------------------*/
        public bool postFile(string fileName)
        {
            FileStream fs = null;
            long bytesRemaining;
            try
            {
                string path = fileName;
                string name = Path.GetFileName(path);
                fs = File.OpenRead(path);
                bytesRemaining = fs.Length;
                channel.openFileForWrite(this.WritePath_,name, port);
                while (true)
                {
                    long bytesToRead = Math.Min(1024, bytesRemaining);
                    byte[] blk = new byte[bytesToRead];
                    long numBytesRead = fs.Read(blk, 0, (int)bytesToRead);
                    bytesRemaining -= numBytesRead;
            
                    channel.writeFileBlock(blk);
                    if (bytesRemaining <= 0)
                        break;
                }
                channel.closeFile();
                fs.Close();
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
            return true;
        }

    }


    ///////////////////////////////////////////////////////////////////
    // Receiver class - receives CommMessages and Files from Senders
    public class Receiver : Ibuilder
    {
        public static SWTools.BlockingQueue<CommMessage> replyRcvQ { get; set; } = null;
        public bool restartFailed { get; set; } = false;
        private ServiceHost commHost = null;
        private FileStream fs = null;
        private string lastError = "";
        public string writePath{get;set;}= "..\\..\\..\\ProcessMother\\test1";

        /*----< constructor >------------------------------------------*/
        public Receiver()
        {
            if (replyRcvQ == null)
                replyRcvQ = new SWTools.BlockingQueue<CommMessage>();
        }

        /*----< create ServiceHost listening on specified endpoint >---*/
        /*
        * baseAddress is of the form: http://IPaddress or http://networkName
        */
        public bool start(string baseAddress, int port)
        {
            try
            {
                string address = baseAddress + ":" + port.ToString() + "/Ibuilder";
                createCommHost(address);
                restartFailed = false;
                return true;
            }

            catch (Exception ex)
            {
                restartFailed = true;
                Console.Write("\n{0}\n", ex.Message);
                Console.Write("\n  You can't restart a listener on a previously used port");
                Console.Write(" - Windows won't release it until the process shuts down");
                return false;
            }

        }

        /*----< create ServiceHost listening on specified endpoint >---*/
        /*
         * address is of the form: http://IPaddress:8080/IPluggableComm
         */
        public void createCommHost(string address)
        {
            WSHttpBinding binding = new WSHttpBinding();
            Uri baseAddress = new Uri(address);
            commHost = new ServiceHost(typeof(Receiver), baseAddress);
            commHost.AddServiceEndpoint(typeof(Ibuilder), binding, baseAddress);
            commHost.Open();
        }

        /*----< enqueue a message for transmission to a Receiver >-----*/

        public void postMessage(CommMessage msg)
        {
            msg.threadId = Thread.CurrentThread.ManagedThreadId;
            replyRcvQ.enQ(msg);
        }

        /*----< retrieve a message sent by a Sender instance >---------*/

        public CommMessage getMessage()
        {
            CommMessage msg = replyRcvQ.deQ();
            if (msg.type == CommMessage.MessageType.closeReceiver)
            {
                close();
            }
            if (msg.type == CommMessage.MessageType.connect)
            {
                msg = replyRcvQ.deQ();  // discarding the connect message
            }
            return msg;
           
        }

        /*----< close ServiceHost >------------------------------------*/

        public void close()
        {
            Console.Write("\n  closing receiver - please wait");
            commHost.Close();
        }

        /*---< called by Sender's proxy to open file on Receiver >-----*/

        public bool openFileForWrite(string path_,string name, int port)
        {
            try
            {
                string path = Path.Combine(path_, name);
                fs = File.OpenWrite(path);
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
        }
        /*----< write a block received from Sender instance >----------*/

        public bool writeFileBlock(byte[] block)
        {
            try
            {
                fs.Write(block, 0, block.Length);
                return true;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return false;
            }
        }
        /*----< close Receiver's uploaded file >-----------------------*/

        public void closeFile()
        {
            fs.Close();
        }
    }

    #if (TEST_PROGRAM)
    class Program
    {
        static void Main(string[] args)
        {
            Receiver rcvr = new Receiver();
            rcvr.start("http://localhost", 8080);
            Sender sndr = new Sender("http://localhost", 8080);

            CommMessage sndMsg = new CommMessage(CommMessage.MessageType.request);
            sndMsg.command = "show";
            sndMsg.author = "Jim Fawcett";
            sndMsg.to = "http://localhost:8080/IPluggableComm";
            sndMsg.from = "http://localhost:8080/IPluggableComm";
            sndr.postMessage(sndMsg);
            CommMessage rcvMsg;
            // get connection message
            rcvMsg = rcvr.getMessage();
            rcvMsg.show();
            // get first info message
            rcvMsg = rcvr.getMessage();
            rcvMsg.show();
            
        }
    }
#endif
}
