// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    /// <summary>
    /// Add this interface to all classes that can generate part of the content of a dynamic hint by examining an instance of an object
    /// </summary>
    internal interface IComponentDynamicHintContentGenerator
    {
        VisualElement CreateContent(GameObject obj);
    }

    /// <summary>
    /// Add this interface to all classes that can generate part of the content of a dynamic hint by examining an asset
    /// </summary>
    internal interface IAssetDynamicHintContentGenerator
    {
        VisualElement CreateContent(UnityEngine.Object obj);
    }

    /// <summary>
    /// Base class for all classes that can generate part of the content of a dynamic hint by examining an instance of a ScriptableObject
    /// </summary>
    /// <typeparam name="T">The component for which to generate the content</typeparam>
    internal abstract class ScriptableObjectContentGenerator<T> : IAssetDynamicHintContentGenerator where T : ScriptableObject
    {
        readonly string m_Title;
        static T s_ComponentCache;

        protected ScriptableObjectContentGenerator(string title)
        {
            m_Title = title;
        }

        /// <summary>
        /// Defines the logic with which the object is examined to generate the content of the hint
        /// </summary>
        /// <param name="root">The root visualElement of the hint</param>
        /// <param name="objectToAnalyze">The object this Generator will examine</param>
        protected abstract void GenerateContent(VisualElement root, T objectToAnalyze);

        public VisualElement CreateContent(UnityEngine.Object obj)
        {
            s_ComponentCache = obj as T;
            if (s_ComponentCache == null) { return null; }

            VisualElement content = new VisualElement();
            content.style.marginTop = 4;

            Label title = new Label();
            title.style.marginBottom = 2;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.text = m_Title;
            content.Add(title);

            GenerateContent(content, s_ComponentCache);
            return content;
        }

        protected VisualElement AddInfoField(string label, string value)
        {
            VisualElement root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.flexGrow = 1;
            root.style.height = 15;
            root.Add(new Label(label));
            Label valueLabel = new Label(value);
            root.Add(valueLabel);
            valueLabel.style.unityTextAlign = TextAnchor.UpperRight;
            return root;
        }
    }

    /// <summary>
    /// Base class for all classes that can generate part of the content of a dynamic hint by examining an instance of a GameObject
    /// </summary>
    /// <typeparam name="T">The component for which to generate the content</typeparam>
    internal abstract class ComponentContentGenerator<T> : IComponentDynamicHintContentGenerator where T : Component
    {
        readonly string m_Title;
        static readonly List<T> s_ComponentCache = new List<T>();

        protected ComponentContentGenerator(string title)
        {
            m_Title = title;
        }

        /// <summary>
        /// Defines the logic with which the targeted Components of an object are examined to generate the content of the hint
        /// </summary>
        /// <param name="root">The root visualElement of the hint</param>
        /// <param name="components">The components this Generator will examine</param>
        protected abstract void GenerateContent(VisualElement root, List<T> components);

        public VisualElement CreateContent(GameObject obj)
        {
            obj.GetComponents(s_ComponentCache);
            if (s_ComponentCache.Count == 0) { return null; }

            VisualElement content = new VisualElement();
            content.style.marginTop = 4;

            Label title = new Label();
            title.style.marginBottom = 2;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.text = m_Title;
            content.Add(title);

            GenerateContent(content, s_ComponentCache);
            return content;
        }

        protected VisualElement AddInfoField(string label, string value)
        {
            VisualElement root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.flexGrow = 1;
            root.style.height = 15;
            root.Add(new Label(label));
            Label valueLabel = new Label(value);
            root.Add(valueLabel);
            valueLabel.style.unityTextAlign = TextAnchor.UpperRight;
            return root;
        }
    }

    /// <summary>
    /// Base class for all dynamic hints
    /// </summary>
    internal abstract class DynamicHintContent
    {
        protected VisualElement root;
        bool m_Extended;

        /// <summary>
        /// Gets or sets the state of the hint.
        /// </summary>
        public bool Extended
        {
            get { return m_Extended; }
            set
            {
                if (m_Extended == value) { return; }

                m_Extended = value;
                OnExtendedStateChanged(m_Extended);
            }
        }

        /// <summary>
        /// Called whenever the Extended state of the hint changes.
        /// </summary>
        /// <param name="extended"></param>
        protected internal virtual void OnExtendedStateChanged(bool extended) {}

        /// <summary>
        /// Override this to update the content of the dynamic hint every editor frame (I.E: when playing videos)
        /// </summary>
        internal virtual void Update() {}

        /// <summary>
        /// Override this to load the UXML + USS of your dynamic hint and to initialize its UI logic and callbacks
        /// </summary>
        /// <returns></returns>
        protected internal virtual VisualElement CreateContent() { return null; }

        /// <summary>
        /// Override this in order to return the right dimensions dependning on the dynamic hints' state and displayed controls
        /// </summary>
        /// <returns></returns>
        protected internal virtual Vector2 GetContentSize() { return Vector2.zero; }

        internal Rect GetRect() { return root.contentRect; }

        /// <summary>
        /// Loads the default StyleSheet according to the desired type of dynamic hint.
        /// The StyleSheet will be added as the first StyleSheet of the element.
        /// </summary>
        /// <param name="element">The element to which the StyleSheet will be added to. This is usually the root of your dynamic hint.</param>
        /// <param name="useInstanceTooltipStyleSheet">If true, the StyleSheet commonly used in dynamic hints that represent data of an instance of an Object will be applied.
        /// Otherwise, the style commonly applied to dynamic hints that explain how properties or editor tools work will be applied.</param>
        protected void AddDefaultStyleSheetAsFirstTo(VisualElement element, bool useInstanceTooltipStyleSheet)
        {
            StyleSheet defaultStyleSheet = useInstanceTooltipStyleSheet ? DynamicHintUtility.GetDefaultInstanceDynamicHintStyleSheet()
                : DynamicHintUtility.GetDefaultDynamicHintStyleSheet();

            if (element.styleSheets.count > 0)
            {
                element.styleSheetList.Insert(0, defaultStyleSheet);
            }
            else
            {
                element.styleSheets.Add(defaultStyleSheet);
            }
        }

        internal string ToTooltipString() { return DynamicHintUtility.Serialize(this); }

        /// <summary>
        /// Converts the hint to a string
        /// </summary>
        /// <param name="hint">the hint to convert</param>
        public static implicit operator string(DynamicHintContent hint) { return hint.ToTooltipString(); }
    }

    /// <summary>
    /// Tells the Dynamic Hint system which method to use to generate the Dynamic Hint of a specific class.
    /// Must be placed on a static method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal /*sealed*/ class DynamicHintGeneratorAttribute : Attribute //todo: make this sealed once it becomes public. For now, it can't be sealed as internal bridges need to inherit from it
    {
        internal Type m_Type;

        public DynamicHintGeneratorAttribute(Type type)
        {
            m_Type = type;
        }
    }
}
