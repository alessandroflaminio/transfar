using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Transfar
{
    public class Client
    {
        private UdpClient udpClient;
        private Byte[] announcementBytes;
        //private IPEndPoint multicastEndpoint;
        private IPEndPoint broadcastEndpoint;
        TcpListener tcpListener;

        private const string tfString = "Transfar"; // String broadcasted to detect Transfar packets
        //private static string multicastAddress = "239.255.42.99";
        private const int udpPort = 51000; // Listening UDP port (server)

        public String Path { get; set; }
        public static int tcpPort = 50000; // Listening TCP port (client)


        public Client()
        {
            Path = Properties.Settings.Default.Path;
            if (Path == "") // That's the case of the default Path which is encoded in the .config file as an empty string
            {
                Path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads\\Transfar";
            }

            udpClient = new UdpClient();
            announcementBytes = Encoding.ASCII.GetBytes(tfString + '_' +  Environment.UserName + '_' + tcpPort);

            udpClient.EnableBroadcast = true; // HACK: problems with multicast
            broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, udpPort);
            //multicastEndpoint = new IPEndPoint(IPAddress.Parse(multicastAddress), udpPort);
        }


        public void Dispose()
        {
            udpClient.Dispose();
            tcpListener.Stop();
        }


        /*
         * Starts the listening of connections from hosts that want to send a file.
         */
        public void StartListening()
        {
            if (tcpListener == null) // Singleton: must only be initialized once
            {
                tcpListener = new TcpListener(IPAddress.Any, tcpPort);
            }

            tcpListener.Start();
        }


        /*
         * Stops the listening of connections.
         */
        public void StopListening()
        {
            if (tcpListener != null)
            {
                tcpListener.Stop();
            }
        }


        /*
         * Sends a broadcast packet for discovery (must be put inside a loop for keeping the discovery active).
         */
        public void Announce()
        {
            Console.WriteLine("[CLIENT] Sending broadcast datagrams...");
            
            udpClient.Send(announcementBytes, announcementBytes.Length, broadcastEndpoint);
        }


        /*
         * Listens for file transfer requests, it returns the object representing the 
         */
        public TcpClient ListenRequests()
        {
            Console.WriteLine("[CLIENT] Waiting for incoming file transfers...");
            TcpClient client = tcpListener.AcceptTcpClient(); // Blocking until a new request is received
            return client;
        }
        

        /*
         * Starts receiving the first metadata of the file (host name, file name and file size).
         */
        public FileTransferData StartReceiving(TcpClient client)
        {
            FileTransferData fileTransferData = new FileTransferData();
            NetworkStream netStream = client.GetStream();
            //netStream.ReadTimeout = 5000; // TODO: Not working, find another way to realize that the sender has stopped

            Console.WriteLine("[CLIENT] Receiving new file...");

            byte[] hostNameLengthBuffer = new byte[sizeof(int)];
            netStream.Read(hostNameLengthBuffer, 0, hostNameLengthBuffer.Length);
            int hostNameLength = BitConverter.ToInt32(hostNameLengthBuffer, 0);

            byte[] hostNameBuffer = new byte[hostNameLength];
            netStream.Read(hostNameBuffer, 0, hostNameBuffer.Length);


            byte[] fileNameLengthBuffer = new byte[sizeof(int)];
            netStream.Read(fileNameLengthBuffer, 0, fileNameLengthBuffer.Length);
            int fileNameLength = BitConverter.ToInt32(fileNameLengthBuffer, 0);

            byte[] fileNameBuffer = new byte[fileNameLength];
            netStream.Read(fileNameBuffer, 0, fileNameBuffer.Length);


            byte[] fileLengthBuffer = new byte[sizeof(long)];
            netStream.Read(fileLengthBuffer, 0, fileLengthBuffer.Length);

            
            string hostName = Encoding.Unicode.GetString(hostNameBuffer);
            string fileName = Encoding.Unicode.GetString(fileNameBuffer);
            long fileLength = BitConverter.ToInt64(fileLengthBuffer, 0);
            Console.WriteLine("[CLIENT] Received host name: " + hostName);
            Console.WriteLine("[CLIENT] Received file name: " + fileName);
            Console.WriteLine("[CLIENT] Received length: " + fileLength);

            DirectoryInfo di = Directory.CreateDirectory(Path);

            fileTransferData.HostName = hostName;
            fileTransferData.Name = fileName;
            fileTransferData.Path = Path + "//" + fileName;
            fileTransferData.Length = fileLength;
            fileTransferData.NetworkStream = netStream;
            fileTransferData.FileStream = null; //  File.Create(fileTransferData.Path); that's because the management is done on the GUI
            return fileTransferData;
        }


        /*
         * Receive a chunk of data (to be in a while loop)
         */
        public void Receive(FileTransferData fileTransferData)
        {
            //fileTransferData.NetworkStream.ReadTimeout = 5000; // HACK: not working

            var buffer = new byte[256 * 1024];
            int bytesRead;
            if (((bytesRead = fileTransferData.NetworkStream.Read(buffer, 0, buffer.Length)) > 0) && (fileTransferData.Length > 0))
            {
                fileTransferData.FileStream.Write(buffer, 0, bytesRead);
                fileTransferData.Length -= bytesRead;
            }
            else
                throw new SocketException(1);
        }


        /*
         * Ends gracefully the file reception.
         */
        public void EndReceiving(FileTransferData fileTransferData)
        {
            fileTransferData.FileStream.Flush();
            fileTransferData.NetworkStream.Dispose();
            fileTransferData.FileStream.Dispose();
        }


        /*
         * Stops the file reception and deletes the file stored. 
         */
        public void CancelReceiving(FileTransferData fileTransferData)
        {
            fileTransferData.NetworkStream.Dispose();
            fileTransferData.FileStream?.Dispose();
            File.Delete(fileTransferData.Path);
        }
        

        /*
         * Old function for command line usage
         */
        /*
        public void ReceiveFile(TcpClient client)
        {
            Console.WriteLine("[CLIENT] Receiving new file...");

            using (NetworkStream netStream = client.GetStream())
            {
                byte[] fileNameLengthBuffer = new byte[sizeof(int)];
                netStream.Read(fileNameLengthBuffer, 0, fileNameLengthBuffer.Length);
                int fileNameLength = BitConverter.ToInt32(fileNameLengthBuffer, 0);

                byte[] fileLengthBuffer = new byte[sizeof(long)];
                netStream.Read(fileLengthBuffer, 0, fileLengthBuffer.Length);

                byte[] fileNameBuffer = new byte[fileNameLength];
                netStream.Read(fileNameBuffer, 0, fileNameBuffer.Length);

                long fileLength = BitConverter.ToInt64(fileLengthBuffer, 0);
                string fileName = Encoding.Unicode.GetString(fileNameBuffer);

                DirectoryInfo di = Directory.CreateDirectory(Path); //Crea la directory specificata dal Path se non già esistente
                using (FileStream fileStream = File.Create(Path + "//" + fileName)) //Utilizzo la direttiva using per rilasciare automaticamente le risorse alla fine del blocco
                {
                    var buffer = new byte[256 * 1024];
                    int bytesRead;
                    while ((bytesRead = netStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                    }

                    fileStream.Flush();
                }

                Console.WriteLine("[CLIENT] Received length: " + fileLength);
                Console.WriteLine("[CLIENT] Received file name: " + fileName);
            }
        }
        */
    }
}