// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace UnityEditor
{
    internal class AssemblyReferenceChecker
    {
        private readonly HashSet<string>    _referencedMethods   = new HashSet<string>();
        private HashSet<string>             _referencedTypes     = new HashSet<string>();
        private readonly HashSet<string>    _userReferencedMethods = new HashSet<string>();
        private readonly HashSet<string>    _definedMethods      = new HashSet<string>();
        private HashSet<AssemblyDefinition> _assemblyDefinitions = new HashSet<AssemblyDefinition>();
        private readonly HashSet<string>    _assemblyFileNames   = new HashSet<string>();

        private DateTime _startTime = DateTime.MinValue;
        private float _progressValue = 0.0f;

        private Action _updateProgressAction;

        public bool HasMouseEvent { get; private set; }

        public AssemblyReferenceChecker()
        {
            HasMouseEvent = false;
            _updateProgressAction = DisplayProgress;
        }

        public static AssemblyReferenceChecker AssemblyReferenceCheckerWithUpdateProgressAction(Action action)
        {
            var checker = new AssemblyReferenceChecker();
            checker._updateProgressAction = action;
            return checker;
        }

        // Follows actually referenced libraries only
        private void CollectReferencesFromRootsRecursive(string dir, IEnumerable<string> roots, bool ignoreSystemDlls)
        {
            var resolver = AssemblyResolverFor(dir);

            foreach (var assemblyFileName in roots)
            {
                var fileName = Path.Combine(dir, assemblyFileName);
                if (_assemblyFileNames.Contains(assemblyFileName))
                    continue;

                var assemblyDefinition = AssemblyDefinition.ReadAssembly(fileName, new ReaderParameters { AssemblyResolver = resolver });

                if (ignoreSystemDlls && IsIgnoredSystemDll(assemblyDefinition.Name.Name))
                    continue;

                _assemblyFileNames.Add(assemblyFileName);
                _assemblyDefinitions.Add(assemblyDefinition);

                foreach (var reference in assemblyDefinition.MainModule.AssemblyReferences)
                {
                    var refFileName = reference.Name + ".dll";
                    if (_assemblyFileNames.Contains(refFileName))
                        continue;
                    CollectReferencesFromRootsRecursive(dir, new string[] {refFileName}, ignoreSystemDlls);
                }
            }
        }

        // Follows actually referenced libraries only
        public void CollectReferencesFromRoots(string dir, IEnumerable<string> roots, bool collectMethods, float progressValue, bool ignoreSystemDlls)
        {
            _progressValue = progressValue;

            CollectReferencesFromRootsRecursive(dir, roots, ignoreSystemDlls);

            var assemblyDefinitionsAsArray = _assemblyDefinitions.ToArray();
            _referencedTypes = MonoAOTRegistration.BuildReferencedTypeList(assemblyDefinitionsAsArray);

            if (collectMethods)
                CollectReferencedAndDefinedMethods(assemblyDefinitionsAsArray);
        }

        public void CollectReferences(string path, bool collectMethods, float progressValue, bool ignoreSystemDlls)
        {
            _progressValue = progressValue;

            _assemblyDefinitions = new HashSet<AssemblyDefinition>();

            var filePaths = Directory.Exists(path) ? Directory.GetFiles(path) : new string[0];

            var resolver = AssemblyResolverFor(path);

            foreach (var filePath in filePaths)
            {
                if (Path.GetExtension(filePath) != ".dll")
                    continue;

                var assembly = AssemblyDefinition.ReadAssembly(filePath, new ReaderParameters { AssemblyResolver = resolver });

                if (ignoreSystemDlls && IsIgnoredSystemDll(assembly.Name.Name))
                    continue;

                _assemblyFileNames.Add(Path.GetFileName(filePath));
                _assemblyDefinitions.Add(assembly);
            }

            var assemblyDefinitionsAsArray = _assemblyDefinitions.ToArray();
            _referencedTypes = MonoAOTRegistration.BuildReferencedTypeList(assemblyDefinitionsAsArray);

            if (collectMethods)
                CollectReferencedAndDefinedMethods(assemblyDefinitionsAsArray);
        }

        private void CollectReferencedAndDefinedMethods(IEnumerable<AssemblyDefinition> assemblyDefinitions)
        {
            foreach (var assembly in assemblyDefinitions)
            {
                bool boolIsSystem = IsIgnoredSystemDll(assembly.Name.Name);
                foreach (var type in assembly.MainModule.Types)
                    CollectReferencedAndDefinedMethods(type, boolIsSystem);
            }
        }

        internal void CollectReferencedAndDefinedMethods(TypeDefinition type)
        {
            CollectReferencedAndDefinedMethods(type, false);
        }

        internal void CollectReferencedAndDefinedMethods(TypeDefinition type, bool isSystem)
        {
            if (_updateProgressAction != null)
                _updateProgressAction();

            foreach (var nestedType in type.NestedTypes)
                CollectReferencedAndDefinedMethods(nestedType, isSystem);

            foreach (var method in type.Methods)
            {
                if (!method.HasBody)
                    continue;

                foreach (var instr in method.Body.Instructions)
                {
                    if (OpCodes.Call == instr.OpCode)
                    {
                        var name = instr.Operand.ToString();
                        if (!isSystem)
                        {
                            _userReferencedMethods.Add(name);
                        }
                        _referencedMethods.Add(name);
                    }
                }
                _definedMethods.Add(method.ToString());

                HasMouseEvent |= MethodIsMouseEvent(method);
            }
        }

        private bool MethodIsMouseEvent(MethodDefinition method)
        {
            var methodNameIsMouseEvent =
                method.Name == "OnMouseDown"
                || method.Name == "OnMouseDrag"
                || method.Name == "OnMouseEnter"
                || method.Name == "OnMouseExit"
                || method.Name == "OnMouseOver"
                || method.Name == "OnMouseUp"
                || method.Name == "OnMouseUpAsButton";

            if (!methodNameIsMouseEvent)
                return false;

            if (method.Parameters.Count != 0)
                return false;

            bool isInUnityEngineBehavior = InheritsFromMonoBehaviour(method.DeclaringType);

            if (!isInUnityEngineBehavior)
                return false;

            return true;
        }

        private bool InheritsFromMonoBehaviour(TypeReference type)
        {
            // Case 833157: StagingArea\Data\Managed contains user and dependency assemblies, but doesn't contain UnityEngine.dll. This applies for all platforms
            //              Thus we wouldn't be able to load UnityEngine.dll when Resolve() is called. That's why we're delaying Resolve() as much as possible
            if (type.Namespace == "UnityEngine" && type.Name == "MonoBehaviour")
                return true;

            try
            {
                var typeDefinition = type.Resolve();
                if (typeDefinition.BaseType != null)
                    return InheritsFromMonoBehaviour(typeDefinition.BaseType);
            }
            catch (AssemblyResolutionException)
            {
                // We weren't able to resolve the base type - let's assume it's not a monobehaviour
            }

            return false;
        }

        private void DisplayProgress()
        {
            var elapsedTime = DateTime.Now - _startTime;
            var progressStrings = new[]
            {
                "Fetching assembly references",
                "Building list of referenced assemblies..."
            };

            if (elapsedTime.TotalMilliseconds >= 100)
            {
                if (EditorUtility.DisplayCancelableProgressBar(progressStrings[0], progressStrings[1], _progressValue))
                    throw new OperationCanceledException();

                _startTime = DateTime.Now;
            }
        }

        public bool HasReferenceToMethod(string methodName)
        {
            return HasReferenceToMethod(methodName, false);
        }

        public bool HasReferenceToMethod(string methodName, bool ignoreSystemDlls)
        {
            return !ignoreSystemDlls? _referencedMethods.Any(item => item.Contains(methodName)) : _userReferencedMethods.Any(item => item.Contains(methodName));
        }

        public bool HasDefinedMethod(string methodName)
        {
            return _definedMethods.Any(item => item.Contains(methodName));
        }

        public bool HasReferenceToType(string typeName)
        {
            return _referencedTypes.Any(item => item.StartsWith(typeName));
        }

        public AssemblyDefinition[] GetAssemblyDefinitions()
        {
            return _assemblyDefinitions.ToArray();
        }

        public string[] GetAssemblyFileNames()
        {
            return _assemblyFileNames.ToArray();
        }

        public string WhoReferencesClass(string klass, bool ignoreSystemDlls)
        {
            foreach (var assembly in _assemblyDefinitions)
            {
                if (ignoreSystemDlls && IsIgnoredSystemDll(assembly.Name.Name))
                    continue;

                var assemblyDefinitionsAsArray = new[] {assembly};
                var types = MonoAOTRegistration.BuildReferencedTypeList(assemblyDefinitionsAsArray);

                if (types.Any(item => item.StartsWith(klass)))
                    return assembly.Name.Name;
            }

            return null;
        }

        public static bool IsIgnoredSystemDll(string name)
        {
            return name.StartsWith("System")
                || name.Equals("UnityEngine")
                || (name.StartsWith("UnityEngine.") && name.EndsWith("Module"))
                || name.Equals("UnityEngine.Networking")
                || name.Equals("Mono.Posix")
                || name.Equals("Moq");
        }

        public static bool GetScriptsHaveMouseEvents(string path)
        {
            var checker = new AssemblyReferenceChecker();
            checker.CollectReferences(path, true, 0.0f, true);

            return checker.HasMouseEvent;
        }

        private static DefaultAssemblyResolver AssemblyResolverFor(string path)
        {
            var resolver = new DefaultAssemblyResolver();
            if (File.Exists(path) || Directory.Exists(path))
            {
                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.Directory) != FileAttributes.Directory)
                    path = Path.GetDirectoryName(path);
                resolver.AddSearchDirectory(Path.GetFullPath(path));
            }

            return resolver;
        }
    }
}
