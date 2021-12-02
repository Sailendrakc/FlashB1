using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace FlashB1
{
    /// <summary>
    /// This is the base class that should be inherited by any type of listner or sender of data (Channel),
    /// For eg, UDP, TCP or Websockets etc.
    /// </summary>
    /// <typeparam name="Setting">Object with info on listening and sending data.</typeparam>
    public abstract class IBaseChannel<Setting>
    {
        public event EventHandler<IArgObject> RecCompleteEvent;

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
