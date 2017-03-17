using System;
using System.Collections.Generic;
using System.IO;
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

namespace Wpf_FB
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {

        private List<FB_Message> msgHistory;
        int position, msgback;
        private String windowID, myID;
        public delegate void MyEventHandler(string e, string c, bool a, int p, int op);
        public event MyEventHandler SomethingHappened;
        FB_TheadInfo threadInfo = new FB_TheadInfo();
        Rect desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
        string myfacebookID = (App.Current as App).fbChat.currentUserID;
        public ChatWindow(string ID,string NM, int PO)
        {
            msgback = 0;
            position = PO;
            windowID = ID;
            myID = ID;
            InitializeComponent();
            getMsgHistory(20);
            scroller.ScrollToBottom();
            chatName.Content = NM;
            this.Left = desktopWorkingArea.Left+position*3.1;
            Console.WriteLine(SystemParameters.PrimaryScreenWidth +" and "+ ((Panel)Application.Current.MainWindow.Content).ActualWidth);
            this.Top = desktopWorkingArea.Bottom- this.Height;
            


        }


        public void getMsgHistory(int i)
        {
            int e;
            int d = 0;
            e = i + msgback;
            threadInfo = (App.Current as App).fbChat.getThreadInfo(windowID);
            msgHistory = (App.Current as App).fbChat.getThreadHistory(threadInfo, e);
            

            Dispatcher.Invoke((Action)delegate ()
            {
                msgHistory.Reverse();
                foreach (FB_Message user in msgHistory)
                {
                    if (d < msgback)
                    {
                        d++;
                    }
                    else
                    {
                        Console.WriteLine(user.timestamp);
                        MyData data = new MyData(user.senderID, user.body, user.timestamp.ToString());
                        Console.WriteLine(user.senderID + user.body);
                        if (user.senderID.Substring(5, user.senderID.Length - 5) == myfacebookID)
                        {
                            Console.Write(user.body);
                            sentMessage(user.body, true);
                        }
                        else
                        {
                            sendMessage(user.body, true);
                        }

                    }
                }
                d = 0;
                msgHistory = null;
            });
            msgback += 20;
        }

        public void changePosition(int np)
        {
            this.Left = this.Left + np * 3.1;
        }
        public void moveTo(int np)
        {
            this.Left = desktopWorkingArea.Left + np * 3.1;
        }
        public void sendMessage(String i, bool insert)
        {
            Border border = new Border();
            double bleft = 6;
            double left = 36, top = 2, right = 0, bottom = 0;
            border.Margin = new Thickness(left, top, right, bottom);
            border.CornerRadius = new CornerRadius(bleft);
            border.BorderThickness = new Thickness(1);
            border.BorderBrush = new SolidColorBrush(Color.FromRgb(213, 213, 213));
            border.Background = new SolidColorBrush(Color.FromRgb(254, 254, 254));
            border.HorizontalAlignment = HorizontalAlignment.Left;
            TextBlock item = new TextBlock();
            item.Text = i;
            item.TextWrapping = TextWrapping.Wrap;
            item.Margin = new Thickness(4);
            //item.Foreground = new SolidColorBrush(Colors.White);
            border.Child = item;
            if (!insert)
            {
                chatContent.Items.Add(border);
            }
            else
            {
                chatContent.Items.Insert(0, border);
            }
        }

        private void sentMessage(String i, bool insert)
        {
            Border border = new Border();
            double bleft = 6;
            double left = 36, top = 2, right = 8, bottom = 0;
            border.Margin = new Thickness(left, top, right, bottom);
            border.CornerRadius = new CornerRadius(bleft);
            border.BorderThickness = new Thickness(0);
            border.BorderBrush = new SolidColorBrush(Color.FromRgb(78, 105, 162));
            border.Background = new SolidColorBrush(Color.FromRgb(78, 105, 162));
            border.HorizontalAlignment = HorizontalAlignment.Right;

            TextBlock item = new TextBlock();
            item.Text = i;
            item.TextWrapping = TextWrapping.Wrap;
            item.Margin = new Thickness(5, 4, 4, 4);
            item.Foreground = new SolidColorBrush(Colors.White);
            border.Child = item;
            
            if (!insert)
            {
                chatContent.Items.Add(border);
            }
            else
            {
                chatContent.Items.Insert(0, border);
            }

            //Grid.SetRow(border, 0);
        }


        private void chatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (chatInput.Text.Trim() != "")
                {
                    sentMessage(chatInput.Text, false);
                    scroller.ScrollToBottom();
                    SomethingHappened(chatInput.Text, windowID, false, 0, 0);
                    chatInput.Text = "";
                }
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            int oldposition = Convert.ToInt32(this.Left - desktopWorkingArea.Left);
            this.Close();
            SomethingHappened("", windowID, true, 0, oldposition);
            
        }

        private void sendButton_Click(object sender, RoutedEventArgs e)
        {
            if (chatInput.Text.Trim() != "")
            {
                sentMessage(chatInput.Text, false);
                scroller.ScrollToBottom();
                SomethingHappened(chatInput.Text, windowID, false, 0, 0);
                chatInput.Text = "";
            }
        }

        private void scroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            Console.WriteLine("scrolling");
            var scrollViewer = (ScrollViewer)sender;
            if (scrollViewer.VerticalOffset == 0)
            {
                getMsgHistory(msgback);
                scrollViewer.LineDown();
            }
        }


        private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            double oldTop = this.Top;
            int oldposition = Convert.ToInt32(this.Left - desktopWorkingArea.Left);
            if (e.ChangedButton == MouseButton.Left) { 
            this.DragMove();
                this.Top = oldTop;
                SomethingHappened(null, windowID, false, Convert.ToInt32(this.Left - desktopWorkingArea.Left), oldposition);
            }


        }

    }

}
