using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cogito.Kademlia.Tests
{

    [TestClass]
    public class KFixedRoutingTableTests
    {

        [TestMethod]
        public void Should_find_proper_bucket_for_int32()
        {
            var s = new KNodeId32(0);
            KFixedTableRouter.GetBucketIndex(s, new KNodeId32(1)).Should().Be(0);
            KFixedTableRouter.GetBucketIndex(s, new KNodeId32(2)).Should().Be(1);
            KFixedTableRouter.GetBucketIndex(s, new KNodeId32(2147483648)).Should().Be(31);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int32()
        {
            var s = new KNodeId32(0);
            var t = new KFixedTableRouter<KNodeId32>(s);

            var r = new Random();
            for (int i = 0; i < 262144 * 8; i++)
                await t.UpdatePeerAsync(new KNodeId32((uint)r.Next(int.MinValue, int.MaxValue)), null, null);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int32_double()
        {
            var s = new KNodeId32(0);
            var t = new KFixedTableRouter<KNodeId32>(s);

            for (int i = 1; i <= 262144 * 8; i++)
                await t.UpdatePeerAsync(new KNodeId32((uint)i), null, null);

            var c = t.Count;

            for (int i = 1; i <= 262144 * 8; i++)
                await t.UpdatePeerAsync(new KNodeId32((uint)i), null, null);

            t.Count.Should().Be(c);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int32_mt()
        {
            var s = new KNodeId32(0);
            var t = new KFixedTableRouter<KNodeId32>(s);

            var r = new Random();
            var l = new List<Task>();
            for (int i = 0; i < 1024; i++)
                l.Add(t.UpdatePeerAsync(new KNodeId32((uint)r.Next(int.MinValue, int.MaxValue)), null, null).AsTask());

            await Task.WhenAll(l);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int64()
        {
            var s = new KNodeId64(0);
            var t = new KFixedTableRouter<KNodeId64>(s);

            var r = new Random();
            for (int i = 0; i < 262144 * 8; i++)
                await t.UpdatePeerAsync(new KNodeId64((ulong)r.NextInt64()), null, null);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int64_mt()
        {
            var s = new KNodeId64(0);
            var t = new KFixedTableRouter<KNodeId64>(s);

            var r = new Random();
            var l = new List<Task>();
            for (int i = 0; i < 1024; i++)
                l.Add(t.UpdatePeerAsync(new KNodeId64((ulong)r.NextInt64()), null, null).AsTask());

            await Task.WhenAll(l);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int128()
        {
            var s = new KNodeId128(Guid.Empty);
            var t = new KFixedTableRouter<KNodeId128>(s);

            for (int i = 0; i < 262144 * 8; i++)
                await t.UpdatePeerAsync(new KNodeId128(Guid.NewGuid()), null, null);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int128_mt()
        {
            var s = new KNodeId128(Guid.Empty);
            var t = new KFixedTableRouter<KNodeId128>(s);

            var l = new List<Task>();
            for (int i = 0; i < 1024; i++)
                l.Add(t.UpdatePeerAsync(new KNodeId128(Guid.NewGuid()), null, null).AsTask());

            await Task.WhenAll(l);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int160()
        {
            var s = new KNodeId160(0, 0, 0);
            var t = new KFixedTableRouter<KNodeId160>(s);

            var r = new Random();
            for (int i = 0; i < 262144 * 8; i++)
                await t.UpdatePeerAsync(new KNodeId160((ulong)r.NextInt64(), (ulong)r.NextInt64(), (uint)r.Next(int.MinValue, int.MaxValue)), null, null);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int160_mt()
        {
            var s = new KNodeId160(0, 0, 0);
            var t = new KFixedTableRouter<KNodeId160>(s);

            var r = new Random();
            var l = new List<Task>();
            for (int i = 0; i < 1024; i++)
                l.Add(t.UpdatePeerAsync(new KNodeId160((ulong)r.NextInt64(), (ulong)r.NextInt64(), (uint)r.Next(int.MinValue, int.MaxValue)), null, null).AsTask());

            await Task.WhenAll(l);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int256()
        {
            var s = new KNodeId256(0, 0, 0, 0);
            var t = new KFixedTableRouter<KNodeId256>(s);

            var r = new Random();
            for (int i = 0; i < 262144 * 8; i++)
                await t.UpdatePeerAsync(new KNodeId256((ulong)r.NextInt64(), (ulong)r.NextInt64(), (ulong)r.NextInt64(), (ulong)r.NextInt64()), null, null);
        }

        [TestMethod]
        public async Task Can_randomly_populate_int256_mt()
        {
            var s = new KNodeId256(0, 0, 0, 0);
            var t = new KFixedTableRouter<KNodeId256>(s);

            var r = new Random();
            var l = new List<Task>();
            for (int i = 0; i < 1024; i++)
                l.Add(t.UpdatePeerAsync(new KNodeId256((ulong)r.NextInt64(), (ulong)r.NextInt64(), (ulong)r.NextInt64(), (ulong)r.NextInt64()), null, null).AsTask());

            await Task.WhenAll(l);
        }

    }

}
