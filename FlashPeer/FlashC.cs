using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Timers;
using FlashB1;
using System.Threading;
using System.Collections;

namespace FlashPeer
{
    public static class FClient
    {
        public static bool isClient = false;

        internal static Dictionary<string, FlashPeeer> Connections = new Dictionary<string, FlashPeeer>();
        private static internalClient localClient = null;

        private static ServerInfo Serverinfo = null;

        public static event EventHandler<RecArgs> DataReceiveEvent;
        public static event EventHandler<EventColl> OtherEvents;
        public static event EventHandler<string> DisconnectEvent; // pass Ip of disconnected peer.

        private static System.Timers.Timer KeepAliveOrDeadTimer = new System.Timers.Timer();
        private static int AliveOrDeadInterval = 30; //sec

        internal static int Max_Client;
        internal static int Live_Client;

        public static PacketManager Pmanager = new PacketManager();
        internal static Crypto crypto;

        #region Schedule DataTimeout
        public static int TCPtimeout = 1140;
        public static int RTTms = 256;
        internal static readonly int TCPtimeout2 = 400;
        internal static readonly int TCPtimeout3 = RTTms;
        #endregion

        internal static readonly byte WinSize = 24; // 3 bytes

        #region Schedule Ack Timeout
        public static int AckTimeout = TCPtimeout - RTTms;
        public static int AckTimeout2 = RTTms;
        #endregion


        //public static string tempKeys;

        public static void FlushCache()
        {
            Connections.Clear();
            if (Serverinfo != null)
            {
                Serverinfo = null;
            }

        }

       

        /// <summary>
        /// Starts client instance with auto deteted IP and port. Prioritzes IPv6 addresses.
        /// </summary>
        public static FlashPeeer StartClient(bool isClientt, string ipofPeer, string SuitableKey, int max_peer)
        {
            //remove all the catche first
            FlushCache();

            isClient = isClientt;

            if (SuitableKey == null)
            {
                RaiseOtherEvent("Key is null. Cannot start instance without it.", null, EventType.NullError, null);
                return null;
            }

            if (ipofPeer == null && isClient)
            {
                RaiseOtherEvent("ip of server is not given in 66.", null, EventType.NullError, null);
                return null;
            }

            crypto = new Crypto(SuitableKey);

            IOevents.RecCompleteEvent -= S1CB1;
            IOevents.RecCompleteEvent += S1CB1;

            if (!isClient)
            {
                //server
                socketDataCard card = new socketDataCard(false);
                localClient = new internalClient(card); //port is 48748

                if (max_peer > 2000)
                {
                    max_peer = 2000;
                    RaiseOtherEvent("Max peer cannot exceed 2000 but program continues.", null, EventType.ConsoleMessage, null);
                }

                Max_Client = max_peer;

                KeepAliveOrDeadTimer = new System.Timers.Timer();

                KeepAliveOrDeadTimer.Interval = (AliveOrDeadInterval + 2) * 1000;
                KeepAliveOrDeadTimer.AutoReset = true;
                KeepAliveOrDeadTimer.Elapsed -= DeadTimer_Elapsed;
                KeepAliveOrDeadTimer.Elapsed += DeadTimer_Elapsed;
                KeepAliveOrDeadTimer.Start();
                return null;
            }
            else
            {
                //client
                socketDataCard card = new socketDataCard(true);
                localClient = new internalClient(card);

                try
                {

                    IPEndPoint ipp = IPEPparser(ipofPeer);
                }
                catch (Exception)
                {
                    RaiseOtherEvent("Serveer ipep format is incorrect.Terminating.", null, EventType.ValueError, null);
                    return null;
                }

                FlashPeeer p = DefServerPeerMaker(ipofPeer);

                TestHelloSend();

                //reset counts

                KeepAliveOrDeadTimer = new System.Timers.Timer();
                KeepAliveOrDeadTimer.Interval = AliveOrDeadInterval * 1000;
                KeepAliveOrDeadTimer.Elapsed -= AliveTimer_Elapsed;
                KeepAliveOrDeadTimer.Elapsed += AliveTimer_Elapsed;
                KeepAliveOrDeadTimer.AutoReset = false;
                return p;
            }

        }

        private static void DeadTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            KillOfflineClient(61);
        }

        private static void AliveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //This is called every given interval in client

