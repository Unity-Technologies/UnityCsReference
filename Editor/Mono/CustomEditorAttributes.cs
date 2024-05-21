// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Unity.Collections;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    internal partial class CustomEditorAttributes
    {
        static CustomEditorAttributes instance => k_Instance.Value;

        static readonly Lazy<CustomEditorAttributes> k_Instance = new(() => new CustomEditorAttributes());

        readonly CustomEditorCache m_Cache = new CustomEditorCache();

        readonly Func<List<MonoEditorType>, Type>[] m_GetEditorWhenSRPEnabled =
        {
            FindEditorsByRenderPipeline,
            FindEditorsOnlyByAttribute,
            UseNotSupportedOnInspector
        };

        readonly Func<List<MonoEditorType>, Type>[] m_GetEditorWhenBuiltinEnabled =
        {
            FindEditorsOnlyByAttribute,
            UseNotSupportedOnInspector
        };

        Func<List<MonoEditorType>, Type>[] m_CurrentGetEditorList => GraphicsSettings.isScriptableRenderPipelineEnabled ? m_GetEditorWhenSRPEnabled : m_GetEditorWhenBuiltinEnabled;

        CustomEditorAttributes()
        {
            Initialize();
        }

        Type GetCustomEditorType(Type type, bool multiEdit)
        {
            if (type == null)
                return null;

            var editor = GetEditor(type, multiEdit, Pass.Regular);
            return editor ?? GetEditor(type, multiEdit, Pass.Fallback);
        }

        Type GetEditor(Type type, bool multiEdit, Pass pass)
        {
            for (var inspected = type; inspected != null; inspected = inspected.BaseType)
            {
                if (!m_Cache.TryGet(inspected, multiEdit, out var foundEditors))
                    continue;

                var filteredEditors = foundEditors
                    .Where(e => IsAppropriateEditor(e, type != inspected, pass == Pass.Fallback))
                    .ToList();

                var inspectorType = FindEditors(filteredEditors);
                if (inspectorType != null)
                    return inspectorType;
            }

            return null;
        }

        Type FindEditors(List<MonoEditorType> editors)
        {
            if (!editors.Any())
                return null;

            var getEditorActions = m_CurrentGetEditorList;
            for (int i = 0; i < getEditorActions.Length; i++)
            {
                var inspectorType = getEditorActions[i].Invoke(editors);
                if (inspectorType != null)
                    return inspectorType;
            }

            return null;
        }

        static Type FindEditorsByRenderPipeline(List<MonoEditorType> editors)
        {
            Type editorToUse = null;
            var rpType = GraphicsSettings.currentRenderPipelineAssetType;
            for (var i = 0; i < editors.Count; i++)
            {
                var editor = editors[i];
                if (editor.supportedRenderPipelineTypes == null)
                    continue;

                var isEditorSupported = SupportedOnRenderPipelineAttribute.GetSupportedMode(editor.supportedRenderPipelineTypes, rpType);
                if (isEditorSupported == SupportedOnRenderPipelineAttribute.SupportedMode.Supported)
                    return editor.inspectorType;

                if (isEditorSupported == SupportedOnRenderPipelineAttribute.SupportedMode.SupportedByBaseClass)
                    editorToUse = editor.inspectorType;
            }

            return editorToUse;
        }

        static Type FindEditorsOnlyByAttribute(List<MonoEditorType> editors)
        {
            for (var i = 0; i < editors.Count; i++)
            {
                var editor = editors[i];
                if (editor.supportedRenderPipelineTypes != null)
                    continue;

                return editor.inspectorType;
            }

            return null;
        }

        static Type UseNotSupportedOnInspector(List<MonoEditorType> editors)
        {
            if (!editors.Any())
                return null;

            var allEditorHaveSupportedOn = true;
            for (var i = 0; i < editors.Count; i++)
            {
                var editor = editors[i];
                if (editor.supportedRenderPipelineTypes == null)
                    allEditorHaveSupportedOn = false;
            }

            return allEditorHaveSupportedOn ? typeof(NotSupportedOnRenderPipelineInspector) : null;
        }

        static bool IsAppropriateEditor(MonoEditorType editor, bool isChildClass, bool isFallbackPass)
        {
            if (isChildClass && !editor.editorForChildClasses)
                // skip if it's a child class and this editor doesn't want to match on children
                return false;

            //if it is fallback pas we expect every editor to be fallback editor
            return isFallbackPass == editor.isFallback;
        }

        internal static void Rebuild()
        {
            instance.Initialize();
        }

        void Initialize()
        {
            m_Cache.Clear();
            var types = TypeCache.GetTypesWithAttribute<CustomEditor>();
            foreach (var type in types)
            {
                var inspectAttr = type.GetCustomAttribute<CustomEditor>(false);
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
                    var isValid = TryGatherRenderPipelineTypes(type, inspectAttr, out var renderPipelineTypes);
                    if (!isValid)
                    {
                        Debug.LogError($"Inspector {type.FullName} contains invalid attribute. This inspector will be skipped.");
                        continue;
                    }

                    var monoEditorType = new MonoEditorType(type, renderPipelineTypes, inspectAttr.m_EditorForChildClasses, inspectAttr.isFallback);
                    m_Cache.Add(type, inspectAttr, monoEditorType);
                }
            }
        }

        [MustUseReturnValue]
        static bool TryGatherRenderPipelineTypes([DisallowNull] Type type, [DisallowNull] CustomEditor inspectAttr, [NotNullWhen(true)] out Type[] results)
        {
            var supportedOnAttribute = type.GetCustomAttribute<SupportedOnRenderPipelineAttribute>(false);
            if (supportedOnAttribute != null)
            {
                if (supportedOnAttribute.renderPipelineTypes == null)
                {
                    results = null;
                    return false;
                }

                var supportedPipelines = supportedOnAttribute.renderPipelineTypes
                    .Where(r => r != null)
                    .Distinct()
                    .ToArray();
                if (supportedPipelines.Length > 0)
                {
                    results = supportedPipelines;
                    return true;
                }
            }

#pragma warning disable CS0618
            if (inspectAttr is CustomEditorForRenderPipelineAttribute attr)
            {
                results = new[] { attr.renderPipelineType };
                return true;
            }
#pragma warning restore CS0618

            results = null;
            return true;
        }

        enum Pass
        {
            Regular,
            Fallback
        }

        internal readonly struct MonoEditorType
        {
            public readonly Type inspectorType;
            public readonly Type[] supportedRenderPipelineTypes;
            public readonly bool editorForChildClasses;
            public readonly bool isFallback;

            public MonoEditorType(Type inspectorType, Type[] supportedRenderPipelineTypes, bool editorForChildClasses, bool isFallback)
            {
                this.inspectorType = inspectorType;
                this.supportedRenderPipelineTypes = supportedRenderPipelineTypes;
                this.editorForChildClasses = editorForChildClasses;
                this.isFallback = isFallback;
            }
        }

        struct MonoEditorTypeStorage
        {
            public List<MonoEditorType> customEditors;
            public List<MonoEditorType> customEditorsMultiEdition;

            public MonoEditorTypeStorage()
            {
                customEditors = new List<MonoEditorType>();
                customEditorsMultiEdition = new List<MonoEditorType>();
            }
        }

        class CustomEditorCache
        {
            readonly Dictionary<Type, MonoEditorTypeStorage> m_CustomEditorCache = new();
            readonly SortUnityTypesFirstComparer m_SortUnityTypesFirstComparer = new();

            internal void Clear()
            {
                m_CustomEditorCache.Clear();
            }

            internal bool TryGet(Type type, bool multiedition, out List<MonoEditorType> editors)
            {
                if (m_CustomEditorCache.TryGetValue(type, out var storedEditors))
                {
                    editors = multiedition ? storedEditors.customEditorsMultiEdition : storedEditors.customEditors;
                    return true;
                }

                if (!type.IsGenericType)
                {
                    editors = null;
                    return false;
                }

                type = type.GetGenericTypeDefinition();
                if (m_CustomEditorCache.TryGetValue(type, out storedEditors))
                {
                    editors = multiedition ? storedEditors.customEditorsMultiEdition : storedEditors.customEditors;
                    return true;
                }

                editors = null;
                return false;
            }

            internal void Add(Type type, CustomEditor inspectAttr, MonoEditorType monoEditorType)
            {
                var isItExistInCache = m_CustomEditorCache.TryGetValue(inspectAttr.m_InspectedType, out var storage);
                if (!isItExistInCache)
                    storage = new MonoEditorTypeStorage();

                storage.customEditors.AddSorted(monoEditorType, m_SortUnityTypesFirstComparer);
                if (type.GetCustomAttribute<CanEditMultipleObjects>(false) != null)
                    storage.customEditorsMultiEdition.AddSorted(monoEditorType, m_SortUnityTypesFirstComparer);

                if (!isItExistInCache)
                    m_CustomEditorCache.Add(inspectAttr.m_InspectedType, storage);
            }

            struct SortUnityTypesFirstComparer : IComparer<MonoEditorType>
            {
                public int Compare(MonoEditorType typeA, MonoEditorType typeB)
                {
                    return SortUnityTypesFirst(typeA, typeB);
                }

                static int SortUnityTypesFirst(MonoEditorType typeA, MonoEditorType typeB)
                {
                    var xAssemblyIsUnity = InternalEditorUtility.IsUnityAssembly(typeA.inspectorType);
                    var yAssemblyIsUnity = InternalEditorUtility.IsUnityAssembly(typeB.inspectorType);

                    if (xAssemblyIsUnity == yAssemblyIsUnity)
                        return string.CompareOrdinal(typeA.inspectorType.FullName, typeB.inspectorType.FullName);
                    if (xAssemblyIsUnity)
                        return 1;
                    return -1;
                }
            }
        }
    }
}
