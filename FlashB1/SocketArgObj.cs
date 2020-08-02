using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;

namespace FlashB1
{
    public class SocketArgObj : SocketAsyncEventArgs
    {
        public SocketArgObj(Token token)
        {
            base.UserToken = token;
            base.SetBuffer(((Token)UserToken).Data, 0, token.Data.Length);
        }

        public Token GetToken()
        {
            return (Token)base.UserToken;
        }
    }

    public class Token
    {
        public bool beingUsed = false;
        public byte[] Data;
        public bool isSend = true;

        public Token(ushort BuffSize)
        {
            Data = new byte[BuffSize];
        }
    }
}