            RaiseOtherEvent("Keep alive pakcet sent.", null, EventType.ConsoleMessage, null);

            byte[] byt = new byte[PacketManager.Overhead];
            FlashPeeer p = Connections[Serverinfo.getServerIPEP().ToString()];

            if (!(Pmanager.HeadWriter(ref byt, (byte)Opfunctions.keepalive, p.IPEP.ToString(), false, p, 1)))
            {
                return;
            }

            //write crc
            byt[PacketManager.CRC] = Pmanager.ComputeChecksum(byt, Pmanager.Crc16table, (ushort)byt.Length);

            //send it
            SendDataUnreliable(byt, Serverinfo.getServerIPEP(), false);

        }

        public static  FlashPeeer getServerPeer()
        {
            if (isClient)
            {
               return Connections[Serverinfo.getServerIPEP().ToString()];
            }
            return null;
        }

        private static void KillOfflineClient(int timeS)
        {
            RaiseOtherEvent($"Refreshing peer list.", null, EventType.ConsoleMessage, null);
            DateTime d = DateTime.UtcNow;
            List<string> dated = new List<string>();
            double diff = 0;
            foreach (var item in Connections)
            {
                diff = (d - item.Value.DT).TotalSeconds;
                if (diff > timeS)
                {
                    item.Value.connected = false;
                    dated.Add(item.Key);
                }
            }

            foreach (var item in dated)
            {
                Console.WriteLine($"Last time: {Connections[item].DT.ToString("MM/dd/yyyy hh:mm:ss.fff")} , Now time: {d.ToString("MM/dd/yyyy hh:mm:ss.fff")} and diff is: {diff}");
                RemovePeer(item);
            }
        }

        public static void RaiseDisconnection(string Ip)
        {
            if (DisconnectEvent != null)
            {
                DisconnectEvent.Invoke(null, Ip);
            }
        }

        /// <summary>
        /// Test method for sending hello packet in unreliable way totarget ipep
        /// </summary>
        /// <param name="ipepServer">target ipendpoint</param>
        public static void TestHelloSend()
        {
            byte[] b = Pmanager.HelloPacketer();
            SendDataUnreliable(b, Serverinfo.getServerIPEP(), false);

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

        #region Unreleased ipv6
        /*
        public static IPAddress UnreleasedGetIpV6()
        {

            var host = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress a = null;
            foreach (var i in host.AddressList)
            {
                if (i.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    /* if (!(i.ToString().ToLower().Substring(0, 4) == "fe08"))
                     {
                         a = i;
                     }*//*
                    a = i;
                }
            }
            return a;
        }

        public static IPAddress UnreleasedGetIPv6orv4(out AddressFamily addr_Fam)
        {
            addr_Fam = AddressFamily.InterNetworkV6;
            if (GetIpV6() != null)
            {
                return GetIpV6();
            }
            else
            {
                if (GetIpV4() != null)
                {
                    addr_Fam = AddressFamily.InterNetwork;
                    return GetIpV4();
                }
                else
                {
                    RaiseOtherEvent("No ipv4 or ipv6 found.", null, EventType.NullError, null);
                    return null;
                }
            }
        }

        public static IPAddress UnreleasedGetIPv6orv4()
        {
            if (GetIpV6() != null)
            {
                return GetIpV6();
            }
            else
            {
                if (GetIpV4() != null)
                {
                    return GetIpV4();
                }
                else
                {
                    RaiseOtherEvent("No ipv4 or ipv6 found.", null, EventType.NullError, null);
                    return null;
                }
            }
        }
        */
        #endregion


        /// <summary>
        /// Parses given ipv 4 and 6 adresses
        /// </summary>
        /// <param name="IP"></param>
        /// <returns>ipep object from string(It many not be a valid IP)</returns>
        public static IPEndPoint IPEPparser(string IP)
        {
            if (IP == null)
            {
                RaiseOtherEvent("Can't parse null ipep string.", null, EventType.NullError, null);
                return null;
            }
            int i = IP.LastIndexOf(':');
            IPEndPoint ipep1 = new IPEndPoint(IPAddress.Parse(IP.Substring(0, i)), int.Parse(IP.Substring(i + 1, IP.Length - (i + 1)))); //----- string IP to IPEP
            return ipep1;
        }

        private static FlashPeeer DefServerPeerMaker(string ipep)
        {
            Serverinfo = new ServerInfo(ipep);

            FlashPeeer p = new FlashPeeer(false, IPEPparser(ipep));
            Console.WriteLine(" test --adding a peer");
            Connections.Add(ipep, p);
            return p;
        }

        /// <summary>
        /// Is is called when socket 1 (Mserver1) receives data.
        /// All other listners listen event called by this method.
        /// </summary>
        /// <param name="argData"></param>
        private static void OnDataReceiveEvent(RecArgs argData)
        {
            if (DataReceiveEvent != null)
            {
                DataReceiveEvent.Invoke(null, argData);
            }
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

        /// <summary>
        /// It should be called when  socket 1 receives data ie. Mserver1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static private void ProcessData(SocketArgObj ee)
        {
            PacketFilter pf = new PacketFilter(ee);
            RecArgs u = pf.CheckData();

            if (u == null)
            {
                RaiseOtherEvent("Outdated or Hello packet or recQ full. Null packet.", null, EventType.ConsoleMessage, null);
                return;
            }
            Console.WriteLine($"{u.Operation} received on relaible: {u.reliable} with DNO: {u.NetArgs.DNO} on {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");

            //check channel of data
            if (!u.reliable) // if data is unreliable channel
            {
                //update expectedudp ie increase expected receive number
                u.p.incExpectedUDP(u.NetArgs.DNO);

                //return if its keepalive
                if (u.Operation == (byte)Opfunctions.keepalive)
                {
                    return;
                }

                if (u.Operation == (byte)Opfunctions.ack)
                {
                    //removes the acked data from sentQ by processing remote headers 
                    byte[] ackHeaderandBody = new byte[3];
                    if (u.Transfered > PacketManager.Overhead)
                    {
                        //some ack body is there so
                        ackHeaderandBody = new byte[u.Transfered - PacketManager.Overhead];

                    }
                    Array.Copy(u.lateData, PacketManager.PayloadSTR, ackHeaderandBody, 0, ackHeaderandBody.Length);
                    u.p.ProcessAckFields(u.NetArgs.expectedTCP, ackHeaderandBody);
                    return;
                }

                //is the request of byebye?
                if (u.Operation == (byte)Opfunctions.byebye)
                {
                    if (isClient)
                    {
                        ResetConnection("BYE BYE RECEIVED");
                    }
                    else
                    {
                        //remove client
                        RemovePeer(ee.RemoteEndPoint.ToString());
                    }
                    return;
                }

                //received ping from unreliable channel
                if (u.Operation == (byte)Opfunctions.PingPong)
                {
                    bool expReply = false;
                    if (u.lateData[PacketManager.PayloadSTR] == 1)
                    {
                        //senders wants PONG via unreliable channel
                        expReply = true;
                    }
                    PingPongR(ee.RemoteEndPoint.ToString(), false, expReply);
                    return;
                }

                //received handshake data ie. reply of hello
                if (isClient)
                {
                    if (u.Operation == (byte)Opfunctions.Handshake)
                    {
                        RaiseOtherEvent($"Connected peer {ee.RemoteEndPoint.ToString()}", null, EventType.ConsoleMessage, null);
                        u.p.connected = true;
                        KeepAliveOrDeadTimer.Start();
                    }
                }

                //also check split data

                OnDataReceiveEvent(u); // some other app level unrelaible channeled data.. so pass it
                return;
            }
            else
            {

                //add data to q
                u.p.AddtoRecTCPQ(u.NetArgs.DNO, u);

                if (u.p.GetExpectedTCP() != u.NetArgs.DNO)
                {
                    //fire up schedule ack 
                    u.p.ScheduleAck(u.NetArgs.sentTimes);

                    //perform copy and return.. future data
                    Console.WriteLine($"Future data received with dno: {u.NetArgs.DNO} and expected: {u.p.GetExpectedTCP()}");
                    u.lateData = new byte[u.Transfered];
                    Array.Copy(ee.Buffer, 0, u.lateData, 0, ee.BytesTransferred);

                    //edits the local ackfields to register this packet
                    u.p.RegisterAck(u.NetArgs.DNO, u.NetArgs.diff);

                    u.p.futureTCPcount++;

                    return;

                }
                else
                {
                    RecArgs rec;
                    List<RecArgs> dnos = new List<RecArgs>();

                    while (u.p.GetFromRecTCPQ(u.p.GetExpectedTCP(), out rec))
                    {
                        //save dno to remove from peer buffer
                        dnos.Add(rec);
                        u.p.RemoveFromRecTCPQ(u.p.GetExpectedTCP());

                        //increase expected data no value.
                        u.p.incTCPexpectedONE();
                    }

                    //fire up schedule ack 
                    u.p.ScheduleAck(u.NetArgs.sentTimes);

                    foreach (var x in dnos)
                    {
                        //handle ack
                        byte[] ackHeaderandBody = new byte[3];
                        Array.Copy(u.lateData, PacketManager.ACKFIELDS, ackHeaderandBody, 0, 3);

                        u.p.ProcessAckFields(u.NetArgs.expectedTCP, ackHeaderandBody);

                        // IF DATA HAS SOME OTHER LOW LEVEL PURPOSE DO IT HERE

                        //received ping from reliable channel
                        if (x.Operation == (byte)Opfunctions.PingPong) // or other reliable channeled low level functions
                        {
                            bool expReply = false;
                            if (x.lateData[PacketManager.PayloadSTR] == 1)
                            {
                                //senders wants PONG via reliable channel
                                expReply = true;
                            }

                            PingPongR(ee.RemoteEndPoint.ToString(), true, expReply);
                            continue;
                        }

                        if (x.Operation == (byte)Opfunctions.Split)
                        {
                            //check if split all split datas are availabe or not
                            RecArgs x1 = x.p.TrigerSplit(x.lateData);
                            if (x1 == null)
                            {
                                continue;
                            }
                            else
                            {
                                OnDataReceiveEvent(x1);
                                continue;
                            }
                        }

                        OnDataReceiveEvent(x); // calling onDataReceiveEvent to notify suscribers
                    }
                }
            }

        }

        static private void S1CB1(object sender, SocketArgObj ee)
        {
            //call whatever and at last return the ee.
            ProcessData(ee);

            if (ee.GetToken().isSend)
            {
                ee.GetToken().beingUsed = false;
                localClient.Poool.SetSendObjects(ee);
            }
            else
            {
                ee.GetToken().beingUsed = false;
                localClient.Poool.SetRecObjects(ee);
            }

        }

        /// <summary>
        /// Sends data via reliable channel to receverremo
        /// </summary>
        /// <param name="data">bytes of data</param>
        /// <param name="recever">Destination of data</param>
        /// <param name="EventHandlerCB"> Not used yet leave null</param>
        public static void SendDataReliable(byte[] data, IPEndPoint recever, byte times)
        {
            FlashPeeer p;
            if (!Connections.TryGetValue(recever.ToString(), out p))
            {
                return;
            }

            SendDataUnreliable(data, p.IPEP, true);
            //RELIABLE WORKS if not ack
            p.AfterSendingTCP(ref data, times); //---to chedck,, senttimse was 0
        }

        /// <summary>
        /// data is sent via unreliable channel
        /// </summary>
        /// <param name="data"> data to send </param>
        /// <param name="recever">destination of data</param>
        /// <param name="callback">Not used right now leave null</param>
        public static void SendDataUnreliable(byte[] data, IPEndPoint recever, bool fromReliable)
        {
            if (data == null) { RaiseOtherEvent("Data to send is null.", null, EventType.SocketIOError, null); return; }
            if (recever == null) { RaiseOtherEvent("Receiver is null.", null, EventType.SocketIOError, null); return; }
            if (data.Length < PacketManager.Overhead) { Console.WriteLine("Data too small to send"); return; }

            //get object to use
            SocketArgObj socketargToUse = localClient.GetSockObject(true);

            socketargToUse.SetBuffer(data, 0, data.Length);
            socketargToUse.RemoteEndPoint = recever;

            if (!localClient.isNetRunning())
            {
                return;
            }

            //Send
            if (!localClient.SendAsync(socketargToUse))
            {
                ResetConnection("Unable to send data");
                return;
            }
            else
            {
                string s = recever.ToString();
                FlashPeeer p;
                if (!fromReliable && Connections.TryGetValue(s, out p))
                {
                    p.AfterSendingUDP();
                }

                Opfunctions op = (Opfunctions)data[PacketManager.Opcode];
                Console.WriteLine($"{op} Sent with DNO: {BitConverter.ToUInt16(data, PacketManager.DNO1)} and Crc {data[PacketManager.CRC]} at " +
                    $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");
            }

            //now update the keepalive packet
            if (isClient)
            {
                KeepAliveOrDeadTimer.Stop();
                if (!(Connections.Count == 0))
                {
                    KeepAliveOrDeadTimer.Start();
                }
            }
        }

        internal static void RemovePeer(string IP)
        {
            FlashPeeer p;

            lock (Connections)
            {
                if (Connections.TryGetValue(IP, out p))
                {
                    p.connected = false;
                    Connections.Remove(IP);
                    Live_Client--;
                    Console.WriteLine(p.IPEP + " removed");
                    RaiseDisconnection(IP);
                }
            }
        }

        //used in schedule resend function
        internal static bool ResendtoPeer(FlashPeeer p, ushort PrevdataNO, byte sentTimes)
        {
            byte[] data;

            if (!p.GetFromSentTCPQ(PrevdataNO, out data))
            {
                return false;
            }
            else
            {
                Console.WriteLine(PrevdataNO + " Data not acked, resending agian");
            }
            sentTimes += 1;

            //send   
            data[PacketManager.STR] = sentTimes;
            data[PacketManager.CRC] = Pmanager.ComputeChecksum(data, null, data.Length);

            SendDataUnreliable(data, p.IPEP, true);
            Console.WriteLine($"The senttime was : " + sentTimes);

            p.ScheduleResend(PrevdataNO, sentTimes);
            return true;
        }

        internal static void HelloReply(IPEndPoint ipep)
        {
            byte[] data = new byte[PacketManager.Overhead];
            FlashPeeer p = Connections[ipep.ToString()];
            if (!(Pmanager.HeadWriter(ref data, (byte)Opfunctions.Handshake, ipep.ToString(), false, p, 1)))
            {
                return;
            }
            data[PacketManager.CRC] = Pmanager.ComputeChecksum(data, Pmanager.Crc16table, data.Length);
            SendDataUnreliable(data, ipep, false);
        }

        public static void RaiseOtherEvent(string msg, string stacktrace, EventType et, object[] o)
        {
            EventColl ev = new EventColl();
            ev.Message = msg;
            ev.Stacktrace = stacktrace;
            ev.Eventtype = et;
            ev.obj = o;

            OnOtherEvent(ev);
        }

        public static void ByeBye(string ipep)
        {

            int RawLen = 0;
            byte[] data = new byte[PacketManager.Overhead + RawLen];
            FlashPeeer p;
            if (!Connections.TryGetValue(ipep, out p))
            {
                return;
            }

            if (!(Pmanager.HeadWriter(ref data, (byte)Opfunctions.byebye, ipep, false, p, 1)))
            {
                return;
            }

            data[PacketManager.CRC] = Pmanager.ComputeChecksum(data, Pmanager.Crc16table, data.Length);

            SendDataUnreliable(data, IPEPparser(ipep), false);

            p.connected = false;
            p.RemoveFromSentTCPQ(0); // 0 meaning clear the senttcpq
        }

        private static void PingPongR(string ipep, bool fromReliable, bool WantsPONG)
        {
            if (WantsPONG)
            {
                SendPingPong(ipep, null, fromReliable, false);
            }

            /*string s = $"Received PingPong from {ipep} from {fromReliable} reliability channel.";
            RaiseOtherEvent(s, null, EventType.PingPong, null);*/
        }

        public static void SendPingPong(string ipep, string msg, bool inReliable, bool expectPONG)
        {
            string msgg;
            if (msg != null && msg.Length >= 8)
            {
                msgg = msg.Substring(0, 8);
            }
            else
            {
                msgg = "PINGPONG";
            }

            FlashPeeer p;
            if (!Connections.TryGetValue(ipep, out p))
            {
                RaiseOtherEvent("ERROR: Cannot ping unlisted peer.", null, EventType.PingPong, null);
                return;
            }
            else
            {
                if (!p.connected)
                {
                    RaiseOtherEvent("ERROR: Cannot ping unconnected peer.", null, EventType.PingPong, null);
                    return;
                }
            }

            if (inReliable)
            {
                SendDataReliable(Pmanager.PreparePingPacket(ipep, inReliable, expectPONG, msgg), IPEPparser(ipep), 1);
            }
            else
            {
                SendDataUnreliable(Pmanager.PreparePingPacket(ipep, inReliable, expectPONG, msgg), IPEPparser(ipep), false);
            }
        }

        public static void ResetConnection(string msg)
        {
            KeepAliveOrDeadTimer.Stop();
            localClient.CloseClient();

            Connections.Clear();

            if (isClient)
            {
                KeepAliveOrDeadTimer.Elapsed -= AliveTimer_Elapsed;
                RaiseOtherEvent($"Connection reset Client (STOP): {msg}", null, EventType.ConnectionReset, null);
            }
            else
            {
                KeepAliveOrDeadTimer.Elapsed -= DeadTimer_Elapsed;
                RaiseOtherEvent($"Connection reset Server (STOP) FATAL: {msg}", null, EventType.ConnectionReset, null);
            }
        }

        // crypto methods

        public static byte[] GetAESKey()
        {
            return crypto.GetAESKey();
        }

        public static byte[] GetAESIV()
        {
            return crypto.GetAESIV();
        }

        public static byte[] RSAEncrypt(ref byte[] data)
        {
            if (data.Length > 245)
            {
                FClient.RaiseOtherEvent("RSA cannot encrypt more than 245 bytes", null, EventType.cryptography, null);
                return null;
            }


            return crypto.RSAEncrypt(ref data);
        }

        public static byte[] RSADecrypt(ref byte[] data)
        {
            if (isClient)
            {
                EventColl ev = new EventColl();
                ev.Message = "Client cannot decrypt anything.";
                ev.Eventtype = EventType.cryptography;

                OnOtherEvent(ev);

                return null;

            }

            return crypto.RSADecrypt(data);
        }

        public static byte[] AESEcryptBytes(ref byte[] data)
        {
            return crypto.AESEncryptBytes(ref data);
        }

        public static byte[] AESDecryptBytes(ref byte[] data)
        {
            return crypto.AESDecryptBytes(ref data);
        }

        public static byte[] AESEcryptText(string Text)
        {
            return crypto.AESEncryptText(Text);
        }

        public static string AESDecryptText(ref byte[] data)
        {
            return crypto.AESDecryptText(ref data);
        }

        private static SocketAsyncEventArgs GetSendSocketArg(ref byte[] dataTosend, ref IPEndPoint remotepoint)
        {
            SocketAsyncEventArgs s = new SocketAsyncEventArgs();
            s.SetBuffer(dataTosend, 0, dataTosend.Length);
            s.RemoteEndPoint = remotepoint;

            return s;
        }

        public static bool getisNetrunning()
        {
            return localClient.isNetRunning();
        }

        public static void StopClient(string message)
        {
            ResetConnection(message);
        }

        public static void CloseServer() //for test purpose only
        {
            KeepAliveOrDeadTimer.Elapsed -= DeadTimer_Elapsed;
            KeepAliveOrDeadTimer.Stop();
            KeepAliveOrDeadTimer.Dispose();
        }

    }

    public class RecArgs
    {
        public bool reliable = false;
        public byte Operation;
        public byte[] lateData;
        public FlashPeeer p;
        public string date;
        public ushort Transfered;

        public lowData NetArgs = new lowData();
    }

    public class lowData
    {
        public ushort DNO =0; // tcps sent
        public ushort expectedTCP =0; // expected tcp
        public byte diff =0;
        public byte sentTimes=0;
    }

    public class DataPockets
    {
        private byte[] fullPayload;
        private int startIndex;
        private int lenpocket;
        public byte OperationCode;

        public DataPockets(ref byte[] data, int stIndex)
        {
            fullPayload = data;
            startIndex = stIndex;

            lenpocket = BitConverter.ToUInt16(data, stIndex + 1);
            OperationCode = data[stIndex];
        }

        public string getUTFfrom(int localStartIndex, int len)
        {
            if(lenpocket < len + localStartIndex)
            {
                //error.
                return null;
            }
            else
            {
                return Encoding.UTF8.GetString(fullPayload, startIndex + localStartIndex, len);
            }
        }

        public int getLenPocket()
        {
            return lenpocket;
        }
    }
}
//C:\Users\saile\source\repos\FlashPeer\FlashPeer\bin\Debug\FlashPeer.dll