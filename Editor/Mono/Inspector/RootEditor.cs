// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.Internal;

namespace UnityEditor
{
    [ExcludeFromDocs]
    public class RootEditorAttribute : System.Attribute
    {
        public delegate System.Type RootEditorHandler(UnityEngine.Object[] objects);

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
    }

    internal static class RootEditorUtils
    {
        internal class RootEditorDesc
        {
            public RootEditorAttribute.RootEditorHandler needsRootEditor;
            public System.Type rootEditorType;
            public bool supportsAddComponent;
        }

        private static readonly List<RootEditorDesc> kSRootEditor = new List<RootEditorDesc>();

        static RootEditorUtils()
        {
            Rebuild();
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

        internal static Type FindRootEditor(UnityEngine.Object[] objects)
        {
            foreach (var desc in kSRootEditor)
            {
                var rootEditorType = desc.needsRootEditor(objects);
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
            kSRootEditor.Clear();
            var rootEditorMethods = AttributeHelper.GetMethodsWithAttribute<RootEditorAttribute>(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (var method in rootEditorMethods.methodsWithAttributes)
            {
                var callback = Delegate.CreateDelegate(typeof(RootEditorAttribute.RootEditorHandler), method.info) as RootEditorAttribute.RootEditorHandler;
                if (callback != null)
                {
                    var attr = method.attribute as RootEditorAttribute;
                    kSRootEditor.Add(new RootEditorDesc() { needsRootEditor = callback, supportsAddComponent = attr.supportsAddComponent });
                }
            }
        }
    }
}
