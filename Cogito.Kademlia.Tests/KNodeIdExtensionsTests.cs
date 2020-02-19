using System;

using Cogito.Kademlia.Core;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cogito.Kademlia.Tests
{

    [TestClass]
    public class KNodeIdExtensionsTests
    {

        [TestMethod]
        public void Should_generate_random_nodeid_for_32()
        {
            var kid1 = KNodeId.CreateNodeId<KNodeId32>();
        }

        [TestMethod]
        public void Should_generate_random_nodeid_for_64()
        {
            var kid1 = KNodeId.CreateNodeId<KNodeId64>();
        }

        [TestMethod]
        public void Should_generate_random_nodeid_for_128()
        {
            var kid1 = KNodeId.CreateNodeId<KNodeId128>();
        }

        [TestMethod]
        public void Should_generate_random_nodeid_for_160()
        {
            var kid1 = KNodeId.CreateNodeId<KNodeId160>();
        }

        [TestMethod]
        public void Should_generate_random_nodeid_for_256()
        {
            var kid1 = KNodeId.CreateNodeId<KNodeId256>();
        }

        [TestMethod]
        public void Should_calculate_proper_distance_offset_for_int32()
        {
            var a = new KNodeId32(0);
            var b = new KNodeId32(1);
            var o = (Span<byte>)new byte[4];
            var s = (ReadOnlySpan<byte>)o;
            KNodeId.CalculateDistance(a, b, o);
            s.CountLeadingZeros().Should().Be(31);
        }

        [TestMethod]
        public void Should_calculate_proper_distance_offset_for_int64()
        {
            var a = new KNodeId64(0);
            var b = new KNodeId64(1);
            var o = (Span<byte>)new byte[8];
            var s = (ReadOnlySpan<byte>)o;
            KNodeId.CalculateDistance(a, b, o);
            s.CountLeadingZeros().Should().Be(63);
        }

        [TestMethod]
        public void Should_calculate_proper_distance_offset_for_int128()
        {
            var a = new KNodeId128(0, 0);
            var b = new KNodeId128(0, 1);
            var o = (Span<byte>)new byte[16];
            var s = (ReadOnlySpan<byte>)o;
            KNodeId.CalculateDistance(a, b, o);
            s.CountLeadingZeros().Should().Be(127);
        }

        [TestMethod]
        public void Should_calculate_proper_distance_offset_for_int160()
        {
            var a = new KNodeId160(0, 0, 0);
            var b = new KNodeId160(0, 0, 1);
            var o = (Span<byte>)new byte[20];
            var s = (ReadOnlySpan<byte>)o;
            KNodeId.CalculateDistance(a, b, o);
            s.CountLeadingZeros().Should().Be(159);
        }

        [TestMethod]
        public void Should_calculate_proper_distance_offset_for_int256()
        {
            var a = new KNodeId256(0, 0, 0, 0);
            var b = new KNodeId256(0, 0, 0, 1);
            var o = (Span<byte>)new byte[32];
            var s = (ReadOnlySpan<byte>)o;
            KNodeId.CalculateDistance(a, b, o);
            s.CountLeadingZeros().Should().Be(255);
        }

    }

}
