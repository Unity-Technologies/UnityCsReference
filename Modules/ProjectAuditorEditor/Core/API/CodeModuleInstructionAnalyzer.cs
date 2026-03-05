// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.ProjectAuditor.Editor.AssemblyUtils;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// A context object passed by CodeModule to a CodeModuleInstructionAnalyzer's Analyze() method.
    /// </summary>
    internal class MethodAnalysisContext : AnalysisContext
    {
        /// <summary>
        /// A Mono.Cecil Method Definition containing information about the current method being analyzed.
        /// </summary>
        public MethodDefinition MethodDefinition;

        /// <summary>
        /// Custom metadata that an analyzer can setup for each Assembly.
        /// </summary>
        public object AssemblyUserData;
    }

    /// <summary>
    /// A context object passed by CodeModule to a CodeModuleInstructionAnalyzer's Analyze() method.
    /// </summary>
    internal class InstructionAnalysisContext : AnalysisContext
    {
        // TODO: these 2 fields used to be public, but we can't leak the Cecil implementation out in the public API, as we might move away from using Cecil in the future.

        /// <summary>
        /// A Mono.Cecil Method Definition containing information about the current method being analyzed.
        /// </summary>
        public MethodDefinition MethodDefinition;

        /// <summary>
        /// A Mono.Cecil Instruction containing information about the current code instruction being analyzed.
        /// </summary>
        public Instruction Instruction;

        /// <summary>
        /// Assembly the instruction belongs to.
        /// </summary>
        public AssemblyInfo AssemblyInfo;

        /// <summary>
        /// Custom metadata that an analyzer can setup for each Assembly.
        /// </summary>
        public object AssemblyUserData;
    }

    /// <summary>
    /// Abstract base class for an Analyzer to be invoked by CodeModule
    /// </summary>
    internal abstract class CodeModuleInstructionAnalyzer : ModuleAnalyzer
    {
        /// <summary>
        /// A collection of Mono.Cecil OpCodes which are used by this analyzer.
        /// </summary>
        /// <remarks>
        /// To speed up the code analysis process, each CodeModuleInstructionAnalyzer must provide a list of the
        /// Instruction OpCodes it's interested in. Project Auditor will only invoke an InstructionAnalyzer if the
        /// OpCode of the Instruction currently under analysis matches one of the OpCodes in this list. For more
        /// details, refer to the [Mono.Cecil Github page](https://github.com/jbevain/cecil/blob/master/Mono.Cecil.Cil/OpCodes.cs).
        /// </remarks>
        public abstract IReadOnlyList<OpCode> opCodes { get; }

        /// <summary>
        /// Implement this method to detect Issues in a code instruction, and construct a ReportItemBuilder object with
        /// basic information about a ReportItem object to describe the issue.
        /// </summary>
        /// <param name="context">Context object containing information necessary to perform analysis</param>
        /// <returns>A ReportItemBuilder containing a partially-constructed ReportItem</returns>
        /// <remarks>
        /// When Instruction Analyzers detect an issue, they should call <see cref="AnalysisContext.CreateIssue"/>
        /// to begin creating an issue with an IssueCategory, a DescriptorId and any other relevant data. The Code Module
        /// will add further information including the DependencyNode, Location and assembly name and add the resulting
        /// ReportItem to the report.
        /// </remarks>
        public abstract ReportItemBuilder Analyze(InstructionAnalysisContext context);

        /// <summary>
        /// Implement this method to store custom per-assembly data
        /// </summary>
        internal virtual object OnAnalyzeAssembly()
        {
            return null;
        }

        /// <summary>
        /// Implement this method to store custom per-method data (requires allocating data first via OnAnalyzeAssembly)
        /// </summary>
        internal virtual ReportItemBuilder OnAnalyzeMethodBody(MethodAnalysisContext context)
        {
            return null;
        }
    }
}
