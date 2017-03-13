// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.Reflection;

namespace UnityEditor
{
    /// Remap Viewed type to inspector type
    internal class CustomEditorAttributes
    {
        private static readonly List<MonoEditorType> kSCustomEditors = new List<MonoEditorType>();
        private static readonly List<MonoEditorType> kSCustomMultiEditors = new List<MonoEditorType>();
        private static readonly Dictionary<Type, Type> kCachedEditorForType = new Dictionary<Type, Type>();
        private static readonly Dictionary<Type, Type> kCachedMultiEditorForType = new Dictionary<Type, Type>();
        private static bool s_Initialized;

        class MonoEditorType
        {
            public Type       m_InspectedType;
            public Type       m_InspectorType;
            public bool       m_EditorForChildClasses;
            public bool       m_IsFallback;
        }

        internal static Type FindCustomEditorType(UnityEngine.Object o, bool multiEdit)
        {
            return FindCustomEditorTypeByType(o.GetType(), multiEdit);
        }

        internal static Type FindCustomEditorTypeByType(System.Type type, bool multiEdit)
        {
            if (!s_Initialized)
            {
                var editorAssemblies = EditorAssemblies.loadedAssemblies;
                for (int i = editorAssemblies.Length - 1; i >= 0; i--)
                    Rebuild(editorAssemblies[i]);

                s_Initialized = true;
            }

            if (type == null)
                return null;

            var cachedEditors = multiEdit ? kCachedMultiEditorForType : kCachedEditorForType;

            Type resultEditorType;
            if (cachedEditors.TryGetValue(type, out resultEditorType))
                return resultEditorType;

            var editors = multiEdit ? kSCustomMultiEditors : kSCustomEditors;
            for (int pass = 0; pass < 2; ++pass)
            {
                for (Type inspected = type; inspected != null; inspected = inspected.BaseType)
                {
                    for (int i = 0; i < editors.Count; ++i)
                    {
                        if (IsAppropriateEditor(editors[i], inspected, type != inspected, pass == 1)) // pass=0 normal, pass=1 fallback
                        {
                            resultEditorType = editors[i].m_InspectorType;
                            cachedEditors.Add(type, resultEditorType);
                            return resultEditorType;
                        }
                    }
                }
            }
            cachedEditors.Add(type, null);
            return null;
        }

        private static bool IsAppropriateEditor(MonoEditorType editor, Type parentClass, bool isChildClass, bool isFallback)
        {
            if (isChildClass && !editor.m_EditorForChildClasses)
                // skip if it's a child class and this editor doesn't want to match on children
                return false;
            if (isFallback != editor.m_IsFallback)
                return false;

            return parentClass == editor.m_InspectedType ||
                (parentClass.IsGenericType && parentClass.GetGenericTypeDefinition() == editor.m_InspectedType);
        }

        internal static void Rebuild(Assembly assembly)
        {
            Type[] types = AssemblyHelper.GetTypesFromAssembly(assembly);
            foreach (var type in types)
            {
                object[] attrs = type.GetCustomAttributes(typeof(CustomEditor), false);
                foreach (CustomEditor inspectAttr in  attrs)
                {
                    var t = new MonoEditorType();
                    if (inspectAttr.m_InspectedType == null)
                        Debug.Log("Can't load custom inspector " + type.Name + " because the inspected type is null.");
                    else if (!type.IsSubclassOf(typeof(Editor)))
                    {
                        // Suppress a warning on TweakMode, we did this bad in the default project folder
                        // and it's going to be too hard for customers to figure out how to fix it and also quite pointless.
                        if (type.FullName == "TweakMode" && type.IsEnum && inspectAttr.m_InspectedType.FullName == "BloomAndFlares")
                            continue;

                        Debug.LogWarning(type.Name + " uses the CustomEditor attribute but does not inherit from Editor.\nYou must inherit from Editor. See the Editor class script documentation.");
                    }
                    else
                    {
                        t.m_InspectedType = inspectAttr.m_InspectedType;
                        t.m_InspectorType = type;
                        t.m_EditorForChildClasses = inspectAttr.m_EditorForChildClasses;
                        t.m_IsFallback = inspectAttr.isFallback;
                        kSCustomEditors.Add(t);

                        if (type.GetCustomAttributes(typeof(CanEditMultipleObjects), false).Length > 0)
                            kSCustomMultiEditors.Add(t);
                    }
                }
            }
        }
    }
}
