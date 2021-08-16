using FlashPeer.interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Timers;

namespace FlashPeer
{
    public class FlashPeer : IFlashPeer
    {
        public ushort OurExpectedTcpNo { get; set; } = 0;
        public ushort OurExpectedUdpNo { get; set; }
        public DateTime BaseDateTime { get; set; } = DateTime.UtcNow;
        public long DifferenceTicks { get; set; }
        public DateTime UdpDateTime { get; set; } = DateTime.UtcNow;
        public IPEndPoint endpoint { get; set; }

        public ClientHandshake BeginHandshake;

        public ServerHandshake ReplyHandshake;

        public FlashPeer(IPEndPoint ep)
        {
            endpoint = ep;
        }

        public byte[] GetAckFieldForSending(int bitlength)
        {
            throw new NotImplementedException();
        }

        public ushort GetAndIncDNO(int increaseBy)
        {
            throw new NotImplementedException();
        }

        //handshake

        public void NullifyShakeAndRemoveFromConnectings(bool asServer)
        {
            if (asServer)
            {
                ReplyHandshake = null;
            }
            else
            {
                BeginHandshake = null;
            }

            FlashProtocol.Instance.RemovePeer(endpoint.ToString(), false);
        }

        public DateTime GetLastDateTime(bool forTcp)
        {
            throw new NotImplementedException();
        }

        
        public void HelloReply()
        {
            ReplyHandshake = new ServerHandshake(this);
        }

        //other
        public void SendData(byte[] data)
        {
            FlashProtocol.Instance.channel.StartSendingData(data, this.endpoint);
        }

        public void SetLastDateTime(bool forTcp, DateTime dt)
        {
            if (!forTcp)
            {
                UdpDateTime = dt;
            }
        }


    }
}
