using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FlashB1
{
    /// <summary>
    /// This interface is returned by data listners to the high or app level program.
    /// </summary>
    public interface IArgObject
    {
        public byte[] GetRawData();

        public IPEndPoint GetClient();

        public void ReturnToPool();

        public int getBytesTransferred();

    }
}
