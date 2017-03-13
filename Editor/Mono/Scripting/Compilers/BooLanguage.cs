// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Boo.Lang.Parser;

using System.Linq;

namespace UnityEditor.Scripting.Compilers
{
    internal class BooLanguage : SupportedLanguage
    {
        public override string GetExtensionICanCompile()
        {
            return "boo";
        }

        public override string GetLanguageName()
        {
            return "Boo";
        }

        public override ScriptCompilerBase CreateCompiler(MonoIsland island, bool buildingForEditor, BuildTarget targetPlatform, bool runUpdater)
        {
            return new BooCompiler(island, runUpdater);
        }

        public override string GetNamespace(string fileName, string definedSymbols)
        {
            try
            {
                return BooParser.ParseFile(fileName).Modules.First().Namespace.Name;
            }
            catch {}

            return base.GetNamespace(fileName, definedSymbols);
        }
    }
}
