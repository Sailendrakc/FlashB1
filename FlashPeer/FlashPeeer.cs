using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using FlashB1;
using System.Timers;
using System.Collections;
using FlashPeer.interfaces;

namespace FlashPeer
{
    public class FlashPeeer:IFlashPeer
    {

        public IPEndPoint IPEP = null;
        public bool connected = false;

        ushort upperb = PacketManager.UppB;

        private ushort TCPsent = 1;
        private ushort ExpectedTCP = 1;

        private ushort UDPsent = 1;
        private ushort ExpectedUDP = 1;

        public byte futureTCPcount = 0; // for recever to send ack before header fills up.

        public DateTime DTtcp { get; set; }
        public DateTime DTUdp { get; set; } //TODO
        public bool forClient = true;
        public Crypto crypto;

        public bool isAckScheduled = false;

        //store received tcp datas until executed
        private Dictionary<ushort, RecArgs> ReceivedTCPQ = new Dictionary<ushort, RecArgs>(); // it stores received data arrived in dis order.
        //store the active ackfields datas
        private List<byte> HeaderAckData = new List<byte>(74);

        //store the sent data until acked
        private Dictionary<ushort, byte[]> SentTCPQ = new Dictionary<ushort, byte[]>(FClient.WinSize);
        private List<ushort> KeyforSentQ = new List<ushort>();

        private List<byte[]> TcpSplitData = new List<byte[]>();

        public FlashPeeer(bool isclient, IPEndPoint ipep)
        {
            DT = DateTime.UtcNow;
            forClient = isclient;
            IPEP = ipep;
            crypto = new Crypto(null); // to save aes keys
            HeaderAckData = new List<byte>
            {
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
                0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0
            };
        }

        //increase tcpsent and adds to sent q
        public void AfterSendingTCP(ref byte[] data, byte sentTimes)
        {
            //tcps sent is increased on headwriter

            //add to the list waiting for acks
            ushort dno = BitConverter.ToUInt16(data, PacketManager.DNO1);
            AddtoSentTCPQ(dno, data);
            KeyforSentQ.Add(dno);

            // now use token and async stuffs.
            ScheduleResend(dno, sentTimes);
        }

        //increase udpssent
        public void AfterSendingUDP()
        {
            incUdpSentONE();
        }

        //returns expected tcp
        public ushort GetExpectedTCP()
        {
            return this.ExpectedTCP;
        }

        //increases the expected tcp by one and updates the ackfields list
        public void incTCPexpectedONE()
        {
            if (this.ExpectedTCP == upperb)
            {
                this.ExpectedTCP = 1;
            }
            else
            {
                this.ExpectedTCP++;
            }

            //to make header ack data respect to expectedTCP
            if (HeaderAckData[0] == 1)
            {
                lock (HeaderAckData)
                {
                    HeaderAckData.RemoveAt(0);
                    HeaderAckData.Add(0);
                }
            }

            if (futureTCPcount >= 1)
            {
                --futureTCPcount;
            }
        }

        //increases any given number by 1 for given upper boundry
        private ushort NUMbyOne(ushort num, ushort upperB, bool increase)
        {
            if (increase)
            {
                if (num == upperB)
                {
                    num = 1;
                }
                else
                {
                    num++;
                }
            }
            else
            {
                if (num == 1)
                {
                    num = upperB;
                }
                else
                {
                    --num;
                }
            }

            return num;
        }

        // returns expected UDP
        public ushort GetExpectedUDP()
        {
            return this.ExpectedUDP;
        }

        // updates expected udp with recently received one
        public void incExpectedUDP(ushort latestDNO)
        {
            if (latestDNO == upperb)
            {
                this.ExpectedUDP = 1;
            }
            else
            {
                this.ExpectedUDP = (ushort)(latestDNO + 1);
            }
        }

        //returns total tcps sent
        public ushort getTcpsSent()
        {
            return this.TCPsent;
        }

        //returns total udps sent
        public ushort getUdpsSent()
        {
            return this.UDPsent;
        }

        //increase tcpssent by one, called after sending TCP
        public void incTcpSentONE()
        {
            if (this.TCPsent == upperb)
            {
                this.TCPsent = 1;
            }
            else
            {
                this.TCPsent++;
            }

        }

        // increases udpssent by one, called after sending UDP
        public void incUdpSentONE()
        {
            if (this.UDPsent == upperb)
            {
                this.UDPsent = 1;
            }
            else
            {
                this.UDPsent++;
            }

        }

        /// <summary>
        /// Removes data from list (acks)
        /// </summary>
        /// <param name="dataNo"></param>
        /// <returns></returns>
        public bool AckReceivedFor(ushort dataNo)
        {
            bool b = RemoveFromSentTCPQ(dataNo);
            return b;

        }

