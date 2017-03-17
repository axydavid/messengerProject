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
using EdgeJs;
using System.Threading;
using System.Timers;
using System.Collections;

namespace Wpf_FB
{
    

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        FB_Message msg;
        System.Timers.Timer eventTimer_Login = new System.Timers.Timer();
        System.Timers.Timer eventTimer_RecMsg = new System.Timers.Timer();
        Dictionary<string, ChatWindow> ChatWindowIndex = new Dictionary<string, ChatWindow>();
        double screenWidth = SystemParameters.PrimaryScreenWidth;
        double chatWidth = 310;
        int windowOpen = 0;
        int maxSegments;
        //List<int> screenSegments = new List<int>();
        List<string> screenSegmentss = new List<string>();
        Dictionary<string, int> screenSegments = new Dictionary<string, int>();
        void HandleSomethingHappened(string e, string c, bool a, int p, int op)
        {
            if (p > 0)
            {
                WindowHandler(c, p, op);
            }
            else
            {
                if (!a)
                {
                    Console.WriteLine(e + c);
                    (App.Current as App).fbChat.sendMsg(c, e);
                }
                else
                {
                    Dispatcher.Invoke((Action)delegate ()
                    {
                        ChatWindowIndex.Remove(c);
                        WindowHandler(c, 900000, op);
                        screenSegments.Remove(c);
                        windowOpen = windowOpen - 1;
                    });

                }
            }
        }

        private void WindowHandler(string windowID, int position, int oldposition)
        {
            if (position > oldposition)
            {
                position = position + 300;
            }
            screenSegments[windowID] = 999999999;
            double midvalue = position / 310;
            double midoldvalue = oldposition / 310;
            int detectSegment, detectOldSegment;

            detectSegment = Convert.ToInt32(Math.Round(midvalue, MidpointRounding.AwayFromZero))*100;
            detectOldSegment = Convert.ToInt32(Math.Round(midoldvalue, MidpointRounding.AwayFromZero))*100;
            Console.WriteLine(windowID + " to "+ detectSegment+" from "+ detectOldSegment);
            
            Dictionary<string, int> tempScreenSegments = screenSegments;

            if (detectOldSegment > detectSegment) {

                foreach (var item in screenSegments.ToList())
                {
                    if (item.Value < detectOldSegment & item.Value >= detectSegment)
                    {
                        //add 1 to index
                        Dispatcher.Invoke((Action)delegate () { ChatWindowIndex[item.Key].changePosition(100); });
                        tempScreenSegments[item.Key] = item.Value + 100;
                    }
                }
            }
            if (detectOldSegment < detectSegment)
            {
                foreach (var item in screenSegments.ToList())
                {
                    if (item.Value > detectOldSegment & item.Value <= detectSegment)
                    {
                        //remove 1 to index
                        Dispatcher.Invoke((Action)delegate () { ChatWindowIndex[item.Key].changePosition(-100); });
                        tempScreenSegments[item.Key] = item.Value - 100;
                    }
                }
            }
            tempScreenSegments[windowID] = detectSegment;
            try {
                Dispatcher.Invoke((Action)delegate () { ChatWindowIndex[windowID].moveTo(detectSegment); });
            } catch{ }
            screenSegments = tempScreenSegments;
        }

        void HandleOpenChatWindow(string e, string b)
        {
            createChatWindow(e,b);
        }
        public MainWindow()
        {
            InitializeComponent();
            initWindowHandler();
            txtUsername.Text = "aa16092000@gmail.com";
            pwBox1.Password = "A1r6_C09@2000";
            //ChatWindow winow = new ChatWindow("Hide", "SampleName");
            //winow.Show();
            // Start Timers for different events
            eventTimer_Login.Elapsed += new ElapsedEventHandler(loginChecker);
            eventTimer_Login.Interval = 500;
            eventTimer_Login.Enabled = false;
            eventTimer_RecMsg.Elapsed += new ElapsedEventHandler(msgChecker);
            eventTimer_RecMsg.Interval = 500;
            eventTimer_RecMsg.Enabled = false;

        }


        // Handler for receive message event from fbAPI
        private void msgRecevieHandler(fbAPI api, FB_Message e)
        {
            textBlock1.Text = "Thread " + e.threadID + "\n" + e.body;
        }


