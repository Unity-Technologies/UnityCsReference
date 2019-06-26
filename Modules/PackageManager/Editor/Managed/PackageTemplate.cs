// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using TemplateVariables = System.Collections.Generic.Dictionary<string, string>;

namespace UnityEditor.PackageManager
{
    internal static class PackageTemplate
    {
        private const string k_PackageManifestFileName = "package.json";

        private static string ReplaceVariablesInString(TemplateVariables variables, string txt)
        {
            var result = txt;
            foreach (var kvp in variables)
                result = result.Replace($"#{kvp.Key}#", kvp.Value);

            return result;
        }

        private static void ReplaceVariablesInFile(TemplateVariables variables, string file)
        {
            var original = File.ReadAllText(file);
            var updated = ReplaceVariablesInString(variables, original);

            if (original != updated)
                File.WriteAllText(file, updated);
        }

        private static void RenameFileWithVariables(TemplateVariables variables, string file)
        {
            var originalFileName = Path.GetFileName(file);
            var updatedFileName = ReplaceVariablesInString(variables, originalFileName);

            if (updatedFileName != originalFileName)
            {
                var folder = Path.GetDirectoryName(file);
                FileUtil.MoveFileOrDirectory(file, Path.Combine(folder, updatedFileName));
            }
        }

        private static IEnumerable<string> GetPackageTemplateFiles(string templateFolder)
        {
            var packageManifestFile = Path.Combine(templateFolder, k_PackageManifestFileName);
            if (!File.Exists(packageManifestFile))
                throw new Exception(string.Format(L10n.Tr("Package template must contain a file named {0}."), k_PackageManifestFileName));

            var templateFiles = Directory.GetFiles(templateFolder, "*.*", SearchOption.AllDirectories);
            foreach (var file in templateFiles)
            {
                if (Path.GetExtension(file) == ".meta")
                    throw new Exception(string.Format(L10n.Tr("Package template cannot contain meta files, [{0}] was found."), file));
            }

            return templateFiles;
        }

        private static bool ValidateRootNamespace(string rootNamespace)
        {
            try
            {
                CodeGenerator.ValidateIdentifiers(new CodeNamespace(rootNamespace));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void ValidateOptions(PackageTemplateOptions options)
        {
            var errors = new List<string>();
            var requiredErrorMessage = L10n.Tr("{0} is required");

            options.name = options.name?.Trim();
            if (string.IsNullOrEmpty(options.name))
                errors.Add(string.Format(requiredErrorMessage, nameof(options.name)));
            else if (!PackageValidation.ValidateName(options.name))
                errors.Add(string.Format(L10n.Tr("Package name [{0}] is invalid"), options.name));
            else if (PackageInfo.GetAll().Any(p => p.name == options.name))
                errors.Add(string.Format(L10n.Tr("The project already contains a package with the name [{0}]."), options.name));

            options.displayName = options.displayName?.Trim();
            if (string.IsNullOrEmpty(options.displayName))
                errors.Add(string.Format(requiredErrorMessage, nameof(options.displayName)));

            options.rootNamespace = options.rootNamespace?.Trim();
            if (!string.IsNullOrEmpty(options.rootNamespace) && !ValidateRootNamespace(options.rootNamespace))
                errors.Add(string.Format(L10n.Tr("[{0}] is not a valid namespace"), options.rootNamespace));

            options.templateFolder = options.templateFolder?.Trim();
            if (string.IsNullOrEmpty(options.templateFolder))
                errors.Add(string.Format(requiredErrorMessage, nameof(options.templateFolder)));
            else if (!Directory.Exists(options.templateFolder))
                errors.Add(string.Format(L10n.Tr("The directory [{0}] does not exist"), options.templateFolder));

            if (errors.Count > 0)
            {
                var invalidParamMsg = string.Format(L10n.Tr("{0} parameter is invalid"), nameof(options));
                var errorMsg = string.Join(Environment.NewLine, errors.ToArray());
                throw new ArgumentException($"{invalidParamMsg}:{Environment.NewLine}{errorMsg}");
            }
        }

        private static TemplateVariables CreateTemplateVariables(PackageTemplateOptions options)
        {
            var version = InternalEditorUtility.GetUnityVersion();
            return new Dictionary<string, string>
            {
                { "NAME", options.name },
                { "DISPLAYNAME", options.displayName },
                { "ROOTNAMESPACE", options.rootNamespace },
                { "UNITYVERSION", $"{version.Major}.{version.Minor}" },
            };
        }

        public static string CreatePackage(PackageTemplateOptions options)
        {
            ValidateOptions(options);

            var targetFolder = $"{Folders.GetPackagesPath()}/{options.name}";
            if (Directory.Exists(targetFolder))
                throw new InvalidOperationException(string.Format(L10n.Tr("The target folder [{0}] for this new package already exists."), targetFolder));

            var variables = CreateTemplateVariables(options);
            var templateFolder = options.templateFolder;
            var tempFolder = FileUtil.GetUniqueTempPathInProject();
            try
            {
                FileUtil.CopyFileOrDirectory(templateFolder, tempFolder);

                foreach (var file in GetPackageTemplateFiles(tempFolder))
                {
                    File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
                    ReplaceVariablesInFile(variables, file);
                    RenameFileWithVariables(variables, file);
                }

                Directory.Move(tempFolder, targetFolder);
            }
            finally
            {
                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);
            }

            return targetFolder;
        }
    }
}
