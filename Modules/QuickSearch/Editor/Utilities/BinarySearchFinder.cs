// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Search
{
    interface IBinarySearchRangeData<out TRangeData>
    {
        long length { get; }
        TRangeData this[long index] { get; }
    }

    interface IBinarySearchRange<in TRangeData>
    {
        bool StartIsInRange(TRangeData start);
        bool EndIsInRange(TRangeData end);
    }

    struct BinarySearchRange : IEquatable<BinarySearchRange>
    {
        public long startOffset;
        public long endOffset;
        public long halfOffset;

        public static BinarySearchRange invalid = new BinarySearchRange { startOffset = -1, endOffset = -1 };

        public bool Equals(BinarySearchRange other)
        {
            return startOffset == other.startOffset && endOffset == other.endOffset;
        }

        public override bool Equals(object obj)
        {
            return obj is BinarySearchRange other && Equals(other);
        }

        public static bool operator==(BinarySearchRange lhs, BinarySearchRange rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(BinarySearchRange lhs, BinarySearchRange rhs)
        {
            return !lhs.Equals(rhs);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (startOffset.GetHashCode() * 397) ^ endOffset.GetHashCode();
            }
        }
    }

    static class BinarySearchFinder
    {
        public static BinarySearchRange FindRange<TRangeData>(IBinarySearchRange<TRangeData> range, IBinarySearchRangeData<TRangeData> data)
        {
            {
                var nbValues = data.length;
                if (nbValues == 0)
                    return BinarySearchRange.invalid;

                var binarySearchRangeStart = new BinarySearchRange { startOffset = 0, endOffset = nbValues, halfOffset = nbValues / 2 };
                var binarySearchRangeEnd = new BinarySearchRange { startOffset = 0, endOffset = nbValues, halfOffset = nbValues / 2 };
                var foundStartOffset = false;
                var foundEndOffset = false;
                while (!foundStartOffset || !foundEndOffset)
                {
                    if (!foundStartOffset)
                    {
                        // Update StartIndex
                        var startValue = data[binarySearchRangeStart.halfOffset];
                        if (range.StartIsInRange(startValue))
                        {
                            binarySearchRangeStart.endOffset = binarySearchRangeStart.halfOffset;
                            binarySearchRangeStart.halfOffset = binarySearchRangeStart.startOffset + (binarySearchRangeStart.endOffset - binarySearchRangeStart.startOffset) / 2;

                            if (binarySearchRangeStart.endOffset == binarySearchRangeStart.halfOffset)
                                foundStartOffset = true;
                        }
                        else
                        {
                            // value is outside of the file
                            if (binarySearchRangeStart.halfOffset >= nbValues - 1)
                                return BinarySearchRange.invalid;

                            binarySearchRangeStart.startOffset = binarySearchRangeStart.halfOffset;
                            binarySearchRangeStart.halfOffset = binarySearchRangeStart.startOffset + (binarySearchRangeStart.endOffset - binarySearchRangeStart.startOffset) / 2;

                            if (binarySearchRangeStart.startOffset == binarySearchRangeStart.halfOffset)
                                foundStartOffset = true;
                        }
                    }

                    if (!foundEndOffset)
                    {
                        // Update EndIndex
                        var endValue = data[binarySearchRangeEnd.halfOffset];
                        if (range.EndIsInRange(endValue))
                        {
                            binarySearchRangeEnd.startOffset = binarySearchRangeEnd.halfOffset;
                            binarySearchRangeEnd.halfOffset = binarySearchRangeEnd.startOffset + (binarySearchRangeEnd.endOffset - binarySearchRangeEnd.startOffset) / 2;

                            if (binarySearchRangeEnd.startOffset == binarySearchRangeEnd.halfOffset)
                                foundEndOffset = true;
                        }
                        else
                        {
                            // value is outside of the file
                            if (binarySearchRangeEnd.halfOffset == 0)
                                return BinarySearchRange.invalid;

                            binarySearchRangeEnd.endOffset = binarySearchRangeEnd.halfOffset;
                            binarySearchRangeEnd.halfOffset = binarySearchRangeEnd.startOffset + (binarySearchRangeEnd.endOffset - binarySearchRangeEnd.startOffset) / 2;

                            if (binarySearchRangeEnd.endOffset == binarySearchRangeEnd.halfOffset)
                                foundEndOffset = true;
                        }
                    }
                }

                // We take the endOffset because we know the values of interests lie on these offset.
                return new BinarySearchRange { startOffset = binarySearchRangeStart.endOffset, endOffset = binarySearchRangeEnd.endOffset };
            }
        }
    }
}
