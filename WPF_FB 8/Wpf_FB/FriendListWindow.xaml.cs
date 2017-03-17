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

namespace Wpf_FB
{
    /// <summary>
    /// Interaction logic for FriendListWindow.xaml
    /// </summary>
    public partial class FriendListWindow : Window
    {
        private Dictionary<string, FB_UserInfo> friendList;
        public delegate void MyEventHandler(string e, string b);
        public event MyEventHandler OpenChatWindow;
        public FriendListWindow()
        {
            InitializeComponent();


            /*** Due to FB change, API v1.10 no longer able to get online user anymore ***/
            /*
            friendList = (App.Current as App).fbChat.getFriendList();
            Dispatcher.Invoke((Action)delegate ()
            {
                foreach (FB_UserInfo user in friendList.Values)
                {    //textblock1.text += "\n" + user.name;
                     //listviewitem item = new listviewitem();
                     //item.content=item;
                     //lvfriendlist.items.add(item);

                    MyData data = new MyData(user.userID, user.name, user.status);
                    ListViewItem item = new ListViewItem();
                    item.DataContext = data;
                    lvFriendList.Items.Add(data);

                }
            });
            */

            // Hard-coded firend list for demo
            Dispatcher.Invoke((Action)delegate ()
            {
                MyData data = new MyData("1254932200", "Terry Tsang", "online");
                ListViewItem item = new ListViewItem();
                item.DataContext = data;
                lvFriendList.Items.Add(data);

                data = new MyData("1358411841", "Axy David", "online");
                item = new ListViewItem();
                item.DataContext = data;
                lvFriendList.Items.Add(data);
            });
        }


        private void lvFriendList_MouseDoubleClick_1(object sender, MouseButtonEventArgs e)
        {
            MyData hi = (MyData)lvFriendList.SelectedItem;
            Console.WriteLine(hi.Id + hi.Name);
            OpenChatWindow(hi.Id, hi.Name);
            Console.WriteLine(hi.Id);        }
    }
    public class MyData
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Online { get; set; }
        public MyData(string id, string name, string online)
        {
            Id = id;
            Name = name;
            Online = online;
        }
    }


}
