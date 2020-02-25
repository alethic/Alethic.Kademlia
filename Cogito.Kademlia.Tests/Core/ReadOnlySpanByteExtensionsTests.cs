using System;
using System.Buffers.Binary;

using Cogito.Kademlia.Core;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cogito.Kademlia.Tests.Core
{

    [TestClass]
    public class ReadOnlySpanByteExtensionsTests
    {

        [TestMethod]
        public void Should_get_and_set_bits()
        {
            var s = (Span<byte>)stackalloc byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(s, 0b_1010_0110_1001_1010_0110_1001_1010_0110_1001_1010_0110_1001_1001_1010_0110_1001);
            s.GetBit(0).Should().Be(true);
            s.GetBit(1).Should().Be(false);
            s.GetBit(2).Should().Be(true);
            s.GetBit(3).Should().Be(false);
            s.SetBit(0, false);
            s.GetBit(0).Should().Be(false);
            s.GetBit(1).Should().Be(false);
            s.SetBit(0, true);
            s.GetBit(0).Should().Be(true);
            s.GetBit(1).Should().Be(false);
            s.SetBit(1, true);
            s.GetBit(0).Should().Be(true);
            s.GetBit(1).Should().Be(true);
        }

        [TestMethod]
        public void Should_return_leading_zeros_for_single_byte()
        {
            var z = new byte[1];
            var s = (ReadOnlySpan<byte>)z;
            z[0] = 0b_00100000;
            s.CountLeadingZeros().Should().Be(2);
            z[0] = 0b_10000000;
            s.CountLeadingZeros().Should().Be(0);
            z[0] = 0b_00000010;
            s.CountLeadingZeros().Should().Be(6);
            z[0] = 0b_00000001;
            s.CountLeadingZeros().Should().Be(7);
            z[0] = 0b_00000000;
            s.CountLeadingZeros().Should().Be(8);
        }

        [TestMethod]
        public void Should_return_leading_zeros_for_single_int32()
        {
            var z = new byte[4];
            var s = (ReadOnlySpan<byte>)z;
            BinaryPrimitives.WriteUInt32BigEndian(z, 0b_00000000_00101010_10000000_00000000);
            s.CountLeadingZeros().Should().Be(10);
            BinaryPrimitives.WriteUInt32BigEndian(z, 0b_10000000_00101010_10000000_00000000);
            s.CountLeadingZeros().Should().Be(0);
            BinaryPrimitives.WriteUInt32BigEndian(z, 0b_00001000_00101010_10000000_00000000);
            s.CountLeadingZeros().Should().Be(4);
            BinaryPrimitives.WriteUInt32BigEndian(z, 0b_00000000_00000000_00000000_00100000);
            s.CountLeadingZeros().Should().Be(26);
            BinaryPrimitives.WriteUInt32BigEndian(z, 0b_00000000_00000000_00000000_00000000);
            s.CountLeadingZeros().Should().Be(32);
        }

        [TestMethod]
        public void Should_return_leading_zeros_for_single_int64()
        {
            var z = new byte[8];
            var s = (ReadOnlySpan<byte>)z;
            BinaryPrimitives.WriteUInt64BigEndian(z, 0b_10010000_00101010_10000000_00000000_00000000_00000000_00000000_00000000);
            s.CountLeadingZeros().Should().Be(0);
            BinaryPrimitives.WriteUInt64BigEndian(z, 0b_00010000_00101010_10000000_00000000_00000000_00000000_00000000_00000000);
            s.CountLeadingZeros().Should().Be(3);
            BinaryPrimitives.WriteUInt64BigEndian(z, 0b_00000000_00101010_10000000_00000000_00000000_00000000_00000000_00000000);
            s.CountLeadingZeros().Should().Be(10);
            BinaryPrimitives.WriteUInt64BigEndian(z, 0b_00000000_00000000_00000000_00000000_00000000_00000000_00000001_00000000);
            s.CountLeadingZeros().Should().Be(55);
            BinaryPrimitives.WriteUInt64BigEndian(z, 0b_00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000);
            s.CountLeadingZeros().Should().Be(64);
        }

    }

}
