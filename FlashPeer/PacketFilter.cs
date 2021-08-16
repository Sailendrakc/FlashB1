using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using FlashB1;
using System.Net;
using FlashPeer.interfaces;

namespace FlashPeer
{
    public class PacketFilter
    {
        //what we receive
        IArgObject obj;
        byte[] Buffer;

        //after processing
        bool isreliable;
        IFlashPeer sender;

        public PacketFilter(IArgObject e)
        {
            obj = e;
            Buffer = obj.GetRawData();
        }

        private bool CheckLen()
        {

            if (Buffer.Length < PacketSerializer.MinLenOfPacket)
            {
                Console.WriteLine("length less than overhead");
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool CheckCrc()
        {
            //Check crc
            byte Rcrc = Buffer[PacketSerializer.POS_OF_CRC];

            Buffer[PacketSerializer.POS_OF_CRC] = 0;
            byte Ccrc = FlashProtocol.Instance.Pmaker.ComputeChecksum(Buffer, null, (ushort)obj.BytesTransferred);

            if (Rcrc != Ccrc)
            {
                Console.WriteLine($"Invalid CRC Checksum. CCRC: {Ccrc} and RCRC: {Rcrc}");
                return false;
            }
            else
            {
                return true;
            }
        }

        private IFlashPeer CheckHello()
        {
            var req = BitConverter.ToUInt16(Buffer, PacketManager.OPCODE);

            //does this requesting peer already exists??
            if (FlashProtocol.Instance.Connections.TryGetValue(obj.GetClient().ToString(), out var peer1))
            {
                //the client exists.

                //if the request is handhsake, remove peer, coz we are already connected.
                if((req == (ushort)Opfunctions.Hello2) || (req == (ushort)Opfunctions.Hello3) || 
                    (req == (ushort)Opfunctions.Hello4) || req == (ushort)Opfunctions.Hello)
                {
                    FlashProtocol.Instance.RemovePeer(obj.GetClient().ToString(), true);
                    return null;
                }
                else
                {
                    //not hello
                    if (Buffer[0] < 5)
                    {
                        isreliable = false;
                    }
                    else
                    {
                        isreliable = true;
                    }
                    return peer1;
                } 
            }
            else
            {
                if (Buffer[0] == 0) //needs to be unreliable packet
                {
                    if ((req == (ushort)Opfunctions.Hello))
                    {
                        if (FlashProtocol.Instance.Current_Peers >= FlashProtocol.Instance.Max_Peers)
                        {
                            Console.WriteLine($"Max players: {FlashProtocol.Instance.Max_Peers} reached.");
                            return null;
                        }
                        peer1 = new FlashPeer((IPEndPoint)obj.GetClient());
                        bool b1 = FlashProtocol.Instance.crypto.HelloUnpacker(obj.GetRawData(), peer1);
                        if (b1 == false)
                        {
                            return null;
                        }

                        lock (FlashProtocol.Instance.Connectings)
                        {
                            FlashProtocol.Instance.Connectings.Add(obj.GetClient().ToString(), peer1);
                        }
                        peer1.HelloReply();
                        return null;
                    }

                    if ((req == (ushort)Opfunctions.Hello2) || (req == (ushort)Opfunctions.Hello3) || (req == (ushort)Opfunctions.Hello4))
                    {
                        if (FlashProtocol.Instance.Connectings.TryGetValue(obj.GetClient().ToString(), out var pe))
                        {
                            long tickss = BitConverter.ToInt64(obj.GetRawData(), PacketSerializer.PayloadSTR);
                            ((FlashPeer)pe).ReplyHandshake.t_Received(tickss);
                            return null;
                        }
                    }
                }

                return null;
            }
        }

        // ((dno, diff), peer, expectedno)
        private bool CheckNumber(IFlashPeer peer, byte[] dataToCheck)
        {
            ushort ReceivedDno = BitConverter.ToUInt16(Buffer, PacketManager.POS_OF_DNO);
            if (isreliable)
            {
                var diff = FlashProtocol.Instance.Pmanager.PacketNoFilter(ReceivedDno, peer.OurExpectedTcpNo, peer.GetLastDateTime(true));

                if (diff < 0)
                {
                    Console.WriteLine("Outdated packet received.");
                    return false;
                }
                else
                {
                    peer.SetLastDateTime(true, DateTime.UtcNow);
                    return true;
                }
            }
            else
            {
                var dno = FlashProtocol.Instance.Pmanager.PacketNoFilterUdp(ReceivedDno, peer.OurExpectedUdpNo, peer.GetLastDateTime(false));

                if (dno < 0)
                {
                    Console.WriteLine("Outdated packet received.");
                    return false;
                }
                else
                {
                    peer.SetLastDateTime(false, DateTime.UtcNow);
                    peer.OurExpectedUdpNo = dno + 1;
                    return true;
                }
            }
        }

        public Header CheckData()
        {
            if (!CheckLen())
            {
                return null;
            }

            if (!CheckCrc())
            {
                return null;
            }

            sender = CheckHello();
            if (sender ==null)
            {
                return null;
            }
            

            if(!CheckNumber(sender, obj.GetRawData()))
            {
                return null;
            }

            if (no.Item2.futureTCPcount >= 74 && obj.Buffer[PacketManager.Opcode] != (byte)Opfunctions.ack)
            {
                return null;
            }

            var res = FlashProtocol.Instance.ReadPacket(obj);
            return res;

            if (!isreliable)
            {
                byte[] datee = new byte[4];
                datee[0] = 0;
                Array.Copy(obj.Buffer, PacketManager.DATEFORUDP, datee, 1, 3);
                u.date = BitConverter.ToUInt32(datee, 0).ToString().Insert(2, ":").Insert(4, ":");
            }
        }
    }
}