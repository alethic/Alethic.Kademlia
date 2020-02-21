using System;
using System.Linq;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cogito.Kademlia.Tests
{

    [TestClass]
    public class KNodeIdDistanceComparerTests
    {

        [TestMethod]
        public void Should_sort_32_by_distance()
        {
            var a = new KNodeId32(uint.MinValue);
            var l = Enumerable.Range(0, 256).Select(i => KNodeId<KNodeId32>.Create()).ToArray();
            Array.Sort(l, new KNodeIdDistanceComparer<KNodeId32>(a));
            l.Should().BeInAscendingOrder(KNodeIdComparer<KNodeId32>.Default);
        }

        [TestMethod]
        public void Should_sort_32_by_distance_from_end()
        {
            var a = new KNodeId32(uint.MaxValue);
            var l = Enumerable.Range(0, 256).Select(i => KNodeId<KNodeId32>.Create()).ToArray();
            Array.Sort(l, new KNodeIdDistanceComparer<KNodeId32>(a));
            l.Should().BeInDescendingOrder(KNodeIdComparer<KNodeId32>.Default);
        }

        [TestMethod]
        public void Should_sort_64_by_distance()
        {
            var a = new KNodeId64(ulong.MinValue);
            var l = Enumerable.Range(0, 256).Select(i => KNodeId<KNodeId64>.Create()).ToArray();
            Array.Sort(l, new KNodeIdDistanceComparer<KNodeId64>(a));
            l.Should().BeInAscendingOrder(KNodeIdComparer<KNodeId64>.Default);
        }

        [TestMethod]
        public void Should_sort_64_by_distance_from_end()
        {
            var a = new KNodeId64(ulong.MaxValue);
            var l = Enumerable.Range(0, 256).Select(i => KNodeId<KNodeId64>.Create()).ToArray();
            Array.Sort(l, new KNodeIdDistanceComparer<KNodeId64>(a));
            l.Should().BeInDescendingOrder(KNodeIdComparer<KNodeId64>.Default);
        }

        [TestMethod]
        public void Should_sort_128_by_distance()
        {
            var a = new KNodeId128(ulong.MinValue, ulong.MinValue);
            var l = Enumerable.Range(0, 256).Select(i => KNodeId<KNodeId128>.Create()).ToArray();
            Array.Sort(l, new KNodeIdDistanceComparer<KNodeId128>(a));
            l.Should().BeInAscendingOrder(KNodeIdComparer<KNodeId128>.Default);
        }

        [TestMethod]
        public void Should_sort_128_by_distance_from_end()
        {
            var a = new KNodeId128(ulong.MaxValue, ulong.MaxValue);
            var l = Enumerable.Range(0, 256).Select(i => KNodeId<KNodeId128>.Create()).ToArray();
            Array.Sort(l, new KNodeIdDistanceComparer<KNodeId128>(a));
            l.Should().BeInDescendingOrder(KNodeIdComparer<KNodeId128>.Default);
        }

    }

}
