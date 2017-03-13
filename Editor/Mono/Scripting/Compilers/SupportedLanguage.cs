// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Scripting.Compilers
{
    internal abstract class SupportedLanguage
    {
        public abstract string GetExtensionICanCompile();
        public abstract string GetLanguageName();
        public abstract ScriptCompilerBase CreateCompiler(MonoIsland island, bool buildingForEditor, BuildTarget targetPlatform, bool runUpdater);
        public virtual string GetNamespace(string fileName, string definedSymbols)
        {
            return string.Empty;
        }
    }
}
