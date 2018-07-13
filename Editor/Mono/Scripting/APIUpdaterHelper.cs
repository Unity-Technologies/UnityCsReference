// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor.Utils;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using System.Linq;

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;
using Mono.Cecil;
using Unity.DataContract;
using UnityEditor.Modules;
using UnityEditor.Scripting.Compilers;
using Debug = UnityEngine.Debug;
using SupportedLanguage = ICSharpCode.NRefactory.SupportedLanguage;
using TypeReference = ICSharpCode.NRefactory.Ast.TypeReference;

namespace UnityEditor.Scripting
{
    internal class APIUpdaterHelper
    {
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

        public static bool IsReferenceToTypeWithChangedNamespace(string normalizedErrorMessage)
        {
            try
            {
                var lines = normalizedErrorMessage.Split('\n');
                var simpleOrQualifiedName = GetValueFromNormalizedMessage(lines, "EntityName=");

                var found = FindExactTypeMatchingMovedType(simpleOrQualifiedName) ?? FindTypeMatchingMovedTypeBasedOnNamespaceFromError(lines);
                return found != null;
            }
            catch (ReflectionTypeLoadException ex)
            {
                throw new Exception(ex.Message + ex.LoaderExceptions.Aggregate("", (acc, curr) => acc + "\r\n\t" + curr.Message));
            }
        }

        public static void Run(string commaSeparatedListOfAssemblies)
        {
            var assemblies = commaSeparatedListOfAssemblies.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var assemblyPath in assemblies)
            {
                if ((File.GetAttributes(assemblyPath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    APIUpdaterLogger.WriteErrorToConsole("Error can't update assembly {0} the file is read-only", assemblyPath);
                    return;
                }
            }

            APIUpdaterLogger.WriteToFile("Started to update {0} assemblie(s)", assemblies.Count());

            var sw = new Stopwatch();
            sw.Start();

            foreach (var assemblyPath in assemblies)
            {
                if (!AssemblyHelper.IsManagedAssembly(assemblyPath))
                    continue;

                string stdOut, stdErr;
                var assemblyFullPath = ResolveAssemblyPath(assemblyPath);
                var exitCode = RunUpdatingProgram("AssemblyUpdater.exe", "-u -a " + assemblyFullPath + APIVersionArgument() + AssemblySearchPathArgument() + ConfigurationProviderAssembliesPathArgument(), out stdOut, out stdErr);
                if (stdOut.Length > 0)
                    APIUpdaterLogger.WriteToFile("Assembly update output ({0})\r\n{1}", assemblyFullPath, stdOut);

                if (IsError(exitCode))
                    APIUpdaterLogger.WriteErrorToConsole("Error {0} running AssemblyUpdater. Its output is: `{1}`", exitCode, stdErr);
            }

            APIUpdaterLogger.WriteToFile("Update finished in {0}s", sw.Elapsed.TotalSeconds);
        }

        private static bool IsError(int exitCode)
        {
            return (exitCode & (1 << 7)) != 0;
        }

        private static string ResolveAssemblyPath(string assemblyPath)
        {
            return CommandLineFormatter.PrepareFileName(assemblyPath);
        }

        public static bool DoesAssemblyRequireUpgrade(string assemblyFullPath)
        {
            if (!File.Exists(assemblyFullPath))
                return false;

            if (!AssemblyHelper.IsManagedAssembly(assemblyFullPath))
                return false;

            if (!MayContainUpdatableReferences(assemblyFullPath))
                return false;

            string stdOut, stdErr;
            var ret = RunUpdatingProgram("AssemblyUpdater.exe", TimeStampArgument() + APIVersionArgument() + "--check-update-required -a " + CommandLineFormatter.PrepareFileName(assemblyFullPath) + AssemblySearchPathArgument() + ConfigurationProviderAssembliesPathArgument(), out stdOut, out stdErr);
            {
                Console.WriteLine("{0}{1}", stdOut, stdErr);
                switch (ret)
                {
                    // See AssemblyUpdater/Program.cs
                    case 0:
                    case 1: return false;
                    case 2: return true;

                    default:
                        Debug.LogError(stdOut + Environment.NewLine + stdErr);
                        return false;
                }
            }
        }

        private static string AssemblySearchPathArgument()
        {
            var searchPath = Path.Combine(MonoInstallationFinder.GetFrameWorksFolder(), "Managed") + ","
                + "+" + Path.Combine(EditorApplication.applicationContentsPath, "UnityExtensions/Unity") + ","
                + "+" + Application.dataPath;

            return " -s \"" + searchPath + "\"";
        }

        private static string ConfigurationProviderAssembliesPathArgument()
        {
            var paths = new StringBuilder();
            foreach (var ext in ModuleManager.packageManager.unityExtensions)
            {
                foreach (var dllPath in ext.files.Where(f => f.Value.type == PackageFileType.Dll).Select(pi => pi.Key))
                {
                    paths.AppendFormat(" {0}", CommandLineFormatter.PrepareFileName(Path.Combine(ext.basePath, dllPath)));
                }
            }

            var editorManagedPath = GetUnityEditorManagedPath();
            paths.AppendFormat(" {0}", CommandLineFormatter.PrepareFileName(Path.Combine(editorManagedPath, "UnityEngine.dll")));
            paths.AppendFormat(" {0}", CommandLineFormatter.PrepareFileName(Path.Combine(editorManagedPath, "UnityEditor.dll")));

            return paths.ToString();
        }

        private static string GetUnityEditorManagedPath()
        {
            return Path.Combine(MonoInstallationFinder.GetFrameWorksFolder(), "Managed");
        }

        private static int RunUpdatingProgram(string executable, string arguments, out string stdOut, out string stdErr)
        {
            var scriptUpdater = EditorApplication.applicationContentsPath + "/Tools/ScriptUpdater/" + executable;
            var program = new ManagedProgram(MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge"), null, scriptUpdater, arguments, false, null);

            program.LogProcessStartInfo();
            program.Start();
            program.WaitForExit();

            stdOut = program.GetStandardOutputAsString();
            stdErr = string.Join("\r\n", program.GetErrorOutput());

            return program.ExitCode;
        }

        private static string APIVersionArgument()
        {
            return " --api-version " + Application.unityVersion + " ";
        }

        private static string TimeStampArgument()
        {
            return " --timestamp " + DateTime.Now.Ticks + " ";
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
            return from.Namespace == namespaceName;
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
            return _ignoredAssemblies.Any(candidate => Regex.IsMatch(name,  candidate));
        }

        internal static bool MayContainUpdatableReferences(string assemblyPath)
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(assemblyPath))
            {
                if (assembly.Name.IsWindowsRuntime)
                    return false;

                if (!IsTargetFrameworkValidOnCurrentOS(assembly))
                    return false;
            }

            return true;
        }

