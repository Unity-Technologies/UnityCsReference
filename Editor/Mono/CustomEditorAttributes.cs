// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.Reflection;
using UnityEngine.Rendering;

namespace UnityEditor
{
    /// Remap Viewed type to inspector type
    internal class CustomEditorAttributes
    {
        private static readonly List<MonoEditorType> kSCustomEditors = new List<MonoEditorType>();
        private static readonly List<MonoEditorType> kSCustomMultiEditors = new List<MonoEditorType>();
        private static bool s_Initialized;

        class MonoEditorType
        {
            public Type       m_InspectedType;
            public Type       m_InspectorType;
            public Type       m_RenderPipelineType;
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

            var editors = multiEdit ? kSCustomMultiEditors : kSCustomEditors;
            for (int pass = 0; pass < 2; ++pass)
            {
                for (Type inspected = type; inspected != null; inspected = inspected.BaseType)
                {
                    // Capture for closure
                    var inspected1 = inspected;
                    var pass1 = pass;

                    var validEditors = editors.Where(x => IsAppropriateEditor(x, inspected1, type != inspected1, pass1 == 1));

                    // we have a render pipeline...
                    // we need to select the one with the correct RP asset
                    if (GraphicsSettings.renderPipelineAsset != null)
                    {
                        var rpType = GraphicsSettings.renderPipelineAsset.GetType();
                        foreach (var editor in validEditors)
                        {
                            if (editor.m_RenderPipelineType == rpType)
                                return editor.m_InspectorType;
                        }
                    }

                    // no RP, fallback!
                    var found = validEditors.FirstOrDefault(x => x.m_RenderPipelineType == null);
                    if (found != null)
                        return found.m_InspectorType;
                }
            }
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
                        var attr = inspectAttr as CustomEditorForRenderPipelineAttribute;
                        if (attr != null)
                            t.m_RenderPipelineType = attr.renderPipelineType;
                        kSCustomEditors.Add(t);
                        if (type.GetCustomAttributes(typeof(CanEditMultipleObjects), false).Length > 0)
                            kSCustomMultiEditors.Add(t);
                    }
                }
            }
        }
    }
}
