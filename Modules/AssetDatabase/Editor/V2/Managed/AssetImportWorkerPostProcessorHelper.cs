// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace UnityEditor.AssetImporters
{
    internal class StaticFieldCollector
    {
        private static IReadOnlyList<FieldInfo> GetAllStaticFieldsInPostProcessorsAndScriptedImporters(IList<Type> specificAssetPostProcessors = null, IList<Type> specificScriptedImporters = null)
        {
            List<FieldInfo> staticFields = new List<FieldInfo>();

            try
            {
                var ignoreFieldsHashSet = new HashSet<FieldInfo>(TypeCache.GetFieldsWithAttribute<AssetPostprocessorStaticVariableIgnoreAttribute>());

                // Get all types in the assembly
                IList<Type> assetPostProcessors = specificAssetPostProcessors ?? TypeCache.GetTypesDerivedFrom<AssetPostprocessor>();
                CollectOffendingFields(assetPostProcessors, staticFields, ignoreFieldsHashSet);

                IList<Type> scriptedImporters = specificScriptedImporters ?? TypeCache.GetTypesDerivedFrom<ScriptedImporter>();
                CollectOffendingFields(scriptedImporters, staticFields, ignoreFieldsHashSet);
            }
            catch (ReflectionTypeLoadException ex)
            {
                UnityEngine.Debug.LogException(ex);
            }

            return staticFields;
        }

        private static void CollectOffendingFields(IList<Type> extractedTypes, List<FieldInfo> staticFieldsToReport, HashSet<FieldInfo> ignoreFieldsHashSet) 
        {
            foreach (var type in extractedTypes)
            {
                // Get all static fields in the type
                FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                // Add the fields to the list
                foreach (FieldInfo curField in fields)
                {
                    if (!ignoreFieldsHashSet.Contains(curField))
                        staticFieldsToReport.Add(curField);
                }
            }
        }


        private static void BruteForceFindClassInProject(string containingClassFullName, ref string[] paths, out string additionalWarning)
        {
            additionalWarning = string.Empty;

            var allMatchGUIDs = new List<string>();
            var allScripts = AssetDatabase.FindAssets("t:Script");
            foreach (var curScriptGUID in allScripts)
            {
                MonoScript monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(curScriptGUID));

                // This is the case when there are no classes inside the Script,
                // such as, a C# file with only enums in it
                if (monoScript == null)
                    continue;

                Type scriptClass = monoScript.GetClass();

                if (scriptClass != null &&
                    (scriptClass.IsSubclassOf(typeof(AssetPostprocessor)) || scriptClass.IsSubclassOf(typeof(ScriptedImporter))) &&
                    scriptClass.FullName == containingClassFullName)
                {
                    allMatchGUIDs.Add(curScriptGUID);
                }
            }

            paths = allMatchGUIDs.ToArray();

            if (paths.Length == 0)
                additionalWarning =
                    L10n.Tr("Unable to locate a corresponding file for this type. Check your project for instances where multiple classes are defined in a single file, with one class sharing the file name.");
        }

        private static void TryExtractInfoForWarningMessage(string[] foundGUIDs, string containingClassFullName, Regex regEx, FieldInfo curField, out string scriptPath, out int lineNumber, out int columnNumber)
        {
            //Initialize to default values
            scriptPath = string.Empty;
            lineNumber = 0;
            columnNumber = 0;

            foreach (var curGUID in foundGUIDs)
            {
                var curPath = AssetDatabase.GUIDToAssetPath(curGUID);
                var importer = AssetImporter.GetAtPath(curPath) as MonoImporter;

                if (importer == null || importer.GetScript().GetClass().FullName != containingClassFullName)
                    continue;

                // We have found the script that corresponds
                // to where the class it. Store it here
                // in case we don't find the line or column numbers
                scriptPath = curPath;

                var scriptContents = importer.GetScript().text;

                // Line numbers begin at 1
                int lineCount = 1;

                // Use StringReader to read line by line, so that we can
                // keep count of where the line number is
                using (StringReader reader = new StringReader(scriptContents))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // We have a match, now we need
                        // to extract the column number from it
                        if (regEx.IsMatch(line))
                        {
                            lineNumber = lineCount;
                            // We add 1 because column numbers start at 1
                            columnNumber = line.IndexOf(curField.Name, StringComparison.Ordinal) + 1;
                            break;
                        }
                        lineCount++;
                    }
                }

                // No need to process any more paths, we found the
                // class which contains our static variable
                return;
            }
        }

        // Usage: If specificAssetPostProcessors or specificScriptedImporters are specified,
        // only those will be used and the TypeCache will be side-stepped.
        // These were added mostly for testing, but can also be used when this API becomes
        // more widely used.
        internal static ImportActivityWindow.PostProcessorStaticFieldWarningMessage[] FindStaticVariablesInPostProcessorsAndScriptedImporters(IList<Type> specificAssetPostProcessors = null, IList<Type> specificScriptedImporters = null)
        {
            var warnings = new List<ImportActivityWindow.PostProcessorStaticFieldWarningMessage>();

            var applicationContentsPath = Path.GetFullPath(EditorApplication.applicationContentsPath);

            // There should only be a few postprocessors in a project, so the
            // staticFields list won't be too long
            var staticFields = GetAllStaticFieldsInPostProcessorsAndScriptedImporters(specificAssetPostProcessors, specificScriptedImporters);

            foreach (var curField in staticFields)
            {
                // We're not interested in readonly fields, as their value
                // is always initialized in the MonoDomain
                // We also filter out constant fields, as their value is implicitly 
                // static and their value is also initialized when being declared
                if (curField == null || curField.DeclaringType == null || curField.IsInitOnly || curField.IsLiteral)
                    continue;

                var assemblyLocation = Path.GetFullPath(curField.DeclaringType.Assembly.Location);

                // Ignore Editor Assemblies since we can't get source for them
                if (assemblyLocation.StartsWith(applicationContentsPath, StringComparison.Ordinal))
                    continue;

                var containingClassName = curField.DeclaringType.Name;
                var containingClassFullName = curField.DeclaringType.FullName;

                // Types can be nested, so we need to make sure we can extract nested types
                var declaringType = curField.DeclaringType;
                while (declaringType != null &&
                        (declaringType.Attributes.HasFlag(TypeAttributes.NestedPrivate) ||
                        declaringType.Attributes.HasFlag(TypeAttributes.NestedPublic) ||
                        declaringType.Attributes.HasFlag(TypeAttributes.NestedFamORAssem) ||
                    declaringType.Attributes.HasFlag(TypeAttributes.NestedFamily)))
                {
                    containingClassName = declaringType.DeclaringType.Name;
                    containingClassFullName = declaringType.DeclaringType.FullName;
                    declaringType = declaringType.DeclaringType;
                }

                if (string.IsNullOrEmpty(containingClassName))
                    continue;

                string scriptPath = string.Empty;
                int lineNumber = 0;
                int columnNumber = 0;

                // This regex pattern matches the word "static" followed by any number of
                // characters that are not parentheses, braces, or brackets, and then
                // matches the full word for the field name,
                // ensuring that the field name is not a part of a longer word.
                // Note: '([^\\(\\[\\{{])*' -> This part matches zero or more characters
                // that are not parentheses, braces, or brackets.
                var regEx = new Regex($"static ([^\\(\\[\\{{])*\\b{curField.Name}\\b", RegexOptions.Compiled);
                var foundGUIDs = AssetDatabase.FindAssets($"{containingClassName}");
                var additionalWarning = string.Empty;

                // This should be quite rare, but can still happen in case
                // we have a C# file with a class that does not have the same name
                // as the file. In that case, we fallback to getting all scripts,
                // then only going through AssetPostprocessors (or ScriptedImporters)
                // and then check if the class names match.
                // If there's a match, we add it to paths
                if (foundGUIDs.Length == 0)
                    BruteForceFindClassInProject(containingClassFullName, ref foundGUIDs, out additionalWarning);

                // Getting the scriptPath should happen most of the time
                // but getting the line number and columnNumber may not always be possible
                TryExtractInfoForWarningMessage(foundGUIDs, containingClassFullName, regEx, curField, out scriptPath, out lineNumber, out columnNumber);

                string warningMessage = $"{curField.DeclaringType.FullName} contains static variable: {curField.Name}.";
                const string additionalInfo =
                    "Using static variables can cause hard to detect bugs when performing parallel imports. This warning can be disabled by placing the [AssetPostprocessorStaticVariableIgnore] attribute over the reported variable.";

                warnings.Add(new ImportActivityWindow.PostProcessorStaticFieldWarningMessage()
                {
                    message = warningMessage,
                    additionalInfo = additionalInfo,
                    additionalWarning = additionalWarning,
                    filePath = scriptPath,
                    lineNumber = lineNumber,
                    columnNumber = columnNumber
                });
            }

            return warnings.ToArray();
        }
    }
}
