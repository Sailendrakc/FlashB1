using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace FlashB1
{
    public abstract class IBaseChannel<Setting>
    {
        public event EventHandler<IArgObject> RecCompleteEvent;

        public void SendInvoke(object sender, bool s)
        {
            if (SendingCompleteEvent == null)
            {
                return;
            }
            SendingCompleteEvent.Invoke(sender, s);
        }

        public void RecInvoke(object sender, IArgObject s)
        {
            if (RecCompleteEvent == null)
            {
                return;
            }
            RecCompleteEvent.Invoke(sender, s);
        }

        internal abstract void StartRecevingData(Setting setting);

        public abstract void StartSendingData(byte[] dataToSend, IPEndPoint clientToSendTo);

        public abstract void StartListener();

        public abstract void CloseListener();
    }
}
