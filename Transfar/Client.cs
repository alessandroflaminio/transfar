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
        private IPEndPoint multicastEndpoint;
        TcpListener tcpListener;

        private const string tfString = "Transfar"; //Stringa da inviare in broadcast
        private static string multicastAddress = "239.255.42.99";
        private const int udpPort = 51000; //Porta di ascolto server UDP

        public String path; // TODO check that //"C://Users//Riccardo//Desktop//Test"; //Path di ricezione file che andrebbe specificato tramite GUI
        public static int tcpPort = 50000; //Porta di di ascolto client TCP
        //Vorrei poter settare queste cose via software e dunque riavviare il thread di segnalazione della presenza

        public Client()
        {
            path = Properties.Settings.Default.Path;
            if (path == "") // That's the case of the default path which is encoded in the .config file as an empty string
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads\\Transfar";
            }

            udpClient = new UdpClient();
            announcementBytes = Encoding.ASCII.GetBytes(tfString + '_' +  Environment.UserName + '_' + tcpPort);
            multicastEndpoint = new IPEndPoint(IPAddress.Parse(multicastAddress), udpPort);
        }

        public void Dispose()
        {
            udpClient.Dispose();
            tcpListener.Stop();
        }

        /*
         * Funzione che avvia l'ascolto di eventuali connessioni da parte di un server che
         * vorrebbe inviare un file.
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
         * Funzione per stoppare la ricezione
         */
        public void StopListening()
        {
            if (tcpListener != null)
            {
                tcpListener.Stop();
            }
        }

        /*
         * Funzione che comunica la presenza ad altri host ogni 100 ms.
         * Tramite GUI bisognerebbe attivare un thread che esegue questa funzione.
         * Bisognerebbe uscire dal while premendo un pulsante da GUI.
         */
        public void Announce()
        {
            //udpClient.JoinMulticastGroup(IPAddress.Parse(multicastAddress)); Devo solo inviare nel gruppo multicast
            Console.WriteLine("[CLIENT] Sending broadcast datagrams...");
            
            udpClient.Send(announcementBytes, announcementBytes.Length, multicastEndpoint);
            //Thread.Sleep(50);
        }

        /*
         * Metodo che si occupa di ascoltare le richieste di invio file.
         * Ad ogni nuova richiesta crea un nuovo thread che si occupa della vera e propria ricezione.
         * Bisognerebbe uscire dal while a causa dello stesso evento che interrompe la Broadcast()
         */
        public TcpClient ListenRequests()
        {
            Console.WriteLine("[CLIENT] Waiting for incoming file transfers...");
            TcpClient client = tcpListener.AcceptTcpClient(); //Si blocca fino a che non riceve nuove richieste
            return client;

            //Thread receiverThread = new Thread(() => ReceiveFile(client)); //Lambda function per passare il parametro al thread
            //receiverThread.Start();
            //while (!receiverThread.IsAlive);
            ////Bisogna vedere la questione del joining del thread

            //tcpListener.Stop();
        }

        //public void ReceiveFile(TcpClient client)
        //{
        //    Console.WriteLine("[CLIENT] Receiving new file...");

        //    using (NetworkStream netStream = client.GetStream())
        //    {
        //        byte[] fileNameLengthBuffer = new byte[sizeof(int)];
        //        netStream.Read(fileNameLengthBuffer, 0, fileNameLengthBuffer.Length);
        //        int fileNameLength = BitConverter.ToInt32(fileNameLengthBuffer, 0);

        //        byte[] fileLengthBuffer = new byte[sizeof(long)];
        //        netStream.Read(fileLengthBuffer, 0, fileLengthBuffer.Length);

        //        byte[] fileNameBuffer = new byte[fileNameLength];
        //        netStream.Read(fileNameBuffer, 0, fileNameBuffer.Length);

        //        long fileLength = BitConverter.ToInt64(fileLengthBuffer, 0);
        //        string fileName = Encoding.Unicode.GetString(fileNameBuffer);

        //        DirectoryInfo di = Directory.CreateDirectory(path); //Crea la directory specificata dal path se non già esistente
        //        using (FileStream fileStream = File.Create(path + "//" + fileName)) //Utilizzo la direttiva using per rilasciare automaticamente le risorse alla fine del blocco
        //        {
        //            var buffer = new byte[256 * 1024];
        //            int bytesRead;
        //            while ((bytesRead = netStream.Read(buffer, 0, buffer.Length)) > 0)
        //            {
        //                fileStream.Write(buffer, 0, bytesRead);
        //            }

        //            fileStream.Flush();
        //        }

        //        Console.WriteLine("[CLIENT] Received length: " + fileLength);
        //        Console.WriteLine("[CLIENT] Received file name: " + fileName);
        //    }
        //}

        public FileTransferData StartReceiving(TcpClient client)
        {
            FileTransferData fileTransferData = new FileTransferData();
            NetworkStream netStream = client.GetStream();
            netStream.ReadTimeout = 5; // TODO: Not working, find another way to realize that the sender has stopped

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

            DirectoryInfo di = Directory.CreateDirectory(path);

            fileTransferData.HostName = hostName;
            fileTransferData.Name = fileName;
            fileTransferData.Path = path + "//" + fileName;
            fileTransferData.Length = fileLength;
            fileTransferData.NetworkStream = netStream;
            fileTransferData.FileStream = null; //  File.Create(fileTransferData.Path); that's because the management is done on the GUI
            return fileTransferData;
        }

        // To be in a while loop
        public void Receive(FileTransferData fileTransferData)
        {
            var buffer = new byte[256 * 1024];
            int bytesRead;
            if (((bytesRead = fileTransferData.NetworkStream.Read(buffer, 0, buffer.Length)) > 0) && (fileTransferData.Length > 0))
            {
                fileTransferData.FileStream.Write(buffer, 0, bytesRead);
                fileTransferData.Length -= bytesRead;
            }
        }

        public void EndReceiving(FileTransferData fileTransferData)
        {
            fileTransferData.FileStream.Flush();
            fileTransferData.NetworkStream.Dispose();
            fileTransferData.FileStream.Dispose();
        }

        public void CancelReceiving(FileTransferData fileTransferData)
        {
            fileTransferData.NetworkStream.Dispose();
            fileTransferData.FileStream.Dispose();
            File.Delete(fileTransferData.Path);
        }
    }
}