// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class ZIndexLayerSplittingDemo : ElementSnippet<ZIndexLayerSplittingDemo>
    {
        internal override void Apply(VisualElement container)
        {
            container.AddToClassList(EditorGUIUtility.isProSkin ? "dark" : "light");

            /// <sample>
            #region sample
            // This demo is static; the stacking is expressed entirely in USS.
            #endregion
            /// </sample>
        }
    }
}
