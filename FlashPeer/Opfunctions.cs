using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashPeer
{
    public enum Opfunctions
    {
        send1 = 2,
        send2 = 3,
        packetstart = 1,
        packetstrtunreliable = 4,

        byebye = 5,
        ack = 6,
        keepalive = 7,
        Hello = 8,
        Handshake = 9,
        PingPong = 10,
        Split = 11,
        HelloClose = 12,
        Hello2 = 13,
        Hello3=14,
        Hello4 = 15,
    }

    public enum EventType
    {
        ConsoleMessage = 1,
        CodeError = 2,
        ConnectionReset = 3,
        ConnectedToServer = 4,
        HeadwritingError = 5,
        cryptography = 6,
        SocketBindError = 7,
        DataCorrupt = 8,
        PingPong = 9,
        ValueError = 10,
        NullError = 11,
        SocketIOError = 12,
        SocketEvent = 13,
    }
}
