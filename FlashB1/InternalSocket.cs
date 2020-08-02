using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace FlashB1
{
    internal class InternalSockets
    {
        private static int portno; //official

        private SocketObj WorkingSocket = null;
        public ArgPool Pool;

        public InternalSockets(bool isClient, int port1, ArgPool pool)
        {
            if (isClient)
            {
                portno = 0;
            }
            else
            {
                portno = port1;
            }

            this.Pool = pool;
            MakeSocket();
        }

        private void MakeSocket()
        {
            WorkingSocket = new SocketObj(portno, false);

            //fill objects in pool
            //max sending
            for (int i = 0; i < 1; i++)
            {
                SocketArgObj s = new SocketArgObj(new Token(Pool.RecBuffer));
                s.GetToken().isSend = true;
                s.Completed += Send_completed;
                Pool.SetSendObjects(s);
            }

            //max receving
            for (int i = 0; i < 1; i++)
            {
                SocketArgObj s = new SocketArgObj(new Token(Pool.RecBuffer));
                s.GetToken().isSend = false;
                s.Completed += rec_Completed;
                Pool.SetRecObjects(s);
            }

            StartReceive(this.GetSocketObject(false));
        }

        public SocketArgObj GetSocketObject(bool forSending)
        {
            SocketArgObj s;
            if (forSending)
            {
                s = Pool.GetSendObject();
                if (s == null)
                {
                    s = new SocketArgObj(new Token(Pool.RecBuffer));
                    s.GetToken().isSend = true;
                    s.Completed += Send_completed;
                }
            }
            else
            {
                s = Pool.GetRecObject();
                if (s == null)
                {
                    s = new SocketArgObj(new Token(Pool.RecBuffer));
                    s.GetToken().isSend = false;
                    s.Completed += rec_Completed;
                }
            }

            if (s.GetToken().beingUsed)
            {
                Console.WriteLine($"PROBLEM!! recArg count- {Pool.RecCount} sendCount - {Pool.SendCount} -------------");
            }

            return s;
        }

        public void EraseLocalSockets()
        {
            WorkingSocket.isFunctional = false;
        }

        public bool StartReceive(SocketArgObj s)
        {
            if (s == null || !WorkingSocket.isFunctional || s.GetToken().isSend) { return false; }
            if (s.GetToken().beingUsed)
            {
                s = GetSocketObject(false);
            }
            s.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                bool isPending = WorkingSocket.ReceiveFromAsync(s);
                s.GetToken().beingUsed = isPending;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Fatal Error: Cannot Receive [startReceive()] : {e.ToString()}");
                WorkingSocket.isFunctional = false;
                return false;
            }

            if (!s.GetToken().beingUsed)
            {
                rec_Completed(null, s);
            }
            return true;
        }

        public bool SendAsync(SocketArgObj s)
        {
            if (isFunctional())
            {
                try
                {
                    bool pending = WorkingSocket.SendToAsync(s);
                    s.GetToken().beingUsed = pending;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error sending data [internalSocket.SendAsync] :" + e.ToString());
                    WorkingSocket.isFunctional = false;
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        internal void rec_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (!WorkingSocket.isFunctional)
            {
                return;
            }

            StartReceive(GetSocketObject(false));

            if (e.SocketError != SocketError.Success || e.BytesTransferred == 0)
            {
                Pool.SetRecObjects((SocketArgObj)e);
                return;
            }

            IOevents.RecInvoke(null, (SocketArgObj)e);
        }

        internal void Send_completed(object sender, SocketAsyncEventArgs e)
        {
            if (!WorkingSocket.isFunctional)
            {
                return;
            }

            /*SocketArgObj s = (SocketArgObj)e;
            s.GetToken().beingUsed = false;
            Pool.SetSendObjects(s);*/
        }

        public bool isFunctional()
        {
            return WorkingSocket.isFunctional;
        }

        public IPEndPoint GetLocalIPEP()
        {
            return (IPEndPoint)WorkingSocket.LocalEndPoint;
        }
    }
}
