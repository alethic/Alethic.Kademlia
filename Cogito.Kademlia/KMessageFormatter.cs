//using System;
//using System.Buffers;
//using System.Collections.Generic;
//using System.Linq;

//namespace Cogito.Kademlia
//{

//    /// <summary>
//    /// Provides access to encode or decode a message sequence.
//    /// </summary>
//    /// <typeparam name="TNodeId"></typeparam>
//    public class KMessageFormatter<TNodeId>
//        where TNodeId : unmanaged
//    {

//        readonly IEnumerable<IKMessageFormat<TNodeId>> formats;

//        /// <summary>
//        /// Initializes a new instance.
//        /// </summary>
//        /// <param name="encoders"></param>
//        public KMessageFormatter(IEnumerable<IKMessageFormat<TNodeId>> formats)
//        {
//            this.formats = formats ?? throw new ArgumentNullException(nameof(formats));
//        }

//        /// <summary>
//        /// Gets the set of available message formats.
//        /// </summary>
//        public IEnumerable<IKMessageFormat<TNodeId>> Formats => formats;

//        /// <summary>
//        /// Gets the default message format.
//        /// </summary>
//        public IKMessageFormat<TNodeId> DefaultFormat => formats.First();

//        /// <summary>
//        /// Encodes the sequence of messages into the buffer.
//        /// </summary>
//        /// <param name="context"></param>
//        /// <param name="messages"></param>
//        /// <param name="buffer"></param>
//        /// <returns></returns>
//        public void Encode(IKMessageContext<TNodeId> context, IBufferWriter<byte> buffer, KMessageSequence<TNodeId> messages)
//        {
//            foreach (var accept in context.Target.Accepts)
//            {
//                var encoder = formats.FirstOrDefault(i => i.ContentType == accept);
//                if (encoder != null)
//                {
//                    encoder.Encode(context, buffer, messages);
//                    return;
//                }
//            }

//            throw new KProtocolException(KProtocolError.Invalid, $"No format available for '{context.Target.Accepts}'.");
//        }

//        /// <summary>
//        /// Decodes the sequence of messages from the buffer.
//        /// </summary>
//        /// <param name="context"></param>
//        /// <param name="buffer"></param>
//        /// <returns></returns>
//        public KMessageSequence<TNodeId> Decode(IKMessageContext<TNodeId> context, ReadOnlySequence<byte> buffer)
//        {
//            foreach (var accept in context.Sender.Accepts)
//            {
//                var decoder = formats.FirstOrDefault(i => i.ContentType == accept);
//                if (decoder != null)
//                    return decoder.Decode(context, buffer);
//            }

//            throw new KProtocolException(KProtocolError.Invalid, $"No format available for '{context.Target.Accepts}'.");
//        }

//    }

//}
