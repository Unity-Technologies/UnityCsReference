// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace UnityEditor
{
    internal class MonoCecilHelper : IMonoCecilHelper
    {
        private static SequencePoint GetMethodFirstSequencePoint(MethodDefinition methodDefinition)
        {
            if (methodDefinition == null)
            {
                Debug.Log("MethodDefinition cannot be null. Check if any method was found by name in its declaring type TypeDefinition.");
                return null;
            }

            if (!methodDefinition.HasBody || !methodDefinition.Body.Instructions.Any() || methodDefinition.DebugInformation == null)
            {
                Debug.Log(string.Concat("To get SequencePoints MethodDefinition for ", methodDefinition.Name, " must have MethodBody, DebugInformation and Instructions."));
                return null;
            }

            if (!methodDefinition.DebugInformation.HasSequencePoints)
            {
                // If we don't have any sequence points for this method, it could be that
                // the method is delegating its implementation to a state machine method (e.g using yield).
                // The state machine method will contain the debug info, not this method.
                // In that case, the debug information of the state machine method (e.g MoveNext)
                // will contain a StateMachineKickOffMethod that will link backward to the
                // this original method that was containing the yield.
                // That's the method we are looking for to extract correct sequence points after.
                foreach (var nestedType in methodDefinition.DeclaringType.NestedTypes)
                {
                    foreach (var method in nestedType.Methods)
                    {
                        if (method.DebugInformation != null && method.DebugInformation.StateMachineKickOffMethod == methodDefinition && method.HasBody && method.Body.Instructions.Count > 0)
                        {
                            methodDefinition = method;
                            goto foundKickOffMethod;
                        }
                    }
                }

                Debug.Log(string.Concat("No SequencePoints for MethodDefinition for ", methodDefinition.Name));
                return null;
            }

        foundKickOffMethod:

            foreach (var instruction in methodDefinition.Body.Instructions)
            {
                // An instruction might be attached to a hidden sequence point (or no seq point at all)
                // so we need to skip them as they don't have any debug line info.
                var sequencePoint = methodDefinition.DebugInformation.GetSequencePoint(instruction);
                if (sequencePoint != null && !sequencePoint.IsHidden)
                {
                    return sequencePoint;
                }
            }

            return null;
        }

        private static AssemblyDefinition ReadAssembly(string assemblyPath)
        {
            using var assemblyResolver = new DefaultAssemblyResolver();
            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));
            var readerParameters = new ReaderParameters
            {
                ReadSymbols = true,
                SymbolReaderProvider = new DefaultSymbolReaderProvider(false),
                AssemblyResolver = assemblyResolver,
                ReadingMode = ReadingMode.Deferred
            };

            try
            {
                return AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
            }
            catch (Exception exception)
            {
                Debug.Log(exception.Message);
                return null;
            }
        }

        public IFileOpenInfo TryGetCecilFileOpenInfo(Type type, MethodInfo methodInfo)
        {
            var assemblyPath = type.Assembly.Location;

            // Get the sequence point directly from the method token (to avoid scanning all types/methods)
            using var assemblyDefinition = ReadAssembly(assemblyPath);
            var methodDefinition = assemblyDefinition.MainModule.LookupToken(methodInfo.MetadataToken) as MethodDefinition;
            var sequencePoint = GetMethodFirstSequencePoint(methodDefinition);

            var fileOpenInfo = new FileOpenInfo();
            if (sequencePoint != null) // Can be null in case of yield return in target method
            {
                fileOpenInfo.LineNumber = sequencePoint.StartLine;
                fileOpenInfo.FilePath = sequencePoint.Document.Url;
            }

            return fileOpenInfo;
        }
    }
}
