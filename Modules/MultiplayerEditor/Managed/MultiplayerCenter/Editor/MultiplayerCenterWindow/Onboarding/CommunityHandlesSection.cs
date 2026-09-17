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
    /// Provides shortcuts in the <see cref="OnboardingSectionCategory.GettingStarted"/> section
    /// for users to access the community links, such as discord and discussions.
    /// </summary>
    [UsedImplicitly]
    class CommunityHandlesSection : OnboardingGUIProvider<CommunityHandlesSection>
    {
        public override (OnboardingSectionCategory, int)[] Categories => new[]
        {
            (OnboardingSectionCategory.GettingStarted, 160),
            (OnboardingSectionCategory.Other, -100)
        };


        public override VisualElement CreateGUI()
        {
            var asset = EditorGUIUtility.LoadRequired(
                "Multiplayer/MultiplayerCenter/UI/Community.uxml") as VisualTreeAsset;

            var root =new VisualElement();

            asset.CloneTree(root);

            return root;

        }

    }
}
