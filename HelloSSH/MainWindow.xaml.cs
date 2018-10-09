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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using HelloSSH.Services;

namespace HelloSSH
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SshAgent _sshAgent;

        public MainWindow()
        {
            InitializeComponent();

            Activated += MainWindow_Activated;

            Deactivated += MainWindow_Deactivated;
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            _sshAgent = new SshAgent();
            //var test = new TestHelloService();

            //test.ParentHWind = new WindowInteropHelper(GetWindow(this)).Handle;

            //var input = Encoding.UTF8.GetBytes("This is a test string");

            //var data = test.Encrypt(input);

            //System.Diagnostics.Debug.WriteLine($"encrypted data = {Convert.ToString(data)}");

            //var output = test.PromptToDecrypt(data, "Decrypt test data");

            //System.Diagnostics.Debug.WriteLine($"Output data = {Encoding.UTF8.GetString(output)}");
        }

        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            _sshAgent.Dispose();
        }
    }
}
