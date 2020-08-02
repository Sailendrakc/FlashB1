using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace FlashB1
{

    internal class SocketObj : Socket
    {
        public IPEndPoint localIPEP = new IPEndPoint(GetIpV4(), 0);
        private AddressFamily addr_Fam = AddressFamily.InterNetwork;
        public bool isFunctional = true;

        public SocketObj(int DefPortNo, bool hasipv6)
            : base(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
        {
            localIPEP.Port = DefPortNo;
            localIPEP.Address = GetIpV4();

            if (localIPEP.Address == null)
            {
                return;
            }

            try
            {
                base.Bind(localIPEP);
                localIPEP = (IPEndPoint)base.LocalEndPoint;
                Console.WriteLine($"Binded in: {localIPEP.Address} : {localIPEP.Port} [SocketObj()]");
            }
            catch (Exception e)
            {
                isFunctional = false;
                Console.WriteLine("Fatal Binding error: " + e.ToString());
                return;
            }
        }

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
