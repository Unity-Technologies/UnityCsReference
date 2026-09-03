// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class ZIndexPopupDemo : ElementSnippet<ZIndexPopupDemo>
    {
        internal override void Apply(VisualElement container)
        {
            container.AddToClassList(EditorGUIUtility.isProSkin ? "dark" : "light");

            /// <sample>
            #region sample
            var popupOverlay = container.Q("popup-overlay");
            container.Q<Button>("open-popup-btn").clicked += () => popupOverlay.style.display = DisplayStyle.Flex;
            container.Q<Button>("close-popup-btn").clicked += () => popupOverlay.style.display = DisplayStyle.None;
            #endregion
            /// </sample>
        }
    }
}
