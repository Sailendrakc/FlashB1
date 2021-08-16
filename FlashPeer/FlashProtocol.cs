using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using FlashB1;
using FlashPeer.interfaces;

namespace FlashPeer
{
    internal class FlashProtocol
    {
        public static FlashProtocol Instance;
        private UdpDataCard socketConfig;
        internal UdpListener channel;
        public PacketDeserializer Pockets = new PacketDeserializer();
        public PacketSerializer Pmaker = new PacketSerializer();
        internal Crypto crypto;

        public static event EventHandler<EventColl> OtherEvents;


        public Dictionary<string, IFlashPeer> Connections { get; set; }
        public Dictionary<string, IFlashPeer> Connectings { get; set; }

        public int Max_Peers;
        public int Current_Peers;

        public FlashProtocol()
        {
            socketConfig = new UdpDataCard(5124, 150, 150, 512);
            channel = new UdpListener(socketConfig);
            channel.RecCompleteEvent += Channel_RecCompleteEvent;

            Instance = this;
        }

        private void Channel_RecCompleteEvent(object sender, IArgObject e)
        {
            IdentifyRequest(ProcessExistingClientRequest, ProcessNewClientRequest, e);
        }

        public void DeSerializeAndMapRequest(Action<IInternalRequest> onInternalRequsest, 
            Action<IExternalRequest> onExternalRequest, IArgObject actualRequest)
        {
            throw new NotImplementedException();
        }

        public void IdentifyRequest(Action<IFlashPeer, IArgObject> ExistingClient, 
            Action<IArgObject> NewRequest, IArgObject actualRequest)
        {
            var pf = new PacketFilter(actualRequest);
            var res = pf.CheckData();

            if(!(Connections.TryGetValue(actualRequest.GetClient().ToString(), out var client))){
                //client does not exists
                NewRequest(actualRequest);
            }
            else
            {
                //client exists
                ExistingClient(client, actualRequest);
            }
        }

        public void ProcessHelloRequest(IArgObject helloRequest)
        {
            throw new NotImplementedException();
        }

        private void ProcessExistingClientRequest(IFlashPeer client, IArgObject requestData)
        {

        }

        private void ProcessNewClientRequest(IArgObject requestData)
        {
            //first deserialize

        }



        //functions
        internal void RemovePeer(string IP, bool fromConnected)
        {
            IFlashPeer p;
            if (fromConnected)
            {
                lock (Connections)
                {
                    if (Connections.TryGetValue(IP, out p))
                    {
                        //p.connected = false;
                        Connections.Remove(IP);
                        Current_Peers--;
                        Console.WriteLine(IP.ToString() + " removed");
                    }
                }
            }
            else
            {
                if (Connectings.TryGetValue(IP, out p))
                {
                    //p.connected = false;
                    Connectings.Remove(IP);
                }
            }

        }

        internal bool AddClientFromConnectings(string ip)
        {
            if(Connections.TryGetValue(ip, out var theclient) || (!Connectings.TryGetValue(ip, out theclient)))
            {
                RemovePeer(ip, true);
                return false;
            }
            else
            {
                lock (Connections)
                {
                    Connections.Add(ip, theclient);
                    Current_Peers++;
                }

                RemovePeer(ip, false);
                return true;
            }
        }

        public void StartHello(IPEndPoint ep)
        {
            FlashPeer target = new FlashPeer(ep);
            var sh = new ClientHandshake(target);
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

            var counter = PacketManager.Overhead;
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
            for (int i = PacketManager.Overhead; i < len;)
            {
                var lenofcurrent = BitConverter.ToUInt16(packet.GetRawData(), i);
                var pic = new Pockets(packet.GetRawData(), i, lenofcurrent, packet.GetRawData()[i + 2]);
                ii.AllPicklets[counter] = pic;
                counter++;
                i += lenofcurrent;
            }

            return ii;
        }


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
                FClient.RaiseOtherEvent("RSA cannot encrypt more than 245 bytes", null, EventType.cryptography, null);
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

        public static void RaiseOtherEvent(string msg, string stacktrace, EventType et, object[] o)
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
            if (OtherEvents != null)
            {
                OtherEvents.Invoke(null, EventDetail);
            }
        }
    }
}
