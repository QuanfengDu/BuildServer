/* 
  ///////////////////////////////////////////////////////////////////////
  // MainWindow.xaml - Client prototype GUI                            //
  // Ver 1.0                                                           //
  // Quanfeng Du, CSE681-OnLine, Fall 2017                             //
  ///////////////////////////////////////////////////////////////////////
  
   Purpose:
     Prototype for a client Fedration.This application
     connect to the Morther Process.
     It simply explores the kinds of user interface elements needed for that.

  Public Interface
  ----------------
  MainWindow - initialize the main window
  dirict - get the directory choosed in the list



  All other functions:
  ------------------
  rcvThreadProc - Thread pool deque message from child process and deal with the message
  initializeMessageDispatcher() - Initialize the dispatcher Dictionary
  
  allFilesinRep - get all the files in the repo which will used to generate the xml files
  showFilesinDir - send the show dir message, request source file in the directory to repo 
  showSelectedFiles - Show the files selected in the repo 
  showSelectedFiles - Show the files selected in the repo 
  allRequest - add request fils in the request folder to the boxlist
  SendRequestFilestoRepo - send the sendRequest message to repository and send the request file to repo
  RequestRepoSendRequestFiles - the Gui send request to repository which ask repository send files selected in list box to build mother
  repoRequestfile - request repository return all request files' name in repository to GUI
  SendNumProcess - generate the number of process message 
  SendProcessNumber - send the number of process
  Sendquit - generate the quit message 
  quitmessage send the quit message 


   Required Files:
     MainWindow.xaml, MainWindow.xaml.cs
     Window1.xaml, Window1.xaml.cs

   Maintenance History:
     ver 2.0 : 12 Dec 2017
     - second release
     - add functions:
         get file form repo
         send message and files to repo
*/



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Threading;
using System.ServiceModel;
using System.Diagnostics;

namespace ClientGUI
{
   using CM = ProcessPool.CommMessage;

    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public String dirict { get; set; } = "";//get the directory choosed in the list
        //sender to Build Mother
        private ProcessPool.Sender sndr = new ProcessPool.Sender("http://localhost", 8079);
        //sender to Repo 
        private ProcessPool.Sender sndrToRepo = new ProcessPool.Sender("http://localhost",8068);
        //receiver in GUI
        private ProcessPool.Receiver rece = new ProcessPool.Receiver();
        //Disparcher dictionary
        private Dictionary<string, Action<CM>> messageDispatcher = new Dictionary<string, Action<CM>>();
        Thread rcvThread = null;

        private SelectedWindow sw;

        //the Main windoe function
        public MainWindow()
        {
            InitializeComponent();
            initializeMessageDispatcher();
            rece.start("http://localhost", 8069);
            Process p = new Process();
            string BuildMotherFileName = "..\\..\\..\\ProcessMother\\bin\\Debug\\ProcessMother.exe";
            Process.Start(BuildMotherFileName);
            string RepositoryFileName = "..\\..\\..\\Repository\\bin\\Debug\\Repository.exe";
            Process.Start(RepositoryFileName);
            string testHarnessFileName = "..\\..\\..\\TestHarness\\bin\\Debug\\TestHarness.exe";
            Process.Start(testHarnessFileName);
            rcvThread = new Thread(rcvThreadProc);
            rcvThread.Start();
           
        }

 
            
           
        

       


