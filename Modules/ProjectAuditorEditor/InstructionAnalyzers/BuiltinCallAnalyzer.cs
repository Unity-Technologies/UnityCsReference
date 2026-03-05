// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.CodeAnalysis;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class BuiltinCallAnalyzer : CodeModuleInstructionAnalyzer
    {
        Dictionary<string, List<Descriptor>> m_Descriptors; // method name as key, list of type names as value
        Dictionary<string, Descriptor> m_NamespaceOrClassDescriptors; // namespace/class name as key

        readonly OpCode[] m_OpCodes =
        {
            OpCodes.Call,
            OpCodes.Callvirt
        };

        public override IReadOnlyList<OpCode> opCodes => m_OpCodes;

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            var descriptors = DescriptorLoader.LoadFromJson(ProjectAuditor.s_RulesDataPath, "ApiDatabase");
            foreach (var descriptor in descriptors)
            {
                registerDescriptor(descriptor);
            }

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var methodDescriptors = descriptors.Where(
#pragma warning restore UA2001
                descriptor => !descriptor.Method.Equals("*") &&
                !string.IsNullOrEmpty(descriptor.Type) &&
                descriptor.IsSupported());

            m_Descriptors = new Dictionary<string, List<Descriptor>>();
            foreach (var d in methodDescriptors)
            {
                if (!m_Descriptors.ContainsKey(d.Method))
                {
                    m_Descriptors.Add(d.Method, new List<Descriptor>());
                }
                m_Descriptors[d.Method].Add(d);
            }

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_NamespaceOrClassDescriptors = descriptors.Where(descriptor => descriptor.Method.Equals("*")).ToDictionary(d => d.Type);
#pragma warning restore UA2001
        }

        internal override ReportItemBuilder OnAnalyzeMethodBody(MethodAnalysisContext context)
        {
            var methodDefinition = context.MethodDefinition;
            if (!MonoBehaviourAnalysis.IsMonoBehaviourEvent(methodDefinition))
                return null;

            var declaringType = methodDefinition.DeclaringType;
            if (!MonoBehaviourAnalysis.IsMonoBehaviour(declaringType))
                return null;

            var description = SearchForApi(methodDefinition, out var descriptor);
            if (string.IsNullOrEmpty(description))
                return null;

            return context.CreateIssue(IssueCategory.Code, descriptor.Id).WithDescription(description);
        }

        public override ReportItemBuilder Analyze(InstructionAnalysisContext context)
        {
            var callee = (MethodReference)context.Instruction.Operand;
            var description = string.Empty;
            var methodName = callee.Name;

            Descriptor descriptor;
            var declaringType = callee.DeclaringType;

            // first check if type name, then namespace, then method/property name
            if (m_NamespaceOrClassDescriptors.TryGetValue(declaringType.FastFullName(), out descriptor))
            {
                description = string.Format("'{0}.{1}' usage", declaringType, methodName);
            }
            else if (m_NamespaceOrClassDescriptors.TryGetValue(declaringType.Namespace, out descriptor))
            {
                description = string.Format("'{0}.{1}' usage", declaringType, methodName);
            }
            else
            {
                description = SearchForApi(callee, out descriptor);
                if (string.IsNullOrEmpty(description))
                    return null;
            }

            return context.CreateIssue(IssueCategory.Code, descriptor.Id)
                .WithDescription(description);
        }

        string SearchForApi(MethodReference callee, out Descriptor descriptor)
        {
            descriptor = default;

            string methodName = callee.Name;
            if (methodName.StartsWith("get_", StringComparison.Ordinal))
                methodName = methodName.Substring("get_".Length);

            List<Descriptor> descriptors;
            if (!m_Descriptors.TryGetValue(methodName, out descriptors))
                return null;

            descriptor = descriptors.Find(d => MonoCecilHelper.IsOrInheritedFrom(callee.DeclaringType, d.Type));

            if (descriptor == null)
                return null;

            if (!string.IsNullOrEmpty(descriptor.ReturnType))
            {
                bool not = descriptor.ReturnType[0] == '!';
                bool valid = Enum.TryParse(typeof(MetadataType), not ? descriptor.ReturnType.Substring(1) : descriptor.ReturnType, true, out var returnTypeEnum);
                if (valid)
                {
                    if (not)
                    {
                        if (callee.MethodReturnType.ReturnType.MetadataType == (MetadataType)returnTypeEnum)
                            return null;
                    }
                    else
                    {
                        if (callee.MethodReturnType.ReturnType.MetadataType != (MetadataType)returnTypeEnum)
                            return null;
                    }
                }
            }

            string description = string.Empty;
            var genericInstanceMethod = callee as GenericInstanceMethod;
            if (genericInstanceMethod != null && genericInstanceMethod.HasGenericArguments)
            {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var genericTypeNames = genericInstanceMethod.GenericArguments.Select(a => a.FullName).ToArray();
#pragma warning restore UA2001
                description = $"'{descriptor.Title}<{string.Join(", ", genericTypeNames)}>' usage";
            }
            else
            {
                // by default use descriptor issue description
                description = $"'{descriptor.Title}' usage";
            }

            return description;
        }
    }
}
