// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.SearchService
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ObjectSelectorHandlerWithLabelsAttribute : Attribute
    {
        public string[] labels { get; }
        public bool matchAll { get; }

        public ObjectSelectorHandlerWithLabelsAttribute(params string[] labels)
        {
            this.labels = labels;
            this.matchAll = true;
        }

        public ObjectSelectorHandlerWithLabelsAttribute(bool matchAll, params string[] labels)
        {
            this.labels = labels;
            this.matchAll = matchAll;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class ObjectSelectorHandlerWithTagsAttribute : Attribute
    {
        public string[] tags { get; }

        public ObjectSelectorHandlerWithTagsAttribute(params string[] tags)
        {
            this.tags = tags;
        }
    }
}