        //Thread pool deque message from child process and deal with the message
        private void rcvThreadProc()
        {
            Console.Write("\n  starting client's receive thread");
            while (true)
            {
                CM msg = rece.getMessage();
                msg.show();
                if (msg.command == null)
                    continue;

                // pass the Dispatcher's action value to the main thread for execution
                Dispatcher.Invoke(messageDispatcher[msg.command], new object[] { msg });
          
            }
        }
        //Initialize the dispatcher Dictionary
        private void initializeMessageDispatcher()
        {
            messageDispatcher["RequestDirs"] = (CM msg) => {
                filesListBox.Items.Clear();
                foreach (string file in msg.arguments)
                {
                    string f = file.Substring(file.LastIndexOf('\\')+1);
                    filesListBox.Items.Add(f);
                }
            
            };

            messageDispatcher["viewSourceFiles"] = (CM msg) => {
                filesListBox.Items.Clear();
                foreach (string file in msg.arguments)
                {
                    string f = file.Substring(file.LastIndexOf('\\') + 1);
                    filesListBox.Items.Add(f);
                }
            };

            messageDispatcher["repoRequestfile"] = (CM msg) => {
                RequestFiles.Items.Clear();
                foreach (string file in msg.arguments)
                {
                    string f = file.Substring(file.LastIndexOf('\\') + 1);
                    RequestFiles.Items.Add(f);
                }
            };
        }
        /*----< get all the files in the repo which will used to generate the xml files >-------------*/
        private void allFilesinRepo(object sender,RoutedEventArgs e)
        {
            CM msg1 = new CM(CM.MessageType.request);
            msg1.from = "http://localhost:" + (8068).ToString() + "/Ibuilder";
            msg1.to= "http://localhost:" + (8068).ToString() + "/Ibuilder";
            msg1.author = "Quanfeng Du";
            msg1.command = "RequestDirs";
            msg1.arguments.Add("");
            sndrToRepo.postMessage(msg1);
        }
        //send the show dir message, request source file in the directory to repo 
        private void showFilesinDir(object sender,RoutedEventArgs e)
        {
            CM msg1 = new CM(CM.MessageType.request);
            msg1.from = "http://localhost:" + (8068).ToString() + "/Ibuilder";
            msg1.to = "http://localhost:" + (8068).ToString() + "/Ibuilder";
            msg1.author = "Quanfeng Du";
            msg1.command = "viewSourceFiles";
            msg1.arguments.Add("");
            msg1.filename = filesListBox.SelectedItem as string;
            dirict = filesListBox.SelectedItem as string;
            sndrToRepo.postMessage(msg1);
        }
        /*----< Show the files selected in the repo >-------------*/
        private void showSelectedFiles(object sender,RoutedEventArgs e)
        {
            List<String> list = new List<string>();//store selected files as a list
           foreach(Object selecteditem in filesListBox.SelectedItems)
            {
                string file = selecteditem as string;
                list.Add(file);
            }
            //open the selected window, used to generate the xml request files 
            //SelectedWindow sw = new SelectedWindow(list);
            sw = new SelectedWindow(list);
            sw.dirrc = this.dirict;
            sw.Show();
        }
        /*----< add request fils in the request folder to the boxlist>-------------*/
        private void allRequest(object sender,RoutedEventArgs e)
        {
            RequestFiles.Items.Clear();
            string path = "../../../request";
            string[] files = Directory.GetFiles(path, "*.xml");
            foreach(string file in files)
            {
                RequestFiles.Items.Add(System.IO.Path.GetFileName(file));
            }
        }
        //send the sendRequest message to repository and send the request file to repo
        private void SendRequestFilestoRepo(object sender,RoutedEventArgs e)
        {
            sndrToRepo.WritePath_ = "..\\..\\..\\Repository\\BuildRequests";
            CM msg1 = new CM(CM.MessageType.request);
            msg1.from = "http://localhost:" + (8068).ToString() + "/Ibuilder";
            msg1.to = "http://localhost:" + (8068).ToString() + "/Ibuilder";
            msg1.author = "Quanfeng Du";
            msg1.command = "SendRequest";
            msg1.arguments.Add(RequestFiles.SelectedItem as string);
            msg1.filename = filesListBox.SelectedItem as string;
            sndrToRepo.postMessage(msg1);
            //get the file name being selected in GUI
            string selet = "..\\..\\..\\request\\" + RequestFiles.SelectedItem as string;
            Console.Write("\n\n client send {0} to repository ..\\..\\..\\Repository\\BuildRequests", filesListBox.SelectedItem as string);
            sndrToRepo.postFile(selet);
           
        }

        //the Gui send request to repository which ask repository send files selected in list box to build mother
        private void RequestRepoSendRequestFiles(object sender,RoutedEventArgs e)
        {
            CM msg1 = new CM(CM.MessageType.request);
            msg1.from = "http://localhost:" + (8068).ToString() + "/Ibuilder";
            msg1.to = "http://localhost:" + (8068).ToString() + "/Ibuilder";
            msg1.author = "Quanfeng Du";
            msg1.command = "SendRequestToBuildServer";
            msg1.filename = RequestFiles.SelectedItem as string;// get the selected file
            sndrToRepo.postMessage(msg1);
        }

        //request repository return all request files' name in repository to GUI
        private void repoRequestfile(object sender,RoutedEventArgs e)
        {
            CM msg1 = new CM(CM.MessageType.request);
            msg1.from = "http://localhost:" + (8068).ToString() + "/Ibuilder";
            msg1.to = "http://localhost:" + (8068).ToString() + "/Ibuilder";
            msg1.author = "Quanfeng Du";
            msg1.command = "repoRequestfile";
            msg1.arguments.Add("");
            msg1.filename = filesListBox.SelectedItem as string;
            sndrToRepo.postMessage(msg1);
        }
        /*----< generate the number of process message >-------------*/
        private CM SendNumProcess()
        {
            CM sndMsg = new CM(CM.MessageType.startBuild);
            sndMsg.command = "Start";
            sndMsg.author = "Quanfeng Du";
            sndMsg.processNum = processNum.Text.ToString();
            sndMsg.to = "http://localhost:" + (8079).ToString() + "/Ibuilder";
            sndMsg.from = "http://localhost:" + (8079).ToString() + "/Ibuilder";
            return sndMsg;
        }
        /*----< send the number of process >-------------*/
        private void SendProcessNumber(object sender,RoutedEventArgs e)
        {
            sndr.postMessage(SendNumProcess());
        }
        /*----< generate the quit message >-------------*/
        private CM Sendquit()
        {
            CM sndMsg = new CM(CM.MessageType.request);
            sndMsg.command = "close";
            sndMsg.author = "Quanfeng Du";
            sndMsg.processNum = processNum.Text.ToString();
            sndMsg.to = "http://localhost:" + (8079).ToString() + "/Ibuilder";
            sndMsg.from = "http://localhost:" + (8079).ToString() + "/Ibuilder";
            sndMsg.show();
            return sndMsg;
        }
        /*----< send the quit message >-------------*/
        private void quitmessage(object sender, RoutedEventArgs e)
        {
            CM sndMsg = Sendquit();
            sndr.postMessage(sndMsg);
        }
    }
}
