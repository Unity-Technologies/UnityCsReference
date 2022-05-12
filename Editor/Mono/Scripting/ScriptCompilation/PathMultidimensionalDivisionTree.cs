// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal class PathMultidimensionalDivisionTree<T>
    {
        private readonly Node _root;

        public PathMultidimensionalDivisionTree(T defaultValue = default)
        {
            _root = new Node(defaultValue, null, 0);
        }

        public void Insert(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            var keyMemory = TrimTrailingPathSeparator(key);
            _root.SplitInsert(keyMemory, value);
        }

        public T MatchClosest(ReadOnlySpan<char> span, out string matchedBy)
        {
            var node = Node.MatchClosest(_root, span);
            matchedBy = node.FullPrefix.ToString();
            return node.Value;
        }

        private static bool IsEqualIgnoreCase(char a, char b)
        {
            return (Utility.IsPathSeparator(a) && Utility.IsPathSeparator(b)) || Utility.FastToLower(a) == Utility.FastToLower(b);
        }

        private static ReadOnlyMemory<char> TrimTrailingPathSeparator(string key)
        {
            for (int i = key.Length - 1; i >= 0; i--)
            {
                if (Utility.IsPathSeparator(key[i]))
                {
                    continue;
                }

                return key.AsMemory(0, i + 1);
            }

            return ReadOnlyMemory<char>.Empty;
        }

        private class Node
        {
            public T Value { get; }
            private bool IsLeaf { get; }
            private int OffsetFromParent { get; }
            private ReadOnlySpan<char> StringSegment => FullPrefix.Span[OffsetFromParent..];
            public ReadOnlyMemory<char> FullPrefix { get; private set; }
            private List<Node> ChildNodes { get; }

            public Node(T value, ReadOnlyMemory<char> fullPrefix, int offsetFromParent)
            {
                Value = value;
                OffsetFromParent = offsetFromParent;
                IsLeaf = true;
                FullPrefix = fullPrefix;
                ChildNodes = new List<Node>();
            }

            public Node(T value, ReadOnlyMemory<char> fullPrefix, int offsetFromParent, List<Node> nodes)
            {
                Value = value;
                OffsetFromParent = offsetFromParent;
                IsLeaf = true;
                FullPrefix = fullPrefix;
                ChildNodes = nodes;
            }

            private Node(ReadOnlyMemory<char> fullPrefix, int offsetFromParent, List<Node> childNodes)
            {
                IsLeaf = false;
                OffsetFromParent = offsetFromParent;
                FullPrefix = fullPrefix;
                ChildNodes = childNodes;
            }

            private Node WithDifferentOffsetFromParent(int offsetFromParent)
            {
                return new Node(Value, FullPrefix, offsetFromParent, ChildNodes);
            }

            private bool IsMatch(ReadOnlySpan<char> span)
            {
                if (span.Length < StringSegment.Length)
                {
                    return false;
                }

                var segmentSpan = StringSegment;
                for (int i = 0; i < StringSegment.Length; i++)
                {
                    if (!IsEqualIgnoreCase(span[i], segmentSpan[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public static Node MatchClosest(Node current, ReadOnlySpan<char> span)
            {
                Node currentLeaf = current;
                Next:
                if (span.Length == 0)
                {
                    return currentLeaf;
                }

                foreach (var childNode in current.ChildNodes)
                {
                    // We can only have 1 candidate
                    if (childNode.IsCandidate(span[0]))
                    {
                        if (childNode.IsMatch(span))
                        {
                            span = span[childNode.StringSegment.Length..];
                            if (childNode.IsLeaf && (span.Length == 0 || Utility.IsPathSeparator(span[0])))
                            {
                                current = currentLeaf = childNode;
                                goto Next;
                            }
                            current = childNode;
                            goto Next;
                        }

                        return currentLeaf;
                    }
                }

                return currentLeaf;
            }

            public void SplitInsert(ReadOnlyMemory<char> fullPrefix, T value)
            {
                Assert.IsTrue(fullPrefix.Length > 0);

                if (ChildNodes.Count == 0)
                {
                    ChildNodes.Add(new Node(value, fullPrefix, this.FullPrefix.Length));
                    return;
                }

                var currentStringSegmentSpan = fullPrefix.Span[this.FullPrefix.Length..];

                for (var childIndex = 0; childIndex < ChildNodes.Count; childIndex++)
                {
                    var childNode = ChildNodes[childIndex];
                    if (childNode.IsCandidate(currentStringSegmentSpan[0]))
                    {
                        var childSegmentSpan = childNode.StringSegment;
                        for (int i = 0; childSegmentSpan.Length > i; i++)
                        {
                            if (i >= currentStringSegmentSpan.Length ||
                                !IsEqualIgnoreCase(currentStringSegmentSpan[i], childSegmentSpan[i]))
                            {
                                // Split existing node
                                var splitPoint = i + this.FullPrefix.Length;

                                if (i < currentStringSegmentSpan.Length)
                                {
                                    // Create non-leaf node at split point, containing the new node, and the replacement
                                    // of the existing node

                                    var replacementNode = childNode.WithDifferentOffsetFromParent(splitPoint);
                                    var newNode = new Node(value, fullPrefix, splitPoint);
                                    var splitNode = new Node(childNode.FullPrefix[..splitPoint], childNode.OffsetFromParent,
                                        new List<Node> { replacementNode, newNode });
                                    ChildNodes[childIndex] = splitNode;
                                }
                                else
                                {
                                    // Create the new node as a parent to the existing one
                                    var newNode = new Node(value, fullPrefix, childNode.OffsetFromParent,
                                        new List<Node> { childNode.WithDifferentOffsetFromParent(fullPrefix.Length) });
                                    ChildNodes[childIndex] = newNode;
                                }

                                return;
                            }
                        }

                        if (currentStringSegmentSpan.Length == childSegmentSpan.Length)
                        {
                            // Replace existing value
                            ChildNodes[childIndex] = new Node(value, fullPrefix, this.FullPrefix.Length, childNode.ChildNodes);
                        }
                        else
                        {
                            childNode.SplitInsert(fullPrefix, value);
                        }

                        return;
                    }
                }

                // No candidates found add new branch to the tree
                ChildNodes.Add(new Node(value, fullPrefix, this.FullPrefix.Length));
            }

            private bool IsCandidate(char firstCharacter)
            {
                return IsEqualIgnoreCase(firstCharacter, StringSegment[0]);
            }

            public override string ToString()
            {
                return $"{FullPrefix[..OffsetFromParent]}|{FullPrefix[OffsetFromParent..]} ({Value}) children: {ChildNodes?.Count ?? 0}";
            }
        }
    }
}
