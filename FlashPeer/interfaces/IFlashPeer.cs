using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace FlashPeer.interfaces
{
    public interface IFlashPeer
    {
        public  IPEndPoint endpoint { get; set; }

        public ushort OurExpectedTcpNo { get; set; }

        public DateTime UdpDateTime { get; set; }

        public DateTime BaseDateTime { get; set; }

        public long DifferenceTicks { get; set; }

        public DateTime GetLastDateTime(bool forTcp);

        public void SetLastDateTime(bool forTcp, DateTime dt);

        public byte[] GetAckFieldForSending(int bitlength);

        public ushort GetAndIncDNO(int increaseBy);

        public void SendData(byte[] data);

        public void HelloReply();

    }
}
