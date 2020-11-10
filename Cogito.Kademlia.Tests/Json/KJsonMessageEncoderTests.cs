using System;
using System.Buffers;
using System.IO;
using System.Linq;
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
            var node = new KNodeId32(1);
            var buffer = new ArrayBufferWriter<byte>();
            var encoder = new KJsonMessageEncoder<KNodeId32>();
            var message = new KPingRequest<KNodeId32>(new[] { new Uri("http://www.google.com") });
            var request = new KRequest<KNodeId32, KPingRequest<KNodeId32>>(new KMessageHeader<KNodeId32>(node, 1), message);

            encoder.Encode(new KMessageContext<KNodeId32>("application/json".Yield()), buffer, new KMessageSequence<KNodeId32>(1, new IKMessage<KNodeId32>[] { request }));
            var j = JObject.Parse(Encoding.UTF8.GetString(buffer.WrittenSpan.ToArray()));
            var z = JObject.ReadFrom(new JsonTextReader(new StreamReader(typeof(KJsonMessageEncoderTests).Assembly.GetManifestResourceStream("Cogito.Kademlia.Tests.Json.Samples.ping_request.json"))));
            j.Should().BeEquivalentTo(z);
        }

        [TestMethod]
        public void Can_decode_ping_request()
        {
            var node = new KNodeId32(1);
            var decoder = new KJsonMessageDecoder<KNodeId32>();
            var json = new StreamReader(typeof(KJsonMessageEncoderTests).Assembly.GetManifestResourceStream("Cogito.Kademlia.Tests.Json.Samples.ping_request.json")).ReadToEnd();
            var sequence = decoder.Decode(new KMessageContext<KNodeId32>("application/json".Yield()), new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json)));
            sequence.Network.Should().Be(1);
            sequence.Should().HaveCount(1);
            sequence[0].Should().BeOfType<KRequest<KNodeId32, KPingRequest<KNodeId32>>>();

            var message = (KRequest<KNodeId32, KPingRequest<KNodeId32>>)sequence[0];
            message.Header.Sender.Should().Be(node);
            message.Header.ReplyId.Should().Be(1);
            message.Body.Value.Endpoints.Should().HaveCount(1);
            message.Body.Value.Endpoints[0].Should().Be(new Uri("http://www.google.com"));
        }

        [TestMethod]
        public void Can_encode_findvalue_request()
        {
            var node = new KNodeId32(1);
            var buffer = new ArrayBufferWriter<byte>();
            var encoder = new KJsonMessageEncoder<KNodeId32>();
            var message = new KFindValueRequest<KNodeId32>(node);
            var request = new KRequest<KNodeId32, KFindValueRequest<KNodeId32>>(new KMessageHeader<KNodeId32>(node, 1), message);

            encoder.Encode(new KMessageContext<KNodeId32>("application/json".Yield()), buffer, new KMessageSequence<KNodeId32>(1, new IKMessage<KNodeId32>[] { request }));
            var j = JObject.Parse(Encoding.UTF8.GetString(buffer.WrittenSpan.ToArray()));
            var z = JObject.ReadFrom(new JsonTextReader(new StreamReader(typeof(KJsonMessageEncoderTests).Assembly.GetManifestResourceStream("Cogito.Kademlia.Tests.Json.Samples.findvalue_request.json"))));
            j.Should().BeEquivalentTo(z);
        }

        [TestMethod]
        public void Can_decode_findvalue_request()
        {
            var node = new KNodeId32(1);
            var decoder = new KJsonMessageDecoder<KNodeId32>();
            var json = new StreamReader(typeof(KJsonMessageEncoderTests).Assembly.GetManifestResourceStream("Cogito.Kademlia.Tests.Json.Samples.findvalue_request.json")).ReadToEnd();
            var sequence = decoder.Decode(new KMessageContext<KNodeId32>("application/json".Yield()), new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json)));
            sequence.Network.Should().Be(1);
            sequence.Should().HaveCount(1);
            sequence[0].Should().BeOfType<KRequest<KNodeId32, KFindValueRequest<KNodeId32>>>();

            var message = (KRequest<KNodeId32, KFindValueRequest<KNodeId32>>)sequence[0];
            message.Header.Sender.Should().Be(node);
            message.Header.ReplyId.Should().Be(1);
            message.Body.Value.Key.Should().Be(node);
        }

        [TestMethod]
        public void Can_encode_findvalue_response()
        {
            var node = new KNodeId32(1);
            var buffer = new ArrayBufferWriter<byte>();
            var encoder = new KJsonMessageEncoder<KNodeId32>();
            var body = new KFindValueResponse<KNodeId32>(new KNodeInfo<KNodeId32>[] { new KNodeInfo<KNodeId32>(node, new[] { new Uri("http://www.google.com") }) }, new KValueInfo(new byte[0], 1, DateTime.UtcNow.AddSeconds(.9)));
            var message = new KResponse<KNodeId32, KFindValueResponse<KNodeId32>>(new KMessageHeader<KNodeId32>(node, 1), KResponseStatus.Success, body);

            encoder.Encode(new KMessageContext<KNodeId32>("application/json".Yield()), buffer, new KMessageSequence<KNodeId32>(1, new IKMessage<KNodeId32>[] { message }));
            var j = JObject.Parse(Encoding.UTF8.GetString(buffer.WrittenSpan.ToArray()));
            var z = JObject.ReadFrom(new JsonTextReader(new StreamReader(typeof(KJsonMessageEncoderTests).Assembly.GetManifestResourceStream("Cogito.Kademlia.Tests.Json.Samples.findvalue_response.json"))));
            j.Should().BeEquivalentTo(z);
        }

        [TestMethod]
        public void Can_decode_findvalue_response()
        {
            var node = new KNodeId32(1);
            var decoder = new KJsonMessageDecoder<KNodeId32>();
            var json = new StreamReader(typeof(KJsonMessageEncoderTests).Assembly.GetManifestResourceStream("Cogito.Kademlia.Tests.Json.Samples.findvalue_response.json")).ReadToEnd();
            var sequence = decoder.Decode(new KMessageContext<KNodeId32>("application/json".Yield()), new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json)));
            sequence.Network.Should().Be(1);
            sequence.Should().HaveCount(1);
            sequence[0].Should().BeOfType<KResponse<KNodeId32, KFindValueResponse<KNodeId32>>>();

            var message = (KResponse<KNodeId32, KFindValueResponse<KNodeId32>>)sequence[0];
            message.Header.Sender.Should().Be(node);
            message.Header.ReplyId.Should().Be(1);
            message.Status.Should().Be(KResponseStatus.Success);
            message.Body.Value.Value.Value.Version.Should().Be(1);
            message.Body.Value.Value.Value.Expiration.Should().BeCloseTo(DateTime.UtcNow.AddSeconds(1), 5000);
            message.Body.Value.Nodes.Should().HaveCount(1);
        }

    }

}
