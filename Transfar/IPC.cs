using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transfar
{
    class IPC
    {
        // This function must be executed by the first instance of Transfar
        public static void Server()
        {
            var server = new NamedPipeServerStream("TransfarContextualMenuHandler");
            server.WaitForConnection();
            StreamReader reader = new StreamReader(server);
            int i = 0;
            while (true)
            {
                // TODO: To check that
                string line = reader.ReadLine();
                if (i++ < 4)
                    Console.WriteLine(line);
            }
        }

        public static void Client(string[] args)
        {
            var client = new NamedPipeClientStream("TransfarContextualMenuHandler");
            client.Connect();
            StreamWriter writer = new StreamWriter(client);

            args.ToList().ForEach(writer.WriteLine); // Every string is sent individually
            writer.Flush();
        }
    }
}
