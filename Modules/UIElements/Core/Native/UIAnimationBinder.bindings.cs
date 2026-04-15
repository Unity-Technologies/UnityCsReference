// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Properties;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.UIR;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UIElements.Experimental;

// This bindings.cs file will be processed by the script binding generator
// which will generate c++ code to bind this c# class with the c++ one.
namespace UnityEngine.UIElements
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct UIAnimationBoundProperty
    {
        public int elementIndex;
        public int propertyId;
        public int channel;
        public float currentValue;
        public EntityId currentObjectValue;
        private uint hash; // Do not use on the managed side
        int currentValueUpToDate;
    };

    [NativeHeader("Modules/UIElements/Core/Native/UIAnimationBinder.h")]
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal sealed partial class UIAnimationBinder : Object, IValueAnimationUpdate
    {
        extern private void Internal_AssignKnownElementNames(string[] names, PropertyName[] propertyHashes);
        extern private void Internal_ApplyBoundValues();

        VisualElement rootVisualElement { get; set; }

        // Event invoked when element caches are cleared (for external selection cache management)
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal event Action ElementCachesClearedEvent;

        private bool exposeRootElement;
        internal void RegisterRootDocument(VisualElement element, bool exposeRootElement)
        {
            if (rootVisualElement != null)
            {
                rootVisualElement.UnregisterAnimation(this);
            }
            this.exposeRootElement = exposeRootElement;
            rootVisualElement = element;

            rootVisualElement.RegisterAnimation(this);

            ClearElementCaches();
        }

        void OnEnable()
        {
            if(m_ElementsMap != null)
            {
                ClearElementNames();
            }
        }

        [RequiredMember]
        [RequiredByNativeCode(Optional = true)]
        void ClearElementCaches()
        {
            ClearElementNames();
            m_Elements?.Clear();
            m_ElementsMap?.Clear();
            ElementCachesClearedEvent?.Invoke();
        }

        [RequiredMember]
        [RequiredByNativeCode(Optional = true)]
        void UnregisterRootElement()
        {
            if(rootVisualElement != null)
            {
                rootVisualElement.UnregisterAnimation(this);
                rootVisualElement= null;
                ClearElementCaches();
            }
        }

        void ClearElementNames()
        {
            var names = Array.Empty<string>();
            var propertyNames = Array.Empty<PropertyName>();

            Internal_AssignKnownElementNames(names, propertyNames);
        }

        [RequiredMember]
        [RequiredByNativeCode(Optional = true)]
        internal void UpdateElementNames()
        {
            if (m_Elements == null)
            {
                m_Elements = new();
                m_ElementsMap = new();
            }
            else
            {
                m_Elements.Clear();
                m_ElementsMap.Clear();
                ElementCachesClearedEvent?.Invoke();

            }

            var animationRoot = rootVisualElement;

            if (animationRoot != null)
            {
                GatherAnimatableElements(string.Empty, animationRoot, !exposeRootElement);
            }

            var names = new string[m_Elements.Count]; //TODO: Don't allocate each time
            var propertyNames = new PropertyName[m_Elements.Count]; //TODO: Don't allocate each time

            for (int i = 0; i < m_Elements.Count; i++)
            {
                var name = m_Elements[i].Key;
                names[i] = name;
                propertyNames[i] = new PropertyName(name);
            }


            Internal_AssignKnownElementNames(names, propertyNames);
        }


        static internal Object ReadCurrentObjectReferenceValue(VisualElement element, StylePropertyId propertyID, int channel)
        {
            const Object invalid = null;
            switch (propertyID)
            {
                //Background (Image/Sprite/VectorImage/etc
                case StylePropertyId.BackgroundImage:
                    return element.resolvedStyle.backgroundImage.GetSelectedImage();
                case StylePropertyId.UnityFont:
                    return element.resolvedStyle.unityFont;
                case StylePropertyId.UnityFontDefinition:
                    return element.resolvedStyle.unityFontDefinition.font;

                default:
                    return invalid;
            }
        }

        void GatherAnimatableElements(string path, VisualElement element, bool skipElement)
        {
            string subPath = path;

            if (!string.IsNullOrEmpty(element.name) && !skipElement)
            {
                if (string.IsNullOrEmpty(path))
                {
                    subPath = $"#{element.name}";
                }
                else
                {
                    subPath = $"{path}/#{element.name}";
                }

                var propName = new PropertyName(subPath);

                if(!m_ElementsMap.ContainsKey(propName))
                {
                    m_Elements.Add(new KeyValuePair<string, VisualElement>(subPath, element));
                    m_ElementsMap[propName] = element;
                }
            }

            for (int i = 0; i < element.childCount; i++)
            {
                var child = element[i];
                GatherAnimatableElements(subPath, child, false);
            }
        }


        internal void ApplyAnimatedValues()
        {
            // We shouldn't need this
            if (this == null)
            {
                UnregisterRootElement();
                return;
            }

            Internal_ApplyBoundValues();
        }

        [RequiredMember]
        [RequiredByNativeCode(Optional = true)]
        unsafe void IterateOnBoundValues(IntPtr values, int count)
        {
            var boundValues = new ReadOnlySpan<UIAnimationBoundProperty>(values.ToPointer(), count);

            foreach (ref readonly var boundValue in boundValues)
            {
                StylePropertyId id = (StylePropertyId)boundValue.propertyId;

                switch (id)
                {
                    case StylePropertyId.BackgroundImage:
                    case StylePropertyId.UnityFont:
                    case StylePropertyId.UnityMaterial:
                        SetObjectValue(boundValue.elementIndex, boundValue.propertyId, boundValue.channel, boundValue.currentObjectValue);
                        break;
                    default:
                        SetFloatValue(boundValue.elementIndex, boundValue.propertyId, boundValue.channel, boundValue.currentValue);
                        break;
                }
            }
        }

        [RequiredMember]
        [RequiredByNativeCode(Optional = true)]
        unsafe void FetchCurrentValue(IntPtr values, int count)
        {
            var boundValues = new Span<UIAnimationBoundProperty>(values.ToPointer(), count);

            foreach (ref var boundValue in boundValues)
            {
                StylePropertyId id = (StylePropertyId)boundValue.propertyId;

                switch (id)
                {
                    case StylePropertyId.BackgroundImage:
                    case StylePropertyId.UnityFont:
                    case StylePropertyId.UnityMaterial:
                        boundValue.currentObjectValue = GetObjectValue(boundValue.elementIndex, boundValue.propertyId, boundValue.channel);
                        break;
                    default:
                        boundValue.currentValue = GetFloatValue(boundValue.elementIndex, boundValue.propertyId, boundValue.channel);
                        break;
                }
            }

        }

        void IValueAnimationUpdate.Tick(long currentTimeMs)
        {
            ApplyAnimatedValues();
        }

        /// <summary>
        /// Gets the VisualElement corresponding to the given property name.
        /// </summary>
        /// <param name="propertyName">The property name (e.g., "#Container/#Button/translate.x")</param>
        /// <returns>The VisualElement, or null if not found</returns>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal VisualElement GetVisualElementFromPropertyName(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return null;

            string elementPath = ExtractElementPathFromPropertyName(propertyName);
            if (string.IsNullOrEmpty(elementPath))
                return null;

            return GetVisualElementFromPath(elementPath);
        }

        private string ExtractElementPathFromPropertyName(string propertyName)
        {
            // Find the last '/' that separates element path from property name
            // Example: "#Container/#Button/translate.x" -> "#Container/#Button"
            int lastSlashIndex = propertyName.LastIndexOf('/');
            if (lastSlashIndex < 0)
                return null; // No separator found

            return propertyName.Substring(0, lastSlashIndex);
        }

        private VisualElement GetVisualElementFromPath(string elementPath)
        {
            if (m_ElementsMap == null)
                return null;

            // Try direct lookup using PropertyName
            var propName = new PropertyName(elementPath);
            if (m_ElementsMap.TryGetValue(propName, out var element))
                return element;

            return null;
        }
        /// <summary>
        /// Static callback for getting selection EntityId.
        /// Set by editor-only code (VisualElementObjectTypeCustomizer) to provide custom selection handling.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static Func<VisualElement, EntityId> s_GetSelectionEntityIdCallback;

        private VisualElement GetVisualElementForElementIndex(int elementIndex)
        {
            if (m_Elements != null && elementIndex >= 0 && elementIndex < m_Elements.Count)
            {
                return m_Elements[elementIndex].Value;
            }
            return null;
        }

        [RequiredByNativeCode(Optional = true)]
        internal void GetSelectionEntityIdForBindingIndex(int elementIndex, ref EntityId result)
        {
            VisualElement element = GetVisualElementForElementIndex(elementIndex);
            result = s_GetSelectionEntityIdCallback?.Invoke(element) ?? EntityId.None;
        }

    }
}
