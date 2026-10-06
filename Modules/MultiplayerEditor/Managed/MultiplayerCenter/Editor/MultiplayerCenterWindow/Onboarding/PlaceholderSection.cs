// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using Unity.Multiplayer.Center.Common;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Center.Editor
{
    /// <summary>
    /// Provides a placeholder UI element, currently not included anywhere.
    /// Use it for pages under development only.
    /// </summary>
    [UsedImplicitly]
    class PlaceholderSection : OnboardingGUIProvider<PlaceholderSection>
    {
        public override (OnboardingSectionCategory, int)[] Categories => new[]
        {
            (OnboardingSectionCategory.Other, 0),
        };

        public override VisualElement CreateGUI()
        {
            var style = EditorGUIUtility.LoadRequired(
                "Multiplayer/MultiplayerCenter/UI/Recommendations.uss") as StyleSheet;

            var placeHodler = EditorGUIUtility.LoadRequired("Multiplayer/MultiplayerCenter/UI/PlaceHolder.uxml") as VisualTreeAsset;

            var root = new VisualElement() {
                style = { flexGrow = 1,
                    alignContent = Align.Center, paddingTop = 100, paddingBottom = 100} };
            root.styleSheets.Add(style);

            placeHodler.CloneTree(root);

            root.hierarchy[0].style.alignSelf = Align.Center;

            root.AddToClassList("recommendation-box");

            return root;
        }
    }
}
