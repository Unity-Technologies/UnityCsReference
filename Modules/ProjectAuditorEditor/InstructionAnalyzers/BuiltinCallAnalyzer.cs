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

        public override IReadOnlyCollection<OpCode> opCodes => m_OpCodes;

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            var descriptors = DescriptorLoader.LoadFromJson(ProjectAuditor.s_RulesDataPath, "ApiDatabase");
            foreach (var descriptor in descriptors)
            {
                registerDescriptor(descriptor);
            }

            var methodDescriptors = descriptors.Where(
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

            m_NamespaceOrClassDescriptors = descriptors.Where(descriptor => descriptor.Method.Equals("*")).ToDictionary(d => d.Type);
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
                if (methodName.StartsWith("get_", StringComparison.Ordinal))
                    methodName = methodName.Substring("get_".Length);

                List<Descriptor> descriptors;
                if (!m_Descriptors.TryGetValue(methodName, out descriptors))
                    return null;

                descriptor = descriptors.Find(d => CodeAnalysis.MonoCecilHelper.IsOrInheritedFrom(declaringType, d.Type));

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

                var genericInstanceMethod = callee as GenericInstanceMethod;
                if (genericInstanceMethod != null && genericInstanceMethod.HasGenericArguments)
                {
                    var genericTypeNames = genericInstanceMethod.GenericArguments.Select(a => a.FullName).ToArray();
                    description = $"'{descriptor.Title}<{string.Join(", ", genericTypeNames)}>' usage";
                }
                else
                {
                    // by default use descriptor issue description
                    description = $"'{descriptor.Title}' usage";
                }
            }

            return context.CreateIssue(IssueCategory.Code, descriptor.Id)
                .WithDescription(description);
        }
    }
}
