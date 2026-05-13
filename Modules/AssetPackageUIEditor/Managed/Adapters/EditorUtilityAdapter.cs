// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.AssetPackage;

internal interface IEditorUtilityAdapter
{
    public string GetInvalidFilenameChars();
    public bool DisplayDialog(string title, string message, string ok, string cancel);
    public string SaveFilePanel(string title, string directory, string defaultName, string extension);
}

internal class EditorUtilityAdapter : IEditorUtilityAdapter
{
    public string GetInvalidFilenameChars() => EditorUtility.GetInvalidFilenameChars();

    public bool DisplayDialog(string title, string message, string ok, string cancel) => EditorUtility.DisplayDialog(title, message, ok, cancel);

    public string SaveFilePanel(string title, string directory, string defaultName, string extension) => EditorUtility.SaveFilePanel(title, directory, defaultName, extension);
}
