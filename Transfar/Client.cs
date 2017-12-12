using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Transfar
{
    public class Client
    {
        private const string tfString = "TransferFileCS port "; //Stringa da inviare in broadcast
        private static string multicastAddress = "239.255.42.99";
        private const int udpPort = 51000; //Porta di ascolto server UDP

        public static string path = "C://Users//Alessandro Flaminio//Desktop//Test"; //Path di ricezione file che andrebbe specificato tramite GUI
        public static int tcpPort = 50000; //Porta di di ascolto client TCP
        //Vorrei poter settare queste cose via software e dunque riavviare il thread di segnalazione della presenza

        //Funzione per ottenere tutti gli indirizzi IP assegnati al PC (non utilizzata).
        public static List<string> GetLocalIPAddress()
        {
            List<string> myIps = new List<string>();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    myIps.Add(ip.ToString());
                }

            }
            if (!myIps.Any())
                throw new Exception("Local IP Address Not Found!");
            else return myIps;
        }

        /*
         * Funzione che comunica la presenza ad altri host ogni 100 ms.
         * Tramite GUI bisognerebbe attivare un thread che esegue questa funzione.
         * Bisognerebbe uscire dal while premendo un pulsante da GUI.
         */
        public static void Announce()
        {
            using (UdpClient udpClient = new UdpClient())
            {
                //udpClient.JoinMulticastGroup(IPAddress.Parse(multicastAddress)); Devo solo inviare nel gruppo multicast
                Byte[] sendBytes = Encoding.ASCII.GetBytes(tfString + tcpPort);

                Console.WriteLine("[CLIENT] Sending broadcast datagrams...");
                while (true)
                {
                    IPEndPoint multicastEndpoint = new IPEndPoint(IPAddress.Parse(multicastAddress), udpPort);
                    udpClient.Send(sendBytes, sendBytes.Length, multicastEndpoint);
                    Thread.Sleep(50);
                }
            }
        }

        /*
         * Metodo che si occupa di ascoltare le richieste di invio file.
         * Ad ogni nuova richiesta crea un nuovo thread che si occupa della vera e propria ricezione.
         * Bisognerebbe uscire dal while a causa dello stesso evento che interrompe la Broadcast()
         */
        public static void ListenRequests()
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Any, tcpPort);
            tcpListener.Start();

            while (true)
            {
                Console.WriteLine("[CLIENT] Waiting for incoming file transfers...");
                TcpClient client = tcpListener.AcceptTcpClient(); //Si blocca fino a che non riceve nuove richieste

                Thread receiverThread = new Thread(() => ReceiveFile(client)); //Lambda function per passare il parametro al thread
                receiverThread.Start();
                while (!receiverThread.IsAlive);
                //Bisogna vedere la questione del joining del thread
            }

            tcpListener.Stop();
        }

        private static void ReceiveFile(TcpClient client)
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

                DirectoryInfo di = Directory.CreateDirectory(path); //Crea la directory specificata dal path se non già esistente
                using (FileStream fileStream = File.Create(path + "//" + fileName)) //Utilizzo la direttiva using per rilasciare automaticamente le risorse alla fine del blocco
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
    }
}