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
using Attribute = ICSharpCode.NRefactory.Ast.Attribute;

namespace UnityEditor.Scripting.Compilers
{
    class APIUpdaterHelper
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
            bool anyFileInAssetsFolder = false;
            var pathMappingsFilePath = Path.GetTempFileName();
            var filePathMappings = new List<string>(sourceFiles.Length);
            foreach (var source in sourceFiles)
            {
                anyFileInAssetsFolder |= (source.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase) != -1);

                var f = CommandLineFormatter.PrepareFileName(source);
                if (f != source) // assume path represents a virtual path and needs to be mapped.
                {
                    f = Paths.UnifyDirectorySeparator(f);
                    filePathMappings.Add(f + " => " + source);
                }
            }

            // Only try to connect to VCS if there are files under VCS that need to be updated
            if (anyFileInAssetsFolder && !APIUpdaterManager.WaitForVCSServerConnection(true))
            {
                return;
            }

            File.WriteAllLines(pathMappingsFilePath, filePathMappings.ToArray());

            var tempOutputPath = "Library/Temp/ScriptUpdater/" + new System.Random().Next() + "/";
            try
            {
                var arguments = ArgumentsForScriptUpdater(
                    sourceExtension,
                    tempOutputPath,
                    pathMappingsFilePath,
                    responseFile);

                RunUpdatingProgram("ScriptUpdater.exe", arguments, tempOutputPath, anyFileInAssetsFolder);
            }
#pragma warning disable CS0618 // Type or member is obsolete
            catch (Exception ex) when (!(ex is StackOverflowException) && !(ex is ExecutionEngineException))
