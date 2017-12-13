using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
        private CancellationTokenSource cts;

        public ReceivingFileWindow()
        {
            InitializeComponent();
        }

        public ReceivingFileWindow(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
            InitializeComponent();
        }

        private async void Yes_Button_Click(object sender, RoutedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible;

            cts = new CancellationTokenSource();
            var progressIndicator = new Progress<int>(ReportProgress);

            try
            {
                await ReceiveFileAsync(progressIndicator, cts.Token);
            }
            catch (Exception)
            {
                Console.WriteLine("Cancellation requested!");
            }
        }

        private void No_Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ReportProgress(int value)
        {
            progressBar.Value = value;
        }

        private async Task ReceiveFileAsync(IProgress<int> progressIndicator, CancellationToken token)
        {
            await Task.Run(async () => // async put so that the exception is thrown to the caller
            {

                var fileTransferData = Client.StartReceiving(tcpClient);
                long originalLength = fileTransferData.Length;
                try
                {
                    token.ThrowIfCancellationRequested();

                    while (fileTransferData.Length > 0)
                    {
                        Client.Receive(fileTransferData);
                        token.ThrowIfCancellationRequested();
                        progressIndicator.Report((int) (fileTransferData.Length / originalLength) * 100);
                    }

                    Client.EndReceiving(fileTransferData);
                }
                catch (OperationCanceledException)
                {
                    Client.CancelReceiving(fileTransferData);
                    throw;
                }
            }, token);
        }


    }
}
