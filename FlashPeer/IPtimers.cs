using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace FlashPeer
{
    public class IPtimers
    {
        //start the lower networking
        Timer TcpTimer;
        Timer UdpTimer;

        private uint tcpinterval = 85; //represents interval in miliseconds between each tcp data sending to same peer. 
        private uint udpinterval = 16;

        private List<byte[]> tcpAllbyteList;
        private List<byte[]> tcpSendbyteList;
        //private List<byte[]> tcpSplitList;
        private int tcpSendSize;

        private List<byte[]> udpAllbyteList;
        private List<byte[]> udpSendbyteList;

        public int udpSendSize = 0;
        public bool isclient = true;

        public string ID = "kadak"; // lets allow it to be max 15 char

        public IPtimers()
        {
            TcpTimer = new Timer(tcpinterval);
            TcpTimer.Elapsed += SendTcp;
            TcpTimer.AutoReset = true;

            UdpTimer = new Timer(udpinterval);
            UdpTimer.Elapsed += SendUdp;
            UdpTimer.AutoReset = true;

            tcpAllbyteList = new List<byte[]>();
            tcpSendbyteList = new List<byte[]>();
            //tcpSplitList = new List<byte[]>();

            udpAllbyteList = new List<byte[]>();
            udpSendbyteList = new List<byte[]>();

            TcpTimer.Start();
            UdpTimer.Start();
        }

        public void addtoTCPsending(byte[] data)
        {
            if (data == null || data.Length < 3 || data.Length > 1021) // this is limit for now. To make it simple and remove splitting funcitons.
            {
                return;
            }

            lock (tcpAllbyteList)
            {
                tcpAllbyteList.Add(data);
            }
        }

        public void addtoUDPsending(byte[] data)
        {
            if (data == null || data.Length < 3 || data.Length > 1021) // 1024 is limit for UDP.. (not any technical limit just programming limit)
            {
                return;
            }

            lock (udpAllbyteList)
            {
                udpAllbyteList.Add(data);
            }
            return;
        }

        public void ChangeTickRate(bool tcpTick, uint tickInterval)
        {
            if (tcpTick)
            {
                if (tickInterval <= 50) // 50ms is limit for tcp so that networking does not breaks. 
                {
                    return;
                }
                tcpinterval = tickInterval;
                TcpTimer.Stop();
                TcpTimer.Interval = tcpinterval;
                TcpTimer.Start();
            }
            else
            {
                if (tickInterval <= 15) //15ms is limit for UDP so that networkin does not breaks.
                {
                    return;
                }
                udpinterval = tickInterval;
                UdpTimer.Stop();
                UdpTimer.Interval = udpinterval;
                UdpTimer.Start();
            }

        }

        public void SendTcp(object sender, ElapsedEventArgs e)
        {
            List<byte[]> toremove = new List<byte[]>();
            if (tcpAllbyteList.Count == 1)
            {
                FClient.SendDataReliable(tcpAllbyteList[0], this.lowerPeer.IPEP, 1);
                return;
            }
            else
            {
                if (tcpAllbyteList.Count == 0)
                {
                    //no data to send
                    return;
                }

                tcpSendSize = 0;
                lock (tcpAllbyteList)
                {
                    foreach (var item in tcpAllbyteList)
                    {
                        if (tcpSendSize + item.Length < 1024) // this data can go with multiple
                        {
                            // at least something will be sent so all good.
                            tcpSendbyteList.Add(item);
                            tcpSendSize += item.Length;

                            toremove.Add(item);
                        }
                        else
                        {
                            break; 
                        }
                    }

                    //send and remove
                    // now we have a list of packets to be sent.

                    foreach (var item in toremove)
                    {
                        tcpAllbyteList.Remove(item);
                    }
                }

                if (tcpSendSize == 0)
                {
                    //meaning there is nothing to send, split was sent if so return;
                    return;
                }

                byte[] DataToSend = new byte[tcpSendSize + PacketManager.Overhead];

                //meaning we are ready to send some data.
                int pointer = PacketManager.PayloadSTR;

                foreach (var item in tcpSendbyteList)
                { 
                    Array.Copy(item, 0, DataToSend, pointer, item.Length);
                    pointer += item.Length;
                }
                if(tcpSendbyteList.Count == 1)
                {
                    //only one pocket and header
                }

                if (tcpSendbyteList.Count > 1)
                {
                    //that data contains mixed fucntion
                    //now do the headwriter thing
                    FClient.Pmanager.HeadWriter(ref DataToSend, (byte)OpFunctions.ManyFunction, lowerPeer.IPEP.ToString(),
                        true, lowerPeer, 1);
                }
                else
                {
                    //not mixed funciton
                    FClient.Pmanager.HeadWriter(ref DataToSend, tcpSendbyteList[0][0], lowerPeer.IPEP.ToString(),
                        true, lowerPeer, 1);
                }

                //fill crc
                DataToSend[PacketManager.CRC] = FClient.Pmanager.ComputeChecksum(DataToSend, null, DataToSend.Length);

                //remove all data from send list
                tcpSendbyteList.Clear();
                tcpSendSize = 0;

                //now send that data
                FClient.SendDataReliable(DataToSend, lowerPeer.IPEP, 1);
            }
        }

        public void SendUdp(object sender, ElapsedEventArgs e)
        {
            if (udpAllbyteList.Count == 0)
            {
                //no data to send
                return;
            }

            List<byte[]> toremove = new List<byte[]>();

            lock (udpAllbyteList)
            {
                foreach (var item in udpAllbyteList)
                {
                    if (udpSendSize + item.Length < 487) //can send multiple at once
                    {
                        udpSendbyteList.Add(item);
                        toremove.Add(item);
                        udpSendSize += item.Length;
                    }
                    else
                    {
                        break;
                    }
                }

                foreach (var item in toremove)
                {
                    udpAllbyteList.Remove(item);
                }
            }

            byte[] DataToSend = new byte[udpSendSize + PacketManager.Overhead];
            int pointer = PacketManager.PayloadSTR;

            foreach (var item in udpSendbyteList)
            {
                if (udpSendbyteList.Count == 1)
                {
                    Array.Copy(item, 3, DataToSend, pointer, item.Length - 3);
                    break;
                }
                Array.Copy(item, 0, DataToSend, pointer, item.Length);
                pointer += item.Length;
            }

            //for headwriter
            if (udpSendbyteList.Count > 1)
            {
                //sending multiple
                FClient.Pmanager.HeadWriter(ref DataToSend, (byte)OpFunctions.ManyFunction, lowerPeer.IPEP.ToString(),
                    false, lowerPeer, 1);
            }
            else
            {
                //sending one
                FClient.Pmanager.HeadWriter(ref DataToSend, udpSendbyteList[0][0], lowerPeer.IPEP.ToString(),
                    false, lowerPeer, 1);
            }

            //fill crc
            DataToSend[PacketManager.CRC] = FClient.Pmanager.ComputeChecksum(DataToSend, null, DataToSend.Length);

            //clear the list
            udpSendbyteList.Clear();
            udpSendSize = 0;

            //send
            FClient.SendDataUnreliable(DataToSend, null, false);

        }

        public byte[] GpeerDetail()
        {
            return ASCIIEncoding.UTF8.GetBytes(ID);
        }
    }
}
}
