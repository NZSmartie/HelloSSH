using HelloSSH.WPF.Services;
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

namespace HelloSSH.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var test = new TestHelloService();

            test.ParentHWind = new WindowInteropHelper(GetWindow(this)).Handle;

            var input = Encoding.UTF8.GetBytes("This is a test string");

            var data = test.Encrypt(input);

            System.Diagnostics.Debug.WriteLine($"encrypted data = {Convert.ToString(data)}");

            var output = test.PromptToDecrypt(data, "Decrypt test data");

            System.Diagnostics.Debug.WriteLine($"Output data = {Encoding.UTF8.GetString(output)}");
        }
    }
}
