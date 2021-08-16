using FlashB1;
using FlashPeer.interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlashPeer
{
    public class PacketDeserializer
    {
        public Header ReadPacket(IArgObject packet)
        {
            Header ii = new Header();
            var toread = packet.GetRawData();

            ii.SentTimes = toread[0];
            ii.PacketNo = BitConverter.ToUInt16(toread, 1);
            ii.ExpRecivingNo = BitConverter.ToUInt16(toread, 3);
            
            
            if(ii.SentTimes > 5)
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
            for(int i = PacketManager.Overhead; i<len;)
            {            
                var lenofcurrent = BitConverter.ToUInt16(packet.GetRawData(), i);
                var pic = new Pockets(packet.GetRawData(), i, lenofcurrent, packet.GetRawData()[i + 2]);
                ii.AllPicklets[counter] = pic;
                counter++;
                i += lenofcurrent;
            }

            return ii;
        }
    }

    public class PacketSerializer
    {
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
        readonly public static int POS_OF_ACKFIELDS = 3; // 3.4.5
        readonly public static int LEN_OF_ACKFIELDS = 3; //24 bits
        //OR
        readonly public static int POS_OF_DATE = 3; // 3.4.5
        readonly public static int LEN_OF_DATE = 3;

        readonly public static int POS_OF_DNO = 6; // 6.7
        readonly public static int LEN_OF_DNO = 2;

        readonly public static int POS_OF_CRC = 8; // 8

        readonly public static int POS_OF_LEN = 9; //9 , 10
        readonly public static int LEN_OF_LEN = 2;

        readonly public static int POS_OF_OPCODE = 9; //11.12
        readonly public static int LEN_OF_OPCODE = 2;

        readonly public static int PayloadSTR = 13;

        readonly public static int MinLenOfPacket = 14; // coz indexing started from 0
                                                  
        public bool HeadWriter(ref byte[] data, bool isReliable, IFlashPeer destination)
        {
            if(data.Length <= MinLenOfPacket)
            {
                return false;
            }

            if (isReliable)
            {
                //it denotes reliablity and nth time the packet is sent.
                data[POS_OF_STR] = 1;

                //writing our expected Tcp number, (a form of ack)
                Array.Copy(BitConverter.GetBytes((ushort)destination.OurExpectedTcpNo), 0, data, POS_OF_EXPECTED_REC_DNO, LEN_OF_EXPECTED_REC_DNO);

                //writing the fields that represents ack for fast received packets.
                Array.Copy(destination.GetAckFieldForSending(24), 0, data, POS_OF_ACKFIELDS , LEN_OF_ACKFIELDS);

                //Writing DNO for this data.
                Array.Copy(BitConverter.GetBytes((ushort)destination.GetAndIncDNO(1)), 0, data, POS_OF_DNO, LEN_OF_DNO);
            }
            else
            {
                //it denotes un-reliablity.
                data[POS_OF_STR] = 0;

                //writing our expected Tcp number, (a form of ack)
                Array.Copy(BitConverter.GetBytes((ushort)destination.OurExpectedTcpNo), 0, data, POS_OF_EXPECTED_REC_DNO, LEN_OF_EXPECTED_REC_DNO);

                //writing ticks or date.
                var ticks = (DateTime.UtcNow.Subtract(destination.BaseDateTime).TotalMilliseconds);
                if(ticks > uint.MaxValue)
                {
                    //time to reset, extremely unlikely. Player have to play continuously ffor straight four server years for this.
                    Console.WriteLine("Something is wrong!! the ticks is larger than uint.maxvalue");
                    return false;
                }
                else
                {
                    Array.Copy(BitConverter.GetBytes((uint)ticks), 0, data, POS_OF_DATE, LEN_OF_DATE);
                    data[7] = 0; //TODO UNUSED
                }
            }

            //zero out for crc.
            data[POS_OF_CRC] = ComputeChecksum(data, Crc16table, data.Length);

            return true;
        }

        /// <summary>
        /// Calculates the checksum
        /// </summary>
        /// <param name="bytes">Data for checksum</param>
        /// <param name="table">Table for checksum</param>
        /// <param name="length">Length of data</param>
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
