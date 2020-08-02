
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System;

namespace FlashB1
{
    public class internalClient
    {
        private InternalSockets localSocket;
        public ArgPool Poool;
        public bool isClient = true;

        public internalClient(socketDataCard DataCard)
        {
            this.isClient = DataCard.isclient;
            Poool = new ArgPool(DataCard.maxAcceptObjects, DataCard.maxReceiveObjects, DataCard.BufferSize);
            localSocket = new InternalSockets(isClient, DataCard.port, Poool);
        }

        /// <summary>
        /// Sends datas
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="bindedIPEP">local socket to send from</param>
        /// <returns>false means error in sending</returns>
        public bool SendAsync(SocketArgObj arg)
        {
            return localSocket.SendAsync(arg);
        }

        public bool isNetRunning()
        {
            return localSocket.isFunctional();
        }

        public SocketArgObj GetSockObject(bool forSending)
        {
            return localSocket.GetSocketObject(forSending);
        }

        public void CloseClient()
        {
            localSocket.EraseLocalSockets();
        }
    }

    public class socketDataCard
    {
        //for socket
        public ushort port = 48748;
        public bool isclient = true;

        //for socket arg
        public ushort maxReceiveObjects = 15;
        public ushort maxAcceptObjects = 10;

        public ushort BufferSize = 500;

        public socketDataCard(ushort portno, bool isClient, ushort maxRecObject, ushort maxAccObj, ushort recBuffer)
        {
            this.port = portno;
            this.isclient = isClient;
            this.maxReceiveObjects = maxRecObject;
            this.maxAcceptObjects = maxAccObj;
            this.BufferSize = recBuffer;
        }

        /// <summary>
        /// Load with default objects.
        /// </summary>
        /// <param name="isclient"></param>
        public socketDataCard(bool isclient)
        {
            this.isclient = isclient;
            if (isclient)
            {
                port = 0;
            }
        }
    }
}
