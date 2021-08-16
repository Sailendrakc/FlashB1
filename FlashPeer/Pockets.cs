using System;
using System.Collections.Generic;
using System.Text;

namespace FlashPeer
{
    public class Pockets
    {
        public int strIndex { get; private set; }

        public int Length { get; private set; }
        public int Opcode { get; private set; }

        public readonly byte[] data;

        public Pockets(byte[] bigdata, int starting, int length, int opcode )
        {
            data = bigdata;
            strIndex = starting;
            Length = length;
            Opcode = opcode;
        }
    }
}
