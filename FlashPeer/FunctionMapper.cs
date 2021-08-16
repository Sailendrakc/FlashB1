using FlashPeer.interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace FlashPeer
{
    public class FunctionMapper
    {
        public void onHello(Pockets request, IPEndPoint ipep)
        {
            var peer1 = new FlashPeeer(true, ipep);
            bool b1 = FlashProtocol.Instance.crypto.HelloUnpacker(request.data, peer1);

            if (b1 == false)
            {
                return;
            }

            lock (FlashProtocol.Instance.Connectings)
            {
                FlashProtocol.Instance.Connectings.Add(ipep.ToString(), peer1);
                FlashProtocol.Instance.Current_Peers++;
            }

            peer1.HelloReply();
        }

        public void onKeepAlive(Pockets request, IFlashPeer client)
        {

        }

        public void onAck(ushort RecExpNo, byte[] Ackfields)
        {

        }
    }
}
