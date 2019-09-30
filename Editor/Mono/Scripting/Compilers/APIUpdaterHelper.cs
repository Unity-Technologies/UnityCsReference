// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;
using UnityEditor.PackageManager;
using UnityEditor.Utils;
using UnityEditor.VersionControl;
using UnityEditorInternal.APIUpdating;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Scripting.Compilers
{
    internal class APIUpdaterHelper
    {
        public static bool IsReferenceToTypeWithChangedNamespace(string normalizedErrorMessage)
        {
            try
            {
                var lines = normalizedErrorMessage.Split('\n');
                var found = FindTypeMatchingMovedTypeBasedOnNamespaceFromError(lines);
                return found != null;
            }
            catch (ReflectionTypeLoadException ex)
            {
                throw new Exception(ex.Message + ex.LoaderExceptions.Aggregate("", (acc, curr) => acc + "\r\n\t" + curr.Message));
            }
        }

        public static bool IsReferenceToMissingObsoleteMember(string namespaceName, string className)
        {
            try
            {
                var found = FindTypeInLoadedAssemblies(t => t.Name == className && t.Namespace == namespaceName && IsUpdateable(t));
                return found != null;
            }
            catch (ReflectionTypeLoadException ex)
            {
                throw new Exception(ex.Message + ex.LoaderExceptions.Aggregate("", (acc, curr) => acc + "\r\n\t" + curr.Message));
            }
        }

        public static void UpdateScripts(string responseFile, string sourceExtension, string[] sourceFiles)
        {
            if (!APIUpdaterManager.WaitForVCSServerConnection(true))
            {
                return;
            }

            var pathMappingsFilePath = Path.GetTempFileName();

            var filePathMappings = new List<string>(sourceFiles.Length);
            foreach (var source in sourceFiles)
            {
                var f = CommandLineFormatter.PrepareFileName(source);
                f = Paths.UnifyDirectorySeparator(f);

                if (f != source)
                    filePathMappings.Add(f + " => " + source);
            }

            File.WriteAllLines(pathMappingsFilePath, filePathMappings.ToArray());

            var tempOutputPath = "Library/Temp/ScriptUpdater/" + new System.Random().Next() + "/";

            try
            {
                RunUpdatingProgram(
                    "ScriptUpdater.exe",
                    sourceExtension
                    + " "
                    + CommandLineFormatter.PrepareFileName(MonoInstallationFinder.GetFrameWorksFolder())
                    + " "
                    + tempOutputPath
                    + " \"" // Quote the filer (regex) to avoid issues when passing through command line arg.
                    + APIUpdaterManager.ConfigurationSourcesFilter
                    + "\" "
                    + pathMappingsFilePath
                    + " "
                    + responseFile,
                    tempOutputPath);
            }
#pragma warning disable CS0618 // Type or member is obsolete
            catch (Exception ex) when (!(ex is StackOverflowException) && !(ex is ExecutionEngineException))
#pragma warning restore CS0618 // Type or member is obsolete
            {
                Debug.LogError("[API Updater] ScriptUpdater threw an exception. Check the following message in the log.");
                Debug.LogException(ex);

                APIUpdaterManager.ReportExpectedUpdateFailure();
            }
        }

        private static void RunUpdatingProgram(string executable, string arguments, string tempOutputPath)
        {
            var scriptUpdater = EditorApplication.applicationContentsPath + "/Tools/ScriptUpdater/" + executable;
            var program = new ManagedProgram(MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge"), null, scriptUpdater, arguments, false, null);
            program.LogProcessStartInfo();
            program.Start();
            program.WaitForExit();

            Console.WriteLine(string.Join(Environment.NewLine, program.GetStandardOutput()));

            HandleUpdaterReturnValue(program, tempOutputPath);
        }

        private static void HandleUpdaterReturnValue(ManagedProgram program, string tempOutputPath)
        {
            if (program.ExitCode == 0)
            {
                Console.WriteLine(string.Join(Environment.NewLine, program.GetErrorOutput()));
                CopyUpdatedFiles(tempOutputPath);
                return;
            }

            APIUpdaterManager.ReportExpectedUpdateFailure();
            if (program.ExitCode > 0)
                ReportAPIUpdaterFailure(program.GetErrorOutput());
            else
                ReportAPIUpdaterCrash(program.GetErrorOutput());
        }

        private static void ReportAPIUpdaterCrash(IEnumerable<string> errorOutput)
        {
            Debug.LogErrorFormat("Failed to run script updater.{0}Please, report a bug to Unity with these details{0}{1}", Environment.NewLine, errorOutput.Aggregate("", (acc, curr) => acc + Environment.NewLine + "\t" + curr));
        }

        private static void ReportAPIUpdaterFailure(IEnumerable<string> errorOutput)
        {
            var msg = string.Format("APIUpdater encountered some issues and was not able to finish.{0}{1}", Environment.NewLine, errorOutput.Aggregate("", (acc, curr) => acc + Environment.NewLine + "\t" + curr));
            APIUpdaterManager.ReportGroupedAPIUpdaterFailure(msg);
        }

        private static void CopyUpdatedFiles(string tempOutputPath)
        {
            if (!Directory.Exists(tempOutputPath))
                return;

            var files = Directory.GetFiles(tempOutputPath, "*.*", SearchOption.AllDirectories);

            var pathsRelativeToTempOutputPath = files.Select(path => path.Replace(tempOutputPath, ""));
            if (Provider.enabled && !CheckoutAndValidateVCSFiles(pathsRelativeToTempOutputPath))
                return;

            var destRelativeFilePaths = files.Select(sourceFileName => sourceFileName.Substring(tempOutputPath.Length)).ToArray();

            HandleFilesInPackagesVirtualFolder(destRelativeFilePaths);

            if (!CheckReadOnlyFiles(destRelativeFilePaths))
                return;

            foreach (var sourceFileName in files)
            {
                var relativeDestFilePath = sourceFileName.Substring(tempOutputPath.Length);

                // Packman team is considering using hardlinks to implement the private cache (as of today PM simply copies the content of the package into
                // Library/PackageCache folder for each project)
                //
                // If this ever changes we'll need to change our implementation (and remove the link instead of simply updating in place) otherwise updating a package
                // in one project would result in that package being updated in all projects in the local computer.
                File.Copy(sourceFileName,  relativeDestFilePath, true);
            }

            if (destRelativeFilePaths.Length > 0)
            {
                Console.WriteLine("[API Updater] Updated Files:");
                foreach (var path in destRelativeFilePaths)
                    Console.WriteLine(path);

                Console.WriteLine();
            }
            APIUpdaterManager.ReportUpdatedFiles(destRelativeFilePaths);

            FileUtil.DeleteFileOrDirectory(tempOutputPath);
        }

        internal static void HandleFilesInPackagesVirtualFolder(string[] destRelativeFilePaths)
        {
            var filesFromReadOnlyPackages = new List<string>();
            foreach (var path in destRelativeFilePaths.Select(path => path.Replace("\\", "/"))) // package manager paths are always separated by /
            {
                var packageInfo = PackageManager.PackageInfo.FindForAssetPath(path);
                if (packageInfo == null)
                {
                    if (filesFromReadOnlyPackages.Count > 0)
                    {
                        Console.WriteLine(
                            "[API Updater] At least one file from a readonly package and one file from other location have been updated (that is not expected).{0}File from other location: {0}\t{1}{0}Files from packages already processed: {0}{2}",
                            Environment.NewLine,
                            path,
                            string.Join($"{Environment.NewLine}\t", filesFromReadOnlyPackages.ToArray()));
                    }

                    continue;
                }

                if (packageInfo.source == PackageSource.BuiltIn)
                {
                    Debug.LogError($"[API Updater] Builtin package '{packageInfo.displayName}' ({packageInfo.version}) files requires updating (Unity version {Application.unityVersion}). This should not happen. Please, report to Unity");
                    return;
                }

                if (packageInfo.source == PackageSource.Registry || packageInfo.source == PackageSource.Git)
                {
                    // Packman creates a (readonly) cache under Library/PackageCache in a way that even if multiple projects uses the same package each one should have its own
                    // private cache so it is safe for the updater to simply remove the readonly attribute and update the file.
                    filesFromReadOnlyPackages.Add(path);
                }

                // PackageSource.Embedded / PackageSource.Local are considered writtable, so nothing to do, i.e, we can simply overwrite the file contents.
            }

            foreach (var relativeFilePath in filesFromReadOnlyPackages)
            {
                var fileAttributes = File.GetAttributes(relativeFilePath);
                if ((fileAttributes & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
                    continue;

                File.SetAttributes(relativeFilePath, fileAttributes & ~FileAttributes.ReadOnly);
            }
        }

        internal static bool CheckReadOnlyFiles(string[] destRelativeFilePaths)
        {
            // Verify that all the files we need to copy are now writable
            // Problems after API updating during ScriptCompilation if the files are not-writable
            var readOnlyFiles = destRelativeFilePaths.Where(path => (File.GetAttributes(path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);
            if (readOnlyFiles.Any())
            {
                Debug.LogErrorFormat("[API Updater] Files cannot be updated (files not writable): {0}", readOnlyFiles.Select(path => path).Aggregate((acc, curr) => acc + Environment.NewLine + "\t" + curr));
                APIUpdaterManager.ReportExpectedUpdateFailure();
                return false;
            }

            return true;
        }

        internal static bool CheckoutAndValidateVCSFiles(IEnumerable<string> files)
        {
            // We're only interested in files that would be under VCS, i.e. project
            // assets or local packages. Incoming paths might use backward slashes; replace with
            // forward ones as that's what Unity/VCS functions operate on.
            files = files.Select(f => f.Replace('\\', '/')).Where(Provider.PathIsVersioned).ToArray();

            var assetList = new AssetList();
            assetList.AddRange(files.Select(Provider.GetAssetByPath));

            // Verify that all the files are also in assetList
            // This is required to ensure the copy temp files to destination loop is only working on version controlled files
            // Provider.GetAssetByPath() can fail i.e. the asset database GUID can not be found for the input asset path
            foreach (var assetPath in files)
            {
                var foundAsset = assetList.Where(asset => (asset?.path == assetPath));
                if (!foundAsset.Any())
                {
                    Debug.LogErrorFormat("[API Updater] Files cannot be updated (failed to add file to list): {0}", assetPath);
                    APIUpdaterManager.ReportExpectedUpdateFailure();
                    return false;
                }
            }

            var checkoutTask = Provider.Checkout(assetList, CheckoutMode.Exact);
            checkoutTask.Wait();

            // Verify that all the files we need to operate on are now editable according to version control
            // One of these states:
            // 1) UnderVersionControl & CheckedOutLocal
            // 2) UnderVersionControl & AddedLocal
            // 3) !UnderVersionControl
            var notEditable = assetList.Where(asset => asset.IsUnderVersionControl && !asset.IsState(Asset.States.CheckedOutLocal) && !asset.IsState(Asset.States.AddedLocal));
            if (!checkoutTask.success || notEditable.Any())
            {
                Debug.LogErrorFormat("[API Updater] Files cannot be updated (failed to check out): {0}", notEditable.Select(a => a.fullName + " (" + a.state + ")").Aggregate((acc, curr) => acc + Environment.NewLine + "\t" + curr));
                APIUpdaterManager.ReportExpectedUpdateFailure();
                return false;
            }

            return true;
        }

        private struct Identifier : IEquatable<Identifier>
        {
            public string @namespace;
            public string name;

            public bool Equals(Identifier other)
            {
                return (String.IsNullOrEmpty(@namespace) && String.IsNullOrEmpty(other.@namespace) || string.Equals(@namespace, other.@namespace)) &&
                    string.Equals(name, other.name);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is Identifier && Equals((Identifier)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((@namespace != null ? @namespace.GetHashCode() : 0) * 397) ^ (name != null ? name.GetHashCode() : 0);
                }
            }
        }

        // C# compiler does not emmit the full qualified type name when it fails to resolve a 'theorically', fully qualified type reference
        // for instance, if 'NSBar', a namespace gets renamed to 'NSBar2', a refernce to 'NSFoo.NSBar.TypeBaz' will emit an error
        // with only NSBar and NSFoo in the message. In this case we use NRefactory to dive in to the code, looking for type references
        // in the reported error line/column
        private static Type FindTypeMatchingMovedTypeBasedOnNamespaceFromError(IEnumerable<string> lines)
        {
            var value = GetValueFromNormalizedMessage(lines, "Line=");
            var line = (value != null) ? Int32.Parse(value) : -1;

            value = GetValueFromNormalizedMessage(lines, "Column=");
            var column = (value != null) ? Int32.Parse(value) : -1;

            var script = GetValueFromNormalizedMessage(lines, "Script=");
            if (line == -1 || column == -1 || script == null)
            {
                return null;
            }

            try
            {
                using (var scriptStream = File.Open(script, System.IO.FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    var parser = ParserFactory.CreateParser(ICSharpCode.NRefactory.SupportedLanguage.CSharp, new StreamReader(scriptStream));
                    parser.Lexer.EvaluateConditionalCompilation = false;
                    parser.Parse();

                    var self = new InvalidTypeOrNamespaceErrorTypeMapper(line, column);
                    parser.CompilationUnit.AcceptVisitor(self, null);

                    if (self.identifierParts.Count == 0)
                        return null;

                    List<string> candidateNamespaces = new List<string>(self.usings.Where(u => !u.IsAlias).Select(u => u.Name));
                    List<Identifier> candidateFullyQualifiedNames =
                        candidateNamespaces.Select(ns => new Identifier {@namespace = ns, name = self.identifierParts[0] }).ToList();

                    string @namespace = string.Empty;
                    foreach (var identifier in self.identifierParts)
                    {
                        candidateFullyQualifiedNames.Add(new Identifier { name = identifier, @namespace = @namespace });
                        if (@namespace != string.Empty)
                            @namespace += ".";

                        @namespace += identifier;
                    }

                    //If the usage + any of the candidate namespaces matches a real type, this usage is valid and does not need to be updated
                    if (FindTypeInLoadedAssemblies(t => candidateFullyQualifiedNames.Contains(new Identifier {name = t.Name, @namespace = t.Namespace})) != null)
                        return null;

                    return FindTypeInLoadedAssemblies(t =>
                        candidateFullyQualifiedNames.Any(id => id.name == t.Name && NamespaceHasChanged(t, id.@namespace)));
                }
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        private static string Name(string identifier, int genericArgumentCount)
        {
            if (genericArgumentCount > 0)
                return identifier + '`' + genericArgumentCount;

            return identifier;
        }

        private static string GetValueFromNormalizedMessage(IEnumerable<string> lines, string marker)
        {
            string value = null;
            var foundLine = lines.FirstOrDefault(l => l.StartsWith(marker));
            if (foundLine != null)
            {
                value = foundLine.Substring(marker.Length).Trim();
            }
            return value;
        }

        private static bool IsUpdateable(Type type)
        {
            var attrs = type.GetCustomAttributes(typeof(ObsoleteAttribute), false);
            if (attrs.Length != 1)
                return false;

            var oa = (ObsoleteAttribute)attrs[0];
            return oa.Message.Contains("UnityUpgradable");
        }

        private static bool NamespaceHasChanged(Type type, string namespaceName)
        {
            var attrs = type.GetCustomAttributes(typeof(MovedFromAttribute), false);
            if (attrs.Length != 1)
                return false;

            if (string.IsNullOrEmpty(namespaceName))
                return true;

            var from = (MovedFromAttribute)attrs[0];
            return from.data.nameSpace == namespaceName;
        }

        private static Type FindTypeInLoadedAssemblies(Func<Type, bool> predicate)
        {
            var found = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !IsIgnoredAssembly(assembly.GetName()))
                .SelectMany<Assembly, Type>(a => GetValidTypesIn(a))
                .FirstOrDefault(predicate);

            return found;
        }

        private static IEnumerable<Type> GetValidTypesIn(Assembly a)
        {
            Type[] types;
            try
            {
                types = a.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }

            return types.Where(t => t != null);
        }

        private static bool IsIgnoredAssembly(AssemblyName assemblyName)
        {
            var name = assemblyName.Name;
            return _ignoredAssemblies.Any(candidate => Regex.IsMatch(name, candidate));
        }

        private static string[] _ignoredAssemblies = { "^UnityScript$", "^System\\..*", "^mscorlib$" };
    }

    internal class InvalidTypeOrNamespaceErrorTypeMapper : AbstractAstVisitor
    {
        public List<Using> usings = new List<Using>();
        public List<string> identifierParts = new List<string>();

        private Expression lastMatchedExpression = null;

        private readonly int _line;
        private readonly int _column;

        public override object VisitTypeReference(TypeReference typeReference, object data)
        {
            var identifier = typeReference.Type;
            if (MatchesPosition(typeReference.StartLocation, identifier.Length))
            {
                var identifiers = Identifiers(typeReference.GenericTypes.Count, identifier).ToList();

                if (!identifiers.Any())
                    return null;

                var alias = usings.FirstOrDefault(u => u.IsAlias && u.Name == identifiers[0]);
                if (alias != null)
                {
                    identifiers.RemoveAt(0);
                    identifierParts.AddRange(Identifiers(0, alias.Alias.Type));
                }

                identifierParts.AddRange(identifiers);
            }

            return null;
        }

        private static string[] Identifiers(int genericArgumentCount, string identifier)
        {
            var identifiers = identifier.Split('.').ToArray();
            if (genericArgumentCount > 0)
                identifiers[identifiers.Length - 1] = identifiers[identifiers.Length - 1] + '`' + genericArgumentCount;

            return identifiers;
        }

        public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
        {
            var identifier = identifierExpression.Identifier;
            if (MatchesPosition(identifierExpression.StartLocation, identifier.Length))
            {
                var alias = usings.FirstOrDefault(u => u.IsAlias && u.Name == identifier);
                if (alias != null)
                    identifier = alias.Alias.Type;

                var identifiers = Identifiers(identifierExpression.TypeArguments.Count, identifier);
                identifierParts.AddRange(identifiers);
                lastMatchedExpression = identifierExpression;
            }

            return null;
        }

        //For cases like "UnityEngine.Object", "UnityEngine" is an IdentifierExpression that will be matched in the call to base.
        //Here we expand the Found value with the "member name" (".Object" in this example)
        public override object VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, object data)
        {
            base.VisitMemberReferenceExpression(memberReferenceExpression, data);
            if (lastMatchedExpression == memberReferenceExpression.TargetObject)
            {
                lastMatchedExpression = memberReferenceExpression;
                identifierParts.Add(memberReferenceExpression.MemberName);
            }
            return null;
        }

        public override object VisitUsing(Using @using, object data)
        {
            usings.Add(@using);
            return base.VisitUsing(@using, data);
        }

        private bool MatchesPosition(Location startLocation, int length)
        {
            return _column >= startLocation.Column && _column < startLocation.Column + length && startLocation.Line == _line;
        }

        public InvalidTypeOrNamespaceErrorTypeMapper(int line, int column)
        {
            _line = line;
            _column = column;
        }
    }
}
