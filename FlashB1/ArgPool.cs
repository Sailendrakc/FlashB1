using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace FlashB1
{
    public class ArgPool
    {
        public ushort maxRec;
        public ushort maxSend;
        public ushort SendCount = 0;
        public ushort RecCount = 0;
        public ushort RecBuffer;
        //public ConcurrentStack<T> Collection = new ConcurrentStack<T>();

        private Queue<SocketArgObj> Recpool;
        private Queue<SocketArgObj> Sendpool;

        public ArgPool(ushort maxsend, ushort maxrec, ushort Recbuffer)
        {
            this.maxSend = maxsend;
            this.maxRec = maxrec;
            this.RecBuffer = Recbuffer;

            Recpool = new Queue<SocketArgObj>(maxRec);
            Sendpool = new Queue<SocketArgObj>(maxSend);

        }

        /// <summary>
        /// Gets socketARgObjfrom collection if availabe, else returns new obj
        /// </summary>
        /// <returns>socketARgObj from the colleciton.</returns>
        public SocketArgObj GetRecObject()
        {
            lock (Recpool)
            {
                if (Recpool.Count > 1)
                {
                    RecCount--;
                    SocketArgObj i = Recpool.Dequeue();
                    return i;
                }
                else
                {
                    return null;
                }
            }
        }

        public SocketArgObj GetSendObject()
        {
            lock (Sendpool)
            {
                if (Sendpool.Count > 1)
                {
                    SendCount--;
                    return Sendpool.Dequeue();
                }
                else
                {
                    return null;
                }
            }
        }

        public bool SetRecObjects(SocketArgObj obj)
        {
            lock (Recpool)
            {
                if (RecCount < maxRec && obj != null)
                {
                    Recpool.Enqueue(obj);
                    RecCount++;
                    return true;
                }
                else
                {
                    //pool full
                    return false;
                }
            }
        }

        public bool SetSendObjects(SocketArgObj obj)
        {
            lock (Sendpool)
            {
                if (SendCount < maxSend && obj != null)
                {
                    Sendpool.Enqueue(obj);
                    SendCount++;
                    return true;
                }
                else
                {
                    //pool full
                    return false;
                }
            }
        }
    }
}
