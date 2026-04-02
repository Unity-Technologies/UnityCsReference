// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Multiplayer.Internal;
using System;
using UnityEditor.Build.Profile;

namespace Unity.Multiplayer.Editor
{
    internal static class BuildOptionsExtensions
    {
        private static List<IMultiplayerBuildOptionsSection> s_BuildOptionsSections;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            s_BuildOptionsSections = new List<IMultiplayerBuildOptionsSection>();

            foreach(var t in TypeCache.GetTypesDerivedFrom<IMultiplayerBuildOptionsSection>())
            {
                s_BuildOptionsSections.Add((IMultiplayerBuildOptionsSection)Activator.CreateInstance(t));
            }
            s_BuildOptionsSections.Sort((a, b) => a.Order.CompareTo(b.Order));

            EditorMultiplayerManager.drawingMultiplayerBuildOptionsForBuildProfile += OnDrawingBuildOptions;
        }

        private static void OnDrawingBuildOptions(BuildProfile profile)
        {
            foreach (var section in s_BuildOptionsSections)
            {
                section.DrawBuildOptions(profile);
            }
        }
    }
}
