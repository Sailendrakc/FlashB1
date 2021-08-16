using System;
using System.Collections.Generic;
using System.Text;
using FlashB1;

namespace FlashPeer.interfaces
{
    interface IFlashProtocol
    {
        /// <summary>
        /// This stores all the connections
        /// </summary>
        Dictionary<string, IFlashProtocol> Connections { get; set; }

        /// <summary>
        /// First step in protocol is to identify if the request if om existing client or different client.
        /// Then if the the request is not from existing client, is it random request or a handshake (hello) request.
        /// </summary>
        /// <param name="ExistingClient">Method to call if it is existing client</param>
        /// <param name="NewRequest">Method to call if it is hello from new user.</param>
        void IdentifyRequest(Action<IFlashPeer, IArgObject> ExistingClient, Action<IArgObject> NewRequest, IArgObject actualRequest);
        /// <summary>
        /// Converts the byte[] to a object that represets a request.
        /// </summary>
        /// <param name="onInternalRequsest"></param>
        /// <param name="onExternalRequest"></param>
        void DeSerializeAndMapRequest(Action<IInternalRequest> onInternalRequsest, Action<IExternalRequest> onExternalRequest, IArgObject actualRequest);

        
        void ProcessHelloRequest(IArgObject helloRequest);
    }
}
