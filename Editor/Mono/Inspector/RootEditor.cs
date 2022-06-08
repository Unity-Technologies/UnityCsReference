// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Internal;
using UnityEngine.Pool;
using UnityEngine.Scripting;

using RootEditorHandler = UnityEditor.RootEditorAttribute.RootEditorHandler;
using RootEditorWithMetaDataHandler = UnityEditor.RootEditorAttribute.RootEditorWithMetaDataHandler;

namespace UnityEditor
{
    [ExcludeFromDocs]
    public class RootEditorAttribute : System.Attribute
    {
        public delegate System.Type RootEditorHandler(UnityEngine.Object[] objects);
        internal delegate System.Type RootEditorWithMetaDataHandler(UnityEngine.Object[] objects, UnityEngine.Object context, DataMode dataMode);

        public bool supportsAddComponent;
        public RootEditorAttribute(bool supportsAddComponent = false)
        {
            this.supportsAddComponent = supportsAddComponent;
        }

        [RequiredSignature]
        private static System.Type signature(UnityEngine.Object[] objects)
        {
            return null;
        }

        [RequiredSignature]
        private static System.Type signature(UnityEngine.Object[] objects, UnityEngine.Object context, DataMode dataMode)
        {
            return null;
        }
    }

    internal static class RootEditorUtils
    {
        class RootEditorDesc
        {
            public RootEditorHandler rootEditorHandler;
            public RootEditorWithMetaDataHandler rootEditorWithMetaDataHandler;

            public Type rootEditorType;
            public bool supportsAddComponent;
            public bool usesMetaData;
        }

        private static bool s_SuppressRootEditor = false;
        private static readonly List<RootEditorDesc> kSRootEditor = new List<RootEditorDesc>();

        static RootEditorUtils()
        {
            Rebuild();
        }

        internal static Editor CreateNonRootEditor(UnityEngine.Object[] objects)
        {
            try
            {
                s_SuppressRootEditor = true;
                return Editor.CreateEditor(objects);
            }
            finally
            {
                s_SuppressRootEditor = false;
            }
        }

        internal static bool SupportsAddComponent(Editor[] editors)
        {
            if (editors.Length != 1)
                return true;

            foreach (var e in editors)
            {
                var fittingRootEditor = kSRootEditor.FirstOrDefault(rootEditor => rootEditor.rootEditorType == e.GetType());
                if (fittingRootEditor == null)
                    continue;
                return fittingRootEditor.supportsAddComponent;
            }
            return false;
        }

        [UsedByNativeCode, UsedImplicitly] // Currently, only called from native
        internal static Type FindRootEditor(UnityEngine.Object[] objects, UnityEngine.Object context, DataMode dataMode)
        {
            if (s_SuppressRootEditor)
                return null;

            foreach (var desc in kSRootEditor)
            {
                var rootEditorType = desc.usesMetaData
                    ? desc.rootEditorWithMetaDataHandler(objects, context, dataMode)
                    : desc.rootEditorHandler(objects);

                if (rootEditorType != null)
                {
                    desc.rootEditorType = rootEditorType;
                    return rootEditorType;
                }
            }

            return null;
        }

        internal static void Rebuild()
        {
            var rootEditors = ListPool<RootEditorDesc>.Get();
            var rootEditorsWithMetaData = ListPool<RootEditorDesc>.Get();

            var candidates = TypeCache.GetMethodsWithAttribute<RootEditorAttribute>();
            var attributeType = typeof(RootEditorAttribute);
            foreach (var candidate in candidates)
            {
                if (!AttributeHelper.MethodMatchesAnyRequiredSignatureOfAttribute(candidate, attributeType))
                    continue;

                var attribute = candidate.GetCustomAttribute<RootEditorAttribute>(false);
                var desc = new RootEditorDesc { supportsAddComponent = attribute.supportsAddComponent };

                if (Delegate.CreateDelegate(typeof(RootEditorHandler), candidate, false) is RootEditorHandler handler)
                {
                    desc.usesMetaData = false;
                    desc.rootEditorHandler = handler;
                    rootEditors.Add(desc);
                }
                else if (Delegate.CreateDelegate(typeof(RootEditorWithMetaDataHandler), candidate, false) is RootEditorWithMetaDataHandler handlerWithMetaData)
                {
                    desc.usesMetaData = true;
                    desc.rootEditorWithMetaDataHandler = handlerWithMetaData;
                    rootEditorsWithMetaData.Add(desc);
                }
                else
                {
                    var parameters = candidate.GetParameters();
                    var signature = parameters is { Length: > 0 }
                        ? string.Join(", ", parameters.Select(p => p.ParameterType.FullName).ToArray())
                        : string.Empty;
                    throw new InvalidOperationException($"Could not create a valid delegate from method marked: [{nameof(RootEditorAttribute)}] with signature: ({signature})");
                }
            }

            kSRootEditor.Clear();

            // Adding the root editors with metadata first to take precedence over the general version
            kSRootEditor.AddRange(rootEditorsWithMetaData);
            kSRootEditor.AddRange(rootEditors);

            ListPool<RootEditorDesc>.Release(rootEditors);
            ListPool<RootEditorDesc>.Release(rootEditorsWithMetaData);
        }
    }
}
