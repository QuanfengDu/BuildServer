/////////////////////////////////////////////////////////////////////
// Ibuilder.cs - service interface for buildprocess                //
// ver 2.1                                                         //
//Quanfeng Du, CSE681, Fall 2017                                   //
/////////////////////////////////////////////////////////////////////
/*
 * Added references to:
 * - System.ServiceModel
 * - System.Runtime.Serialization
 */
/*
 * This package provides:
 * ----------------------
 * - Ibuilder : interface used for message passing and file transfer
 * - CommMessage         : class representing serializable messages
 * 
 * Public Interface:
 *   show - show the content of the message 
 *   
 * Required Files:
 * ---------------
 * - Ibuilder.cs         : Service interface and Message definition
 * 
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
using System.ServiceModel;
using System.Runtime.Serialization;
using System.Threading;

namespace ProcessPool
{
    using Command = String;
    using EndPoint = String;
    using Argument = String;
    using ErrorMessage = String;

    [ServiceContract(Namespace = "ProcessPool")]

    public interface Ibuilder
    {
        /*----< support for message passing >--------------------------*/
        [OperationContract(IsOneWay = true)]
        void postMessage(CommMessage msg);

        // private to receiver so not an OperationContract

        CommMessage getMessage();

        /*----< support for sending file in blocks >-------------------*/

        [OperationContract]
        bool openFileForWrite(string writePath, string name, int port);

        [OperationContract]
        bool writeFileBlock(byte[] block);

        [OperationContract(IsOneWay = true)]
        void closeFile();

    }


    [DataContract]
    public class CommMessage
    {
        public enum MessageType
        {
            [EnumMember]
            connect,           // initial message sent on successfully connecting
            [EnumMember]
            request,           // request for action from receiver
            [EnumMember]
            reply,             // response to a request
            [EnumMember]
            closeSender,       // close down client
            [EnumMember]
            startBuild,
            [EnumMember]
            closeReceiver      // close down server for graceful termination
        }

        /*----< constructor requires message type >--------------------*/

        public CommMessage(MessageType mt)
        {
            type = mt;
        }
        /*----< data members - all serializable public properties >----*/

        [DataMember]
        public MessageType type { get; set; } = MessageType.connect;



        [DataMember]
        public string to { get; set; }

        [DataMember]
        public string from { get; set; }

        [DataMember]
        public string replyto { get; set; }

        [DataMember]
        public string replyfrom { get; set; }

        [DataMember]
        public string author { get; set; }

        [DataMember]
        public Command command { get; set; }

        [DataMember]
        public string filename { get; set; }

        [DataMember]
        public string processNum { get; set; }

        [DataMember]
        public List<Argument> arguments { get; set; } = new List<Argument>();

        [DataMember]
        public int threadId { get; set; } = Thread.CurrentThread.ManagedThreadId;

        [DataMember]
        public ErrorMessage errorMsg { get; set; } = "no error";

        public void show()
        {
            Console.Write("\n  CommMessage:");
            Console.Write("\n    MessageType : {0}", type.ToString());
            Console.Write("\n    to          : {0}", to);
            Console.Write("\n    from        : {0}", from);
            Console.Write("\n    author      : {0}", author);
            Console.Write("\n    command     : {0}", command);
            Console.Write("\n    filename    : {0}", filename);
            Console.Write("\n    arguments   :");
            if (arguments.Count > 0)
                Console.Write("\n      ");
            foreach (string arg in arguments)
                Console.Write("{0} ", arg);
            Console.Write("\n    ThreadId    : {0}", threadId);
            Console.Write("\n    errorMsg    : {0}\n", errorMsg);
        }
    }
}
