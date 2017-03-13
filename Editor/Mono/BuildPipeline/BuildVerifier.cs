// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Xml.XPath;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace UnityEditor
{
    /// <summary>
    /// Class intented to verify if build will compile / work on specified target platform
    /// Currently only managed references are verified
    /// </summary>
    internal class BuildVerifier
    {
        private Dictionary<string, HashSet<string>> m_UnsupportedAssemblies = null;
        private static BuildVerifier ms_Inst = null;

        protected BuildVerifier()
        {
            m_UnsupportedAssemblies = new Dictionary<string, HashSet<string>>();

            var configPath = Path.Combine(Path.Combine(EditorApplication.applicationContentsPath, "Resources"), "BuildVerification.xml");
            var doc  = new XPathDocument(configPath);
            var navigator = doc.CreateNavigator();
            navigator.MoveToFirstChild();

            var it = navigator.SelectChildren("assembly", "");

            while (it.MoveNext())
            {
                string name = it.Current.GetAttribute("name", "");
                if (string.IsNullOrEmpty(name))
                    throw new ApplicationException(string.Format("Failed to load {0}, <assembly> name attribute is empty", configPath));

                string platform = it.Current.GetAttribute("platform", "");
                if (string.IsNullOrEmpty(platform))
                    platform = "*";

                if (!m_UnsupportedAssemblies.ContainsKey(platform))
                    m_UnsupportedAssemblies.Add(platform, new HashSet<string>());

                m_UnsupportedAssemblies[platform].Add(name);
            }
        }

        protected void VerifyBuildInternal(BuildTarget target, string managedDllFolder)
        {
            foreach (var file in Directory.GetFiles(managedDllFolder))
            {
                if (file.EndsWith(".dll"))
                {
                    var fname = Path.GetFileName(file);
                    if (!VerifyAssembly(target, fname))
                        Debug.LogWarningFormat(
                            "{0} assembly is referenced by user code, but is not supported" +
                            " on {1} platform. Various failures might follow.", fname, target.ToString());
                }
            }
        }

        protected bool VerifyAssembly(BuildTarget target, string assembly)
        {
            if (m_UnsupportedAssemblies.ContainsKey("*") && m_UnsupportedAssemblies["*"].Contains(assembly) ||
                m_UnsupportedAssemblies.ContainsKey(target.ToString()) && m_UnsupportedAssemblies[target.ToString()].Contains(assembly))
                return false;

            return true;
        }

        public static void VerifyBuild(BuildTarget target, string managedDllFolder)
        {
            if (ms_Inst == null)
                ms_Inst = new BuildVerifier();

            ms_Inst.VerifyBuildInternal(target, managedDllFolder);
        }
    }
}
