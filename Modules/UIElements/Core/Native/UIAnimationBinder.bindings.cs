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
        private uint hash; // Do not use on the managed side
    };

    [NativeHeader("Modules/UIElements/Core/Native/UIAnimationBinder.h")]
    internal sealed partial class UIAnimationBinder : Object, IValueAnimationUpdate
    {
        extern private void Internal_AssignKnownElementNames(string[] names, PropertyName[] propertyHashes);
        extern private void Internal_ApplyBoundValues();

        VisualElement rootVisualElement { get; set; }

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

            ClearElementNames();
        }

        void OnEnable()
        {
            if(m_ElementsMap != null)
            {
                ClearElementNames();
            }
        }

        [RequiredByNativeCode]
        void UnregisterRootElement()
        {
            if(rootVisualElement != null)
            {
                rootVisualElement.UnregisterAnimation(this);
                rootVisualElement= null;
            }
        }

        void ClearElementNames()
        {
            var names = Array.Empty<string>();
            var propertyNames = Array.Empty<PropertyName>();

            Internal_AssignKnownElementNames(names, propertyNames);
        }

        [RequiredByNativeCode]
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

        [RequiredByNativeCode]
        unsafe void IterateOnBoundValues(IntPtr values, int count)
        {
            var boundValues = new ReadOnlySpan<UIAnimationBoundProperty>(values.ToPointer(), count);

            foreach (ref readonly var boundValue in boundValues)
            {
                SetFloatValue(boundValue.elementIndex, boundValue.propertyId, boundValue.channel, boundValue.currentValue);
            }
        }

        void IValueAnimationUpdate.Tick(long currentTimeMs)
        {
            ApplyAnimatedValues();
        }
    }
}
