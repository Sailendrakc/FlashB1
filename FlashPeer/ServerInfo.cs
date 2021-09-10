using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace FlashPeer
{
    class ServerInfo
    {
        private IPEndPoint ExternalIPEP1 = null;
        public int Port1 = 0;

        public ServerInfo(string serverIPep)
        {
           // ExternalIPEP1 = FClient.IPEPparser(serverIPep);
            this.Port1 = ExternalIPEP1.Port;
        }

        public IPEndPoint getServerIPEP()
        {
            return ExternalIPEP1;
        }
    }
}
