using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Transfar
{
    /// <summary>
    /// Logica di interazione per ReceivingFileWindow.xaml
    /// </summary>
    public partial class ReceivingFileWindow : Window
    {
        private Client client;
        private TcpClient tcpClient;
        private long originalLength;
        private FileTransferData fileTransferData;

        private CancellationTokenSource cts;


        public ReceivingFileWindow(MainWindow mainWindow, Client client, TcpClient tcpClient)
        {
            this.client = client;
            this.tcpClient = tcpClient;
            fileTransferData = client.StartReceiving(tcpClient);
            
            InitializeComponent();

            if (Properties.Settings.Default.AutoAccept) // if auto-accepting files is enabled
            {
                mainWindow.Ni.BalloonTipTitle = "Transfar";
                mainWindow.Ni.BalloonTipText = "Receiving file " + fileTransferData.Name + " from " + fileTransferData.HostName;
                mainWindow.Ni.ShowBalloonTip(3000);

                Yes_Button_Click(this, new RoutedEventArgs()); // HACK: to check that
            }
            else
            {
                fileInfo.Text = "Do you want to receive file " + fileTransferData.Name + " from " + fileTransferData.HostName + '?';
                this.Show();
                this.Activate();
            }
        }

        private async void Yes_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!Properties.Settings.Default.SetPath) // if the path must be chosen each time the user receives a file
            {
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dialog.Description = "Please choose the folder in which save the file.";
                    System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                    if (dialog.SelectedPath != "") // TODO: Sometimes the default path is already set to Desktop, probably that's an issue of the FolderBrowser that is open in another thread
                    {
                        client.Path = dialog.SelectedPath; // TODO: CHECK THAT, PROBABLY AN ISSUE (no)
                        fileTransferData.Path = dialog.SelectedPath + "//" + fileTransferData.Name;
                    }
                    else // cancel the transfer
                    {
                        this.Close();
                        return;
                    }
                    Debug.WriteLine(dialog.SelectedPath + " chosen for receiving file.");
                }
            }

            if (File.Exists(fileTransferData.Path) && !Properties.Settings.Default.AutoReplace) // if autoreplace is disabled
            {
                if (MessageBox.Show(fileTransferData.Name + " already exists. Do you want to replace it?", "Transfar",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                { // no substitution
                    string appendedTimestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string oldName = fileTransferData.Name;
                    string oldNameWithoutExtension = Path.GetFileNameWithoutExtension(oldName);
                    fileTransferData.Name = oldName.Replace(oldNameWithoutExtension, oldNameWithoutExtension + appendedTimestamp);
                    fileTransferData.Path = fileTransferData.Path.Replace(oldName, fileTransferData.Name);

                    Debug.WriteLine("Name: " + fileTransferData.Name + " Path: " + fileTransferData.Path);
                }
            }

            fileTransferData.FileStream = File.Create(fileTransferData.Path);
            originalLength = fileTransferData.Length;

            progressBar.Visibility = Visibility.Visible;
            cancelButton.Visibility = Visibility.Visible;
            yesButton.Visibility = Visibility.Hidden;
            noButton.Visibility = Visibility.Hidden;
            fileInfo.Text = "Receiving file " + fileTransferData.Name + " from " + fileTransferData.HostName + "...";

            cts = new CancellationTokenSource();
            var progressIndicator = new Progress<double>(ReportProgress);

            try
            {
                await ReceiveFileAsync(progressIndicator, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Cancellation requested!");
            }

            this.Close();
        }

        private void No_Button_Click(object sender, RoutedEventArgs e)
        {
            client.CancelReceiving(fileTransferData);
            tcpClient.Dispose();
            this.Close();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
        }

        private void ReportProgress(double value)
        {
            progressBar.Value = value;
        }

        private async Task ReceiveFileAsync(IProgress<double> progressIndicator, CancellationToken token)
        {
            await Task.Run(async () => // async put so that the exception is thrown to the caller
            {
                try
                {
                    token.ThrowIfCancellationRequested();

                    while (fileTransferData.Length > 0)
                    {
                        client.Receive(fileTransferData);
                        token.ThrowIfCancellationRequested();

                        //Thread.Sleep(500); // HACK: Waiting for testing purposes

                        progressIndicator.Report(100 - ((float) fileTransferData.Length / originalLength * 100));
                    }

                    client.EndReceiving(fileTransferData);
                }
                catch (OperationCanceledException)
                {
                    client.CancelReceiving(fileTransferData);
                    throw;
                }
                catch (SocketException)
                {
                    client.CancelReceiving(fileTransferData);

                    MessageBox.Show("There was an error in receiving the file.", "Transfar", MessageBoxButton.OK,
                        MessageBoxImage.Stop, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    throw new OperationCanceledException();
                }
            }, token);
        }


    }
}
