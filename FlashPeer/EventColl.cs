using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashPeer
{
    public class EventColl
    {
        public string Message = null;
        public string Stacktrace = null;
        public object[] obj = new object[2];
        public EventType Eventtype;

    }
}
