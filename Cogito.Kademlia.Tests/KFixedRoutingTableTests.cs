using System;
using System.Collections.Generic;
using System.Linq;
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
            readonly IKEngine<TKNodeId> engine;

            public FakeNetwork(in TKNodeId self, IKEngine<TKNodeId> engine)
            {
                this.self = self;
                this.engine = engine;
            }

            public IKEngine<TKNodeId> Engine => engine;

            public Guid Id => Guid.Empty;

            public IEnumerable<IKEndpoint<TKNodeId>> Endpoints => Enumerable.Empty<IKEndpoint<TKNodeId>>();

            public ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(in IKEndpoint<TKNodeId> endpoint, in KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                return new ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>>(new KResponse<TKNodeId, KPingResponse<TKNodeId>>(self, new KPingResponse<TKNodeId>()));
            }

            public ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(in IKEndpoint<TKNodeId> endpoint, in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                return new ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>>(new KResponse<TKNodeId, KStoreResponse<TKNodeId>>(self, new KStoreResponse<TKNodeId>(request.Key)));
            }

            public ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(in IKEndpoint<TKNodeId> endpoint, in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                return new ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>>(new KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>(self, new KFindNodeResponse<TKNodeId>(request.Key)));
            }

            public ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(in IKEndpoint<TKNodeId> endpoint, in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                return new ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>>(new KResponse<TKNodeId, KFindValueResponse<TKNodeId>>(self, new KFindValueResponse<TKNodeId>(request.Key, new byte[8])));
            }

        }

        class FakeSlowNetwork<TKNodeId> : IKProtocol<TKNodeId>
            where TKNodeId : unmanaged, IKNodeId<TKNodeId>
        {

            readonly TKNodeId self;
            readonly IKEngine<TKNodeId> engine;

            public FakeSlowNetwork(in TKNodeId self, IKEngine<TKNodeId> engine)
            {
                this.self = self;
                this.engine = engine;
            }

            public IKEngine<TKNodeId> Engine => engine;

            public Guid Id => Guid.Empty;

            public IEnumerable<IKEndpoint<TKNodeId>> Endpoints => Enumerable.Empty<IKEndpoint<TKNodeId>>();

            public ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(in IKEndpoint<TKNodeId> endpoint, in KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                return PingAsync(endpoint, request, cancellationToken);
            }

            async ValueTask<KResponse<TKNodeId, KPingResponse<TKNodeId>>> PingAsync(IKEndpoint<TKNodeId> endpoint, KPingRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                await Task.Delay(100);
                return new KResponse<TKNodeId, KPingResponse<TKNodeId>>(self, new KPingResponse<TKNodeId>());
            }

            public ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(in IKEndpoint<TKNodeId> endpoint, in KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                return StoreAsync(endpoint, request, cancellationToken);
            }

            async ValueTask<KResponse<TKNodeId, KStoreResponse<TKNodeId>>> StoreAsync(IKEndpoint<TKNodeId> endpoint, KStoreRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                await Task.Delay(100);
                return new KResponse<TKNodeId, KStoreResponse<TKNodeId>>(self, new KStoreResponse<TKNodeId>(request.Key));
            }

            public ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(in IKEndpoint<TKNodeId> endpoint, in KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                return FindNodeAsync(endpoint, request, cancellationToken);
            }

            async ValueTask<KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>> FindNodeAsync(IKEndpoint<TKNodeId> endpoint, KFindNodeRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                await Task.Delay(100);
                return new KResponse<TKNodeId, KFindNodeResponse<TKNodeId>>(self, new KFindNodeResponse<TKNodeId>(request.Key));
            }

            public ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(in IKEndpoint<TKNodeId> endpoint, in KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                return FindValueAsync(endpoint, request, cancellationToken);
            }

            async ValueTask<KResponse<TKNodeId, KFindValueResponse<TKNodeId>>> FindValueAsync(IKEndpoint<TKNodeId> endpoint, KFindValueRequest<TKNodeId> request, CancellationToken cancellationToken)
            {
                await Task.Delay(100);
                return new KResponse<TKNodeId, KFindValueResponse<TKNodeId>>(self, new KFindValueResponse<TKNodeId>(request.Key, new byte[8]));
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
                await t.UpdatePeerAsync(new KNodeId32((uint)r.Next(int.MinValue, int.MaxValue)), Enumerable.Empty<IKEndpoint<KNodeId32>>());
        }

        [TestMethod]
        public async Task Can_randomly_populate_int32_double()
        {
            var s = new KNodeId32(0);
            var t = new KFixedRoutingTable<KNodeId32>(s);

            for (int i = 1; i <= 262144 * 8; i++)
                await t.UpdatePeerAsync(new KNodeId32((uint)i), Enumerable.Empty<IKEndpoint<KNodeId32>>());

            var c = t.Count;

            for (int i = 1; i <= 262144 * 8; i++)
                await t.UpdatePeerAsync(new KNodeId32((uint)i), Enumerable.Empty<IKEndpoint<KNodeId32>>());

            t.Count.Should().Be(c);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int32_mt()
        {
            var s = new KNodeId32(0);
            var t = new KFixedRoutingTable<KNodeId32>(s);

            var r = new Random();
            var l = new List<Task>();
            for (int i = 0; i < 1024; i++)
                l.Add(t.UpdatePeerAsync(new KNodeId32((uint)r.Next(int.MinValue, int.MaxValue)), Enumerable.Empty<IKEndpoint<KNodeId32>>()).AsTask());

            await Task.WhenAll(l);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int64()
        {
            var s = new KNodeId64(0);
            var t = new KFixedRoutingTable<KNodeId64>(s);

            var r = new Random();
            for (int i = 0; i < 262144 * 8; i++)
                await t.UpdatePeerAsync(new KNodeId64((ulong)r.NextInt64()), Enumerable.Empty<IKEndpoint<KNodeId64>>());
        }

        [TestMethod]
        public async Task Can_randomly_populate_int64_mt()
        {
            var s = new KNodeId64(0);
            var t = new KFixedRoutingTable<KNodeId64>(s);

            var r = new Random();
            var l = new List<Task>();
            for (int i = 0; i < 1024; i++)
                l.Add(t.UpdatePeerAsync(new KNodeId64((ulong)r.NextInt64()), Enumerable.Empty<IKEndpoint<KNodeId64>>()).AsTask());

            await Task.WhenAll(l);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int128()
        {
            var s = new KNodeId128(Guid.Empty);
            var t = new KFixedRoutingTable<KNodeId128>(s);

            for (int i = 0; i < 262144 * 8; i++)
                await t.UpdatePeerAsync(new KNodeId128(Guid.NewGuid()), Enumerable.Empty<IKEndpoint<KNodeId128>>());
        }

        [TestMethod]
        public async Task Can_randomly_populate_int128_mt()
        {
            var s = new KNodeId128(Guid.Empty);
            var t = new KFixedRoutingTable<KNodeId128>(s);

            var l = new List<Task>();
            for (int i = 0; i < 1024; i++)
                l.Add(t.UpdatePeerAsync(new KNodeId128(Guid.NewGuid()), Enumerable.Empty<IKEndpoint<KNodeId128>>()).AsTask());

            await Task.WhenAll(l);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int160()
        {
            var s = new KNodeId160(0, 0, 0);
            var t = new KFixedRoutingTable<KNodeId160>(s);

            var r = new Random();
            for (int i = 0; i < 262144 * 8; i++)
                await t.UpdatePeerAsync(new KNodeId160((ulong)r.NextInt64(), (ulong)r.NextInt64(), (uint)r.Next(int.MinValue, int.MaxValue)), Enumerable.Empty<IKEndpoint<KNodeId160>>());
        }

        [TestMethod]
        public async Task Can_randomly_populate_int160_mt()
        {
            var s = new KNodeId160(0, 0, 0);
            var t = new KFixedRoutingTable<KNodeId160>(s);

            var r = new Random();
            var l = new List<Task>();
            for (int i = 0; i < 1024; i++)
                l.Add(t.UpdatePeerAsync(new KNodeId160((ulong)r.NextInt64(), (ulong)r.NextInt64(), (uint)r.Next(int.MinValue, int.MaxValue)), Enumerable.Empty<IKEndpoint<KNodeId160>>()).AsTask());

            await Task.WhenAll(l);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int256()
        {
            var s = new KNodeId256(0, 0, 0, 0);
            var t = new KFixedRoutingTable<KNodeId256>(s);

            var r = new Random();
            for (int i = 0; i < 262144 * 8; i++)
                await t.UpdatePeerAsync(new KNodeId256((ulong)r.NextInt64(), (ulong)r.NextInt64(), (ulong)r.NextInt64(), (ulong)r.NextInt64()), Enumerable.Empty<IKEndpoint<KNodeId256>>());
        }

        [TestMethod]
        public async Task Can_randomly_populate_int256_mt()
        {
            var s = new KNodeId256(0, 0, 0, 0);
            var t = new KFixedRoutingTable<KNodeId256>(s);

            var r = new Random();
            var l = new List<Task>();
            for (int i = 0; i < 1024; i++)
                l.Add(t.UpdatePeerAsync(new KNodeId256((ulong)r.NextInt64(), (ulong)r.NextInt64(), (ulong)r.NextInt64(), (ulong)r.NextInt64()), Enumerable.Empty<IKEndpoint<KNodeId256>>()).AsTask());

            await Task.WhenAll(l);
        }

    }

}