#pragma warning restore CS0618 // Type or member is obsolete
            {
                Debug.LogError(L10n.Tr("[API Updater] ScriptUpdater threw an exception. Check the following message in the log."));
                Debug.LogException(ex);

                APIUpdaterManager.ReportExpectedUpdateFailure();
            }
        }

        public static string ArgumentsForScriptUpdater(string sourceExtension, string tempOutputPath, string pathMappingsFilePath, string responseFile)
        {
            return sourceExtension
                + " "
                + CommandLineFormatter.PrepareFileName(MonoInstallationFinder.GetFrameWorksFolder())
                + " "
                + CommandLineFormatter.PrepareFileName(tempOutputPath)
                + " \"" + APIUpdaterManager.ConfigurationSourcesFilter + "\" " // Quote the filter (regex) to avoid issues when passing through command line arg.)
                + CommandLineFormatter.PrepareFileName(pathMappingsFilePath)
                + " "
                + responseFile;  // Response file is always relative and without spaces, no need to quote.
        }

        static void RunUpdatingProgram(string executable, string arguments, string tempOutputPath, bool anyFileInAssetsFolder)
        {
            var scriptUpdaterPath = EditorApplication.applicationContentsPath + "/Tools/ScriptUpdater/" + executable; // ManagedProgram will quote this path for us.
            using (var program = new ManagedProgram(MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge"), null, scriptUpdaterPath, arguments, false, null))
            {
                program.LogProcessStartInfo();
                program.Start();
                program.WaitForExit();

                Console.WriteLine(string.Join(Environment.NewLine, program.GetStandardOutput()));

                HandleUpdaterReturnValue(program, tempOutputPath, anyFileInAssetsFolder);
            }
        }

        static void HandleUpdaterReturnValue(ManagedProgram program, string tempOutputPath, bool anyFileInAssetsFolder)
        {
            if (program.ExitCode == 0)
            {
                Console.WriteLine(string.Join(Environment.NewLine, program.GetErrorOutput()));
                CopyUpdatedFiles(tempOutputPath, anyFileInAssetsFolder);
                return;
            }

            APIUpdaterManager.ReportExpectedUpdateFailure();
            if (program.ExitCode > 0)
                ReportAPIUpdaterFailure(program.GetErrorOutput());
            else
                ReportAPIUpdaterCrash(program.GetErrorOutput());
        }

        static void ReportAPIUpdaterCrash(IEnumerable<string> errorOutput)
        {
            Debug.LogErrorFormat(L10n.Tr("Failed to run script updater.{0}Please, report a bug to Unity with these details{0}{1}"), Environment.NewLine, errorOutput.Aggregate("", (acc, curr) => acc + Environment.NewLine + "\t" + curr));
        }

        static void ReportAPIUpdaterFailure(IEnumerable<string> errorOutput)
        {
            var msg = string.Format(L10n.Tr("APIUpdater encountered some issues and was not able to finish.{0}{1}"), Environment.NewLine, errorOutput.Aggregate("", (acc, curr) => acc + Environment.NewLine + "\t" + curr));
            APIUpdaterManager.ReportGroupedAPIUpdaterFailure(msg);
        }

        static void CopyUpdatedFiles(string tempOutputPath, bool anyFileInAssetsFolder)
        {
            if (!Directory.Exists(tempOutputPath))
                return;

            var files = Directory.GetFiles(tempOutputPath, "*.*", SearchOption.AllDirectories);

            var pathsRelativeToTempOutputPath = files.Select(path => path.Replace(tempOutputPath, ""));
            if (anyFileInAssetsFolder && Provider.enabled && !CheckoutAndValidateVCSFiles(pathsRelativeToTempOutputPath))
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
                File.Copy(sourceFileName, relativeDestFilePath, true);
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
                            L10n.Tr("[API Updater] At least one file from a readonly package and one file from other location have been updated (that is not expected).{0}File from other location: {0}\t{1}{0}Files from packages already processed: {0}{2}"),
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

                if (packageInfo.source != PackageSource.Local && packageInfo.source != PackageSource.Embedded)
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

            PackageManager.ImmutableAssets.SetAssetsAllowedToBeModified(filesFromReadOnlyPackages.ToArray());
        }

        internal static bool CheckReadOnlyFiles(string[] destRelativeFilePaths)
        {
            // Verify that all the files we need to copy are now writable
            // Problems after API updating during ScriptCompilation if the files are not-writable
            var readOnlyFiles = destRelativeFilePaths.Where(path => (File.GetAttributes(path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);
            if (readOnlyFiles.Any())
            {
                Debug.LogErrorFormat(L10n.Tr("[API Updater] Files cannot be updated (files not writable): {0}"), readOnlyFiles.Select(path => path).Aggregate((acc, curr) => acc + Environment.NewLine + "\t" + curr));
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
            var versionedFiles = files.Select(f => f.Replace('\\', '/')).Where(Provider.PathIsVersioned).ToArray();

            // Fail if the asset database GUID can not be found for the input asset path.
            var assetPath = versionedFiles.FirstOrDefault(f => string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(f)));
            if (assetPath != null)
            {
                Debug.LogErrorFormat(L10n.Tr("[API Updater] Files cannot be updated (failed to add file to list): {0}"), assetPath);
                APIUpdaterManager.ReportExpectedUpdateFailure();
                return false;
            }

            var notEditableFiles = new List<string>();
            if (!AssetDatabase.MakeEditable(versionedFiles, null, notEditableFiles))
            {
                var notEditableList = notEditableFiles.Aggregate(string.Empty, (text, file) => text + $"\n\t{file}");
                Debug.LogErrorFormat(L10n.Tr("[API Updater] Files cannot be updated (failed to check out): {0}"), notEditableList);
                APIUpdaterManager.ReportExpectedUpdateFailure();
                return false;
            }

            return true;
        }

        // C# compiler does not emit the full qualified type name when it fails to resolve a 'theoretically', fully qualified type reference
        // for instance, if 'NSBar', a namespace gets renamed to 'NSBar2', a reference to 'NSFoo.NSBar.TypeBaz' will emit an error
        // with only NSBar and NSFoo in the message. In this case we use NRefactory to dive in to the code, looking for type references
        // in the reported error line/column
        static Type FindTypeMatchingMovedTypeBasedOnNamespaceFromError(IEnumerable<string> lines)
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

            var entityName = GetValueFromNormalizedMessage(lines, "EntityName=");
            try
            {
                using (var scriptStream = File.Open(script, System.IO.FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    var parser = ParserFactory.CreateParser(ICSharpCode.NRefactory.SupportedLanguage.CSharp, new StreamReader(scriptStream));
                    parser.Lexer.EvaluateConditionalCompilation = false;
                    parser.Parse();

                    var self = new InvalidTypeOrNamespaceErrorTypeMapper(line, column, entityName);
                    parser.CompilationUnit.AcceptVisitor(self, null);

                    if (self.identifiers.Count == 0)
                        return null;

                    var availableTypes = TypeCache.GetTypesWithAttribute(typeof(MovedFromAttribute));
                    foreach (var ns in self.NamespacesInScope)
                    {
                        foreach (var i in self.identifiers)
                        {
                            foreach (var t in availableTypes)
                            {
                                var @namespace = ns;
                                foreach (var part in i.parts)
                                {
                                    //If the usage + any of the candidate namespaces matches a real type, this usage is valid and does not need to be updated
                                    //this is required to avoid false positives when a type that exists on *editor* (and is marked as moved) is used in a platform that
                                    //does not support it. If we don't check the namespaces in scope we'll flag this as an error due to the type being moved (and
                                    //whence, trigger the updater, whereas the real problem is that the type is not supported in the platform (see issue #96123)
                                    //whence this is indeed a programing error that the user needs to fix.
                                    if (t.Name == part && t.Namespace == @namespace)
                                    {
                                        return null;
                                    }

                                    if (t.Name == part && NamespaceHasChanged(t, @namespace))
                                    {
                                        return t;
                                    }

                                    if (string.IsNullOrEmpty(@namespace))
                                        @namespace = part;
                                    else
                                        @namespace = @namespace + "." + part;
                                }
                            }
                        }
                    }
                    return null;
                }
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        static string GetValueFromNormalizedMessage(IEnumerable<string> lines, string marker)
        {
            string value = null;
            var foundLine = lines.FirstOrDefault(l => l.StartsWith(marker));
            if (foundLine != null)
            {
                value = foundLine.Substring(marker.Length).Trim();
            }
            return value;
        }

        static bool IsUpdateable(Type type)
        {
            var attrs = type.GetCustomAttributes(typeof(ObsoleteAttribute), false);
            if (attrs.Length != 1)
                return false;

            var oa = (ObsoleteAttribute)attrs[0];
            return oa.Message.Contains("UnityUpgradable");
        }

        static bool NamespaceHasChanged(Type type, string namespaceName)
        {
            var attrs = type.GetCustomAttributes(typeof(MovedFromAttribute), false);
            if (attrs.Length != 1)
                return false;

            if (string.IsNullOrEmpty(namespaceName))
                return true;

            var from = (MovedFromAttribute)attrs[0];
            return from.data.nameSpace == namespaceName;
        }

        static Type FindTypeInLoadedAssemblies(Func<Type, bool> predicate)
        {
            var found = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !IsIgnoredAssembly(assembly.GetName()))
                .SelectMany(GetValidTypesIn)
                .FirstOrDefault(predicate);

            return found;
        }

        static IEnumerable<Type> GetValidTypesIn(Assembly a)
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

        static bool IsIgnoredAssembly(AssemblyName assemblyName)
        {
            var name = assemblyName.Name;
            return _ignoredAssemblies.Any(candidate => Regex.IsMatch(name, candidate));
        }

        static string[] _ignoredAssemblies = { "^UnityScript$", "^System\\..*", "^mscorlib$" };
    }

    struct Identifier
    {
        public List<string> parts;

        public override string ToString()
        {
            return string.Join(".", parts.ToArray());
        }

        public string ToStringStartingWith(string prefix) => $"{prefix}.{ToString()}";
    }

    /*
     * Given the following source code with error(s):
     *
     * using Foo;
     * using FooBar.Baz; // E1
     * using X = Foo.Baz.Bar; // E4
     * using Y = Foo.Baz.T; // E5
     *
     * class C : T1 // E2
     * {
     *  public N1.T2 t2; // E3
     * }
     *
     * Errors my be reported by C# compiler as:
     *
     * - Type / Namespace does not exist (E1). Usually this happens when we have only static member access to types moved out
     *   from the imported namespace and part of namespace is still valid (for instance, FooBar exists but not FooBar.Baz)
     *
     * - Type / Namespace does not exist (E2/E3). Usually this happens if the original namespace (and none of its parents)
     *   does not exists.
     *
     * Handling:
     *   1. Collect all imported namespaces
     *
     *   2. Collect all identifiers that can represent type references (T3)
     *
     *   3. Collect all types available in the current AppDomain
     *
     *   4. This scenario also represents an error that requires ScriptUpdater to run if any of the following represents a
     *      type marked as MoveFrom(imported-namespace):
     *       - N1, (ie, T2 is an inner type of N1) or
     *       - N1.T2 (N1 is a namespace, T2 is a type) (note that this is only valid if N1 is an inner namespace of the namespace
     *         from which T2 has been moved from, in this example it means the fully qualified name of T2 would be either
     *         Foo.N1.T2 or Foo.FooBar.Baz.N1.T2)
     *
     * - Main difference among E1 & E2/E3 is that for E1 we need to check T1 & N1.T2 only against "FooBar.Baz" and in E2/E3
     *   we need to check those type references against *all* imported namespaces.
     */
    class InvalidTypeOrNamespaceErrorTypeMapper : AbstractAstVisitor
    {
        public HashSet<Using> usings = new HashSet<Using>();
        public IDictionary<string, TypeReference> aliases = new Dictionary<string, TypeReference>();
        public List<Identifier> identifiers = new List<Identifier>();

        bool _isOffendingUsing;

        readonly int _line;
        readonly int _column;
        readonly string _entityName;

        public IEnumerable<string> NamespacesInScope
        {
            get
            {
                return usings.Select(ns => ns.Name).Concat(new[] { string.Empty });
            }
        }

        public override object VisitAttribute(Attribute attribute, object data)
        {
            var typeName = attribute.Name;
            if (!typeName.EndsWith("Attribute"))
                typeName = typeName + "Attribute";

            AddIdentifierFromString(typeName, 0);
            return base.VisitAttribute(attribute, data);
        }

        public override object VisitTypeReference(TypeReference typeReference, object data)
        {
            if (_isOffendingUsing || MatchesPosition(typeReference.StartLocation, typeReference.Type.Length))
            {
                AddIdentifierFromString(typeReference.Type, typeReference.GenericTypes.Count);
            }

            return base.VisitTypeReference(typeReference, data);
        }

        public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
        {
            if (_isOffendingUsing || MatchesPosition(identifierExpression.StartLocation, identifierExpression.Identifier.Length))
            {
                var identifier = new Identifier { parts = new List<string>() };
                AddIdentifierPartsTakingAliasesIntoAccount(ref identifier, identifierExpression.Identifier);
                identifiers.Add(identifier);
            }

            return null;
        }

        public override object VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, object data)
        {
            var identifier = new Identifier { parts = new List<string>(6) };
            var curr = memberReferenceExpression;
            var last = memberReferenceExpression;
            bool matchesPosition = false;

            //TODO: Can we avoid calling AddIdentifierPartsTakingAliasesIntoAccount() here? Reasoning is that we may not add the identifier to the list
            //      see the condition outside the while.
            while (curr != null)
            {
                if (!matchesPosition && MatchesPosition(curr.StartLocation, curr.EndLocation.Column))
                    matchesPosition = true;

                AddIdentifierPartsTakingAliasesIntoAccount(ref identifier, curr.MemberName);
                last = curr;
                curr = curr.TargetObject as MemberReferenceExpression;
            }

            var root = last.TargetObject as IdentifierExpression;
            // In some scenarios the compiler may not emit an error if an invalid namespace of a FNQ type reference is also imported (*using*). In this scenario the compiler only reports the invalid
            // namespace in the *using statement*; in order to make sure all candidates indentifiers will be considered (added to the identifiers list) we need also check this scenario
            // (method TypeDeclaredInOffendingNamespace()).
            if (root == null || (!TypeDeclaredInOffendingNamespace(identifier.ToStringStartingWith(root.Identifier)) && !matchesPosition && !MatchesPosition(root.StartLocation, root.EndLocation.Column)))
                return base.VisitMemberReferenceExpression(memberReferenceExpression, data);

            AddIdentifierPartsTakingAliasesIntoAccount(ref identifier, root.Identifier);
            identifiers.Add(identifier);

            return null;
        }

        bool TypeDeclaredInOffendingNamespace(string candidateFQN)
        {
            return _isOffendingUsing && usings.Any(u => candidateFQN.StartsWith(u.Name));
        }

        public override object VisitUsing(Using @using, object data)
        {
            if (!_isOffendingUsing)
            {
                if (MatchesUsing(@using))
                {
                    usings.Clear(); // Scenario `E1`, use only the offending using when looking for types marked with MovedFromAttribute()
                    _isOffendingUsing = true;
                }

                if (@using.IsAlias)
                    aliases[@using.Name] = @using.Alias; // remember aliases in order to *expand* them/if identifiers starts with the aliased name.
                else
                    usings.Add(@using);
            }

            return base.VisitUsing(@using, data);
        }

        bool MatchesUsing(Using @using)
        {
            var parent = @using.Parent as UsingDeclaration;
            return parent != null
                && parent.StartLocation.Line == _line
                && parent.StartLocation.Column < _column
                && parent.EndLocation.Column > _column
                && (@using.Name == _entityName || (@using.IsAlias && @using.Alias.ToString() == _entityName));
        }

        bool MatchesPosition(Location startLocation, int length)
        {
            return _column >= startLocation.Column && _column < startLocation.Column + length && startLocation.Line == _line;
        }

        void AddIdentifierPartsTakingAliasesIntoAccount(ref Identifier identifier, string name)
        {
            TypeReference aliased;
            if (aliases.TryGetValue(name, out aliased))
            {
                var parts = SplitIdentifier(aliased.ToString());
                identifier.parts.InsertRange(0, parts);
            }
            else
            {
                identifier.parts.Insert(0, name);
            }
        }

        /*
         * Adds a identifier composed of the parts of the name (split at `.`)  handling the cases in which:
         * 1st part is an alias
         * last part is a generic type (fixes the syntax to be able to match type names from reflection)
         */
        void AddIdentifierFromString(string name, int genericTypesCount)
        {
            var parts = SplitIdentifier(name);
            var first = parts[0];
            var last = parts[parts.Length - 1];
            var reminder = parts.Skip(1).Take(parts.Length - 2); // ignore first and last parts...

            var identifier = new Identifier { parts = reminder.ToList() };

            // first element of a type reference may represent a type or an alias.
            if (parts.Length > 1 || genericTypesCount == 0)
                AddIdentifierPartsTakingAliasesIntoAccount(ref identifier, first);

            if (genericTypesCount > 0)
                last = last + "`" + genericTypesCount;

            if (parts.Length > 1)
                identifier.parts.Add(last);

            identifiers.Add(identifier);
        }

        private static string[] SplitIdentifier(string identifier)
        {
            int last = 0;
            var a = new List<string>();
            var dotIndex = identifier.IndexOf('.');
            while (dotIndex != -1)
            {
                var genericCloseBraceIndex = -1;
                var genericOpenBraceIndex = identifier.IndexOf('<', last, dotIndex - last);
                if (dotIndex > genericOpenBraceIndex && genericOpenBraceIndex != -1)
                    genericCloseBraceIndex = FindClosingGenericBrace(identifier, genericOpenBraceIndex + 1);

                if (dotIndex < genericOpenBraceIndex || genericOpenBraceIndex == -1)
                {
                    a.Add(identifier.Substring(last, dotIndex - last));
                    last = dotIndex + 1;
                }
                else if (dotIndex > genericCloseBraceIndex)
                {
                    a.Add(MapCSharpGenericNameToReflectionName(identifier.Substring(last, dotIndex - last)));
                    last = dotIndex + 1;
                }
                else if (genericCloseBraceIndex != -1)
                {
                    dotIndex = genericCloseBraceIndex;
                }

                dotIndex = identifier.IndexOf('.', dotIndex + 1);
            }

            a.Add(MapCSharpGenericNameToReflectionName(identifier.Substring(last)));

            return a.ToArray();
        }

        private static int FindClosingGenericBrace(string identifier,  int startIndex)
        {
            var index = startIndex;
            byte balanceCount = 1;
            while (balanceCount > 0 && index < identifier.Length)
            {
                switch (identifier[index])
                {
                    case '<': balanceCount++; break;
                    case '>': balanceCount--; break;
                }
                index++;
            }

            return balanceCount == 0 ? (index - 1) : -1;
        }

        /*
         * maps names like A<T> => A`1, A<T,S> => A`2, A<B<C>, D> => A`2
         */
        private static string MapCSharpGenericNameToReflectionName(string typeName)
        {
            var index = typeName.IndexOf('<');
            if (index == -1)
                return typeName;

            var typeNameWithtoutGeneric = typeName.Substring(0, index);

            index++; // skip first '<'
            byte genericArgumentCount = 1;
            byte balanceCount = 1;
            while (balanceCount > 0 && index < typeName.Length)
            {
                switch (typeName[index])
                {
                    case '<': balanceCount++; break;
                    case '>': balanceCount--; break;
                    case ',':
                        if (balanceCount == 1)
                            genericArgumentCount++;
                        break;
                }
                index++;
            }

            return typeNameWithtoutGeneric + "`" + genericArgumentCount;
        }

        public InvalidTypeOrNamespaceErrorTypeMapper(int line, int column, string entityName)
        {
            _line = line;
            _column = column;
            _entityName = entityName;
        }
    }
}
