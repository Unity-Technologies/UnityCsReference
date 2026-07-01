// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class EntityIdAnalyzer : CodeModuleInstructionAnalyzer
    {
        internal const string PAC2011 = nameof(PAC2011);
        internal const string PAC2012 = nameof(PAC2012);
        internal const string PAC2013 = nameof(PAC2013);

        static readonly int k_EntityIdTypeHash = "UnityEngine.EntityId".GetHashCode();
        static readonly int k_DebugTypeHash = "UnityEngine.Debug".GetHashCode();
        static readonly int k_StringTypeHash = "System.String".GetHashCode();

        static readonly Descriptor k_GetHashCodeDescriptor = new Descriptor(
            PAC2011,
            "Object.GetHashCode used to retrieve EntityId",
            Areas.Upgrade,
            "<b>Object.GetHashCode</b> previously returned the InstanceID value, but this is no longer guaranteed. The hash code is not a unique identifier for the object.",
            "Call <b>Object.GetEntityId()</b> to obtain the EntityId. If a numeric value is needed, use <b>EntityId.ToULong</b>, but be aware that the bit arrangement is not guaranteed and may change."
        )
        {
            MessageFormat = "Object.GetHashCode called on '{0}' to obtain EntityId"
        };

        static readonly Descriptor k_ToStringDescriptor = new Descriptor(
            PAC2012,
            "EntityId converted to string",
            Areas.Upgrade,
            "Calling <b>EntityId.ToString</b> produces a human-readable representation that is not suitable for serialization. Parsing the result with <b>int.Parse</b> also loses the version portion of the EntityId and will break when EntityId can no longer be represented as a 32-bit integer.",
            "Use <b>EntityId.ToString()</b> paired with <b>EntityId.Parse()</b> for serialization round-trips, or <b>EntityId.ToULong</b> paired with <b>EntityId.FromULong</b>. For display purposes, <b>EntityId.ToString()</b> is acceptable."
        )
        {
            MessageFormat = "'{0}' converts EntityId to string"
        };

        static readonly Descriptor k_SortDescriptor = new Descriptor(
            PAC2013,
            "EntityId used to sort by creation order",
            Areas.Upgrade,
            "Comparing <b>EntityId</b> values to infer creation order relies on InstanceID sequential-assignment behaviour, which is not guaranteed for EntityId.",
            "Track creation order explicitly if required by your logic, rather than relying on EntityId comparison order."
        )
        {
            MessageFormat = "EntityId '{0}' used for ordering"
        };

        readonly OpCode[] m_OpCodes =
        {
            OpCodes.Call,
            OpCodes.Callvirt,
        };

        public override IReadOnlyList<OpCode> opCodes => m_OpCodes;

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_GetHashCodeDescriptor);
            registerDescriptor(k_ToStringDescriptor);
            registerDescriptor(k_SortDescriptor);
        }

        public override IEnumerable<ReportItemBuilder> Analyze(InstructionAnalysisContext context)
        {
            var callee = (MethodReference)context.Instruction.Operand;
            var methodName = callee.Name;
            var declaringType = callee.DeclaringType;
            var declaringTypeHash = declaringType.FastFullName().GetHashCode();

            // For value-type virtual calls the C# compiler emits a "constrained." prefix
            // instruction before the callvirt. The callee's declaring type is then System.Object
            // (or an interface), not the actual value type. Unwrap it so type-hash checks work.
            var prevInstr = context.Instruction.Previous;
            if (prevInstr != null && prevInstr.OpCode == OpCodes.Constrained)
            {
                declaringType = (TypeReference)prevInstr.Operand;
                declaringTypeHash = declaringType.FastFullName().GetHashCode();
            }

            if (declaringTypeHash == k_EntityIdTypeHash)
            {
                switch (methodName)
                {
                    case "ToString":
                        if (!IsPassedToDebugLog(context.Instruction))
                        {
                            yield return context.CreateIssue(IssueCategory.Code, k_ToStringDescriptor.Id, "EntityId.ToString")
                                .WithUpgradeProperties(["6000.4", null, null]);
                        }
                        break;

                    case "CompareTo":
                    case "op_LessThan":
                    case "op_GreaterThan":
                    case "op_LessThanOrEqual":
                    case "op_GreaterThanOrEqual":
                        yield return context.CreateIssue(IssueCategory.Code, k_SortDescriptor.Id, methodName)
                            .WithUpgradeProperties(["6000.4", null, null]);
                        break;
                }
            }
            else if (methodName == "GetHashCode" && !declaringType.IsValueType)
            {
                // callvirt on GetHashCode always reports System.Object as the declaring type,
                // regardless of which override is actually targeted. Look one instruction back
                // to find the receiver's static type instead.
                var receiverType = GetReceiverType(context);
                if (receiverType != null && !receiverType.IsValueType)
                {
                    bool isOrInheritedFrom = false;
                    try
                    {
                        isOrInheritedFrom = MonoCecilHelper.IsOrInheritedFrom(receiverType, "UnityEngine.Object");
                    }
                    catch (AssemblyResolutionException e)
                    {
                        Debug.LogWarningFormat("EntityIdAnalyzer: Could not resolve {0}: {1}", receiverType.Name, e.Message);
                    }
                    if (isOrInheritedFrom)
                    {
                        yield return context.CreateIssue(IssueCategory.Code, k_GetHashCodeDescriptor.Id, receiverType.Name)
                            .WithUpgradeProperties(["6000.4", null, null]);
                    }
                }
            }
        }

        // Walk forward through a small window of instructions to determine whether the string
        // produced by EntityId.ToString() is ultimately passed to a Debug.Log* method.
        //
        // "Transparent" instructions are those that legitimately appear between ToString() and
        // a Debug.Log call when the caller builds a message string:
        //   - load opcodes (ldloc*, ldarg*, ldfld, ldsfld, ldstr, ldnull)
        //       extra arguments to string.Format/Concat or Debug.Log
        //   - box
        //       coercing a value to object for a params slot
        //   - call string.Concat    — "a" + id.ToString() or id.ToString() + "b"
        //   - call string.Format    — string.Format("{0} {1}", id.ToString(), label)
        //
        // Any other instruction (stores, branches, calls to non-string methods) ends the walk,
        // preventing false suppression when ToString() feeds something other than a log call.
        static bool IsPassedToDebugLog(Instruction instruction)
        {
            const int k_LookaheadLimit = 8;
            var current = instruction.Next;
            for (int step = 0; step < k_LookaheadLimit && current != null; step++, current = current.Next)
            {
                var opCode = current.OpCode;

                if (opCode == OpCodes.Call || opCode == OpCodes.Callvirt)
                {
                    var callee = (MethodReference)current.Operand;
                    var dtHash = callee.DeclaringType.FastFullName().GetHashCode();

                    if (dtHash == k_DebugTypeHash)
                    {
                        switch (callee.Name)
                        {
                            case "Log":
                            case "LogWarning":
                            case "LogError":
                            case "LogFormat":
                            case "LogWarningFormat":
                            case "LogErrorFormat":
                            case "LogAssertion":
                            case "LogAssertionFormat":
                                return true;
                            default:
                                return false;
                        }
                    }

                    if (dtHash == k_StringTypeHash)
                    {
                        var n = callee.Name;
                        if (n == "Concat" || n == "Format")
                            continue;
                    }

                    return false;
                }

                if (IsTransparentLoad(opCode))
                    continue;

                return false;
            }
            return false;
        }

        // Returns true for load opcodes that appear as extra arguments in multi-argument
        // string.Format/Concat calls, and for box which coerces values to object.
        static bool IsTransparentLoad(OpCode opCode)
        {
            switch (opCode.Code)
            {
                case Code.Ldstr:
                case Code.Ldnull:
                case Code.Box:
                case Code.Ldloc_0:
                case Code.Ldloc_1:
                case Code.Ldloc_2:
                case Code.Ldloc_3:
                case Code.Ldloc_S:
                case Code.Ldloc:
                case Code.Ldarg_0:
                case Code.Ldarg_1:
                case Code.Ldarg_2:
                case Code.Ldarg_3:
                case Code.Ldarg_S:
                case Code.Ldarg:
                case Code.Ldfld:
                case Code.Ldsfld:
                case Code.Ldc_I4_M1:
                case Code.Ldc_I4_0:
                case Code.Ldc_I4_1:
                case Code.Ldc_I4_2:
                case Code.Ldc_I4_3:
                case Code.Ldc_I4_4:
                case Code.Ldc_I4_5:
                case Code.Ldc_I4_6:
                case Code.Ldc_I4_7:
                case Code.Ldc_I4_8:
                case Code.Ldc_I4_S:
                case Code.Ldc_I4:
                case Code.Ldc_I8:
                case Code.Ldc_R4:
                case Code.Ldc_R8:
                    return true;
                default:
                    return false;
            }
        }

        // Recover the static type of the value on top of the IL stack at a call site by
        // inspecting the single instruction that pushed it. Handles the most common patterns.
        static TypeReference GetReceiverType(InstructionAnalysisContext context)
        {
            var prev = context.Instruction.Previous;
            if (prev == null)
                return null;

            // Short-form parameter loads (Ldarg_0..Ldarg_3). In instance methods Ldarg_0 is
            // 'this' and Ldarg_1/2/3 are the first three explicit parameters. In static
            // methods Ldarg_0/1/2/3 are the first four explicit parameters.
            switch (prev.OpCode.Code)
            {
                case Code.Ldarg_0: return GetParameterType(context.MethodDefinition, 0);
                case Code.Ldarg_1: return GetParameterType(context.MethodDefinition, 1);
                case Code.Ldarg_2: return GetParameterType(context.MethodDefinition, 2);
                case Code.Ldarg_3: return GetParameterType(context.MethodDefinition, 3);
            }

            // Long-form parameter loads carry the ParameterDefinition directly.
            if (prev.OpCode.Code == Code.Ldarg_S || prev.OpCode.Code == Code.Ldarg)
                return ((ParameterDefinition)prev.Operand).ParameterType;

            // Field load (instance or static)
            if (prev.OpCode.Code == Code.Ldfld || prev.OpCode.Code == Code.Ldsfld)
                return ((FieldReference)prev.Operand).FieldType;

            // Local variable loads
            switch (prev.OpCode.Code)
            {
                case Code.Ldloc_0: return GetLocalType(context.MethodDefinition, 0);
                case Code.Ldloc_1: return GetLocalType(context.MethodDefinition, 1);
                case Code.Ldloc_2: return GetLocalType(context.MethodDefinition, 2);
                case Code.Ldloc_3: return GetLocalType(context.MethodDefinition, 3);
                case Code.Ldloc_S:
                case Code.Ldloc:
                    return ((VariableDefinition)prev.Operand).VariableType;
            }

            // Method calls and property getters — receiver is the return value's static type
            // (e.g. GetComponent<Renderer>().GetHashCode() or obj.SomeProp.GetHashCode()).
            if (prev.OpCode.Code == Code.Call || prev.OpCode.Code == Code.Callvirt)
                return ResolveMethodReturnType((MethodReference)prev.Operand);

            // Reference-type casts — receiver is the cast target type
            // (e.g. ((Object)myVar).GetHashCode() or (myVar as Object).GetHashCode()).
            if (prev.OpCode.Code == Code.Castclass || prev.OpCode.Code == Code.Isinst)
                return (TypeReference)prev.Operand;

            return null;
        }

        // Get a method's return type, substituting generic parameters with their concrete
        // arguments from the call site. For GetComponent<Renderer>() the declared return type
        // is the generic parameter T ("!!0"), which Resolve()s to null — passing that to the
        // base-type walk causes a NRE. Returns null if a generic parameter can't be resolved
        // so the caller skips this call site instead.
        static TypeReference ResolveMethodReturnType(MethodReference method)
        {
            var returnType = method.ReturnType;
            if (returnType is GenericParameter gp)
            {
                if (gp.Type == GenericParameterType.Method &&
                    method is GenericInstanceMethod gim &&
                    gp.Position < gim.GenericArguments.Count)
                {
                    returnType = gim.GenericArguments[gp.Position];
                }
                else if (gp.Type == GenericParameterType.Type &&
                         method.DeclaringType is GenericInstanceType git &&
                         gp.Position < git.GenericArguments.Count)
                {
                    returnType = git.GenericArguments[gp.Position];
                }
            }
            return returnType is GenericParameter ? null : returnType;
        }

        // Map an IL argument index to a parameter type. In instance methods argIndex 0 is
        // the implicit 'this' (its type is the declaring type) and Parameters[0] is the
        // first explicit parameter; static methods have no 'this' so Parameters[0] aligns
        // with argIndex 0.
        static TypeReference GetParameterType(MethodDefinition method, int argIndex)
        {
            if (!method.IsStatic)
            {
                if (argIndex == 0)
                    return method.DeclaringType;
                argIndex--;
            }
            var parameters = method.Parameters;
            return argIndex < parameters.Count ? parameters[argIndex].ParameterType : null;
        }

        static TypeReference GetLocalType(MethodDefinition method, int index)
        {
            var variables = method.Body.Variables;
            return index < variables.Count ? variables[index].VariableType : null;
        }
    }
}
