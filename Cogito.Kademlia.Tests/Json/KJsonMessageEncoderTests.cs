using System;
using System.Buffers;
using System.IO;
using System.Text;

using Cogito.Kademlia.Core;
using Cogito.Kademlia.Json;
using Cogito.Linq;

using FluentAssertions;
using FluentAssertions.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cogito.Kademlia.Tests.Json
{

    [TestClass]
    public partial class KJsonMessageEncoderTests
    {

        [TestMethod]
        public void Can_encode_ping_request()
        {
            var sender = new KNodeId32(1);
            var buffer = new ArrayBufferWriter<byte>();
            var encoder = new KJsonMessageEncoder<KNodeId32>();
            var message = new KPingRequest<KNodeId32>(new[] { new Uri("http://www.google.com") });
            var request = new KRequest<KNodeId32, KPingRequest<KNodeId32>>(new KMessageHeader<KNodeId32>(sender, 1), message);

            encoder.Encode(new KMessageContext<KNodeId32>("application/json".Yield()), buffer, new KMessageSequence<KNodeId32>(1, new IKMessage<KNodeId32>[] { request }));
            var j = JObject.Parse(Encoding.UTF8.GetString(buffer.WrittenSpan.ToArray()));
            var z = JObject.ReadFrom(new JsonTextReader(new StreamReader(typeof(KJsonMessageEncoderTests).Assembly.GetManifestResourceStream("Cogito.Kademlia.Tests.Json.Samples.ping_request.json"))));
            j.Should().BeEquivalentTo(z);
        }

        [TestMethod]
        public void Can_decode_ping_request()
        {
            var decoder = new KJsonMessageDecoder<KNodeId32>();
            var json = new StreamReader(typeof(KJsonMessageEncoderTests).Assembly.GetManifestResourceStream("Cogito.Kademlia.Tests.Json.Samples.ping_request.json")).ReadToEnd();
            var sequence = decoder.Decode(new KMessageContext<KNodeId32>("application/json".Yield()), new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json)));
            sequence.Network.Should().Be(1);
        }

    }

}
