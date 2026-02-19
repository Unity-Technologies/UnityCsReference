// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace Unity.Multiplayer.PlayMode.Editor;

static class ScenarioDialog
{
    internal static Func<string, string, string, string, bool> MockNextDialogResultForTests { private get; set; }

    public static bool DisplayDialog(string title, string message, string ok, string cancel = null)
    {
        if (MockNextDialogResultForTests != null)
        {
            var mockedDialog = MockNextDialogResultForTests;
            MockNextDialogResultForTests = null;

            return mockedDialog.Invoke(title, message, ok, cancel);
        }

        return EditorUtility.DisplayDialog(title, message, ok, cancel);
    }
}
