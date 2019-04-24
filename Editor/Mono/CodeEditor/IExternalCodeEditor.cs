// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.CodeEditor
{
    public interface IExternalCodeEditor
    {
        CodeEditor.Installation[] Installations { get; }
        bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation);
        void OnGUI();
        void SyncIfNeeded(string[] addedFiles, string[] deletedFiles, string[] movedFiles, string[] movedFromFiles, string[] importedFiles);
        void SyncAll();
        void Initialize(string editorInstallationPath);
        bool OpenProject(string filePath = "", int line = -1, int column = -1);
    }
}
