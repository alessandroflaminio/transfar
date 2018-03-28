using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
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

                //server = new NamedPipeServerStream("TransfarContextualMenuHandler", PipeDirection.Out, NamedPipeServerStream.MaxAllowedServerInstances);
        }

        // This function must be executed by the first instance of Transfar
        public List<String> Server()
        {
            List<String> args = new List<String>();
            server.WaitForConnection();
            StreamReader reader = new StreamReader(server);

            int i = 0;
            do
            {
                String received = reader.ReadLine();
                args.Add(received);
                Console.WriteLine("[SERVER] Received IPC string: " + args.Last());
            } while (i++ < 3);//String.IsNullOrWhiteSpace(args.Last()));


            server.Close();
            return args;
        }
    }
}