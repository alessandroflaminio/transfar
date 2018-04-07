using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;

namespace Transfar
{
    class IPCServer
    {
        private NamedPipeServerStream server;

        public IPCServer()
        {
            PipeSecurity pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(new PipeAccessRule("Everyone", PipeAccessRights.ReadWrite, AccessControlType.Allow));
            server = new NamedPipeServerStream("TransfarContextualMenuHandler", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message, PipeOptions.None, 512, 512, pipeSecurity);
        }

        // This function must be executed by the first instance of Transfar
        public string Server()
        {
            server.WaitForConnection();
            StreamReader reader = new StreamReader(server);

            // I just read the file path
            string received = reader.ReadLine();
            Debug.WriteLine("[SERVER] Received IPC string: " + received);

            server.Close();
            return received;
        }
    }
}