        private void startFriendListWindow()
        {
            Dispatcher.Invoke((Action)delegate ()
            {
                FriendListWindow friendwindow = new FriendListWindow();
                friendwindow.Show();
                friendwindow.OpenChatWindow += HandleOpenChatWindow;
            });
        }
        private void loginChecker(object source, ElapsedEventArgs e)
        {
            //Console.WriteLine("Login Check");
            if ((App.Current as App).fbChat.loginState == 1)
            {
                eventTimer_Login.Stop();
                Console.WriteLine("Login sucessful");

                // Start to check new message
                eventTimer_RecMsg.Enabled = true;
                eventTimer_RecMsg.Start();
                // Use Dispatcher to invoke UI update action
                Dispatcher.Invoke((Action)delegate () { textBlock1.Text = "Login sucessful!"; });
                startFriendListWindow();
                Dispatcher.Invoke((Action)delegate () { this.Close(); });

            }
            else if ((App.Current as App).fbChat.loginState == -1)
            {
                eventTimer_Login.Stop();
                Console.WriteLine("Login failed");

                // Use Dispatcher to invoke UI update action
                Dispatcher.Invoke((Action)delegate ()
                {
                    textBlock1.Text = "Login failed!";
                    btnLogin.IsEnabled = true;
                    txtUsername.IsEnabled = true;
                    pwBox1.IsEnabled = true;
                });
            }
        }


        private void msgChecker(object source, ElapsedEventArgs e)
        {
            //Console.WriteLine("Msg check");
            while ((App.Current as App).fbChat.bMsgCollection.TryTake(out msg, 10))
            {

                if (msg.threadID != null)
                {
                    // Update UI
                    /*
                    Dispatcher.Invoke((Action)delegate ()
                    {
                        textBlock1.Text = "Thread: " + msg.threadID + " @ " + msg.timestampToDate() + " UTC\n" + msg.body;
                    });
                    */
                    if (ChatWindowIndex.ContainsKey(msg.threadID))
                    {
                        //ChatWindowIndex[msg.threadID].Show();
                        //Dispatcher.Invoke((Action)delegate () { textBlock1.Text = "Same Window: Thread: " + msg.threadID + "\n" + msg.body; });
                        Dispatcher.Invoke((Action)delegate () { ChatWindowIndex[msg.threadID].sendMessage(msg.body,false); });
                    }
                    else {
                        FB_TheadInfo threadInfo = null;
                        threadInfo = (App.Current as App).fbChat.getThreadInfo(msg.threadID);
                        String threadName = threadInfo.name;
                        Dispatcher.Invoke((Action)delegate () { createChatWindow(msg.threadID, threadName); });
                        //Dispatcher.Invoke((Action)delegate () { textBlock1.Text = "New Window: Thread: " + msg.threadID + "\n" + msg.body; });
                        Dispatcher.Invoke((Action)delegate () { ChatWindowIndex[msg.threadID].sendMessage(msg.body, false); });
                        threadName = "";
                    }
                }
                else
                {
                    Console.WriteLine("Error: Timeout while requesting for thread name");
                }


            }

        }
        private void initWindowHandler()
        {
            double amountDouble = Math.Floor(screenWidth / chatWidth);
            int amount = Convert.ToInt32(amountDouble);
            int intchatWidth = Convert.ToInt32(chatWidth);
            maxSegments = amount;
        }
        public int createChatWindow(String msgID, String threadName)
        {
            screenSegments[msgID] = windowOpen * 100;
            int windowSegment = screenSegments[msgID];
            ChatWindowIndex[msgID] = new ChatWindow(msgID, threadName, windowSegment);
            ChatWindowIndex[msgID].Title = threadName;
            ChatWindowIndex[msgID].Show();
            ChatWindowIndex[msgID].SomethingHappened += new ChatWindow.MyEventHandler(HandleSomethingHappened);

            windowOpen++;
            return 1;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            // Disable input fields
            btnLogin.IsEnabled = false;
            txtUsername.IsEnabled = false;
            pwBox1.IsEnabled = false;
            textBlock1.Text = "Logging in...";
            (App.Current as App).fbChat.username = txtUsername.Text;
            (App.Current as App).fbChat.password = pwBox1.Password;

            // The following way won't work as the parameters and the method are in different thread
            //Task.Run(() => (App.Current as App).Start(txtUsername.Text, pwBox1.Password));   // () is lambda expression
            Task.Run((Action)(App.Current as App).fbChat.Start);

            // Start login state checker
            eventTimer_Login.Enabled = true;

            // Start the facebook api and login
            //Task.Run((Action)Start);
        }
    }
}
