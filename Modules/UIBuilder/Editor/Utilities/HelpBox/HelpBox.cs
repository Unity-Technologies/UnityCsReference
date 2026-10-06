// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    [UxmlElement]
    partial class HelpBox : BindableElement
    {
        [UxmlAttribute]
        public string text { get; set; }

        public HelpBox()
        {
            Add(new IMGUIContainer(() =>
            {
                EditorGUILayout.HelpBox(text, MessageType.Info, true);
            }));
        }
    }
}
