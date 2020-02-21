using System;

using Cogito.Kademlia.Core;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cogito.Kademlia.Tests.Core
{

    [TestClass]
    public class PriorityQueueTests
    {

        [TestMethod]
        public void Should_dequeue_in_order()
        {
            var q = new PriorityQueue<int>();
            q.Enqueue(3);
            q.Enqueue(1);
            q.Enqueue(0);
            q.Enqueue(4);
            q.Enqueue(9);
            q.Dequeue().Should().Be(0);
            q.Dequeue().Should().Be(1);
            q.Dequeue().Should().Be(3);
            q.Dequeue().Should().Be(4);
            q.Dequeue().Should().Be(9);
        }

        [TestMethod]
        public void Should_dequeue_massive_amount_in_order()
        {
            var q = new PriorityQueue<int>();
            var r = new Random();
            for (int i = 0; i < 8192; i++)
                q.Enqueue(r.Next());

            var c = int.MinValue;
            while (q.Count > 0)
            {
                var i = q.Dequeue();
                i.Should().BeGreaterOrEqualTo(c);
                c = i;
            }
        }

    }

}
