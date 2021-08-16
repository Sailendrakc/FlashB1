using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashPeer
{
    public class PacketManager
    {
        #region packetheaders
        /// <summary>
        /// Its value points the position of fist byte in packet.
        /// <5 represets unreliabile channel and > 5 represents reliable channel.
        /// 1-3 or 6-8 inclusive represents sent times of the packet.
        /// </summary>
        readonly public static int POS_OF_STR = 0; //0

        /// <summary>
        /// The value points the position of byte[] (ushort) that tells which Dno the sender is expecting.
        /// For example, if sender is expecting 15th data to receive next, its value will be 15.
        /// This will represent all data with numbering 14 or less has been received.
        /// </summary>
        readonly public static int POS_OF_EXPECTED_REC_DNO = 1; // 1.2
        readonly public static int LEN_OF_EXPECTED_REC_DNO = 2;

        /// <summary>
        /// This points the poistion of byte[] that acks the data we received faster.
        /// It is relative to expected rec dno.
        /// </summary>
        readonly public static int ACKFIELDS = 3; // 3.4.5
        readonly public static int ACKFIELDSLEN = 3; //24 bits
        //OR
        readonly public static int DATEFORUDP = 3; // 3.4.5
        readonly public static int DATELEN = 3;

        readonly public static int POS_OF_DNO = 6; // 6.7
        readonly public static int LEN_OF_DNO = 2;

        readonly public static int POS_OF_CRC = 8; // 8

        readonly public static int OPCODE = 9; //11.12
        readonly public static int LEN_OF_OPCODE = 2;



        readonly public static int PayloadSTR = 10;

        readonly public static int Overhead = 10; // coz indexing started from 0

        //payload optional headers
        readonly public static int SplitOriginalFuc = 10;
        readonly public static int ChunkNO = 11;
        readonly public static int TotalChunk = 12;

        #endregion

        #region packet checker bounds
        internal static byte tickMS = 16;
        internal static ushort UppB = ushort.MaxValue;
        //rtt cannot be more than 255*tickMS == 1300
        static byte MaxPackets = 24;//74 wy?
        #endregion


        public void Splitter(byte[] data, string IpofDestination, FlashPeeer peer, byte senttimes, out List<byte[]> outData)
        {
            // only call when higher needs data to split.

            int remainingbytes = data.Length;
            int pointer = 0;
            int lentoCopy = 1021;
            byte totalchunk = (byte)Math.Ceiling((double)data.Length / 1024); //3 bytes for chunk header;
            byte chunks = 1;

            List<byte[]> li1 = new List<byte[]>();

            while (remainingbytes > 0)
            {
                byte[] c1 = null;
                if (remainingbytes > 1021)
                {
                    c1 = new byte[1024];
                }
                else
                {
                    c1 = new byte[remainingbytes + 13];
                    lentoCopy = remainingbytes;
                }

                Array.Copy(data, pointer, c1, 13, lentoCopy);
                if (!HeadWriter(ref c1, (byte)Opfunctions.Split, IpofDestination, true, peer, senttimes)) //only reliable
                {
                    outData = null;
                    return;
                }

                c1[SplitOriginalFuc] = data[0];
                c1[ChunkNO] = chunks;
                c1[TotalChunk] = totalchunk;

                //calculate checksum and add to list
                c1[CRC] = ComputeChecksum(c1, null, c1.Length);
                li1.Add(c1);

                pointer += lentoCopy;
                remainingbytes -= lentoCopy;
                chunks += 1;               
            }

            outData = li1;
            return;
        }

        /// <summary>
        /// Overrides headers to pre existing array without changing size of array.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="Fuc"></param>
        /// <param name="IPofDestination"></param>
        /// <param name="Relaible"></param>
        public bool HeadWriter(ref byte[] data, byte Fuc, string IPofDestination, bool Relaible, FlashPeeer peer, byte sentimes)
        {
            //check length
            if (data.Length > 1024 || (Relaible && peer == null))
            {
                return false;
            }

            data[CRC] = 0;
            if (Relaible)
            {
                data[STR] = sentimes;

                //set ack fields
                Array.Copy(peer.GetAckFieldForSending(0, 24), 0, data, ACKFIELDS, ACKFIELDSLEN);
            }
            else
            {
                //set date insted of ack fields
                string date = DateTime.UtcNow.ToString("mm:ss.fff").Replace(":", "").Replace(".", "");
                byte[] datearr = BitConverter.GetBytes(uint.Parse(date));

                Array.Copy(datearr, 1, data, DATEFORUDP, DATELEN);
                data[STR] = (byte)Opfunctions.packetstrtunreliable;
            }

            //filling function in header
            data[Opcode] = (byte)Fuc;

            ushort dnoToUse = 1;
            ushort rectcpToUse = 1;

            //getting data for dno
            if (peer != null)
            {
                rectcpToUse = peer.GetExpectedTCP();

                if (Relaible)
                {
                    dnoToUse = peer.getTcpsSent();
                    peer.incTcpSentONE();
                }
                else
                {
                    dnoToUse = peer.getUdpsSent();
                }

                if (dnoToUse == 0 || rectcpToUse == 0)
                {
                    //some error
                    return false;
                }
            }

            // filling dno in header
            /* byte[] barr = BitConverter.GetBytes((ushort)dnoToUse);

             for (int i = DNO1; i <= DNO1 + 1; i++)
             {
                 data[i] = barr[i - DNO1];
             }*/
            Array.Copy(BitConverter.GetBytes((ushort)dnoToUse), 0, data, DNO1, 2);
            // filling rec tcp in header
            /* byte[] tcpp = BitConverter.GetBytes((ushort)rectcpToUse);

             for (int i = RECTCP; i <= RECTCP + 1; i++)
             {
                 data[i] = tcpp[i - RECTCP];
             }*/
            Array.Copy(BitConverter.GetBytes((ushort)rectcpToUse), 0, data, RECTCP, 2);
            return true;
        }

        public byte[] HelloPacketer() //unreliable in packet format
        {

            //aes for sending
            byte[] aesKeys = FClient.GetAESKey();
            byte[] aesIv = FClient.GetAESIV();

            //copy aes
            byte[] aesparms = new byte[aesKeys.Length + aesIv.Length]; //48
            Array.Copy(aesKeys, 0, aesparms, 0, aesKeys.Length); //32
            Array.Copy(aesIv, 0, aesparms, aesKeys.Length, aesIv.Length); //16

            //encrypt it 
            byte[] EncPld = FClient.RSAEncrypt(ref aesparms);

            //prepare hello packet
            byte[] data = new byte[EncPld.Length + Overhead];

            Array.Copy(EncPld, 0, data, PayloadSTR, EncPld.Length);

            if (!HeadWriter(ref data, (byte)Opfunctions.Hello, "NO IP NEEDED", false, null, 1))
            {
                FClient.RaiseOtherEvent("Headwriting error in HeloPacketer()", null, EventType.ConsoleMessage, null);
                return null;
            }

            // perform crc
            //Console.WriteLine($"crc of enc payld is: {ComputeChecksum(EncPld, null, EncPld.Length)}");

            data[POS_OF_CRC] = ComputeChecksum(data, Crc16table, data.Length);
            return data;
        }

        public byte[] PreparePingPacket(string ipep, bool relaible, bool expectReplyofPONG, string msg)
        {
            byte[] b = new byte[Overhead + 1 + msg.Trim().Length];
            FlashPeeer p;
            FClient.IpepAndPeer.TryGetValue(ipep, out p);
            if (!HeadWriter(ref b, (byte)Opfunctions.PingPong, ipep, relaible, p, 1))
            {
                return null;
            }

            if (expectReplyofPONG)
            {
                b[PayloadSTR] = 1;
            }
            else
            {
                b[PayloadSTR] = 0;
            }

            byte[] msddata = Encoding.UTF8.GetBytes(msg.Trim());

            Array.Copy(msddata, 0, b, PayloadSTR + 1, msddata.Length);

            b[CRC] = ComputeChecksum(b, Crc16table, b.Length);
            return b;
        }

        /// <summary>
        /// Calculates the checksum
        /// </summary>
        /// <param name="bytes">Data for checksum</param>
        /// <param name="table">Table for checksum</param>
        /// <returns>Returns the calculated checksum for the given data</returns>
        public byte ComputeChecksum(byte[] bytes, ushort[] table, int length)
        {
            ushort[] tab = table;
            if (table == null)
            {
                tab = Crc16table;
            }

            ushort crc = 0;
            for (int i = 0; i < length; ++i)
            {
                byte index = (byte)(crc ^ bytes[i]);
                crc = (ushort)((crc >> 8) ^ tab[index]);
            }
            return (byte)crc;
        }

        /// <summary>
        /// Checks the packets according to packet number for buffering future packets
        /// </summary>
        /// <param name="diffinPosition"></param>
        /// <param name="dno"></param>
        /// <param name="expdataNo"></param>
        /// <returns> <0 for old and invalid, 0 for exact and 0+diff for valid future packets</returns>
        public int PacketNoFilter(ushort dno, ushort OurExpectedNo, DateTime last)
        {
            double validDiff = DateTime.UtcNow.Subtract(last).TotalMilliseconds / tickMS;

            if (dno == 0 || last == null)
            {
                //error
                return -1;
            }

            if ((OurExpectedNo + MaxPackets) > UppB) //edge
            {
                if (dno < OurExpectedNo) // old or reset
                {
                    if (dno <= (MaxPackets - (UppB - OurExpectedNo))) // valid reset
                    {
                        int diff = (UppB - OurExpectedNo) + dno;
                        // return dno;
                        if (validDiff < diff) // like if 10th future packet sent in next 30 ms which is impossible
                        {
                            return -1;
                        }
                        else
                        {
                            return diff;
                        }
                    }
                    else
                    {
                        return -1;
                    }
                }
                else // newer
                {
                    int diff = dno - OurExpectedNo;
                    if (diff >= MaxPackets) // more future
                    {
                        return -1;
                    }
                    else
                    {

                        // return dno;
                        if (validDiff < diff) // like if 10th future packet sent in next 30 ms which is impossible
                        {
                            return -1;
                        }
                        else
                        {
                            return diff;
                        }
                    }
                }
            }
            else // not edge
            {
                if (dno < OurExpectedNo) //old
                {
                    return -1;
                }
                else // not old
                {
                    int diff = dno - OurExpectedNo;
                    if (diff >= MaxPackets) // more future
                    {
                        return -1;
                    }
                    else
                    {
                        // return dno;
                        if (validDiff < diff) // like if 10th future packet sent in next 30 ms which is impossible
                        {
                            return -1;
                        }
                        else
                        {
                            return diff;
                        }
                    }
                }
            }

        }

        public int PacketNoFilterUdp(ushort dno, DateTime packetTime, DateTime last)
        {
            var timeDiff = DateTime.UtcNow.Subtract(packetTime).TotalMilliseconds;
            //double validDiff = DateTime.UtcNow.Subtract(last).TotalMilliseconds / tickMS;

            if (last == null)
            {
                //error
                return -1;
            }

            if(timeDiff > 1.5 * 1000) // older than one and half a second so ignore
            {
                return -1;
            }

            if(packetTime < last)
            {
                return -1;
            }

            return dno;
        }

        /// <summary>
        /// checks if the data has came from reliable channel
        /// </summary>
        /// <param name="data"></param>
        /// <returns>return true if data came from reliable channel and vice versa</returns>
        public bool IsReliable(ref byte[] data)
        {
            if (data[STR] == (byte)Opfunctions.packetstrtunreliable)
            {
                return false;
            }
            else
            {
                return true;
            }
        }


        public readonly UInt16[] Crc16table ={0x0000, 0x1189, 0x2312, 0x329B, 0x4624, 0x57AD, 0x6536, 0x74BF,
    0x8C48, 0x9DC1, 0xAF5A, 0xBED3, 0xCA6C, 0xDBE5, 0xE97E, 0xF8F7,
    0x0919, 0x1890, 0x2A0B, 0x3B82, 0x4F3D, 0x5EB4, 0x6C2F, 0x7DA6,
    0x8551, 0x94D8, 0xA643, 0xB7CA, 0xC375, 0xD2FC, 0xE067, 0xF1EE,
    0x1232, 0x03BB, 0x3120, 0x20A9, 0x5416, 0x459F, 0x7704, 0x668D,
    0x9E7A, 0x8FF3, 0xBD68, 0xACE1, 0xD85E, 0xC9D7, 0xFB4C, 0xEAC5,
    0x1B2B, 0x0AA2, 0x3839, 0x29B0, 0x5D0F, 0x4C86, 0x7E1D, 0x6F94,
    0x9763, 0x86EA, 0xB471, 0xA5F8, 0xD147, 0xC0CE, 0xF255, 0xE3DC,
    0x2464, 0x35ED, 0x0776, 0x16FF, 0x6240, 0x73C9, 0x4152, 0x50DB,
    0xA82C, 0xB9A5, 0x8B3E, 0x9AB7, 0xEE08, 0xFF81, 0xCD1A, 0xDC93,
    0x2D7D, 0x3CF4, 0x0E6F, 0x1FE6, 0x6B59, 0x7AD0, 0x484B, 0x59C2,
    0xA135, 0xB0BC, 0x8227, 0x93AE, 0xE711, 0xF698, 0xC403, 0xD58A,
    0x3656, 0x27DF, 0x1544, 0x04CD, 0x7072, 0x61FB, 0x5360, 0x42E9,
    0xBA1E, 0xAB97, 0x990C, 0x8885, 0xFC3A, 0xEDB3, 0xDF28, 0xCEA1,
    0x3F4F, 0x2EC6, 0x1C5D, 0x0DD4, 0x796B, 0x68E2, 0x5A79, 0x4BF0,
    0xB307, 0xA28E, 0x9015, 0x819C, 0xF523, 0xE4AA, 0xD631, 0xC7B8,
    0x48C8, 0x5941, 0x6BDA, 0x7A53, 0x0EEC, 0x1F65, 0x2DFE, 0x3C77,
    0xC480, 0xD509, 0xE792, 0xF61B, 0x82A4, 0x932D, 0xA1B6, 0xB03F,
    0x41D1, 0x5058, 0x62C3, 0x734A, 0x07F5, 0x167C, 0x24E7, 0x356E,
    0xCD99, 0xDC10, 0xEE8B, 0xFF02, 0x8BBD, 0x9A34, 0xA8AF, 0xB926,
    0x5AFA, 0x4B73, 0x79E8, 0x6861, 0x1CDE, 0x0D57, 0x3FCC, 0x2E45,
    0xD6B2, 0xC73B, 0xF5A0, 0xE429, 0x9096, 0x811F, 0xB384, 0xA20D,
    0x53E3, 0x426A, 0x70F1, 0x6178, 0x15C7, 0x044E, 0x36D5, 0x275C,
    0xDFAB, 0xCE22, 0xFCB9, 0xED30, 0x998F, 0x8806, 0xBA9D, 0xAB14,
    0x6CAC, 0x7D25, 0x4FBE, 0x5E37, 0x2A88, 0x3B01, 0x099A, 0x1813,
    0xE0E4, 0xF16D, 0xC3F6, 0xD27F, 0xA6C0, 0xB749, 0x85D2, 0x945B,
    0x65B5, 0x743C, 0x46A7, 0x572E, 0x2391, 0x3218, 0x0083, 0x110A,
    0xE9FD, 0xF874, 0xCAEF, 0xDB66, 0xAFD9, 0xBE50, 0x8CCB, 0x9D42,
    0x7E9E, 0x6F17, 0x5D8C, 0x4C05, 0x38BA, 0x2933, 0x1BA8, 0x0A21,
    0xF2D6, 0xE35F, 0xD1C4, 0xC04D, 0xB4F2, 0xA57B, 0x97E0, 0x8669,
    0x7787, 0x660E, 0x5495, 0x451C, 0x31A3, 0x202A, 0x12B1, 0x0338,
    0xFBCF, 0xEA46, 0xD8DD, 0xC954, 0xBDEB, 0xAC62, 0x9EF9, 0x8F70
    };
    }
}
