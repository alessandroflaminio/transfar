using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace Transfar
{
    public class Server
    {
        UdpClient udpClient;

        private const string tfString = "TransferFileCS port ";
        private const int udpPort = 51000;
        private List<IPEndPoint> availableClients;

        public Server()
        {
            availableClients = new List<IPEndPoint>();

            udpClient = new UdpClient(51000);
            udpClient.JoinMulticastGroup(IPAddress.Parse("239.255.42.99"));

        }

        public void Dispose()
        {
            udpClient.Dispose();
        }

        /*
         * Metodo che si occupa della scoperta di host disponibili.
         * Il while (availableClients.Count < 1) dovrebbe essere interrotto da un pulsante presente nella GUI.
         */
        public IPEndPoint ClientDiscovery()
        {
            Console.WriteLine("[SERVER] Searching hosts...");

            if (udpClient.Available > 0)
            {
                IPEndPoint clientEp = new IPEndPoint(0, 0); //Inizializzo un oggetto "vuoto" di tipo IPEndPoint

                var clientRequestData = udpClient.Receive(ref clientEp);
                var clientRequest = Encoding.ASCII.GetString(clientRequestData);

                if (clientRequest.Contains(tfString)) //Se ho ricevuto il pacchetto di broadcast contente la stringa tfString
                {
                    clientEp.Port = Convert.ToInt32(clientRequest.Replace(tfString, "")); //Sostituendo la porta con la porta comunicatami all'interno del payload UDP
                    if (!availableClients.Contains(clientEp))
                    {
                        availableClients.Add(clientEp); //Aggiungo il client alla lista dei client disponibili
                        return clientEp;
                    }
                }
            }

            return null;

            //Console.WriteLine("[SERVER] Received IP addresses:");
            //foreach (var x in availableClients)
            //    Console.WriteLine(x.ToString());
        }

        public FileTransferData StartSending(string filePath, IPEndPoint selectedClient)
        {
            FileTransferData fileTransferData = new FileTransferData();

            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect(selectedClient); //Mi connetto al relativo client (lancia un'eccezione se non disponibile)

            FileInfo fi = new FileInfo(filePath); //Ottengo informazioni sul file specificato
            fileTransferData.Name = fi.Name;
            fileTransferData.Length = fi.Length;
            Console.WriteLine("[SERVER] File length of the sent file: " + fileTransferData.Length);
            Console.WriteLine("[SERVER] File name of the sent file: " + fileTransferData.Name);

            fileTransferData.NetworkStream = tcpClient.GetStream();

            byte[] fileNameLengthBuffer = BitConverter.GetBytes(Encoding.Unicode.GetByteCount(fileTransferData.Name));
            fileTransferData.NetworkStream.Write(fileNameLengthBuffer, 0, fileNameLengthBuffer.Length);

            byte[] fileLengthBuffer = BitConverter.GetBytes(fileTransferData.Length);
            fileTransferData.NetworkStream.Write(fileLengthBuffer, 0, fileLengthBuffer.Length);

            byte[] fileNameBuffer = Encoding.Unicode.GetBytes(fileTransferData.Name);
            fileTransferData.NetworkStream.Write(fileNameBuffer, 0, fileNameBuffer.Length);

            //using (FileStream fileStream = File.OpenRead(filePath))
            //    fileStream.CopyTo(netStream);
            
            // TODO: zip a folder
            fileTransferData.FileStream = File.OpenRead(filePath);

            Console.WriteLine("[SERVER] Initial file data sent successfully");

            return fileTransferData;
        }

        // To be in a while loop
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

        public void EndSending(FileTransferData fileTransferData)
        {
            fileTransferData.NetworkStream.Flush();
            fileTransferData.NetworkStream.Dispose();
            fileTransferData.FileStream.Dispose();
        }

        public void CancelSending(FileTransferData fileTransferData)
        {
            fileTransferData.NetworkStream.Dispose();
            fileTransferData.FileStream.Dispose();
        }

        //Funzione che permette la scelta dell'host a cui inviare il file ed invia il file.
        public void SendFile(string fileNamePath)
        {
            //Tramite una finestra della GUI dovrei selezionare il file che vorrei inviare
            using (TcpClient tcpClient = new TcpClient()) //Apro la socket TCP
            {
                //Questa parte dovrebbe essere realizzata con la GUI
                System.Console.WriteLine("[SERVER] Select client to which send file");
                int i = 0;
                IPEndPoint selectedClient;
                foreach (var client in availableClients)
                    System.Console.WriteLine("(" + i++ + "): " + client.ToString()); //[i]: 192.168.1.1 
                var selectedIndex = Convert.ToInt32(System.Console.ReadLine()); //Seleziono il client al quale voglio inviare il file
                selectedClient = availableClients.ElementAt(selectedIndex);
                //

                tcpClient.Connect(selectedClient); //Mi connetto al relativo client (lancia un'eccezione se non disponibile)

                FileInfo fi = new FileInfo(fileNamePath); //Ottengo informazioni sul file specificato
                long fileLength = fi.Length;
                string fileName = fi.Name;
                Console.WriteLine("[SERVER] File length of the sent file: " + fileLength);
                Console.WriteLine("[SERVER] File name of the sent file: " + fileName);

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

                    Console.WriteLine("[SERVER] File sent successfully");
                }
            }
        }
    }
}