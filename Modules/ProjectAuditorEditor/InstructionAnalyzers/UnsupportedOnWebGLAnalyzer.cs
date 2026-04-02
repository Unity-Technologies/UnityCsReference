// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    internal class UnsupportedOnWebGLAnalyzer : CodeModuleInstructionAnalyzer
    {
        internal const string PAC1005 = nameof(PAC1005);
        internal const string PAC1006 = nameof(PAC1006);
        internal const string PAC0233 = nameof(PAC0233);

        internal static readonly Descriptor k_DescriptorSystemNet = new Descriptor
            (
            PAC1005,
            "System.Net",
            Areas.Support,
            "<b>System.Net</b> is not supported on this platform. This might lead to build/runtime errors.",
            "Do not use the System.Net API on this platform."
            )
        {
            MessageFormat = "'{0}' usage",
            Platforms = [BuildTarget.WebGL]
        };

        internal static readonly Descriptor k_DescriptorSystemThreading = new Descriptor
            (
            PAC1006,
            "System.Threading",
            Areas.Support,
            "Dot Net threads are not supported on this platform. Using System.Threading might lead to build/runtime errors.",
            "Do not use the <b>System.Threading</b> API on this platform."
            )
        {
            MessageFormat = "'{0}' usage",
            Platforms = [BuildTarget.WebGL]
        };

        internal static readonly Descriptor k_DescriptorMicrophone = new Descriptor
            (
            PAC0233,
            "UnityEngine.Microphone",
            Areas.Support,
            "The <b>UnityEngine.Microphone</b> API is not supported on this platform. Using Microphone might lead to build/runtime errors.",
            "Do not use the Microphone API on this platform."
            )
        {
            MessageFormat = "'{0}' usage",
            Platforms = [BuildTarget.WebGL]
        };

        readonly OpCode[] m_OpCodes =
        {
            OpCodes.Call,
            OpCodes.Callvirt
        };

        bool descriptorSystemNetSupported;
        bool descriptorSystemThreadingSupported;
        bool descriptorMicrophoneSupported;

        public override IReadOnlyList<OpCode> opCodes => m_OpCodes;

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_DescriptorSystemNet);
            registerDescriptor(k_DescriptorSystemThreading);
            registerDescriptor(k_DescriptorMicrophone);

            descriptorSystemNetSupported = k_DescriptorSystemNet.IsSupported();
            descriptorSystemThreadingSupported = k_DescriptorSystemThreading.IsSupported();
            descriptorMicrophoneSupported = k_DescriptorMicrophone.IsSupported();
        }

        public override IEnumerable<ReportItemBuilder> Analyze(InstructionAnalysisContext context)
        {
            var methodReference = (MethodReference)context.Instruction.Operand;
            if (descriptorSystemNetSupported && methodReference.DeclaringType.FullName.StartsWith("System.Net."))
            {
                yield return context.CreateIssue(IssueCategory.Code, k_DescriptorSystemNet.Id, methodReference.FullName);
            }
            else if (descriptorSystemThreadingSupported && methodReference.DeclaringType.FullName.StartsWith("System.Threading."))
            {
                yield return context.CreateIssue(IssueCategory.Code, k_DescriptorSystemThreading.Id, methodReference.FullName);
            }
            else if (descriptorMicrophoneSupported && methodReference.DeclaringType.FullName.Equals("UnityEngine.Microphone"))
            {
                yield return context.CreateIssue(IssueCategory.Code, k_DescriptorMicrophone.Id, methodReference.FullName);
            }
        }
    }
}
