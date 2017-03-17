using EdgeJs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Wpf_FB
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static MainWindow main;

        public fbAPI fbChat = new fbAPI();

        /* The entry point of the WPF app */
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // create the main window and assign your datacontext
            main = new MainWindow();
            //main2 = new MainWindow();
            main.Show();
            //main2.Show();
            
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            MessageBox.Show("App has exited ");
        }
    }
}
