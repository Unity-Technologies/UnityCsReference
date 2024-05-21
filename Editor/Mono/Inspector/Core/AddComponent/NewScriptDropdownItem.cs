// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditorInternal;
using Microsoft.CSharp;

namespace UnityEditor.AddComponent
{
    class NewScriptDropdownItem : ComponentDropdownItem
    {
        private readonly char[] kInvalidPathChars = new char[] {'<', '>', ':', '"', '|', '?', '*', (char)0};
        private readonly char[] kPathSepChars = new char[] {'/', '\\'};
        private static System.CodeDom.Compiler.CodeDomProvider s_CSharpDOMProvider;

        private string m_Directory = string.Empty;

        internal string m_ClassName = "NewBehaviourScript";

        public string className
        {
            set { m_ClassName = value; }
            get { return m_ClassName; }
        }

        public NewScriptDropdownItem()
            : base("New Script", L10n.Tr("New Script"))
        {
        }

        internal bool CanCreate()
        {
            return m_ClassName.Length > 0 &&
                !ClassNameIsInvalidIdentifier() &&
                !ClassNameIsInvalid() &&
                !File.Exists(TargetPath()) &&
                !ClassAlreadyExists() &&
                !InvalidTargetPath();
        }

        public string GetError()
        {
            // Create string to tell the user what the problem is
            var blockReason = string.Empty;
            if (m_ClassName != string.Empty)
            {

                if (ClassNameIsInvalid())
                    blockReason = "The script name may only consist of a-z, A-Z, 0-9, _.";
                else if (ClassNameIsInvalidIdentifier())
                    blockReason = $"The script name is invalid in C#: {m_ClassName}.";
                else if (File.Exists(TargetPath()))
                    blockReason = "A script called \"" + m_ClassName + "\" already exists at that path.";
                else if (ClassAlreadyExists())
                    blockReason = "A class called \"" + m_ClassName + "\" already exists.";
                else if (InvalidTargetPath())
                    blockReason = "The folder path contains invalid characters.";
            }
            return blockReason;
        }

        internal void Create(GameObject[] gameObjects, string searchString)
        {
            if (!CanCreate())
                return;

            CreateScript();

            foreach (var go in gameObjects)
            {
                var script = AssetDatabase.LoadAssetAtPath(TargetPath(), typeof(MonoScript)) as MonoScript;
                script.SetScriptTypeWasJustCreatedFromComponentMenu();
                InternalEditorUtility.AddScriptComponentUncheckedUndoable(go, script);
            }
        }

        private bool InvalidTargetPath()
        {
            if (m_Directory.IndexOfAny(kInvalidPathChars) >= 0)
                return true;
            if (TargetDir().Split(kPathSepChars, StringSplitOptions.None).Contains(string.Empty))
                return true;
            return false;
        }

        private string TargetPath()
        {
            return Path.Combine(TargetDir(), m_ClassName + ".cs");
        }

        private string TargetDir()
        {
            return Path.Combine("Assets", m_Directory.Trim(kPathSepChars));
        }

        private bool ClassNameIsInvalid()
        {
            return !System.CodeDom.Compiler.CodeGenerator.IsValidLanguageIndependentIdentifier(className);
        }

        private bool ClassNameIsInvalidIdentifier()
        {
            return !IsValidIdentifier(m_ClassName);
        }

        internal static bool IsValidIdentifier(string className)
        {
            if (s_CSharpDOMProvider == null)
            {
                s_CSharpDOMProvider = new CSharpCodeProvider();
            }
            return s_CSharpDOMProvider.IsValidIdentifier(className);
        }

        private bool ClassExists(string className)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetType(className, false) != null);
        }

        private bool ClassAlreadyExists()
        {
            if (m_ClassName == string.Empty)
                return false;
            return ClassExists(m_ClassName);
        }

        private string GetTemplatePath()
        {
            var scriptTemplatePath = AssetsMenuUtility.GetScriptTemplatePath(ScriptTemplate.CSharp_NewBehaviourScript);
            var scriptTemplateFilename = scriptTemplatePath.Substring(scriptTemplatePath.LastIndexOf('/'));

            var localScriptTemplatePath = $"Assets/ScriptTemplates/{scriptTemplateFilename}";

            if(File.Exists(localScriptTemplatePath))
                return localScriptTemplatePath;

            return scriptTemplatePath;
        }
        private void CreateScript()
        {
            ProjectWindowUtil.CreateScriptAssetFromTemplate(TargetPath(), GetTemplatePath());
            AssetDatabase.Refresh();
        }
    }
}
