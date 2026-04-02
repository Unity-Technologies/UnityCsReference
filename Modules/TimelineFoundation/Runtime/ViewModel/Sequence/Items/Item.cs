// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Time;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct Item : IEquatable<Item>
    {
        [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
        internal enum Type
        {
            Invalid,
            Clip,
            Gap,
            Transition,
            Marker
        }

        public static Item Invalid = new Item(UniqueID.Invalid, -1, Type.Invalid, TimeRange.Empty, TimeRange.Empty, null, null);

        public readonly int index;
        public readonly UniqueID ID;
        public readonly Type type;
        public readonly Track parent;
        public readonly TimeRange range;
        public readonly TimeRange contentRange;
        readonly IItemContent m_Content;

        public DiscreteTime duration => range.duration;
        public DiscreteTime start => range.start;
        public DiscreteTime end => range.end;
        public string name => m_Content == null ? string.Empty : m_Content.name;

        public Item(UniqueID id, int index, Type type, TimeRange range, TimeRange contentRange, Track parent, IItemContent content)
        {
            ID = id;
            this.index = index;
            this.type = type;
            this.range = range;
            this.contentRange = contentRange;
            this.parent = parent;
            m_Content = content;
        }

        public bool isClip => type == Type.Clip;
        public bool isGap => type == Type.Gap;
        public bool isTransition => type == Type.Transition;
        public bool isMarker => type == Type.Marker;

        public override string ToString()
        {
            string contentName = m_Content == null ? "No content" : m_Content.name;
            return $"{type} - {range} Content: {contentName} Content range: {contentRange}";
        }

        public IItemContent GetGenericContent()
        {
            return m_Content;
        }

        public T GetContent<T>() where T : IItemContent
        {
            if (typeof(T) == typeof(IItemContent))
                return (T)m_Content;

            switch (m_Content)
            {
                case T specificClipContent:
                    return specificClipContent;
                case null:
                    throw new InvalidOperationException("Null content data.");
                default:
                    throw new InvalidOperationException($"Incorrect clip content type. Actual: {m_Content.GetType()} Expected:{typeof(T)}");
            }
        }

        public bool Equals(Item other)
        {
            return ID.Equals(other.ID) && type == other.type;
        }

        public override bool Equals(object obj)
        {
            return obj is Item other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (ID, type).GetHashCode();
        }

        public static bool operator ==(Item left, Item right)
        {
            return left.ID == right.ID && left.type == right.type;
        }

        public static bool operator !=(Item left, Item right)
        {
            return !(left == right);
        }

        internal Item ChangeIndex_Internal(int idx)
        {
            return new Item(ID, idx, type, range, contentRange, parent, m_Content);
        }
    }
}
