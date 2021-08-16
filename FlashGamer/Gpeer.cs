using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using FlashPeer;

namespace FlashGamer
{
    public class Gpeer
    {
        //start the lower networking
        Timer TcpTimer;
        Timer UdpTimer;

        private uint tcpinterval = 85;
        private uint udpinterval = 16;

        private List<byte[]> tcpAllbyteList;
        private List<byte[]> tcpSendbyteList;
        private List<byte[]> tcpSplitList;
        private int tcpSendSize;

        private List<byte[]> udpAllbyteList;
        private List<byte[]> udpSendbyteList;

        public int udpSendSize = 0;
        public bool isclient = true;

        public string ID = "kadak";
        public Peer lowerPeer = null;
        public Room SmallRoom;

        public Gpeer(Peer lowPeer, string Id)
        {
            lowerPeer = lowPeer;
            TcpTimer = new Timer(tcpinterval);
            TcpTimer.Elapsed += SendTcp;
            TcpTimer.AutoReset = true;

            UdpTimer = new Timer(udpinterval);
            UdpTimer.Elapsed += SendUdp;
            UdpTimer.AutoReset = true;

            tcpAllbyteList = new List<byte[]>();
            tcpSendbyteList = new List<byte[]>();
            tcpSplitList = new List<byte[]>();

            udpAllbyteList = new List<byte[]>();
            udpSendbyteList = new List<byte[]>();
            this.ID = Id;

            if (!FClient.isClient) // only we need id if it is server, client dont need id for server's Gpeer
            {
                SmallRoom = new Room(5, null, true, ID);
            }

            TcpTimer.Start();
            UdpTimer.Start();
        }

        public void addtoTCPsending(byte[] data)
        {
            if (data == null || data.Length < 3)
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
            if (data == null || data.Length < 3 || data.Length > 490)
            {
                return;
            }

            lock (udpAllbyteList)
            {
                udpAllbyteList.Add(data);
            }
        }

        public void ChangeTickRate(bool tcpTick, uint tickInterval)
        {
            if (tcpTick)
            {
                if (tickInterval <= 50)
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
                if (tickInterval <= 15)
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
            if (tcpSplitList.Count > 0)
            {
                FClient.SendDataReliable(tcpSplitList[0], this.lowerPeer.IPEP, 1);
                tcpSplitList.RemoveAt(0);
                return;
            }
            else
            {
                if (tcpAllbyteList.Count == 0)
                {
                    //no data to send
                    return;
                }

                List<byte[]> toremove = new List<byte[]>();

                lock (tcpAllbyteList)
                {
                    foreach (var item in tcpAllbyteList)
                    {
                        if (tcpSendSize + item.Length < 487) // this data can go with multiple
                        {
                            // at least something will be sent so all good.
                            tcpSendbyteList.Add(item);
                            tcpSendSize += item.Length;

                            toremove.Add(item);
                        }
                        else
                        {
                            if (item.Length > 487)
                            {
                                //split and insert to the split list and remove the original data
                                var templist = new List<byte[]>();
                                FClient.Pmanager.Splitter(item, lowerPeer.IPEP.ToString(), lowerPeer, 1, out templist);

                                foreach (var ite in templist)
                                {
                                    tcpSplitList.Add(ite);
                                }

                                toremove.Add(item);

                                if (tcpSendSize == 0)
                                {
                                    //nothing to send so split and send.
                                    FClient.SendDataReliable(tcpSplitList[0], this.lowerPeer.IPEP, 1);
                                    tcpSplitList.RemoveAt(0);
                                }

                                break;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

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
                    if (tcpSendbyteList.Count == 1)
                    {
                        Array.Copy(item, 3, DataToSend, pointer, item.Length - 3);
                        break;
                    }

                    Array.Copy(item, 0, DataToSend, pointer, item.Length);
                    pointer += item.Length;
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
    }
}
