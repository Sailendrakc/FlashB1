using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace FlashB1
{
    public static class IOevents
    {
        // any async eventhandlers??
        public static event EventHandler<SocketArgObj> SendingCompleteEvent;
        public static event EventHandler<SocketArgObj> RecCompleteEvent;
        public static void SendInvoke(object sender, SocketArgObj s)
        {
            if (SendingCompleteEvent == null)
            {
                return;
            }
            SendingCompleteEvent.Invoke(sender, s);
        }

        public static void RecInvoke(object sender, SocketArgObj s)
        {
            if (RecCompleteEvent == null)
            {
                return;
            }
            RecCompleteEvent.Invoke(sender, s);
        }
    }
}
