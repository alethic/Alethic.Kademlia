//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Cogito.Kademlia
//{
//    class KBinaryTreeRouter
//    {

//        /// <summary>
//        /// Yields out the buckets that should be consulted for routing to the specified key.
//        /// </summary>
//        /// <param name="key"></param>
//        /// <returns></returns>
//        IEnumerable<KBucket<TNodeId, TKPeerData>> GetBucketsByKey(TNodeId key)
//        {
//            // starting index
//            var beg = GetBucketIndex(SelfId, key);

//            // index and bucket
//            var idx = beg;
//            var bck = buckets[idx];

//            // continue until we run out of buckets
//            while (true)
//            {
//                // yield current bucket
//                yield return bck;

//                // advance to next bucket index
//                GetNextBucket(SelfId, key, ref idx);
//                bck = buckets[idx];

//                // we looped back to ourselves
//                if (idx == beg)
//                    break;
//            }

//        }

//        /// <summary>
//        /// Advances <paramref name="idx"/> to the next bucket index.
//        /// </summary>
//        /// <param name="key"></param>
//        /// <param name="idx"></param>
//        internal static void GetNextBucket(in TNodeId selfId, in TNodeId key, ref int idx)
//        {
//            // output self ID bytes
//            var self = (Span<byte>)stackalloc byte[KNodeId<TNodeId>.SizeOf];
//            selfId.Write(self);

//            // output key ID bytes
//            var target = (Span<byte>)stackalloc byte[KNodeId<TNodeId>.SizeOf];
//            key.Write(target);

//            // construct the prefix from the bucket ID
//            var prefixBytes = (Span<byte>)stackalloc byte[KNodeId<TNodeId>.SizeOf];
//            var prefixSize = GetBucketPrefix(selfId, idx, prefixBytes);

//            // calculate distance between the target and the current bucket prefix
//            var targetToPrefixDistance = (Span<byte>)stackalloc byte[KNodeId<TNodeId>.SizeOf];
//            target.Xor(prefixBytes, targetToPrefixDistance);

//            // increment the least significant prefix bit
//            var targetToPrefixDistancePlusOne = (Span<byte>)stackalloc byte[KNodeId<TNodeId>.SizeOf];
//            targetToPrefixDistance.CopyTo(targetToPrefixDistancePlusOne);
//            IncrementBit(targetToPrefixDistancePlusOne, prefixSize - 1);

//            // translate back into natural distance
//            var nextTargetBytes = (Span<byte>)stackalloc byte[KNodeId<TNodeId>.SizeOf];
//            target.CopyTo(nextTargetBytes);
//            for (int i = 0; i < prefixSize; i++)
//                nextTargetBytes.SetBit(i, target.GetBit(i) ^ targetToPrefixDistancePlusOne.GetBit(i));

//            // reload as a node ID
//            var nextTarget = KNodeId<TNodeId>.Read(nextTargetBytes);
//            if (nextTarget.Equals(selfId))
//                idx = -1;
//            else
//                idx = GetBucketIndex(selfId, nextTarget);
//        }

///// <summary>
///// Increments the specified index within the bit array.
///// </summary>
///// <param name="span"></param>
///// <param name="index"></param>
//internal static void IncrementBit(Span<byte> span, int index)
//{
//    var c = true;

//    for (var i = index; i >= 0; i--)
//    {
//        if (c)
//        {
//            if (span.GetBit(i) == false)
//            {
//                span.SetBit(i, true);
//                c = false;
//            }
//            else
//            {
//                span.SetBit(i, false);
//                c = true;
//            }
//        }
//    }
//}




///// <summary>
///// Outputs the prefix and prefix length of the specified bucket index.
///// </summary>
///// <param name="self"></param>
///// <param name="target"></param>
///// <param name="index"></param>
///// <param name="span"></param>
//internal static int GetBucketPrefix(in TNodeId selfId, int index, Span<byte> prefix)
//{
//    // output self ID bytes
//    var self = (Span<byte>)stackalloc byte[KNodeId<TNodeId>.SizeOf];
//    selfId.Write(self);

//    // the size of the prefix
//    var prefixSize = KNodeId<TNodeId>.SizeOf * 8 - index;

//    // prepare a mask to AND against the self ID to determine the prefix
//    var prefixMask = (Span<byte>)stackalloc byte[KNodeId<TNodeId>.SizeOf];
//    for (int i = 0; i < prefixSize; i++)
//        prefixMask.SetBit(i, true);

//    // AND the self ID with the prefix MASK to strip off the remainder
//    // increment the LSB of the prefix to represent 
//    self.CopyTo(prefix);
//    self.And(prefixMask, prefix);
//    prefix.SetBit(prefixSize - 1, !prefix.GetBit(prefixSize - 1));

//    return prefixSize;
//}

//    }

//}
