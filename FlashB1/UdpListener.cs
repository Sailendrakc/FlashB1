using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace FlashB1
{
    public class UdpListener: IBaseChannel<UdpDataCard>
    {
        private int portno; //official
        private SocketObj WorkingSocket = null;
        private BasePooling<UdpArgObj> recPool;
        private BasePooling<UdpArgObj> sendPool;
        private UdpDataCard setting;

        public UdpListener(UdpDataCard _setting)
        {
            setting = _setting;
            WorkingSocket = new SocketObj(setting.port, false);
            recPool = new BasePooling<UdpArgObj>(setting.initReceiveObjects, setting.maxReceiveObjects, GenerateRecObject);
            sendPool = new BasePooling<UdpArgObj>(setting.initSendObjects, setting.maxSendObjects, GenerateSendObject);

        }

        public override void StartListener()
        {
            StartRecevingData(setting);
        }

        public IPEndPoint GetLocalIPEP()
        {
            return (IPEndPoint)WorkingSocket.LocalEndPoint;
        }

        internal override void StartRecevingData(UdpDataCard setting)
        {
            var s = recPool.GetObjectFromPool();
            if (s == null || !WorkingSocket.isFunctional) { return; }
            if (s.beingUsed)
            {
                throw new Exception("The arg object is already in use, [UdpListener.StartRecevingData()].");
            }
            s.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                bool isPending = WorkingSocket.ReceiveFromAsync(s);
                s.beingUsed = isPending;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Fatal Error: Cannot Receive [UdpListner.StartRecevingData()] : {e.ToString()}");
                WorkingSocket.isFunctional = false;
                return;
            }
            if (!s.beingUsed) { rec_Completed(null, s);}
            return;
        }

        internal void rec_Completed(object sender, SocketAsyncEventArgs e)
        {
            //get new object and start Listening.
            StartRecevingData(setting);

            //Process the object that contains data.
            if (e.SocketError != SocketError.Success || e.BytesTransferred == 0)
            {
                recPool.SetObjectIntoPool((UdpArgObj)e);
                return;
            }

            RecInvoke(null, (UdpArgObj)e);
        }

        public override void StartSendingData(byte[] data, IPEndPoint destination)
        {
            if (WorkingSocket.isFunctional)
            {
                var s = sendPool.GetObjectFromPool();
                s.RemoteEndPoint = destination;
                s.SetBuffer(data, 0, data.Length);

                try
                {
                    bool pending = WorkingSocket.SendToAsync(s);
                    s.beingUsed = pending;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error sending data [internalSocket.startsendingdata] :" + e.ToString());
                    WorkingSocket.isFunctional = false;
                    return;
                }

                if (!s.beingUsed) { send_Completed(null, s); }
                return;
            }
            else
            {
                Console.WriteLine("Unable to send data, non functional socket [internalSocket.startsendingdata].");
            }
        }

        internal void send_Completed(object sender, SocketAsyncEventArgs e)
        {
            //recycle object
            sendPool.SetObjectIntoPool((UdpArgObj)e);
        }

        internal UdpArgObj GenerateRecObject()
        {
            var obj = new UdpArgObj(recPool, false, setting.BufferSize);
            obj.Completed += rec_Completed;
            return obj;
        }

        internal UdpArgObj GenerateSendObject()
        {
            var obj = new UdpArgObj(sendPool, true, 0);
            obj.Completed += send_Completed;
            return obj;
        }

        public override void CloseListener()
        {
            WorkingSocket.Shutdown(SocketShutdown.Both);
            WorkingSocket.Close(1000);
            WorkingSocket.isFunctional = false;
            Console.WriteLine("The socket has been closed now");
        }
    }

    public class UdpDataCard
    {
        //for socket
        public ushort port = 48748;

        //for socket arg
        public int maxReceiveObjects = 15;
        public int maxSendObjects = 10;
        public int initReceiveObjects = 15;
        public int initSendObjects = 10;

        public int BufferSize = 500;

        public UdpDataCard(ushort? portno, int maxRecObject, int maxSendObj, int recBuffer)
        {
            this.port = portno ?? 0;
            this.maxReceiveObjects = maxRecObject;
            this.maxSendObjects = maxSendObj;
            this.BufferSize = recBuffer;
        }
    }

    public class UdpArgObj: SocketAsyncEventArgs, IArgObject
    {
        private readonly BasePooling<UdpArgObj> pool;

        private byte[] buffer;
        public bool beingUsed { get; set; } = false;

        public UdpArgObj(BasePooling<UdpArgObj> token, bool forSending, int recSize)
        {
            pool = token;
            if (!forSending)
            {
                if(recSize < 10)
                {
                    throw new Exception("The receving buffer size is too small.");
                }

                buffer = new byte[recSize];
                SetBuffer(buffer, 0, buffer.Length);
            }
        }

        public IPEndPoint GetClient()
        {
            return (IPEndPoint)base.RemoteEndPoint;
        }

        public byte[] GetRawData()
        {
            return base.Buffer;
        }

        public int getBytesTransferred()
        {
            return base.BytesTransferred;
        }

        public void ReturnToPool()
        {
            pool.SetObjectIntoPool(this);
        }
    }

    internal class SocketObj : Socket
    {
        public IPEndPoint localIPEP;
        private AddressFamily addr_Fam = AddressFamily.InterNetwork;
        public bool isFunctional = true;

        public SocketObj(int DefPortNo, bool hasipv6)
            : base(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
        {
            localIPEP =  new IPEndPoint(GetIPv4.GetIpV4(), 0);
            localIPEP.Port = DefPortNo;
            localIPEP.Address = GetIPv4.GetIpV4();

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
    }
}