        // process ack fields reads which data are acked from header and removes from sentq
        public void ProcessAckFields(ushort PeerExpTcp, byte[] ackHeaderAndpayload)
        {
            ushort virTcpAcked = PeerExpTcp;
            ushort FastAcked = PeerExpTcp;

            virTcpAcked = NUMbyOne(virTcpAcked, upperb, false);

            //data before external expected tcp are removed with this.
            byte ind = (byte)KeyforSentQ.IndexOf(virTcpAcked);

            if (ind < 74) // when indexof is not found it returns -1 which is converted to 255.
            {
                for (byte i = 0; i <= ind; i++) // remove first item ind times
                {
                    RemoveFromSentTCPQ(KeyforSentQ[0]);
                    KeyforSentQ.RemoveAt(0);
                }
            }

            //data after external expected tcp are removed with this
            BitArray ackfields = new BitArray(ackHeaderAndpayload);

            for (int i = 0; i < ackfields.Length; i++)
            {
                FastAcked = NUMbyOne(FastAcked, upperb, true);
                if (ackfields[i] == true)
                {
                    RemoveFromSentTCPQ(FastAcked);
                }
            }
        }

        // updates specefic bit of ackfield with respect to DNO
        public void RegisterAck(ushort dno, byte diff)
        {
            if (dno == 0 || diff == 0 || diff >= 74) // dont have to change ackfield is we get expected bytes
            {
                return;
            }
            else
            {
                diff = --diff;
            }

            lock (HeaderAckData)
            {
                HeaderAckData[diff] = 1;
            }

            /* //test
             StringBuilder sb = new StringBuilder();
             sb.Append("[ ");
             for (int i = 0; i < 24; i++)
             {
                 sb.Append(" "+HeaderAckData[i].ToString() + " ");
             }

             sb.Append(" ]");

             Console.WriteLine(sb.ToString());
             //test*/
        }

        //For adding to senttcpq
        public bool AddtoSentTCPQ(ushort dno, byte[] data)
        {
            if (KeyforSentQ.Contains(dno))
            {
                Console.WriteLine($"data with same dno: {dno} already added in sentTCPQ");
                return false;
            }

            SentTCPQ.Add(dno, data);
            return true;
        }

        //removing
        public bool RemoveFromSentTCPQ(ushort dno)
        {
            lock (SentTCPQ)
            {
                if (dno == 0)
                {
                    SentTCPQ.Clear();
                    KeyforSentQ.Clear();
                    return true;
                }

                if (KeyforSentQ.Contains(dno))
                {
                    SentTCPQ.Remove(dno);
                    return true;
                }

                return false;
            }
        }

        //getting a specefic data from Q, returns false if not found
        public bool GetFromSentTCPQ(ushort dno, out byte[] boolDat)
        {
            return SentTCPQ.TryGetValue(dno, out boolDat);
        }

        //return the ackfield when we are going to send data, mostly for headwriter
        public byte[] GetAckFieldForSending(int startIndex, int bitlength)
        {
            BitArray b = new BitArray(bitlength);
            lock (HeaderAckData)
            {
                int count = 0;
                for (int i = startIndex; i < bitlength; i++)
                {
                    if (HeaderAckData[i] == 1)
                    {
                        b[count] = true;
                    }
                    else
                    {
                        b[count] = false;
                    }

                    count += 1;
                }
            }

            byte[] b1 = new byte[(bitlength / 8)];
            b.CopyTo(b1, 0);
            return b1;
        }

        //test
        public string getackfieldfortesting()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[ ");
            lock (HeaderAckData)
            {
                for (int i = 0; i < 24; i++)
                {
                    if (HeaderAckData[i] == 1)
                    {
                        sb.Append(" 1");
                    }
                    else
                    {
                        sb.Append(" 0");
                    }
                }
            }

            sb.Append(" ]");
            return sb.ToString();
        }

