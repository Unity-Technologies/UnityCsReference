// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Assemblies;
using Unity.Collections;

namespace UnityEditor.AddComponent
{
    class NewScriptDropdownItem : ComponentDropdownItem
    {
        private const string NewScriptDirectoryPref = "NewScriptDropdownItem.m_Directory";

        private readonly char[] kInvalidPathChars = new char[] {'<', '>', ':', '"', '|', '?', '*', (char)0};
        private readonly char[] kPathSepChars = new char[] {'/', '\\'};
        [NoAutoStaticsCleanup] // static config set, allocated once, contains only string literals
        private static readonly HashSet<string> s_CSharpKeywords = new(new[]
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum",
            "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto",
            "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
            "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
            "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
            "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
            "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
            "__arglist", "__makeref", "__reftype", "__refvalue",
            // Contextual keywords, rejected as class names (matches the previous CodeDom behavior).
            "partial", "where", "yield",
        }, StringComparer.Ordinal);

        private string m_Directory = string.Empty;

        internal string m_ClassName = "NewBehaviourScript";

        public string className
        {
            set => m_ClassName = value;
            get => m_ClassName;
        }

        public NewScriptDropdownItem() : base("New Script", L10n.Tr("New Script", null)) {}

        internal bool CanCreate() =>
            m_ClassName.Length > 0 &&
            !ClassNameIsInvalidIdentifier() &&
            !File.Exists(TargetPath()) &&
            !ClassAlreadyExists() &&
            !InvalidTargetPath();

        public string GetError()
        {
            // Create string to tell the user what the problem is
            var blockReason = string.Empty;
            if (m_ClassName != string.Empty)
            {
                if (m_ClassName.Contains('.'))
                    blockReason = "The script name may only consist of a-z, A-Z, 0-9, _ characters.";
                else if (ClassNameIsInvalidIdentifier())
                    blockReason = $"The script name is invalid in C#: {m_ClassName}.";
                else if (File.Exists(TargetPath()))
                    blockReason = $"A script called \"{m_ClassName}\" already exists at that path.";
                else if (ClassAlreadyExists())
                    blockReason = $"A class called \"{m_ClassName}\" already exists.";
                else if (InvalidTargetPath())
                    blockReason = "The folder path contains invalid characters.";
            }
            return blockReason;
        }

        internal void Create(GameObject[] gameObjects, string searchString)
        {
            m_Directory = EditorPrefs.GetString(NewScriptDirectoryPref);

            if (!CanCreate())
                return;

            if (!CreateScript())
                return;

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

        private string TargetPath() => Path.Combine(TargetDir(), m_ClassName + ".cs");

        private string TargetDir() => Path.Combine("Assets", m_Directory.Trim(kPathSepChars));

        private void SetDirectory(string newDir)
        {
            m_Directory = newDir;
            EditorPrefs.SetString(NewScriptDirectoryPref, newDir);
        }

        private bool ClassNameIsInvalidIdentifier() => !IsValidIdentifier(m_ClassName);

        // Validates a C# class name usable as both class name and file name: no '@' verbatim prefix, no reserved keywords.
        internal static bool IsValidIdentifier(string className) =>
            HasValidIdentifierCharacters(className) && !s_CSharpKeywords.Contains(className);

        // Character rules only, without the keyword check. For names that are not C# identifiers themselves,
        // such as USS and UXML file names, where a keyword like "base" is a perfectly good name.
        internal static bool HasValidIdentifierCharacters(string name)
        {
            if (string.IsNullOrEmpty(name) || !IsIdentifierStartChar(name[0]))
                return false;

            for (var i = 1; i < name.Length; i++)
            {
                if (!IsIdentifierPartChar(name[i]))
                    return false;
            }

            return true;
        }

        private static bool IsIdentifierStartChar(char c)
        {
            if (c == '_')
                return true;

            switch (char.GetUnicodeCategory(c))
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.LetterNumber:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsIdentifierPartChar(char c)
        {
            if (IsIdentifierStartChar(c))
                return true;

            switch (char.GetUnicodeCategory(c))
            {
                case UnicodeCategory.DecimalDigitNumber:
                case UnicodeCategory.ConnectorPunctuation:
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.SpacingCombiningMark:
                case UnicodeCategory.Format:
                    return true;
                default:
                    return false;
            }
        }

        private bool ClassExists(string className) => CurrentAssemblies.GetLoadedAssemblies().Exists(a => a.GetType(className, false) != null);
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
        private bool CreateScript()
        {
            var userPath = EditorUtility.SaveFilePanel("Create MonoBehaviour Script", TargetDir(), m_ClassName, "cs");
            if (string.IsNullOrEmpty(userPath)) return false;

            var indexToLocal = userPath.IndexOf(Application.dataPath);
            if (indexToLocal == -1)
            {
                EditorUtility.DisplayDialog("Unable to Create MonoBehaviour Script", "You can't create a script outside of the project's Assets directory. Select a location in the Assets directory instead.", "Ok");
                return false;
            }

            var localPathAndFile = userPath.Remove(indexToLocal, Application.dataPath.Length);
            var lastPathSeperator = localPathAndFile.LastIndexOf('/');
            var localPath = localPathAndFile.Substring(0, lastPathSeperator);
            SetDirectory(localPath);

            // we dont want to include the last seperator so we need a +1 & -1 here
            var returnedFileName = localPathAndFile.Substring(lastPathSeperator + 1, localPathAndFile.Length - lastPathSeperator - 1);

            var extensionIndex = returnedFileName.LastIndexOf(".cs");
            if (extensionIndex != -1) returnedFileName = returnedFileName.Substring(0, extensionIndex);

            if (returnedFileName != m_ClassName) m_ClassName = returnedFileName;

            ProjectWindowUtil.CreateScriptAssetFromTemplate(TargetPath(), GetTemplatePath());
            AssetDatabase.Refresh();

            return true;
        }
    }
}
