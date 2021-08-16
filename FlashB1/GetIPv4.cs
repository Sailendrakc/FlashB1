using System.Net;
using System.Net.Sockets;

namespace FlashB1
{
    static class GetIPv4
    {
        public static IPAddress GetIpV4()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress a = null;
            foreach (var i in host.AddressList)
            {
                if (i.AddressFamily == AddressFamily.InterNetwork)
                {
                    a = i;
                    break;
                }
            }
            return a;
        }
    }
}
