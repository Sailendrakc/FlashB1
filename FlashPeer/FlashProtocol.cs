using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using FlashB1;
using System.Timers;

namespace FlashPeer
{
    public class FlashProtocol
    {
        public static FlashProtocol Instance;
        private UdpDataCard socketConfig;
        internal UdpListener channel;
        public PacketDeserializer Pockets = new PacketDeserializer();
        public PacketSerializer Pmaker = new PacketSerializer();
        public ConnectionManager ConnectionPlugin = new ConnectionManager();
        internal Crypto crypto;

        //public event EventHandler<RecArgs> DataReceiveEvent;
        public event EventHandler<EventColl> OtherEvents;
        public event EventHandler<string> DisconnectEvent; // pass Ip of disconnected peer.

        private Timer DeadTimer = new Timer();
        private int DeadInterval = 30; //sec
        private Timer AliveTimer = new Timer();
        private int AliveInterval = 30;//sec

        public bool isServer = false;

        public Dictionary<string, FlashPeer> Connections { get; set; } = new Dictionary<string, FlashPeer>();
        public Dictionary<string, HSfromServer> pendingClients { get; set; } = new Dictionary<string, HSfromServer>();
        public Dictionary<string, HSfromClient> pendingServers { get; set; } = new Dictionary<string, HSfromClient>();

        public int Max_Peers;
        public int Current_Peers;

        public FlashProtocol(bool isserver, int? port, string key)
        {
            if(port == null)
            {
                port = 5124;
            }
            socketConfig = new UdpDataCard((ushort)port, 150, 150, 512);
            isServer = isserver;
            Max_Peers = 1000;
            Current_Peers = 0;
            channel = new UdpListener(socketConfig);
            channel.RecCompleteEvent += Channel_RecCompleteEvent;
            crypto = new Crypto(key, isserver);
            if (isServer)
            {
                //timer
                DeadTimer.Interval = (DeadInterval + 2) * 1000;
                DeadTimer.AutoReset = true;
                DeadTimer.Elapsed -= DeadTimer_Elapsed;
                DeadTimer.Elapsed += DeadTimer_Elapsed;
            }
            else
            {

                AliveTimer.Interval = AliveInterval * 1000;
                AliveTimer.AutoReset = true;
                /*AliveTimer.Elapsed -= AliveTimer_Elapsed;
                AliveTimer.Elapsed += AliveTimer_Elapsed;*/
            }
            Instance = this;
        }

       /* private void AliveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(Current_Peers > 0)
            {
                foreach (var item in Connections)
                {
                    SendAck(item.Value);
                }
            }
        }*/

        private void Channel_RecCompleteEvent(object sender, IArgObject e)
        {
            var pf = new PacketFilter(e);
            var res = pf.CheckData();
            if(res== null)
            {
                return;
            }
            res.peer.RecData(res);
        }

        //listeners events and timers
        public void StartPeer()
        {
            if(channel != null)
            {
                channel.StartListener();

                //also start the keep alive timer
                if (isServer)
                {
                    DeadTimer.Start();
                }
                else
                {
                    AliveTimer.Start();
                }
                RaiseOtherEvent("Started Listening.", null, EventType.ConsoleMessage, null);
            }
        }

        public void StopListening()
        {
            
        }

        private void DeadTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            KillOfflineClient(61);
        }

        private void KillOfflineClient(int timeS)
        {
            RaiseOtherEvent($"Refreshing peer list.", null, EventType.ConsoleMessage, null);
            DateTime d = DateTime.UtcNow;
            List<string> dated = new List<string>();
            double diff = 0;
            foreach (var item in Instance.Connections)
            {
                diff = (d - item.Value.lastDateTime).TotalSeconds;
                if (diff > timeS)
                {
                    item.Value.connected = false;
                    dated.Add(item.Key);
                }
            }

            foreach (var item in dated)
            {
                Console.WriteLine($"Last time: {Instance.Connections[item].lastDateTime.ToString("MM/dd/yyyy hh:mm:ss.fff")} , Now time: {d.ToString("MM/dd/yyyy hh:mm:ss.fff")} and diff is: {diff}");
                Instance.RemovePeer(item, true);
            }
        }

        //functions
        internal void RemovePeer(string IP, bool fromHandshakes)
        {
            if (fromHandshakes)
            {
                if (pendingClients.TryGetValue(IP, out var handshake))
                {
                    //p.connected = false;
                    pendingClients.Remove(IP);
                }
                else
                {
                    pendingServers.Remove(IP);
                }
            }
            else
            {
                lock (Connections)
                {
                    if (Connections.TryGetValue(IP, out var peer))
                    {
                        //p.connected = false;
                        Connections.Remove(IP);
                        Current_Peers--;
                        Console.WriteLine(IP.ToString() + " removed");
                    }
                }
            }

        }

