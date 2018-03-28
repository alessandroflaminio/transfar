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
