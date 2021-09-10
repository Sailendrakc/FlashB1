using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Fleck;

namespace FlashB1
{
    public class WebSocketListener
    {
        private webSocketObject WebsocketServer = null;
        static EventHandler<IArgObject> WebSocketRecEvent;

        public WebSocketListener(WebSocketCard connSetting)
        {
            WebsocketServer = new webSocketObject(connSetting.portNo);
        }

        public void CloseListener()
        {
            throw new NotImplementedException();
        }

        public void StartListener()
        {
            WebsocketServer.WebSocketServerObj.Start((websockconn) => {
                websockconn.OnBinary += InvokeReceiveBinary;
                websockconn.OnPing += Pong;
                websockconn.OnError += WebSocketError;
            });
        }

        private void WebSocketError(Exception e)
        {
            //shoutdown

            Console.WriteLine("There was error in websocket: " + e.ToString());
        }

        private void Pong(byte[] pingdata)
        {
            //echo message
            
        }

        private void InvokeReceiveBinary(byte[] data)
        {
            var webdata = new webSocketData(data);
            WebSocketRecEvent.Invoke(null, webdata);
        }

        internal void StartSendingData(byte[] dataToSend, IPEndPoint clientToSendTo)
        {
            
        }
    }

    public class webSocketData : IArgObject
    {
        public byte[] data { get; private set; }

        public webSocketData(byte[] dat)
        {
            data = dat;
        }

        public IPEndPoint GetClient()
        {
            throw new NotImplementedException();
        }

        public byte[] GetRawData()
        {
            return data;
        }

        public void ReturnToPool()
        {
            throw new NotImplementedException();
        }

        public int getBytesTransferred()
        {
            throw new NotImplementedException();
        }
    }

    public class webSocketObject
    {
        internal WebSocketServer WebSocketServerObj;
        internal IPEndPoint localEndPoint;
        public webSocketObject(ushort portNo)
        {
            localEndPoint = new IPEndPoint(GetIPv4.GetIpV4(), portNo);
            var localep = "ws:" + localEndPoint.ToString();
            WebSocketServerObj = new WebSocketServer(localep);
        }
    }

    public class WebSocketCard
    {
        public ushort portNo;
    }
}