        internal bool AddPeerFromPending(string ip, bool fromPendingClients)
        {
            if (Connections.TryGetValue(ip, out var theclient))
            {
                RemovePeer(ip, false);
                return false;
            }

            if (fromPendingClients)
            {
                if (pendingClients.TryGetValue(ip, out var theshake))
                {
                    lock (pendingClients)
                    {
                        Connections.Add(ip, theshake.Ipeer);
                        Current_Peers++;
                    }
                    RemovePeer(ip, true);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (pendingServers.TryGetValue(ip, out var theshake))
                {
                    lock (pendingServers)
                    {
                        Connections.Add(ip, theshake.Speer);
                        Current_Peers++;
                    }
                    RemovePeer(ip, true);

                    return true;
                }
                else
                {
                    return false;
                }
            }

        }

        public void StartHello(IPEndPoint ep)
        {
            FlashPeer target = new FlashPeer(ep);
            var sh = new HSfromClient(target);
        }

        internal Header ReadPacket(IArgObject packet)
        {
            Header ii = new Header();
            var toread = packet.GetRawData();

            ii.SentTimes = toread[0];
            ii.PacketNo = BitConverter.ToUInt16(toread, 1);
            ii.ExpRecivingNo = BitConverter.ToUInt16(toread, 3);


            if (ii.SentTimes > 0)
            {
                //reliable //ackfields
                Array.Copy(toread, 5, ii.AckFields, 0, 3);
                ii.isReliable = true;
            }
            else
            {
                //unreliable //time
                ii.DateForUDP = 0; //TODOD IT DEPENDS ON HOW I PACK
                ii.isReliable = false;
            }

            var noOfPicklets = 1;
            var len = packet.GetRawData().Length;

            var counter = PacketSerializer.MinLenOfPacket;
            while (counter < len)
            {
                var lenofcurrent = BitConverter.ToUInt16(packet.GetRawData(), counter);
                if (counter + lenofcurrent < len)
                {
                    noOfPicklets++;
                    counter += lenofcurrent;
                }
            }


            ii.AllPicklets = new Pockets[noOfPicklets];

            counter = 0;
            for (int i = PacketSerializer.MinLenOfPacket; i < len;)
            {
                var lenofcurrent = BitConverter.ToUInt16(packet.GetRawData(), i);
                var pic = new Pockets(packet.GetRawData(), i, lenofcurrent, packet.GetRawData()[i + 2]);
                ii.AllPicklets[counter] = pic;
                counter++;
                i += lenofcurrent;
            }

            return ii;
        }

        /*public void SendAck(FlashPeer p)
        {
            //prepare ack packet.
            byte[] dat = new byte[PacketSerializer.MinLenOfPacket + 3];
            Array.Copy((BitConverter.GetBytes((ushort)Opfunctions.ack)), 0, dat, PacketSerializer.POS_OF_OPCODE, 2);
            Array.Copy((BitConverter.GetBytes((ushort)3)), 0, dat, PacketSerializer.POS_OF_LEN, 2);
            Array.Copy(p.GetAckFieldForSending(24), 0, dat, PacketSerializer.PayloadSTR, 3);

            if (Pmaker.HeadWriter(ref dat, false, p))
            {
                p.SendData(dat);
            }
        }
        */

        //crypto methods
        public byte[] GetAESKey()
        {
            return crypto.GetAESKey();
        }

        public byte[] GetAESIV()
        {
            return crypto.GetAESIV();
        }

        public byte[] RSAEncrypt(ref byte[] data)
        {
            if (data.Length > 245)
            {
                RaiseOtherEvent("RSA cannot encrypt more than 245 bytes", null, EventType.cryptography, null);
                return null;
            }

            return crypto.RSAEncrypt(ref data);
        }

        public byte[] RSADecrypt(ref byte[] data)
        {
            return crypto.RSADecrypt(data);
        }

        public byte[] AESEcryptBytes(ref byte[] data)
        {
            return crypto.AESEncryptBytes(ref data);
        }

        public byte[] AESDecryptBytes(ref byte[] data)
        {
            return crypto.AESDecryptBytes(ref data);
        }

        public byte[] AESEcryptText(string Text)
        {
            return crypto.AESEncryptText(Text);
        }

        public string AESDecryptText(ref byte[] data)
        {
            return crypto.AESDecryptText(ref data);
        }

        //events

        public void RaiseOtherEvent(string msg, string stacktrace, EventType et, object[] o)
        {
            EventColl ev = new EventColl();
            ev.Message = msg;
            ev.Stacktrace = stacktrace;
            ev.Eventtype = et;
            ev.obj = o;

            OnOtherEvent(ev);

        }

        /// <summary>
        /// It is called when other events are needed to be raised.
        /// </summary>
        /// <param name="EventDetail"></param>
        private static void OnOtherEvent(EventColl EventDetail)
        {
            if (Instance.OtherEvents != null)
            {
                Instance.OtherEvents.Invoke(null, EventDetail);
            }
        }
    }
}
