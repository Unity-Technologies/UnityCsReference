// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Scripting.Compilers
{
    /// Marks the type of a [[CompilerMessage]]
    internal enum CompilerMessageType
    {
        /// The message is an error. The compilation has failed.
        Error = 0,
        /// The message is an warning only. If there are no error messages, the compilation has completed successfully.
        Warning = 1,
        // This message type is required because "Info" is an accepted action type in Microsoft rule set files:
        // https://docs.microsoft.com/en-us/visualstudio/code-quality/working-in-the-code-analysis-rule-set-editor?view=vs-2019
        Information = 2
    }

    /// This struct should be returned from GetCompilerMessages() on ScriptCompilerBase implementations
    internal struct CompilerMessage
    {
        /// The text of the error or warning message
        public string message;
        /// The path name of the file the message refers to
        public string file;
        /// The line in the source file the message refers to
        public int line;
        /// The column of the line the message refers to
        public int column;
        /// The type of the message. Either Error or Warning
        public CompilerMessageType type;

        //This field is dead and not used. The reason it's still here is that its used through [InternalsVibislbeTo] by the burst package (as of oktober2020), so we cannot yet remove this
        // ReSharper disable once NotAccessedField.Global
        internal string assemblyName;
    }
}
