// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    internal class CallTreeNode : DependencyNode
    {
        /// <summary>
        /// Assembly name
        /// </summary>
        public readonly string AssemblyName;

        /// <summary>
        /// Full name of the type, including namespace
        /// </summary>
        public readonly string TypeFullName;

        /// <summary>
        /// Full name of the method, including parameters and return type
        /// </summary>
        public readonly string MethodFullName;

        /// <summary>
        /// User-friendly name of the type
        /// </summary>
        public readonly string PrettyTypeName;

        /// <summary>
        /// User-friendly name of the method
        /// </summary>
        public readonly string PrettyMethodName;

        public CallTreeNode(MethodReference methodReference)
        {
            MethodFullName = methodReference.FastFullName();
            TypeFullName = methodReference.DeclaringType.FullName;
            PrettyMethodName = "(anonymous)"; // default value
            AssemblyName = methodReference.Module.Name;

            // check if it's a coroutine
            if (methodReference.DeclaringType.FullName.IndexOf("/<", StringComparison.Ordinal) >= 0)
            {
                var fullName = methodReference.DeclaringType.FullName;
                var methodStartIndex = fullName.IndexOf("<", StringComparison.Ordinal) + 1;
                if (methodStartIndex > 0)
                {
                    var length = fullName.IndexOf(">", StringComparison.Ordinal) - methodStartIndex;
                    PrettyTypeName = fullName.Substring(0, fullName.IndexOf("/", StringComparison.Ordinal));
                    if (length > 0)
                    {
                        PrettyMethodName = fullName.Substring(methodStartIndex, length);
                    }
                    else
                    {
                        // handle example: System.Int32 DelegateTest/<>c::<Update>b__1_0()
                        methodStartIndex = MethodFullName.LastIndexOf("<", StringComparison.Ordinal) + 1;
                        if (methodStartIndex > 0)
                        {
                            length = MethodFullName.LastIndexOf(">", StringComparison.Ordinal) - methodStartIndex;
                            PrettyMethodName = MethodFullName.Substring(methodStartIndex, length) + ".(anonymous)";
                        }
                    }
                }
                else
                {
                    // for some reason, some generated types don't have the same syntax
                    PrettyTypeName = fullName;
                }
            }
            else
            {
                PrettyTypeName = methodReference.DeclaringType.Name;
                PrettyMethodName = methodReference.Name;
            }

            PerfCriticalContext = false;
        }

        public override void BuildHierarchy(int depth, DependencyBuildContext context)
        {
            // this check should be removed. Instead, the deep callstacks should be built on-demand
            if (depth++ == k_MaxDepth)
                return;

            // let's find all callers with matching callee (this)
            if (context.BucketedCalls.TryGetValue(MethodFullName, out var callPairs))
            {
                var children = new List<DependencyNode>(callPairs.Count);

                foreach (var call in callPairs)
                {
                    if (call.Hierarchy != null)
                    {
                        // use previously built hierarchy
                        children.Add(call.Hierarchy);
                        continue;
                    }

                    var hierarchy = new CallTreeNode(call.Caller)
                    {
                        Location = call.Location,
                        PerfCriticalContext = call.IsPerfCriticalContext
                    };

                    // Set before recursing so mutual-recursion cycles (A->B->A) are detected
                    // by subsequent lookups finding a non-null Hierarchy, rather than relying
                    // solely on the depth limit.
                    call.Hierarchy = hierarchy;

                    var callerName = call.Caller.FastFullName();
                    if (!callerName.Equals(MethodFullName))
                        hierarchy.BuildHierarchy(depth, context);

                    children.Add(hierarchy);
                }

                AddChildren(children);
            }
        }

        public override string GetName()
        {
            return MethodFullName;
        }

        public override string GetPrettyName()
        {
            if (string.IsNullOrEmpty(PrettyTypeName))
                return MethodFullName;
            return $"{PrettyTypeName}.{PrettyMethodName}";
        }

        public override bool IsPerfCritical()
        {
            return PerfCriticalContext;
        }
    }
}
