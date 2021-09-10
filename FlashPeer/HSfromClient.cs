using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Timers;

namespace FlashPeer
{
    public class HSfromClient
    {
        public FlashPeer Speer;
        Timer timer = new Timer();
        Timer endtimer = new Timer();
        private int nth = 1;

        private long? diffticks = null;
        private bool isconnected = false;

        public HSfromClient(FlashPeer server)
        {
            Speer = server;
            StartHandShake();
        }

        private void StartHandShake()
        {
            if (!SendHello())
            {
                Console.WriteLine("Unable to send hello.");
                return;
            }

            nth++;
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = false;

            endtimer.Elapsed += Endtimer_Elapsed;
            endtimer.AutoReset = false;
            endtimer.Interval = 6000;
            endtimer.Enabled = true;
            endtimer.Start();
            //send three packets according to timer.

            //wait for closing packet

            //transfer the server peer to connection.
        }

        private void Endtimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            endtimer.Stop();
            endtimer.Close();
            endtimer.Dispose();

            timer.Stop();
            timer.Close();
            timer.Dispose();

            if (isconnected)
            {
                FlashProtocol.Instance.ConnectionPlugin.OnConnectionConfirmed(Speer.endpoint.ToString(), false);
            }
            else
            {
                FlashProtocol.Instance.ConnectionPlugin.RemoveFromPendingConnections(Speer.endpoint.ToString(), false, true);
            }
        }

        public void AfterHelloReply(long tiks)
        {
            //send first h1 immediately
            if (!SendHello())
            {
                Console.WriteLine("Unable to send hello.");
                Endtimer_Elapsed(null, null);
            }
            nth++;
            timer.Interval = 100;
            timer.Start();

            //set base time to now.
            Speer.BaseDateTime = new DateTime(tiks);
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (nth == 3)
            {
                if (!SendHello())
                {
                    Console.WriteLine("Unable to send hello.");
                    Endtimer_Elapsed(null, null);
                    return;
                }
                nth++;
                timer.Interval = 200;
                timer.Start();
            }

            if (nth == 4)
            {
                if (!SendHello())
                {
                    Console.WriteLine("Unable to send hello.");
                    Endtimer_Elapsed(null, null);
                    return;
                }
                nth++;
            }
        }

        public void AfterHelloClosing(long ticks)
        {
            //get the diff and store somewhere.
            //diff ticks are ticks of difference.
            //add these ticks to utcnow for pretty accurate server utcnow
            //save utc difference and end.
            if (diffticks != null)
            {
                Speer.DifferenceTimespan = new TimeSpan((long)diffticks);
                isconnected = true;
            }

            Endtimer_Elapsed(null, null);
        }

        public bool SendHello() //unreliable in packet format
        {
            if (nth > ) { return false; }

            byte[] data;

            if (nth == 1)
            {
                //aes for sending
                byte[] aesKeys = FlashProtocol.Instance.GetAESKey();
                byte[] aesIv = FlashProtocol.Instance.GetAESIV();

                //copy aes
                byte[] aesparms = new byte[aesKeys.Length + aesIv.Length]; //48
                Array.Copy(aesKeys, 0, aesparms, 0, aesKeys.Length); //32
                Array.Copy(aesIv, 0, aesparms, aesKeys.Length, aesIv.Length); //16

                //encrypt it 
                byte[] EncPld = FlashProtocol.Instance.RSAEncrypt(ref aesparms);

                //prepare hello packet
                data = new byte[EncPld.Length + PacketSerializer.MinLenOfPacket];

                Array.Copy(EncPld, 0, data, PacketSerializer.PayloadSTR, EncPld.Length);
                Array.Copy(BitConverter.GetBytes((ushort)(EncPld.Length + 4)), 0, data, PacketSerializer.POS_OF_LEN, PacketSerializer.LEN_OF_LEN);
                Array.Copy(BitConverter.GetBytes((ushort)(Opfunctions.Hello)), 0, data, PacketSerializer.POS_OF_OPCODE, PacketSerializer.LEN_OF_OPCODE);

                if (!FlashProtocol.Instance.Pmaker.HeadWriter(ref data, false, Speer))
                {
                    FlashProtocol.Instance.RaiseOtherEvent("Headwriting error in Flashprotocol.SendHello()", null, EventType.ConsoleMessage, null);
                    return false;
                }

                Speer.SendData(data);
                return true;
            }

            Opfunctions opcode = Opfunctions.Hello2;
            if (nth == 2)
            {
                opcode = Opfunctions.Hello2;
            }

            if (nth == 3)
            {
                opcode = Opfunctions.Hello3;
            }

            if (nth == 4)
            {
                opcode = Opfunctions.Hello4;
            }

            data = new byte[PacketSerializer.MinLenOfPacket + 8];

            Array.Copy(BitConverter.GetBytes((ushort)8), 0, data, PacketSerializer.POS_OF_LEN, PacketSerializer.LEN_OF_LEN);
            Array.Copy(BitConverter.GetBytes((ushort)(opcode)), 0, data, PacketSerializer.POS_OF_OPCODE, PacketSerializer.LEN_OF_OPCODE);
            Array.Copy(BitConverter.GetBytes((long)DateTime.UtcNow.Ticks), 0, data, PacketSerializer.PayloadSTR, 8);

            if (!FlashProtocol.Instance.Pmaker.HeadWriter(ref data, false, Speer))
            {
                FlashProtocol.Instance.RaiseOtherEvent("Headwriting error in Flashprotocol.SendHello()", null, EventType.ConsoleMessage, null);
                return false;
            }

            Speer.SendData(data);
            return true;

            //todo also wait for closing reply.
        }
    }
}
