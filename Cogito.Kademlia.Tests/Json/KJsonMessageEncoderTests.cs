using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Kademlia.Core;
using Cogito.Kademlia.Json;
using Cogito.Kademlia.Network;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cogito.Kademlia.Tests.Json
{

    [TestClass]
    public partial class KJsonMessageEncoderTests
    {

        [TestMethod]
        public void Can_serialize_ping_request()
        {
            var sender = KNodeId.Create<KNodeId32>();
            var protocol = new TestIpProtocol<KNodeId32>();
            var buffer = new ArrayBufferWriter<byte>();

            var encoder = new KJsonMessageEncoder<KNodeId32>();
            var decoder = new KJsonMessageDecoder<KNodeId32>();

            var ipv4 = protocol.CreateEndpoint(new KIpEndpoint(new KIp4Address(IPAddress.Parse("1.1.1.1")), 123));
            var ipv6 = protocol.CreateEndpoint(new KIpEndpoint(new KIp6Address(IPAddress.Parse("::1")), 123));
            var message = new KMessage<KNodeId32, KPingRequest<KNodeId32>>(new KMessageHeader<KNodeId32>(sender, 1), new KPingRequest<KNodeId32>(new IKProtocolEndpoint<KNodeId32>[] { ipv4, ipv6 }));
            var sequence = new KMessageSequence<KNodeId32>(1, new IKMessage<KNodeId32>[] { message });

            encoder.Encode(protocol, buffer, sequence);
            var json = Encoding.UTF8.GetString(buffer.WrittenSpan.ToArray());

            var sequence2 = decoder.Decode(protocol, new ReadOnlySequence<byte>(buffer.WrittenMemory));
            sequence2.Equals(sequence).Should().BeTrue();
        }

    }

}
