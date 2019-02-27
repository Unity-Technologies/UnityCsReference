// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.UIElements.Samples
{
    [Serializable]
    internal class UIElementsSnippetAsset : ScriptableObject
    {
        public string text
        {
            get { return m_Text; }
            set { m_Text = value; }
        }

        [SerializeField]
        private string m_Text;
    }
}
