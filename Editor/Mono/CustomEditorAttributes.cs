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
        private static readonly Dictionary<Type, List<MonoEditorType>> kSCustomEditors = new Dictionary<Type, List<MonoEditorType>>();
        private static readonly Dictionary<Type, List<MonoEditorType>> kSCustomMultiEditors = new Dictionary<Type, List<MonoEditorType>>();
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

        private static List<MonoEditorType> s_SearchCache = new List<MonoEditorType>();
        internal static Type FindCustomEditorTypeByType(Type type, bool multiEdit)
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
                    List<MonoEditorType> foundEditors;
                    if (!editors.TryGetValue(inspected, out foundEditors))
                    {
                        if (!inspected.IsGenericType)
                            continue;

                        inspected = inspected.GetGenericTypeDefinition();

                        if (!editors.TryGetValue(inspected, out foundEditors))
                            continue;
                    }

                    s_SearchCache.Clear();
                    foreach (var result in foundEditors)
                    {
                        if (!IsAppropriateEditor(result, inspected, type != inspected, pass == 1))
                            continue;

                        s_SearchCache.Add(result);
                    }

                    Type toUse = null;

                    // we have a render pipeline...
                    // we need to select the one with the correct RP asset
                    if (GraphicsSettings.renderPipelineAsset != null)
                    {
                        var rpType = GraphicsSettings.renderPipelineAsset.GetType();
                        foreach (var editor in s_SearchCache)
                        {
                            if (editor.m_RenderPipelineType == rpType)
                            {
                                toUse = editor.m_InspectorType;
                                break;
                            }
                        }
                    }

                    // no RP, fallback!
                    if (toUse == null)
                    {
                        foreach (var editor in s_SearchCache)
                        {
                            if (editor.m_RenderPipelineType == null)
                            {
                                toUse = editor.m_InspectorType;
                                break;
                            }
                        }
                    }

                    s_SearchCache.Clear();
                    if (toUse != null)
                        return toUse;
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
                        if (type.FullName == "TweakMode" && type.IsEnum &&
                            inspectAttr.m_InspectedType.FullName == "BloomAndFlares")
                            continue;

                        Debug.LogWarning(
                            type.Name +
                            " uses the CustomEditor attribute but does not inherit from Editor.\nYou must inherit from Editor. See the Editor class script documentation.");
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

                        List<MonoEditorType> editors;
                        if (!kSCustomEditors.TryGetValue(inspectAttr.m_InspectedType, out editors))
                        {
                            editors = new List<MonoEditorType>();
                            kSCustomEditors[inspectAttr.m_InspectedType] = editors;
                        }
                        editors.Add(t);

                        if (type.GetCustomAttributes(typeof(CanEditMultipleObjects), false).Length > 0)
                        {
                            List<MonoEditorType> multiEditors;
                            if (!kSCustomMultiEditors.TryGetValue(inspectAttr.m_InspectedType, out multiEditors))
                            {
                                multiEditors = new List<MonoEditorType>();
                                kSCustomMultiEditors[inspectAttr.m_InspectedType] = multiEditors;
                            }
                            multiEditors.Add(t);
                        }
                    }
                }
            }
        }
    }
}
