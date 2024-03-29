﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

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

        private long timestamp;
        private int updateEstimation;
        private double oldValue;

        private CancellationTokenSource cts;

        // For hiding the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);


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

                this.Show();
                this.Activate();
                Yes_Button_Click(null, null);
            }
            else
            {
                fileInfo.Text = "Do you want to receive file " + fileTransferData.Name + " from " + fileTransferData.HostName + '?';
                this.Show();
                this.Activate();
            }
        }


        private void ReceivingFileWindowLoaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }


        private async void Yes_Button_Click(object sender, RoutedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible;
            cancelButton.Visibility = Visibility.Visible;
            yesButton.Visibility = Visibility.Hidden;
            noButton.Visibility = Visibility.Hidden;
            fileInfo.Text = "Receiving file " + fileTransferData.Name + " from " + fileTransferData.HostName + "...";

            if (!Properties.Settings.Default.SetPath) // if the path must be chosen each time the user receives a file
            {
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dialog.Description = "Please choose the folder in which save the file.";
                    System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                    if (dialog.SelectedPath != "") // TODO: Sometimes the default path is already set to Desktop, probably that's an issue of the FolderBrowser that is open in another thread (happens only when self-transferring)
                    {
                        client.Path = dialog.SelectedPath;
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


        private void Cancel_Button_Click(object sender, RoutedEventArgs e) => cts.Cancel();


        private void ReportProgress(double value)
        {
            if (updateEstimation == 0)
            {
                double diffValue = value - oldValue;
                oldValue = value;

                long nowTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // if i took diff seconds for transferring the diffValue, then I can perform an estimation of the remaining time
                long diffTime = nowTime - timestamp;
                timestamp = nowTime;

                TimeSpan remainingTime = TimeSpan.FromMilliseconds(Math.Ceiling((diffTime * (100 - value)) / diffValue));
                if (remainingTime.TotalSeconds > 0)
                    remainingTimeBlock.Text = "Remaining time: " + remainingTime.Minutes + " minutes and " + remainingTime.Seconds + " seconds";
            }
            updateEstimation = (updateEstimation + 1) % 15; // this is done for preventing inconsistent updates of the estimated time

            progressBar.Value = value;
        }

        private async Task ReceiveFileAsync(IProgress<double> progressIndicator, CancellationToken token)
        {
            await Task.Run(async () => // async put so that the exception is thrown to the caller
            {
                try
                {
                    token.ThrowIfCancellationRequested();

                    updateEstimation = 1;
                    oldValue = 0;
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // initializing the timestamp at the beginning of the transfer

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
                catch (Exception e)
                {
                    if (e is SocketException || e is IOException)
                    {
                        client.CancelReceiving(fileTransferData);

                        MessageBox.Show("There was an error in receiving the file.", "Transfar", MessageBoxButton.OK,
                            MessageBoxImage.Stop, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                        throw new OperationCanceledException();
                    }

                    throw;
                }
            }, token);
        }
    }
}