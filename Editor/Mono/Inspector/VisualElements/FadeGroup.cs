// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class FadeGroup : Foldout
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : Foldout.UxmlSerializedData
        {
#pragma warning disable 649
            [SerializeField] bool applyIndent;
#pragma warning restore 649

            public override object CreateInstance() => new FadeGroup();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (FadeGroup)obj;
                e.applyIndent = applyIndent;
            }
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            readonly UxmlBoolAttributeDescription m_ApplyIndent = new() { name = "apply-indent" };
            readonly UxmlBoolAttributeDescription m_Value = new() { name = "value", defaultValue = true };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ((FadeGroup)ve).applyIndent = m_ApplyIndent.GetValueFromBag(bag, cc);
                ((FadeGroup)ve).value = m_Value.GetValueFromBag(bag, cc);
            }
        }

        public new class UxmlFactory : UxmlFactory<FadeGroup, UxmlTraits> { }

        const string k_IndentStyle = "unity-foldout__content";

        public FadeGroup()
        {
            toggle.style.display = DisplayStyle.None;
            value = true;
        }

        bool m_ApplyIndent = true;
        public bool applyIndent
        {
            get => m_ApplyIndent;
            set
            {
                m_ApplyIndent = value;
                contentContainer.EnableInClassList(k_IndentStyle, m_ApplyIndent);
            }
        }
    }
}
