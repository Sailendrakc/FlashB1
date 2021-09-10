using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using FlashB1;
using System.Net;

namespace FlashPeer
{
    public class PacketFilter
    {
        //what we receive
        IArgObject obj;
        byte[] Buffer;

        //after processing
        bool isreliable;
        FlashPeer sender;

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
            byte Ccrc = FlashProtocol.Instance.Pmaker.ComputeChecksum(Buffer, null, (ushort)obj.getBytesTransferred());

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

        private FlashPeer CheckHello()
        {
            var req = BitConverter.ToUInt16(Buffer, PacketSerializer.POS_OF_OPCODE);

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
                    //good hello packet
                    if ((req == (ushort)Opfunctions.Hello))
                    {
                        if (FlashProtocol.Instance.Current_Peers >= FlashProtocol.Instance.Max_Peers)
                        {
                            //cant accept new users anymore.
                            //Transfer to another server?? --- MAYBE TODO
                            Console.WriteLine($"Max players: {FlashProtocol.Instance.Max_Peers} reached.");
                            return null;
                        }

                        //create new peer for this client
                        peer1 = new FlashPeer((IPEndPoint)obj.GetClient());
                        //check crypto keys
                        if (FlashProtocol.Instance.crypto.HelloUnpacker(obj.GetRawData(), peer1) == false)
                        {
                            //bad crypto keys
                            return null;
                        }
                        //all good, proceed to handshake, create new handshake session and pass the peer.
                        var ch = new HSfromServer(peer1);
                        //add the handshake session to collection.
                        lock (FlashProtocol.Instance.pendingClients)
                        {
                            FlashProtocol.Instance.pendingClients.Add(peer1.endpoint.ToString(), ch);
                        }
                        return null;
                    }

                    //good other hello packets
                    if ((req == (ushort)Opfunctions.Hello2) || (req == (ushort)Opfunctions.Hello3) || (req == (ushort)Opfunctions.Hello4))
                    {
                        if (FlashProtocol.Instance.pendingClients.TryGetValue(obj.GetClient().ToString(), out var pe))
                        {
                            long tickss = BitConverter.ToInt64(obj.GetRawData(), PacketSerializer.PayloadSTR);
                            pe.t_Received(tickss);
                            return null;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                //bad packet, connection does not exists but is reliable paket.
                return null;
            }
        }

        private bool CheckNumber(FlashPeer peer, byte[] dataToCheck)
        {
            if (isreliable)
            {
                /*ushort ReceivedDno = BitConverter.ToUInt16(Buffer, PacketSerializer.POS_OF_DNO);
                  var diff = FlashProtocol.Instance.Pmaker.PacketNoFilter(ReceivedDno, peer.OurExpectedTcpNo, peer.GetLastDateTime(true));

                 if (diff < 0)
                 {
                     Console.WriteLine("Outdated packet received.");
                     return false;
                 }
                 else
                 {
                     peer.SetLastDateTime(DateTime.UtcNow);
                     return true;
                 }*/
                return false; //todo remove this..
            }
            else
            {
                var ts = peer.BaseDateTime+ new TimeSpan((long)(BitConverter.ToUInt32(dataToCheck, PacketSerializer.POS_OF_DATE)));
                var dno = FlashProtocol.Instance.Pmaker.PacketNoFilterUdp(ts, peer.GetLastDateTime());

                if (dno < 0)
                {
                    Console.WriteLine("Outdated packet received.");
                    return false;
                }
                else
                {
                    peer.SetLastDateTime(DateTime.UtcNow);
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

            var res = FlashProtocol.Instance.ReadPacket(obj);
            res.peer = sender;
            return res;
        }
    }
}