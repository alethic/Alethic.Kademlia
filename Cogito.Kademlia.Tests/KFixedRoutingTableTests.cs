using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cogito.Kademlia.Tests
{

    [TestClass]
    public class KFixedRoutingTableTests
    {

        class FakeNetwork<TKNodeId> : IKProtocol<TKNodeId>
            where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        {

            readonly TKNodeId self;

            public FakeNetwork(in TKNodeId self)
            {
                this.self = self;
            }

            public ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(in TKNodeId target, IKEndpoint<TKNodeId> endpoint, in KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                return new ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>>(new KResponse<TKNodeId, KPingResponse<TKNodeId>>(target, self, new KPingResponse<TKNodeId>()));
            }

            public ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(in TKNodeId target, IKEndpoint<TKNodeId> endpoint, in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                return new ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>>(new KResponse<TKNodeId, KStoreResponse<TKNodeId>>(target, self, new KStoreResponse<TKNodeId>(request.Key)));
            }

            public ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(in TKNodeId target, IKEndpoint<TKNodeId> endpoint, in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                return new ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>>(new KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>(target, self, new KFindNodeResponse<TKNodeId>(request.NodeId)));
            }

            public ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(in TKNodeId target, IKEndpoint<TKNodeId> endpoint, in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                return new ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>>(new KResponse<TKNodeId, KFindValueResponse<TKNodeId>>(target, self, new KFindValueResponse<TKNodeId>(request.Key, new byte[8])));
            }

        }

        class FakeSlowNetwork<TKNodeId> : IKProtocol<TKNodeId>
            where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        {

            readonly TKNodeId self;

            public FakeSlowNetwork(in TKNodeId self)
            {
                this.self = self;
            }

            public ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(in TKNodeId target, IKEndpoint<TKNodeId> endpoint, in KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                return PingAsync(target, endpoint, request, cancellationToken);
            }

            async ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(TKNodeId target, IKEndpoint<TKNodeId> endpoint, KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                await Task.Delay(100);
                return new KResponse<TKNodeId, KPingResponse<TKNodeId>>(target, self, new KPingResponse<TKNodeId>());
            }

            public ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(in TKNodeId target, IKEndpoint<TKNodeId> endpoint, in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                return StoreAsync(target, endpoint, request, cancellationToken);
            }

            async ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(TKNodeId target, IKEndpoint<TKNodeId> endpoint, KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                await Task.Delay(100);
                return new KResponse<TKNodeId, KStoreResponse<TKNodeId>>(target, self, new KStoreResponse<TKNodeId>(request.Key));
            }

            public ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(in TKNodeId target, IKEndpoint<TKNodeId> endpoint, in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                return FindNodeAsync(target, endpoint, request, cancellationToken);
            }

            async ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(TKNodeId target, IKEndpoint<TKNodeId> endpoint, KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                await Task.Delay(100);
                return new KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>(target, self, new KFindNodeResponse<TKNodeId>(request.NodeId));
            }

            public ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(in TKNodeId target, IKEndpoint<TKNodeId> endpoint, in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                return FindValueAsync(target, endpoint, request, cancellationToken);
            }

            async ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(TKNodeId target, IKEndpoint<TKNodeId> endpoint, KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                await Task.Delay(100);
                return new KResponse<TKNodeId, KFindValueResponse<TKNodeId>>(target, self, new KFindValueResponse<TKNodeId>(request.Key, new byte[8]));
            }

        }

        [TestMethod]
        public void Should_find_proper_bucket_for_int32()
        {
            var s = new KNodeId32(0);
            KFixedRoutingTable.GetBucketIndex(s, new KNodeId32(1)).Should().Be(0);
            KFixedRoutingTable.GetBucketIndex(s, new KNodeId32(2)).Should().Be(1);
            KFixedRoutingTable.GetBucketIndex(s, new KNodeId32(2147483648)).Should().Be(31);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int32()
        {
            var s = new KNodeId32(0);
            var t = new KFixedRoutingTable<KNodeId32>(s);

            var r = new Random();
            for (int i = 0; i < 262144 * 8; i++)
                await t.TouchAsync(new KNodeId32((uint)r.Next(int.MinValue, int.MaxValue)), null);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int32_mt()
        {
            var s = new KNodeId32(0);
            var t = new KFixedRoutingTable<KNodeId32>(s);

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
            var t = new KFixedRoutingTable<KNodeId64>(s);

            var r = new Random();
            for (int i = 0; i < 262144 * 8; i++)
                await t.TouchAsync(new KNodeId64((ulong)r.NextInt64()), null);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int64_mt()
        {
            var s = new KNodeId64(0);
            var t = new KFixedRoutingTable<KNodeId64>(s);

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
            var t = new KFixedRoutingTable<KNodeId128>(s);

            for (int i = 0; i < 262144 * 8; i++)
                await t.TouchAsync(new KNodeId128(Guid.NewGuid()), null);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int128_mt()
        {
            var s = new KNodeId128(Guid.Empty);
            var t = new KFixedRoutingTable<KNodeId128>(s);

            var l = new List<Task>();
            for (int i = 0; i < 1024; i++)
                l.Add(t.TouchAsync(new KNodeId128(Guid.NewGuid()), null).AsTask());

            await Task.WhenAll(l);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int160()
        {
            var s = new KNodeId160(0, 0, 0);
            var t = new KFixedRoutingTable<KNodeId160>(s);

            var r = new Random();
            for (int i = 0; i < 262144 * 8; i++)
                await t.TouchAsync(new KNodeId160((ulong)r.NextInt64(), (ulong)r.NextInt64(), (uint)r.Next(int.MinValue, int.MaxValue)), null);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int160_mt()
        {
            var s = new KNodeId160(0, 0, 0);
            var t = new KFixedRoutingTable<KNodeId160>(s);

            var r = new Random();
            var l = new List<Task>();
            for (int i = 0; i < 1024; i++)
                l.Add(t.TouchAsync(new KNodeId160((ulong)r.NextInt64(), (ulong)r.NextInt64(), (uint)r.Next(int.MinValue, int.MaxValue)), null).AsTask());

            await Task.WhenAll(l);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int256()
        {
            var s = new KNodeId256(0, 0, 0, 0);
            var t = new KFixedRoutingTable<KNodeId256>(s);

            var r = new Random();
            for (int i = 0; i < 262144 * 8; i++)
                await t.TouchAsync(new KNodeId256((ulong)r.NextInt64(), (ulong)r.NextInt64(), (ulong)r.NextInt64(), (ulong)r.NextInt64()), null);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int256_mt()
        {
            var s = new KNodeId256(0, 0, 0, 0);
            var t = new KFixedRoutingTable<KNodeId256>(s);

            var r = new Random();
            var l = new List<Task>();
            for (int i = 0; i < 1024; i++)
                l.Add(t.TouchAsync(new KNodeId256((ulong)r.NextInt64(), (ulong)r.NextInt64(), (ulong)r.NextInt64(), (ulong)r.NextInt64()), null).AsTask());

            await Task.WhenAll(l);
        }

    }

}
