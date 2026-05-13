// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class StyleInspectorDefaultContent : VisualElement
{
    static bool s_TimeSlice = true;
    static VisualTreeAsset s_DefaultContent;
    static UnityEngine.Pool.ObjectPool<StyleInspectorDefaultContent> s_StyleInspectorDefaultContentPool = new(Create);

    static VisualTreeAsset DefaultContent
    {
        get
        {
            if (s_DefaultContent == null || !s_DefaultContent)
                s_DefaultContent = EditorGUIUtility.Load("UIToolkitAuthoring/Inspector/StyleInspector/StyleInspectorDefaultContent.uxml") as VisualTreeAsset;

            return s_DefaultContent;
        }
    }

    class Initializer
    {
        private List<VisualTreeAsset> m_Assets;
        private List<Type> m_PropertyBagTypes;
        private int m_AssetIndex;
        private int m_PropertyBagTypeIndex;

        public Initializer(List<VisualTreeAsset> assets)
        {
            m_Assets = assets;
            // No initialization to do.
            if (m_Assets?.Count == 0)
                return;

            m_PropertyBagTypes = new List<Type>();
            m_AssetIndex = 0;
            m_PropertyBagTypeIndex = 0;
            EditorApplication.tick += SlowlyCreateContent;
        }

        void SlowlyCreateContent()
        {
            if (!ContentFinishedLoading())
                return;

            if (!PropertyBagsWereGenerated())
                return;

            EditorApplication.tick -= SlowlyCreateContent;
            // Prepopulate the content.
            using var handle = s_StyleInspectorDefaultContentPool.Get(out var _);
        }

        bool ContentFinishedLoading()
        {
            if (m_Assets == null)
                return true;

            if (m_AssetIndex < m_Assets.Count)
            {
                var vta = m_Assets[m_AssetIndex++];
                vta.CloneTree();
                return false;
            }

            m_Assets.Clear();
            m_Assets = null;
            return true;
        }

        bool PropertyBagsWereGenerated()
        {
            if (m_PropertyBagTypes == null)
                return true;

            if (m_PropertyBagTypeIndex < m_PropertyBagTypes.Count)
            {
                var type = m_PropertyBagTypes[m_PropertyBagTypeIndex++];
                PropertyBag.GetPropertyBag(type);
                return false;
            }

            m_PropertyBagTypes.Clear();
            m_PropertyBagTypes = null;
            return true;
        }
    }

    public static void Prepare()
    {
        if (Application.isBuildingEditorResources)
            return;

        if (!UIToolkitAuthoringSettings.EnableHierarchyIntegration)
        {
            UIToolkitAuthoringSettings.HierarchyIntegrationChanged += OnEnableHierarchyIntegration;
            return;
        }

        // A lot of time is spent on Mono.JIT during the first frame, so we'll create the elements by blocks across multiple frames.
        var defaultContent = DefaultContent;
        if (defaultContent == null || !defaultContent)
            return;
        new Initializer(new List<VisualTreeAsset>(defaultContent.templateDependencies));
    }

    static void OnEnableHierarchyIntegration(bool enabled)
    {
        if (!enabled || s_DefaultContent != null)
            return;

        Prepare();
        UIToolkitAuthoringSettings.HierarchyIntegrationChanged -= OnEnableHierarchyIntegration;
    }

    private static StyleInspectorDefaultContent Create()
    {
        return new StyleInspectorDefaultContent();
    }

    public static StyleInspectorDefaultContent Get()
    {
        return s_StyleInspectorDefaultContentPool.Get();
    }

    public static void Release(StyleInspectorDefaultContent element)
    {
        s_StyleInspectorDefaultContentPool.Release(element);
    }

    private TemplateContainer m_TempTimeSlicedContent;
    private IVisualElementScheduledItem m_TempTimeSlicedCloneScheduledItem;

    internal event Action<StyleInspectorDefaultContent> contentWasGenerated;

    public StyleInspectorDefaultContent()
    {
        if (s_TimeSlice)
        {
            CreateTimeSlicedContent();
        }
        else
        {
            DefaultContent.CloneTree(this);
            contentWasGenerated?.Invoke(this);
        }
    }

    private void CreateTimeSlicedContent()
    {
        m_TempTimeSlicedContent = DefaultContent.CloneTree();

        // Transfer style sheets
        for(var i = 0; i < m_TempTimeSlicedContent.styleSheets.count; ++i)
        {
            styleSheets.Add(m_TempTimeSlicedContent.styleSheets[i]);
        }
        m_TempTimeSlicedCloneScheduledItem = schedule.Execute(TimeSliceContent).Every(1);
    }

    void TimeSliceContent()
    {
        const int iterationsPerTick = 4;
        var currentIteration = 0;

        while (m_TempTimeSlicedContent.childCount > 0)
        {
            var section = m_TempTimeSlicedContent[0];
            Add(section);
            if (++currentIteration == iterationsPerTick)
                return;
        }
        m_TempTimeSlicedCloneScheduledItem.Pause();
        s_TimeSlice = false;
        m_TempTimeSlicedContent = null;
        contentWasGenerated?.Invoke(this);
    }
}