        private static bool IsTargetFrameworkValidOnCurrentOS(AssemblyDefinition assembly)
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT
                || !(assembly.HasCustomAttributes && assembly.CustomAttributes.Any(attr => TargetsWindowsSpecificFramework(attr)));
        }

        private static bool TargetsWindowsSpecificFramework(CustomAttribute targetFrameworkAttr)
        {
            if (!targetFrameworkAttr.AttributeType.FullName.Contains("System.Runtime.Versioning.TargetFrameworkAttribute"))
                return false;

            var regex = new Regex("\\.NETCore|\\.NETPortable");
            var targetsNetCoreOrPCL = targetFrameworkAttr.ConstructorArguments.Any(arg => arg.Type.FullName == typeof(string).FullName && regex.IsMatch((string)arg.Value));

            return targetsNetCoreOrPCL;
        }

        private static Type FindExactTypeMatchingMovedType(string simpleOrQualifiedName)
        {
            var match = Regex.Match(simpleOrQualifiedName, @"^(?:(?<namespace>.*)(?=\.)\.)?(?<typename>[a-zA-Z_0-9]+)$");
            if (!match.Success)
                return null;

            var typename = match.Groups["typename"].Value;
            var namespaceName = match.Groups["namespace"].Value;

            return FindTypeInLoadedAssemblies(t => t.Name == typename && NamespaceHasChanged(t, namespaceName));
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
                using (var scriptStream = File.Open(script, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var parser = ParserFactory.CreateParser(SupportedLanguage.CSharp, new StreamReader(scriptStream));
                    parser.Lexer.EvaluateConditionalCompilation = false;
                    parser.Parse();

                    var typeNotFound = InvalidTypeOrNamespaceErrorTypeMapper.IsTypeMovedToNamespaceError(parser.CompilationUnit, line, column);
                    if (typeNotFound == null)
                        return null;

                    return FindExactTypeMatchingMovedType(typeNotFound);
                }
            }
            catch (FileNotFoundException)
            {
                return null;
            }
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

        private static string[] _ignoredAssemblies = { "^UnityScript$", "^System\\..*", "^mscorlib$" };
    }

    internal class InvalidTypeOrNamespaceErrorTypeMapper : AbstractAstVisitor
    {
        public static string IsTypeMovedToNamespaceError(CompilationUnit cu, int line, int column)
        {
            var self = new InvalidTypeOrNamespaceErrorTypeMapper(line, column);
            cu.AcceptVisitor(self, null);

            return self.Found;
        }

        public string Found { get; private set; }

        private readonly int _line;
        private readonly int _column;

        public override object VisitTypeReference(TypeReference typeReference, object data)
        {
            var withinRange = _column >= typeReference.StartLocation.Column && _column < typeReference.StartLocation.Column + typeReference.Type.Length;
            if (typeReference.StartLocation.Line == _line && withinRange)
            {
                Found = typeReference.Type;
                return true;
            }
            return base.VisitTypeReference(typeReference, data);
        }

        private InvalidTypeOrNamespaceErrorTypeMapper(int line, int column)
        {
            _line = line;
            _column = column;
        }
    }
}
