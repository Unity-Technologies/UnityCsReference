// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;

namespace Unity.UIToolkit.Editor.Importers;

internal class TempVisualTreeAssetImporter : UXMLImporterImpl
{
    public override UnityEngine.Object DeclareDependencyAndLoad(string path)
    {
        return EditorGUIUtility.Load(path);
    }
}
