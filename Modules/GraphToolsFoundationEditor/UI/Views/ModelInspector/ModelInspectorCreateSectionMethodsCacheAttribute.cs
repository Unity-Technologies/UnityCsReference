// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;

namespace Unity.GraphToolsFoundation.Editor
{
    [MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    class ModelInspectorCreateSectionMethodsCacheAttribute : Attribute
    {
        internal const int lowestPriority_Internal = 0;

        /// <summary>
        /// Default extension method priority for methods provided by tools.
        /// </summary>
        public const int toolDefaultPriority = 1000;

        /// <summary>
        /// The priority of the extension methods.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// The type of view to which the extension methods apply.
        /// </summary>
        public Type ViewDomain { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelInspectorCreateSectionMethodsCacheAttribute"/> class.
        /// </summary>
        /// <param name="viewDomain">The view domain to use</param>
        /// <param name="priority">The priority of the extension methods.</param>
        public ModelInspectorCreateSectionMethodsCacheAttribute(Type viewDomain,int priority = toolDefaultPriority)
        {
            ViewDomain = viewDomain;
            Priority = priority;
        }
    }
}
