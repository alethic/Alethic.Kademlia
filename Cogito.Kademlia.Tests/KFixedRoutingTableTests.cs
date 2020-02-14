using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Kademlia.Core;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cogito.Kademlia.Tests
{

    [TestClass]
    public class KFixedRoutingTableTests
    {

        class FakeNetwork<TKNodeId, TKNodeData> : IKNodeProtocol<TKNodeId, TKNodeData>
            where TKNodeId : struct, IKNodeId<TKNodeId>
        {

            public ValueTask<KNodePingResponse> PingAsync(TKNodeId nodeId, TKNodeData nodeData, CancellationToken cancellationToken)
            {
                return new ValueTask<KNodePingResponse>(new KNodePingResponse(KNodeResponseStatus.OK));
            }

            public ValueTask<KNodeStoreResponse> StoreAsync(TKNodeId nodeId, TKNodeData nodeData, TKNodeId key, CancellationToken cancellationToken)
            {
                return new ValueTask<KNodeStoreResponse>(new KNodeStoreResponse(KNodeResponseStatus.OK));
            }

            public ValueTask<KNodeFindNodeResponse> FindNodeAsync(TKNodeId nodeId, TKNodeData nodeData, TKNodeId key, CancellationToken cancellationToken)
            {
                return new ValueTask<KNodeFindNodeResponse>(new KNodeFindNodeResponse(KNodeResponseStatus.OK));
            }

            public ValueTask<KNodeFindValueResponse> FindValueAsync(TKNodeId nodeId, TKNodeData nodeData, TKNodeId key, CancellationToken cancellationToken)
            {
                return new ValueTask<KNodeFindValueResponse>(new KNodeFindValueResponse(KNodeResponseStatus.OK));
            }

        }

        class FakeSlowNetwork<TKNodeId, TKNodeData> : IKNodeProtocol<TKNodeId, TKNodeData>
            where TKNodeId : struct, IKNodeId<TKNodeId>
        {

            readonly static Random r = new Random();

            public async ValueTask<KNodePingResponse> PingAsync(TKNodeId nodeId, TKNodeData nodeData, CancellationToken cancellationToken)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(r.Next(0, 100)));
                return (new KNodePingResponse(KNodeResponseStatus.OK));
            }

            public async ValueTask<KNodeStoreResponse> StoreAsync(TKNodeId nodeId, TKNodeData nodeData, TKNodeId key, CancellationToken cancellationToken)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(r.Next(0, 100)));
                return (new KNodeStoreResponse(KNodeResponseStatus.OK));
            }

            public async ValueTask<KNodeFindNodeResponse> FindNodeAsync(TKNodeId nodeId, TKNodeData nodeData, TKNodeId key, CancellationToken cancellationToken)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(r.Next(0, 100)));
                return (new KNodeFindNodeResponse(KNodeResponseStatus.OK));
            }

            public async ValueTask<KNodeFindValueResponse> FindValueAsync(TKNodeId nodeId, TKNodeData nodeData, TKNodeId key, CancellationToken cancellationToken)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(r.Next(0, 100)));
                return (new KNodeFindValueResponse(KNodeResponseStatus.OK));
            }

        }

        [TestMethod]
        public void Should_calculate_proper_distance_offset_for_int32()
        {
            var a = new KNodeId32(0);
            var b = new KNodeId32(1);
            var o = new byte[a.DistanceSize / 8];
            var s = (ReadOnlySpan<byte>)o;
            a.CalculateDistance(b, o);
            s.CountLeadingZeros().Should().Be(31);
        }

        [TestMethod]
        public void Should_calculate_proper_distance_offset_for_int64()
        {
            var a = new KNodeId64(0);
            var b = new KNodeId64(1);
            var o = new byte[a.DistanceSize / 8];
            var s = (ReadOnlySpan<byte>)o;
            a.CalculateDistance(b, o);
            s.CountLeadingZeros().Should().Be(63);
        }

        [TestMethod]
        public void Should_find_proper_bucket_for_int32()
        {
            var s = new KNodeId32(0);
            KTable.GetBucketIndex(s, new KNodeId32(1)).Should().Be(0);
            KTable.GetBucketIndex(s, new KNodeId32(2)).Should().Be(1);
            KTable.GetBucketIndex(s, new KNodeId32(2147483648)).Should().Be(31);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int32()
        {
            var s = new KNodeId32(0);
            var t = new KFixedRoutingTable<KNodeId32, object>(s, new FakeNetwork<KNodeId32, object>());

            var r = new Random();
            for (int i = 0; i < 262144 * 8; i++)
                await t.TouchAsync(new KNodeId32((uint)r.Next(int.MinValue, int.MaxValue)), null);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int32_mt()
        {
            var s = new KNodeId32(0);
            var t = new KFixedRoutingTable<KNodeId32, object>(s, new FakeSlowNetwork<KNodeId32, object>());

            var r = new Random();
            var l = new List<Task>();
            for (int i = 0; i < 1024; i++)
                l.Add(t.TouchAsync(new KNodeId32((uint)r.Next(int.MinValue, int.MaxValue)), null).AsTask());

            await Task.WhenAll(l);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int64()
        {
            var s = new KNodeId64(0);
            var t = new KFixedRoutingTable<KNodeId64, object>(s, new FakeNetwork<KNodeId64, object>());

            var r = new Random();
            for (int i = 0; i < 262144 * 8; i++)
                await t.TouchAsync(new KNodeId64((ulong)r.NextInt64()), null);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int64_mt()
        {
            var s = new KNodeId64(0);
            var t = new KFixedRoutingTable<KNodeId64, object>(s, new FakeSlowNetwork<KNodeId64, object>());

            var r = new Random();
            var l = new List<Task>();
            for (int i = 0; i < 1024; i++)
                l.Add(t.TouchAsync(new KNodeId64((ulong)r.NextInt64()), null).AsTask());

            await Task.WhenAll(l);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int128()
        {
            var s = new KNodeId128(Guid.Empty);
            var t = new KFixedRoutingTable<KNodeId128, object>(s, new FakeNetwork<KNodeId128, object>());

            for (int i = 0; i < 262144 * 8; i++)
                await t.TouchAsync(new KNodeId128(Guid.NewGuid()), null);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int128_mt()
        {
            var s = new KNodeId128(Guid.Empty);
            var t = new KFixedRoutingTable<KNodeId128, object>(s, new FakeSlowNetwork<KNodeId128, object>());

            var l = new List<Task>();
            for (int i = 0; i < 1024; i++)
                l.Add(t.TouchAsync(new KNodeId128(Guid.NewGuid()), null).AsTask());

            await Task.WhenAll(l);
        }

    }

}
