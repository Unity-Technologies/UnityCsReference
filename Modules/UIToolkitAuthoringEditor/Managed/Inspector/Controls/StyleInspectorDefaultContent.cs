// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Properties;
using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class StyleInspectorDefaultContent : VisualElement
{
    static bool s_TimeSlice = true;
    static VisualTreeAsset s_DefaultContent;
    static UnityEngine.Pool.ObjectPool<StyleInspectorDefaultContent> s_StyleInspectorDefaultContentPool;

    class Initializer
    {
        struct Job(List<Type> types) : IJob
        {
            private List<Type> m_Types = types;

            public void Execute()
            {
                foreach (var type in m_Types)
                {
                    PropertyBag.GetPropertyBag(type);
                }
                m_Types.Clear();
                m_Types = null;

                PropertyBag.GetPropertyBag<VisualElementSelection>();
                PropertyBag.GetPropertyBag<VisualElementInspector>();
                PropertyBag.GetPropertyBag<VisualElement>();
                PropertyBag.GetPropertyBag<Toggle>();
                PropertyBag.GetPropertyBag<TextField>();
                PropertyBag.GetPropertyBag<StyleTransitionListView>();
            }
        }

        private List<VisualTreeAsset> m_Assets;
        private List<Type> m_PropertyBagTypes;
        private int m_AssetIndex;

        public Initializer(List<VisualTreeAsset> assets)
        {
            m_Assets = assets;
            // No initialization to do.
            if (m_Assets?.Count == 0)
                return;

            m_PropertyBagTypes = new List<Type>();
            m_AssetIndex = 0;
            EditorApplication.tick += SlowlyCreateContent;
        }

        void SlowlyCreateContent()
        {
            if (m_AssetIndex < m_Assets.Count)
            {
                var vta = m_Assets[m_AssetIndex++];
                vta.CloneTree();
                return;
            }

            m_Assets.Clear();
            m_Assets = null;

            EditorApplication.tick -= SlowlyCreateContent;
            // Prepopulate the content.
            using var handle = s_StyleInspectorDefaultContentPool.Get(out var _);

            new Job(m_PropertyBagTypes).Execute();
        }
    }

    public static void Prepare()
    {
        // A lot of time is spent on Mono.JIt during the first frame, so we'll create the elements by blocks across multiple frames.
        s_DefaultContent = EditorGUIUtility.Load("UIToolkitAuthoring/Inspector/StyleInspector/StyleInspectorDefaultContent.uxml") as VisualTreeAsset;

        if (s_DefaultContent == null || !s_DefaultContent)
            return;
        s_StyleInspectorDefaultContentPool = new UnityEngine.Pool.ObjectPool<StyleInspectorDefaultContent>(Create, null, null, null);
        var initializer = new Initializer(new List<VisualTreeAsset>(s_DefaultContent.templateDependencies));
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
        Assert.IsNotNull(s_DefaultContent);
        if (s_TimeSlice)
        {
            CreateTimeSlicedContent();
        }
        else
        {
            s_DefaultContent.CloneTree(this);
            contentWasGenerated?.Invoke(this);
        }
    }

    private void CreateTimeSlicedContent()
    {
        m_TempTimeSlicedContent = s_DefaultContent.CloneTree();

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
