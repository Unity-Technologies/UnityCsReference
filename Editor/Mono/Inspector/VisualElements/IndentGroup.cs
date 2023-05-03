// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class IndentGroup : Foldout
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : Foldout.UxmlSerializedData
        {
            public override object CreateInstance() => new IndentGroup();
        }

        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public new class UxmlFactory : UxmlFactory<IndentGroup, UxmlTraits>
        {
        }

        const string k_IndentStyle = "unity-foldout__content";

        public IndentGroup()
        {
            toggle.style.display = DisplayStyle.None;
            value = true;
            contentContainer.EnableInClassList(k_IndentStyle, true);
        }
    }
}
