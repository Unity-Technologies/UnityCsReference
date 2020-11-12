// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Text.RegularExpressions;
using NiceIO;
using UnityEditor.Scripting.Compilers;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class UnitySpecificCompilerMessages
    {
        public static void AugmentMessagesInCompilationErrorsWithUnitySpecificAdvice(CompilerMessage[] messages, EditorCompilation editorCompilation)
        {
            for (int i = 0; i != messages.Length; i++)
            {
                //we only postprocess errors
                if (messages[i].type != CompilerMessageType.Error)
                    continue;

                UnsafeErrorProcessor.PostProcess(ref messages[i], editorCompilation);
                ModuleReferenceErrorProcessor.PostProcess(ref messages[i]);
                DeterministicAssemblyVersionErrorProcessor.PostProcess(ref messages[i]);
            }
        }

        internal class ModuleReferenceErrorProcessor
        {
            static readonly Regex messageRegex = new Regex("[`']UnityEngine.(\\w*)Module,", RegexOptions.Compiled);

            private static string GetNiceDisplayNameForModule(string name)
            {
                for (int i = 1; i < name.Length; i++)
                    if (char.IsLower(name[i - 1]) && !char.IsLower(name[i]))
                    {
                        name = name.Insert(i, " ");
                        i++;
                    }

                return name;
            }

            public static void PostProcess(ref CompilerMessage message)
            {
                if (!(message.message.Contains("CS1069") || message.message.Contains("CS1070")))
                    return;

                var match = messageRegex.Match(message.message);
                if (!match.Success)
                    return;

                var index = message.message.IndexOf("Consider adding a reference to that assembly.");
                if (index != -1)
                    message.message = message.message.Substring(0, index);

                var moduleName = match.Groups[1].Value;
                message.message += $"Enable the built in package '{GetNiceDisplayNameForModule(moduleName)}' in the Package Manager window to fix this error.";
            }
        }

        internal static class DeterministicAssemblyVersionErrorProcessor
        {
            public static void PostProcess(ref CompilerMessage message)
            {
                if (message.message.Contains("CS8357"))
                    message.message = $"Deterministic compilation failed. You can disable Deterministic builds in Player Settings\n{message.message}";
            }
        }

        internal static class UnsafeErrorProcessor
        {
            public static void PostProcess(ref CompilerMessage message, EditorCompilation editorCompilation)
            {
                if (!message.message.Contains("CS0227"))
                    return;

                var customScriptAssembly = CustomScriptAssemblyFor(message, editorCompilation);

                var unityUnsafeMessage = customScriptAssembly != null
                    ? $"Enable \"Allow 'unsafe' code\" in the inspector for '{customScriptAssembly.FilePath}' to fix this error."
                    : "Enable \"Allow 'unsafe' code\" in Player Settings to fix this error.";

                message.message += $". {unityUnsafeMessage}";
            }

            private static CustomScriptAssembly CustomScriptAssemblyFor(CompilerMessage m, EditorCompilation editorCompilation)
            {
                if (editorCompilation == null)
                    return null;

                var file = new NPath(m.file).MakeAbsolute(editorCompilation.projectDirectory);

                return editorCompilation
                    .GetCustomScriptAssemblies()
                    .FirstOrDefault(c => file.IsChildOf(new NPath(c.PathPrefix).MakeAbsolute()));
            }
        }
    }
}
