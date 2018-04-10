using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace Transfar
{
    public class Server
    {
        UdpClient udpClient;

        private const string tfString = "Transfar";
        private const int udpPort = 51000;
        private List<NamedIPEndPoint> availableClients;


        public Server()
        {
            availableClients = new List<NamedIPEndPoint>();

            udpClient = new UdpClient(51000);
            udpClient.EnableBroadcast = true;
            //udpClient.JoinMulticastGroup(IPAddress.Parse("239.255.42.99"));
        }


        public void ResetAvailableClients()
        {
            // This is done so that if I restart the search the list is reset
            availableClients.Clear();
        }


        public void Dispose()
        {
            udpClient.Dispose();
        }


        /*
         * Discovers hosts that are in public mode (that can accept file transfers).
         */
        public NamedIPEndPoint ClientDiscovery()
        {
            Debug.WriteLine("[SERVER] Searching hosts...");

            if (udpClient.Available > 0)
            {
                IPEndPoint clientEp = new IPEndPoint(0, 0); // Initializing an "empty" IPEndPoint
                
                var clientRequestData = udpClient.Receive(ref clientEp);
                var clientRequest = Encoding.ASCII.GetString(clientRequestData);

                if (clientRequest.Contains(tfString)) // If I received the broadcast packet containing the tfString
                {
                    string[] announcement = clientRequest.Split('_');
                    clientEp.Port = Convert.ToInt32(announcement[2]); // Replacing the endpoint port with the one received into the UDP payload
                    NamedIPEndPoint namedClientEp = new NamedIPEndPoint(announcement[1], clientEp);
                    if (!availableClients.Contains(namedClientEp))
                    {
                        availableClients.Add(namedClientEp); // Client added to the list of available clients
                        return namedClientEp;
                    }
                }
            }

            return null;
        }


        public FileTransferData StartSending(string filePath, NamedIPEndPoint selectedClient)
        {
            FileTransferData fileTransferData = new FileTransferData();
            fileTransferData.HostName = selectedClient.Name;

            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect(selectedClient.EndPoint); // Connecting to the client specified (throws an exception if not available)

            if (Directory.Exists(filePath))
            {
                string tempPath = Path.GetTempPath() + new DirectoryInfo(filePath).Name + ".zip";
                if(File.Exists(tempPath)) // Check if the file already exists so that ZipFile doesn't throw an exception
                    File.Delete(tempPath);
                ZipFile.CreateFromDirectory(filePath, tempPath);
                filePath = tempPath;
            }
            FileInfo fi = new FileInfo(filePath); // Obtaining the infos of the specified file
            fileTransferData.Name = fi.Name;
            fileTransferData.Length = fi.Length;
            Debug.WriteLine("[SERVER] File length of the sent file: " + fileTransferData.Length);
            Debug.WriteLine("[SERVER] File name of the sent file: " + fileTransferData.Name);

            fileTransferData.NetworkStream = tcpClient.GetStream();
            fileTransferData.NetworkStream.WriteTimeout = 20000;

            byte[] hostNameLengthBuffer = BitConverter.GetBytes(Encoding.Unicode.GetByteCount(fileTransferData.HostName));
            fileTransferData.NetworkStream.Write(hostNameLengthBuffer, 0, hostNameLengthBuffer.Length);

            byte[] hostNameBuffer = Encoding.Unicode.GetBytes(fileTransferData.HostName);
            fileTransferData.NetworkStream.Write(hostNameBuffer, 0, hostNameBuffer.Length);


            byte[] fileNameLengthBuffer = BitConverter.GetBytes(Encoding.Unicode.GetByteCount(fileTransferData.Name));
            fileTransferData.NetworkStream.Write(fileNameLengthBuffer, 0, fileNameLengthBuffer.Length);

            byte[] fileNameBuffer = Encoding.Unicode.GetBytes(fileTransferData.Name);
            fileTransferData.NetworkStream.Write(fileNameBuffer, 0, fileNameBuffer.Length);


            byte[] fileLengthBuffer = BitConverter.GetBytes(fileTransferData.Length);
            fileTransferData.NetworkStream.Write(fileLengthBuffer, 0, fileLengthBuffer.Length);
            fileTransferData.FileStream = File.OpenRead(filePath);

            Debug.WriteLine("[SERVER] Initial file data sent successfully");

            return fileTransferData;
        }


        /*
         * Sends a chunk of data (to be in a while loop)
         */
        public void Send(FileTransferData fileTransferData)
        {
            var buffer = new byte[256 * 1024];
            int bytesRead;
            if (((bytesRead = fileTransferData.FileStream.Read(buffer, 0, buffer.Length)) > 0) && (fileTransferData.Length > 0))
            {
                fileTransferData.NetworkStream.Write(buffer, 0, bytesRead);
                fileTransferData.Length -= bytesRead;
            }
        }


        /*
         * Ends gracefully the file transfer.
         */
        public void EndSending(FileTransferData fileTransferData)
        {
            fileTransferData.NetworkStream.Flush();
            fileTransferData.NetworkStream.Dispose();
            fileTransferData.FileStream.Dispose();
        }


        /*
         * Stops the file transfer.
         */
        public void CancelSending(FileTransferData fileTransferData)
        {
            fileTransferData.NetworkStream.Dispose();
            fileTransferData.FileStream.Dispose();
        }


        /*
         * Old function for command line usage
         */
        /*
        //Funzione che permette la scelta dell'host a cui inviare il file ed invia il file.
        public void SendFile(string fileNamePath)
        {
            //Tramite una finestra della GUI dovrei selezionare il file che vorrei inviare
            using (TcpClient tcpClient = new TcpClient()) //Apro la socket TCP
            {
                //Questa parte dovrebbe essere realizzata con la GUI
                System.Debug.WriteLine("[SERVER] Select client to which send file");
                int i = 0;
                IPEndPoint selectedClient;
                foreach (var client in availableClients)
                    System.Debug.WriteLine("(" + i++ + "): " + client.ToString()); //[i]: 192.168.1.1 
                var selectedIndex = Convert.ToInt32(System.Console.ReadLine()); //Seleziono il client al quale voglio inviare il file
                selectedClient = availableClients.ElementAt(selectedIndex);
                //

                tcpClient.Connect(selectedClient); //Mi connetto al relativo client (lancia un'eccezione se non disponibile)

                FileInfo fi = new FileInfo(fileNamePath); //Ottengo informazioni sul file specificato
                long fileLength = fi.Length;
                string fileName = fi.Name;
                Debug.WriteLine("[SERVER] File length of the sent file: " + fileLength);
                Debug.WriteLine("[SERVER] File name of the sent file: " + fileName);

                using (NetworkStream netStream = tcpClient.GetStream())
                {
                    byte[] fileNameLengthBuffer = BitConverter.GetBytes(Encoding.Unicode.GetByteCount(fileName));
                    netStream.Write(fileNameLengthBuffer, 0, fileNameLengthBuffer.Length);

                    byte[] fileLengthBuffer = BitConverter.GetBytes(fileLength);
                    netStream.Write(fileLengthBuffer, 0, fileLengthBuffer.Length);

                    byte[] fileNameBuffer = Encoding.Unicode.GetBytes(fileName);
                    netStream.Write(fileNameBuffer, 0, fileNameBuffer.Length);

                    using (FileStream fileStream = File.OpenRead(fileNamePath))
                        fileStream.CopyTo(netStream);

                    Debug.WriteLine("[SERVER] File sent successfully");
                }
            }
        }
        */
    }
}