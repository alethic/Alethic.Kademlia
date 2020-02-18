using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Cogito.Kademlia.Core
{

    public static class SocketTaskExtensions
    {

#if NET47A

        public static Task<int> SendToAsync(
               this Socket socket,
               ArraySegment<byte> buffer,
               SocketFlags socketFlags,
               EndPoint remoteEP)
        {
            return Task<int>.Factory.FromAsync(
                (targetBuffer, flags, endPoint, callback, state) => ((Socket)state).BeginSendTo(
                                                                        targetBuffer.Array,
                                                                        targetBuffer.Offset,
                                                                        targetBuffer.Count,
                                                                        flags,
                                                                        endPoint,
                                                                        callback,
                                                                        state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndSendTo(asyncResult),
                buffer,
                socketFlags,
                remoteEP,
                state: socket);
        }

#endif

    }

}
