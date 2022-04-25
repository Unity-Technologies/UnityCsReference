// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.SearchService
{
    delegate void AdvancedObjectSelectorHandler(AdvancedObjectSelectorEventType eventType, in AdvancedObjectSelectorParameters parameters);
    delegate bool AdvancedObjectSelectorValidatorHandler(ObjectSelectorSearchContext context);

    interface IAdvancedObjectSelectorAttribute
    {
        internal string id { get; }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class AdvancedObjectSelectorAttribute : Attribute, IAdvancedObjectSelectorAttribute
    {
        internal string id { get; }
        internal string displayName { get; }
        internal int defaultPriority { get; set; }

        internal bool defaultActive { get; set; }

        string IAdvancedObjectSelectorAttribute.id => id;

        public AdvancedObjectSelectorAttribute(string id, string displayName, int defaultPriority, bool defaultActive = true)
        {
            this.id = id;
            this.displayName = displayName;
            this.defaultPriority = defaultPriority;
            this.defaultActive = defaultActive;
        }

        public AdvancedObjectSelectorAttribute(string id, int defaultPriority, bool defaultActive = true)
            : this(id, null, defaultPriority, defaultActive)
        { }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class AdvancedObjectSelectorValidatorAttribute : Attribute, IAdvancedObjectSelectorAttribute
    {
        internal string id { get; }
        string IAdvancedObjectSelectorAttribute.id => id;

        public AdvancedObjectSelectorValidatorAttribute(string id)
        {
            this.id = id;
        }
    }

    public readonly struct AdvancedObjectSelectorParameters
    {
        public ObjectSelectorSearchContext context { get; }
        public Action<UnityEngine.Object, bool> selectorClosedHandler { get; }
        public Action<UnityEngine.Object> trackingHandler { get; }
        public string searchFilter { get; }

        internal AdvancedObjectSelectorParameters(ObjectSelectorSearchContext context, Action<UnityEngine.Object, bool> selectorClosedHandler, Action<UnityEngine.Object> trackingHandler, string searchFilter)
        {
            this.context = context;
            this.selectorClosedHandler = selectorClosedHandler;
            this.trackingHandler = trackingHandler;
            this.searchFilter = searchFilter;
        }

        internal AdvancedObjectSelectorParameters(ObjectSelectorSearchContext context)
            : this(context, null, null, string.Empty)
        { }

        internal AdvancedObjectSelectorParameters(ObjectSelectorSearchContext context, Action<UnityEngine.Object, bool> selectorClosedHandler, Action<UnityEngine.Object> trackingHandler)
            : this(context, selectorClosedHandler, trackingHandler, string.Empty)
        { }

        internal AdvancedObjectSelectorParameters(ObjectSelectorSearchContext context, string searchFilter)
            : this(context, null, null, searchFilter)
        { }

        internal AdvancedObjectSelectorParameters(ISearchContext context)
            : this((ObjectSelectorSearchContext)context, null, null, string.Empty)
        { }

        internal AdvancedObjectSelectorParameters(ISearchContext context, Action<UnityEngine.Object, bool> selectorClosedHandler, Action<UnityEngine.Object> trackingHandler)
            : this((ObjectSelectorSearchContext)context, selectorClosedHandler, trackingHandler, string.Empty)
        { }

        internal AdvancedObjectSelectorParameters(ISearchContext context, string searchFilter)
            : this((ObjectSelectorSearchContext)context, null, null, searchFilter)
        { }
    }

    public enum AdvancedObjectSelectorEventType
    {
        BeginSession,
        EndSession,
        OpenAndSearch,
        SetSearchFilter
    }
}