        //adds
        public bool AddtoRecTCPQ(ushort b, RecArgs u)
        {
            try
            {
                ReceivedTCPQ.Add(b, u);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //removes
        public bool RemoveFromRecTCPQ(ushort b)
        {
            lock (ReceivedTCPQ)
            {
                if (ReceivedTCPQ.ContainsKey(b))
                {
                    ReceivedTCPQ.Remove(b);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        //getting a specefic data from Q, returns false if not found
        public bool GetFromRecTCPQ(ushort dno, out RecArgs Recarg)
        {
            return ReceivedTCPQ.TryGetValue(dno, out Recarg);
        }

        //makes ack byte[] from header and head of line data
        private byte[] AckPacketMaker(byte sentimes)
        {
            //send ack packet
            byte[] b = null;
            byte additionalLen = (byte)Math.Ceiling((double)(HeaderAckData.LastIndexOf(1) + 1) / 8);
            byte[] head = null;

            b = new byte[PacketManager.Overhead + additionalLen];

            head = GetAckFieldForSending(0, additionalLen * 8);
            Array.Copy(head, 0, b, PacketManager.PayloadSTR, head.Length);

            FClient.Pmanager.HeadWriter(ref b, (byte)Opfunctions.ack, IPEP.ToString(), false, this, sentimes);

            byte Ccrc = FClient.Pmanager.ComputeChecksum(b, null, b.Length);
            b[PacketManager.CRC] = Ccrc;

            return b;
        }

        public async void ScheduleAck(byte sentTimes)
        {
            if (sentTimes == 1) //it is first pacet no resend
            {
                if (isAckScheduled)
                {
                    return;
                }

                ushort seq = getTcpsSent();
                isAckScheduled = true;
                await Task.Delay(FClient.AckTimeout);

                if (seq != getTcpsSent()) // meaning some tcp data were sent, so ack is also already sent
                {
                    return;
                }
                else
                {
                    //send ack packet
                    byte[] b = AckPacketMaker(sentTimes);

                    //send it.
                    isAckScheduled = false;
                    FClient.SendDataUnreliable(b, IPEP, false);
                }
            }

            if (sentTimes == 2 || sentTimes == 3)
            {
                //send ack packet immediately              
                byte[] b = AckPacketMaker(sentTimes);

                //send it.
                isAckScheduled = false;
                FClient.SendDataUnreliable(b, IPEP, false);
            }

        }

        public async void ScheduleResend(ushort DataNo, byte sentTimes)
        {
            if (sentTimes >= 4 || sentTimes <= 0)
            {
                return;
            }

            if (!connected)
            {
                //return if removed from server ie. not connected
                return;
            }

            byte[] b;
            if (sentTimes == 1)
            {
                await Task.Delay(FClient.TCPtimeout);  //1500 ms

                //resend again, resend to peer will reject if it was already acked
                if (!FClient.ResendtoPeer(this, DataNo, sentTimes))
                {
                    //meaning data was acked
                    return;
                }
                return;
            }

            if (sentTimes == 2)
            {
                await Task.Delay(FClient.TCPtimeout2); //400 ms

                //resend again, resend to peer will reject if it was already acked
                if (!FClient.ResendtoPeer(this, DataNo, sentTimes))
                {
                    //meaning data was acked
                    return;
                }
                return;
            }

            if (sentTimes == 3)
            {
                //TODO: show network problem SIGN------------------------------------
                Console.WriteLine("Network connection problem");
                await Task.Delay(FClient.TCPtimeout3); //300
                //check and disconnect

                if (GetFromSentTCPQ(DataNo, out b))
                {
                    //data not acked, so now disconnect--  

                    FClient.ByeBye(IPEP.ToString()); //meaning send byebye
                    FClient.ResetConnection("kadak");
                    if (FClient.isClient)
                    {
                        //reconnect
                        Console.WriteLine("Disconnected due to poor conn");
                    }
                    else
                    {
                        FClient.RemovePeer(IPEP.ToString()); //set connected to false and unlist from timer call
                    }
                }
            }
        }

        public RecArgs TrigerSplit(byte[] data)
        {
            TcpSplitData.Insert((data[PacketManager.ChunkNO] - 1), data);

            if (data[PacketManager.ChunkNO] == data[PacketManager.TotalChunk])
            {
                //we received all chunked data

                //calculate the sum of size of all chunks
                int totalSize = 0;
                foreach (var item in TcpSplitData)
                {
                    totalSize += (item.Length - 13);
                }

                //now make the new byte[] with that size
                byte[] finalData = new byte[totalSize];

                //now copy revalent data from chunk
                int pointer = 0; // start from 13th data
                foreach (var item in TcpSplitData)
                {
                    Array.Copy(item, 13, finalData, pointer, (item.Length - 13));

                    pointer += (item.Length - 13);
                }

                RecArgs r = new RecArgs();
                r.lateData = finalData;
                r.Operation = data[PacketManager.SplitOriginalFuc];
                r.p = this;
                r.reliable = true;
                r.NetArgs = null;

                return r;
            }
            else
            {
                return null;
            }
        }

        public void SendData(byte[] data)
        {

        }

        internal void HelloReply()
        {
            byte[] data = new byte[PacketSerializer.MinLenOfPacket];
            Array.Copy(BitConverter.GetBytes((ushort)Opfunctions.Handshake), 0, data, PacketSerializer.POS_OF_OPCODE, 2);
            data[PacketSerializer.POS_OF_LEN] = 0;
            data[PacketSerializer.POS_OF_LEN + 1] = 0;

            if (!(FlashProtocol.Instance.Pmaker.HeadWriter(ref data, false, this)))
            {
                return;
            }
            SendData(data);
        }
    }
}
