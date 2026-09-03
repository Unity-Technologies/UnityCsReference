// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class ZIndexTransitionsDemo : ElementSnippet<ZIndexTransitionsDemo>
    {
        internal override void Apply(VisualElement container)
        {
            container.AddToClassList(EditorGUIUtility.isProSkin ? "dark" : "light");

            /// <sample>
            #region sample
            var hero = container.Q("hero-card");
            hero.RegisterCallback<PointerDownEvent>(_ => hero.ToggleInClassList("raised"));

            var heroB = container.Q("hero-card-b");
            heroB.RegisterCallback<PointerDownEvent>(_ => heroB.ToggleInClassList("raised"));
            #endregion
            /// </sample>
        }
    }
}
