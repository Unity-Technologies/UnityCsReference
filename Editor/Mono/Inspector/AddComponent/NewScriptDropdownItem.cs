// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    class NewScriptDropdownItem : AdvancedDropdownItem
    {
        private readonly char[] kInvalidPathChars = new char[] {'<', '>', ':', '"', '|', '?', '*', (char)0};
        private readonly char[] kPathSepChars = new char[] {'/', '\\'};
        private readonly string kResourcesTemplatePath = "Resources/ScriptTemplates";

        private string m_Directory = string.Empty;

        internal string m_ClassName = "NewBehaviourScript";

        public string className
        {
            set { m_ClassName = value; }
            get { return m_ClassName; }
        }

        internal enum Language
        {
            CSharp = 0
        }

        public NewScriptDropdownItem() : base("New Script", -1) {}

        public override bool OnAction()
        {
            Create(AddComponentWindow.s_AddComponentWindow.m_GameObjects);
            GUIUtility.ExitGUI();
            return true;
        }

        internal bool CanCreate()
        {
            return m_ClassName.Length > 0 &&
                !File.Exists(TargetPath()) &&
                !ClassAlreadyExists() &&
                !ClassNameIsInvalid() &&
                !InvalidTargetPath();
        }

        public string GetError()
        {
            // Create string to tell the user what the problem is
            var blockReason = string.Empty;
            if (m_ClassName != string.Empty)
            {
                if (File.Exists(TargetPath()))
                    blockReason = "A script called \"" + m_ClassName + "\" already exists at that path.";
                else if (ClassAlreadyExists())
                    blockReason = "A class called \"" + m_ClassName + "\" already exists.";
                else if (ClassNameIsInvalid())
                    blockReason = "The script name may only consist of a-z, A-Z, 0-9, _.";
                else if (InvalidTargetPath())
                    blockReason = "The folder path contains invalid characters.";
            }
            return blockReason;
        }

        internal void Create(GameObject[] gameObjects)
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

            AddComponentWindow.SendUsabilityAnalyticsEvent(new AddComponentWindow.AnalyticsEventData
            {
                name = m_ClassName,
                filter = AddComponentWindow.s_AddComponentWindow.searchString,
                isNewScript = true
            });

            AddComponentWindow.s_AddComponentWindow.Close();
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
            return !System.CodeDom.Compiler.CodeGenerator.IsValidLanguageIndependentIdentifier(m_ClassName);
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

        private void CreateScript()
        {
            var basePath = Path.Combine(EditorApplication.applicationContentsPath, kResourcesTemplatePath);
            var templatePath = Path.Combine(basePath, "81-C# Script-NewBehaviourScript.cs.txt");
            ProjectWindowUtil.CreateScriptAssetFromTemplate(TargetPath(), templatePath);
            AssetDatabase.Refresh();
        }
    }
}
