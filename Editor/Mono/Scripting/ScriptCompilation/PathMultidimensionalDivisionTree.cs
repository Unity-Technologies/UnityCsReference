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
            _root = new Node(defaultValue, null);
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

        public T MatchClosest(ReadOnlySpan<char> span)
        {
            return Node.MatchClosest(_root, span).Value;
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
            private ReadOnlyMemory<char> StringSegment { get; }
            private List<Node> ChildNodes { get; }

            public Node(T value, ReadOnlyMemory<char> segment)
            {
                Value = value;
                StringSegment = segment;
                IsLeaf = true;
                ChildNodes = new List<Node>();
            }

            public Node(T value, ReadOnlyMemory<char> segment, List<Node> nodes)
            {
                Value = value;
                StringSegment = segment;
                IsLeaf = true;
                ChildNodes = nodes;
            }

            private Node(T value, ReadOnlyMemory<char> segment, Node left)
            {
                Value = value;
                StringSegment = segment;
                IsLeaf = true;
                ChildNodes = new List<Node>
                {
                    left,
                };
            }

            private Node(Node node, ReadOnlyMemory<char> segment)
            {
                Value = node.Value;
                StringSegment = segment;
                IsLeaf = node.IsLeaf;
                ChildNodes = node.ChildNodes;
            }

            private Node(ReadOnlyMemory<char> segment, Node left, Node right)
            {
                Value = default;
                StringSegment = segment;
                IsLeaf = false;
                ChildNodes = new List<Node>
                {
                    left,
                    right,
                };
            }

            private bool IsMatch(ReadOnlySpan<char> span)
            {
                if (span.Length < StringSegment.Length)
                {
                    return false;
                }

                var segmentSpan = StringSegment.Span;
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

            public void SplitInsert(ReadOnlyMemory<char> currentStringSegment, T value)
            {
                Assert.IsTrue(currentStringSegment.Length > 0);

                if (ChildNodes.Count == 0)
                {
                    ChildNodes.Add(new Node(value, currentStringSegment));
                }

                var currentStringSegmentSpan = currentStringSegment.Span;

                for (var childIndex = 0; childIndex < ChildNodes.Count; childIndex++)
                {
                    var childNode = ChildNodes[childIndex];
                    if (childNode.IsCandidate(currentStringSegmentSpan[0]))
                    {
                        var childSegmentSpan = childNode.StringSegment.Span;
                        for (int i = 0; childSegmentSpan.Length > i; i++)
                        {
                            if (i >= currentStringSegment.Length ||
                                !IsEqualIgnoreCase(currentStringSegmentSpan[i], childSegmentSpan[i]))
                            {
                                // Split existing node
                                var childHead = childNode.StringSegment[..i];
                                var childTail = childNode.StringSegment[i..];
                                var splitTail = currentStringSegment[i..];

                                if (splitTail.Length > 0)
                                {
                                    ChildNodes[childIndex] = new Node(childHead,
                                        new Node(childNode, childTail),
                                        new Node(value, splitTail));
                                    return;
                                }

                                ChildNodes[childIndex] = new Node(value, childHead,
                                    new Node(childNode, childTail));

                                return;
                            }
                        }

                        var remaining = currentStringSegment[childSegmentSpan.Length..];
                        if (remaining.Length == 0)
                        {
                            // Replace existing value
                            ChildNodes[childIndex] = new Node(value, childNode.StringSegment, ChildNodes[childIndex].ChildNodes);
                        }
                        else
                        {
                            childNode.SplitInsert(remaining, value);
                        }

                        return;
                    }
                }

                // No candidates found add new branch to the tree
                ChildNodes.Add(new Node(value, currentStringSegment));
            }

            private bool IsCandidate(char firstCharacter)
            {
                return IsEqualIgnoreCase(firstCharacter, StringSegment.Span[0]);
            }
        }
    }
}
