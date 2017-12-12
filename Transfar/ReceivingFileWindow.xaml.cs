using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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

namespace Transfar
{
    /// <summary>
    /// Logica di interazione per ReceivingFileWindow.xaml
    /// </summary>
    public partial class ReceivingFileWindow : Window
    {
        private TcpClient tcpClient;

        public ReceivingFileWindow()
        {
            InitializeComponent();
        }

        public ReceivingFileWindow(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
            InitializeComponent();
        }

        private void Yes_Button_Click(object sender, RoutedEventArgs e)
        {
            Client.ReceiveFile(tcpClient);
        }

        private void No_Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
