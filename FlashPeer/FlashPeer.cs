using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Timers;

namespace FlashPeer
{
    public class FlashPeer
    {
        #region UDPandRUDP
        /// <summary>
        /// This stores the difference in UTC now time between peer and server in ticks as a milisecond/nano.
        /// </summary>
        public TimeSpan DifferenceTimespan { get; set; }

        /// <summary>
        /// It is the time when server received first hello packet.
        /// </summary>
        public DateTime BaseDateTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// This endpoint represents address of peer.
        /// </summary>
        public IPEndPoint endpoint { get; set; }

        /// <summary>
        /// This is class that does all cryptography for this peer.
        /// </summary>
        public Crypto crypto;

        /// <summary>
        /// The buffer size for receving data.
        /// </summary>
        /// 
        /// <summary>
        /// The latest date when we received any packet.
        /// </summary>
        public DateTime lastDateTime { get; set; } = DateTime.UtcNow;

        public int maxRecBytes = 512;
        public bool connected = false;

        public FlashPeer(IPEndPoint ep)
        {
            endpoint = ep;
            
        }

        public void SetLastDateTime(DateTime dt)
        {
            lastDateTime = dt;
        }

        public void SendData(byte[] data)
        {
            FlashProtocol.Instance.channel.StartSendingData(data, this.endpoint);
        }

        public void RecData(Header data)
        {
            foreach (var item in data.AllPicklets)
            {
                if (item.Opcode == (int)Opfunctions.keepalive)
                {
                    SetLastDateTime(DateTime.UtcNow);
                    continue;
                }

                /*if (item.Opcode == (int)Opfunctions.ack)
                {
                    var ackbits = new byte[3];
                    Array.Copy(item.data, item.strIndex + PacketSerializer.POS_OF_ACKFIELDS, ackbits, 0, PacketSerializer.LEN_OF_ACKFIELDS);
                    ProcessAckFields((ushort)data.ExpRecivingNo, ackbits);
                    continue;
                }*/
            }
        }

        public DateTime GetLastDateTime()
        {
            return lastDateTime;
        }

        #endregion

        #region UDP

        #endregion

        #region RDUP
        /*
        public ushort OurExpectedTcpNo { get; set; } = 0;

        //for storing unacked reliable data
        private Dictionary<ushort, byte[]> SentTCPQ = new Dictionary<ushort, byte[]>(24); //FlashProtocol.Instance.WinSize

        private List<ushort> KeyforSentQ = new List<ushort>();

        private List<byte[]> TcpSplitData = new List<byte[]>();
        //store the active ackfields datas
        private List<byte> HeaderAckData = new List<byte>(24); //74

        public byte[] GetAckFieldForSending(int bitlength)
        {
            throw new NotImplementedException();
        }

        public ushort GetAndIncDNO(int increaseBy)
        {
            throw new NotImplementedException();
        }

        //increases or decreases any given number by 1 for given upper boundry
        private int NUMbyOne(int num, int upperB, bool increase)
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

        // process ack fields reads which data are acked from header and removes from sentq
        public void ProcessAckFields(ushort PeerExpTcp, byte[] ackHeader)
        {
            ushort virTcpAcked = PeerExpTcp;
            ushort FastAcked = PeerExpTcp;

            virTcpAcked = (ushort)NUMbyOne(virTcpAcked, ushort.MaxValue, false); // virtcpacket++

            //data before external expected tcp are removed with this.
            int ind = KeyforSentQ.IndexOf(virTcpAcked);

            if (ind < 24) // when indexof is not found it returns -1 which is converted to 255.
            {
                for (byte i = 0; i <= ind; i++) // remove first item ind times
                {
                    RemoveFromSentTCPQ(KeyforSentQ[0]);
                    KeyforSentQ.RemoveAt(0);
                }
            }

            //data after external expected tcp are removed with this
            BitArray ackfields = new BitArray(ackHeader);

            for (int i = 0; i < ackfields.Length; i++)
            {
                FastAcked = (ushort)NUMbyOne(FastAcked, ushort.MaxValue, true);
                if (ackfields[i] == true)
                {
                    RemoveFromSentTCPQ(FastAcked);
                }
            }
        }

        // updates specefic bit of ackfield with respect to DNO
        public void RegisterAck(ushort dno, byte diff)
        {
            if (dno == 0 || diff == 0 || diff >= 24) // dont have to change ackfield is we get expected bytes
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
             //test*//*
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
        */
        #endregion

    }
}
