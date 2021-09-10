using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Timers;

namespace FlashPeer
{
    public class HSfromServer
    {
        private Stopwatch sw = new Stopwatch();

        private TimeSpan d1, d2, d3;
        private Timer aTimer;
        private int th = 0;

        public FlashPeer Ipeer;
        private bool isConnected = false;

        public HSfromServer(FlashPeer ipeer)
        {
            Ipeer = ipeer;

            SenHi();

            sw.Start();
            // Create a timer and set a two second interval.
            aTimer = new Timer();
            aTimer.Interval = 3000;

            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnNoResponse;

            // Have the timer fire repeated events (true is the default)
            aTimer.AutoReset = false;

            // Start the timer
            aTimer.Enabled = true;

            aTimer.Start();
        }

        private void SenHi()
        {
            byte[] data = new byte[PacketSerializer.MinLenOfPacket+8];
            Array.Copy(BitConverter.GetBytes((ushort)Opfunctions.Handshake), 0, data, PacketSerializer.POS_OF_OPCODE, 2);
            Ipeer.BaseDateTime = DateTime.UtcNow;
            //copy base date time.
            Array.Copy(BitConverter.GetBytes(Ipeer.BaseDateTime.Ticks), 0, data, PacketSerializer.PayloadSTR, 8);
            //copy len
            Array.Copy(BitConverter.GetBytes((ushort)8), 0, data, PacketSerializer.POS_OF_LEN, 2); 

            if (!(FlashProtocol.Instance.Pmaker.HeadWriter(ref data, false, Ipeer)))
            {
                return;
            }
            Ipeer.SendData(data);
        }

        private void OnNoResponse(object sender, ElapsedEventArgs e)
        {
            //close all timers.
            aTimer.Stop();
            aTimer.Close();
            aTimer.Dispose();

            sw.Stop();

            if (!isConnected)
            {
                //failed
                FlashProtocol.Instance.ConnectionPlugin.RemoveFromPendingConnections(Ipeer.endpoint.ToString(), true, true);
            }

        }

        public void t_Received(long ticks)
        {
            var dt = new DateTime(ticks);
            th++;
            if (th == 1)
            {
                var t1 = sw.ElapsedMilliseconds / 2;
                sw.Restart();
                d1 = DateTime.UtcNow - (dt.AddMilliseconds(t1));
                return;
            }

            if (th == 2)
            {
                var t2 = sw.ElapsedMilliseconds - 105;
                sw.Restart();
                d2 = DateTime.UtcNow - (dt.AddMilliseconds(t2));
                return;
            }

            if (th == 3)
            {
                var t3 = sw.ElapsedMilliseconds - 205;
                d3 = DateTime.UtcNow - (dt.AddMilliseconds(t3));
                sw.Stop();
                CalculateDeltaDate();
                return;
            }
        }

        private void CalculateDeltaDate()
        {
            var ddate = (((d1 + d2 + d3).TotalMilliseconds) / 3);
            //send the retdelta
            HelloClose(ddate);
            Ipeer.connected = true;
            OnNoResponse(null, null);
        }

        private void HelloClose(double diff)
        {
            try
            {
                long ldiff = Convert.ToInt64(diff);
                byte[] data = new byte[PacketSerializer.MinLenOfPacket + 8];
                Array.Copy(BitConverter.GetBytes((ushort)Opfunctions.HelloClose), 0, data, PacketSerializer.POS_OF_OPCODE, 2);
                Array.Copy(BitConverter.GetBytes(ldiff), 0, data, PacketSerializer.PayloadSTR, 8);
                Array.Copy(BitConverter.GetBytes((ushort)8), 0, data, PacketSerializer.POS_OF_LEN, 2);

                if (!(FlashProtocol.Instance.Pmaker.HeadWriter(ref data, false, Ipeer)))
                {
                    return;
                }

                //first transfer and confirm space. 
                if (!FlashProtocol.Instance.ConnectionPlugin.OnConnectionConfirmed(Ipeer.endpoint.ToString(), true))
                {
                    return;
                }

                //now send it
                isConnected = true;
                Ipeer.SendData(data);
                Ipeer.DifferenceTimespan = new TimeSpan(ldiff);

                OnNoResponse(null, null);
            }
            catch (Exception)
            {
                Console.WriteLine("Error closing the hello (returning diff ticks)as server");
                OnNoResponse(null, null);
            }
        }
    }
}
