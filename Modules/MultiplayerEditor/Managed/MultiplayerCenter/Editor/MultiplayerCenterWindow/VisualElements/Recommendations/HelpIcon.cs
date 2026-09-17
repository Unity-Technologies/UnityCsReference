// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Center.Editor
{
    [UxmlElement]
    partial class HelpIcon : VisualElement
    {
        string m_PackageName;

        [UxmlAttribute, CreateProperty]
        public string DocumentationUrl { get; set; }

        public string PackageName
        {
            get => m_PackageName;
            set{
                m_PackageName = value;
                tooltip = L10n.Tr($"Navigates to the {m_PackageName} associated online documentation.", null);
            }
        }

        public HelpIcon()
        {
            this.AddManipulator(new Clickable(OnClick));
        }

        void OnClick()
        {
            Application.OpenURL(DocumentationUrl);
        }
    }
}
