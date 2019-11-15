// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;

namespace Unity.Scripting.Compilation
{
    internal interface ICompilerFactoryHelper
    {
        string ReadCachedCompilerName();
        void UpdateCachedCompilerName(string name);
        string GetCurrentCompilerName();
        bool HasExternalCompiler();
    }

    internal class CompilerFactoryHelper : ICompilerFactoryHelper
    {
        private const string SessionStateKey = "CompilerFactory.Compiler";
        private const string CompilerUsedPath = "Library/UnityCSharpCompiler.json";

        public bool HasExternalCompiler()
        {
            return ExternalCSharpCompiler.HasExternalCompiler();
        }

        public string ReadCachedCompilerName()
        {
            var compilerLastUsed = SessionState.GetString(SessionStateKey, null);
            if (compilerLastUsed != null)
            {
                return compilerLastUsed;
            }
            if (!File.Exists(CompilerUsedPath))
            {
                return null;
            }

            var configData = File.ReadAllText(CompilerUsedPath);
            var compilerUsedData = JsonUtility.FromJson<CompilerUsedData>(configData);
            return compilerUsedData.Name;
        }

        public void UpdateCachedCompilerName(string name)
        {
            SessionState.SetString(SessionStateKey, name);
            Console.WriteLine($"Updating compiler used: {name}");

            var compilerUsedData = new CompilerUsedData
            {
                Name = name,
            };
            var json = JsonUtility.ToJson(compilerUsedData);
            File.WriteAllText(CompilerUsedPath, json);
        }

        public string GetCurrentCompilerName()
        {
            if (HasExternalCompiler())
            {
                return ExternalCSharpCompiler.ExternalCompilerName();
            }
            return MicrosoftCSharpCompiler.Name;
        }

        [Serializable]
        private class CompilerUsedData
        {
            public string Name;
        }
    }

    internal class CompilerFactory
    {
        private ICompilerFactoryHelper m_FactoryHelper;

        public CompilerFactory(ICompilerFactoryHelper factoryHelper)
        {
            m_FactoryHelper = factoryHelper;
        }

        public ScriptCompilerBase Create(ScriptAssembly scriptAssembly, string tempOutputDirectory)
        {
            if (scriptAssembly.Files.Length == 0)
            {
                throw new ArgumentException($"Cannot compile ScriptAssembly {scriptAssembly.Filename} with no files");
            }

            ScriptCompilerBase scriptCompilerBase;
            if (m_FactoryHelper.HasExternalCompiler())
            {
                scriptCompilerBase  = new ExternalCSharpCompiler(scriptAssembly, tempOutputDirectory);
            }
            else
            {
                scriptCompilerBase =  new MicrosoftCSharpCompiler(scriptAssembly, tempOutputDirectory);
            }

            if (CompilerChanged())
            {
                UpdateCompilerUsed();
            }

            return scriptCompilerBase;
        }

        public bool CompilerChanged()
        {
            string cachedCompilerName;
            try
            {
                cachedCompilerName = m_FactoryHelper.ReadCachedCompilerName();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Exception getting compiler name: {exception.Message} {Environment.NewLine} {exception.StackTrace}");
                return false;
            }

            if (string.IsNullOrEmpty(cachedCompilerName))
            {
                return false;
            }

            return cachedCompilerName != m_FactoryHelper.GetCurrentCompilerName();
        }

        public void UpdateCompilerUsed()
        {
            try
            {
                var compilerName = m_FactoryHelper.GetCurrentCompilerName();
                m_FactoryHelper.UpdateCachedCompilerName(compilerName);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Exception for updating last used compiler: {exception.Message} {Environment.NewLine} {exception.StackTrace}");
            }
        }
    }
}
