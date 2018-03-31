using System.IO;
using System.Net.Sockets;

namespace Transfar
{
    public class FileTransferData
    {
        public string HostName { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public long Length { get; set; }
        public NetworkStream NetworkStream { get; set; }
        public FileStream FileStream { get; set; }
    }
}
