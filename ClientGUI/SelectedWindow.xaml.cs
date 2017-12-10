/*
  /////////////////////////////////////////////////////////////////////////////
  // SelectedWindow.xaml                                                     //
  // Ver 1.0                                                                 //
  // Quanfeng Du, CSE681, Fall 2017                                          //
  /////////////////////////////////////////////////////////////////////////////
  
   Purpose:
     Prototype for a secondary popup window for the Client.

  Public Interface
  ----------------
  SelectedWindow - initiaize the selected window, take argument as the selected source file in the main window
  dirict - get the directory choosed in the list
  generaterequest - generate the request file



  All other functions:
  ------------------
  initializeListBox - initialize the content of the listbox with name of source files
  generaterequest - generate the request file
  exitButton_Click - Close the selected window
  exitButton_Click - close the window

   Required Files:
     MainWindow.xaml, MainWindow.xaml.cs - view into repository and checkin/checkout
     SelectedWindow.xaml, SelectedWindow.xaml.cs

   Maintenance History:
     ver 2.0 : 12 Dec 2017
     - second release
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
using System.Windows.Shapes;

namespace ClientGUI
{
    /// <summary>
    /// Interaction logic for SelectedWindow.xaml
    /// </summary>
   
    public partial class SelectedWindow : Window
    {

        public String dirrc { get; set; } = "";//store the name of the directory
        static private int num = 0;
        private List<String> filelist { get; set; } = new List<string>();
        //initiaize the selected window, take argument as the selected source file in the main window
        public SelectedWindow(List<String> files)
        {
            InitializeComponent();
            for (int i = 0; i < files.Count(); i++)
            {
                filelist.Add(files[i]);
            }
            initializeListBox();
        }


        /*----< initialize the content of the listbox with name of source files >-------------*/
        private void initializeListBox()
        {
            for(int i = 0; i < filelist.Count; i++)
            {
                selectedFiles.Items.Add(filelist[i]);
            }           
        }

        /*----< generate the request file >-------------*/
        public void generaterequest(object sender, RoutedEventArgs e)
        {
            List<string> list = new List<string>();
           foreach(string item in selectedFiles.Items)
            {
                list.Add(item.ToString());
            }
            xmlgenerator xg = new xmlgenerator();
            xg.author = "QuanfengDu"+(num++).ToString();
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
            Console.Write("\n\n  file {0}.xml generate in ..\\..\\..\\request", xg.author);
        }

        /*----< Close the window >-------------*/
        public void exitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
