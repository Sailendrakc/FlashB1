using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace FlashPeer
{
    public class Header
    {
        public byte SentTimes { get; set; }
        public int PacketNo { get; set; }  //2

        public int ExpRecivingNo { get; set; }//2

        public byte[] AckFields { get; set; } = new byte[3]; //3 or
        public int DateForUDP { get; set; } //3 or

        public bool isReliable = false;

        public FlashPeer peer;

        public Pockets[] AllPicklets;
    }
}
