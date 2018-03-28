using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transfar
{
    class IPCClient
    {
        public static void Client(string arg)
        {
            var client = new NamedPipeClientStream("TransfarContextualMenuHandler");
            client.Connect();
            StreamWriter writer = new StreamWriter(client);

            writer.WriteLine(arg); // Every string is sent individually
            writer.Flush();
        }
    }
}
