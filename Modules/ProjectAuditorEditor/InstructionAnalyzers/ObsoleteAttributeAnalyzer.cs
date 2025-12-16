// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class ObsoleteAttributeAnalyzer : CodeModuleInstructionAnalyzer
    {
        static readonly int k_ObsoleteAttributeHashCode = "System.ObsoleteAttribute".GetHashCode();

        internal const string PAC0194 = nameof(PAC0194);

        static readonly Descriptor k_ObsoleteAttributeIssueDescriptor = new Descriptor
            (
            PAC0194,
            "Use of Obsolete Code",
            Areas.CPU | Areas.Upgrade,
            "Code marked with the <b>System.Obsolete</b> attribute is deprecated, and may be removed in a future version of Unity.",
            "Replace the code with something that is not obsolete."
            )
        {
            DefaultSeverity = Severity.Minor
        };

        readonly OpCode[] m_OpCodes =
        {
            OpCodes.Call,
            OpCodes.Callvirt,
            OpCodes.Newobj,
            OpCodes.Newarr,
            OpCodes.Stfld,
            OpCodes.Stsfld
        };

        class AnalysisCache
        {
            public class CacheData
            {
                public CustomAttribute ObsoleteAttribute;
                public string ObsoleteName;
                public TypeDefinition DeclaringType;
            }

            public bool ObsoleteMethod;
            public Dictionary<object, CacheData> Cache = new Dictionary<object, CacheData>(512);
        }

        public override IReadOnlyCollection<OpCode> opCodes => m_OpCodes;

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_ObsoleteAttributeIssueDescriptor);
        }

        internal override object OnAnalyzeAssembly()
        {
            return new AnalysisCache();
        }

        internal override void OnAnalyzeMethodBody(MethodDefinition methodDefinition, object assemblyUserData)
        {
            var cache = (AnalysisCache)assemblyUserData;

            // If the method being analyzed is obsolete, do not worry about it calling other Obsolete things
            var obsolete = FindObsoleteAttribute(methodDefinition, cache, out var _, out var _);
            cache.ObsoleteMethod = (obsolete != null);
        }

        public override ReportItemBuilder Analyze(InstructionAnalysisContext context)
        {
            var cache = (AnalysisCache)context.AssemblyUserData;

            // If the method being analyzed is obsolete, do not worry about it calling other Obsolete things
            if (cache.ObsoleteMethod)
                return null;

            // Now check the instruction
            if (context.Instruction.Operand is MemberReference callee)
            {
                var obsolete = FindObsoleteAttribute(callee, cache, out var obsoleteName, out var declaringType);
                if (obsolete != null)
                {
                    if (obsoleteName == ".ctor")
                        obsoleteName = $"{declaringType} Constructor";

                    var arguments = obsolete.ConstructorArguments;

                    bool error = (arguments.Count > 1) ? (bool)arguments[1].Value : false;
                    if (error && context.AssemblyInfo.IsUnityOwned) // Unity sometimes needs to use Obsolete code eg to set up obsolete fields in constructors. Downgrade this to a warning.
                        error = false;

                    string msg;
                    if (arguments.Count > 0)
                        msg = $"'{obsoleteName}' is obsolete: '{(string)arguments[0].Value}'";
                    else
                        msg = $"'{obsoleteName}' is obsolete.";

                    return context.CreateIssue(IssueCategory.Code, k_ObsoleteAttributeIssueDescriptor.Id)
                        .WithSeverity(error ? Severity.Error : Severity.Warning)
                        .WithDescription(msg);
                }
            }

            return null;
        }

        private CustomAttribute FindObsoleteAttribute(MemberReference callee, AnalysisCache cache, out string obsoleteName, out TypeDefinition declaringType)
        {
            obsoleteName = string.Empty;
            declaringType = null;

            if (callee == null)
                return null;

            // Check the cache first, to avoid repeated queries
            if (cache.Cache.TryGetValue(callee, out var cacheData))
            {
                obsoleteName = cacheData.ObsoleteName;
                declaringType = cacheData.DeclaringType;
                return cacheData.ObsoleteAttribute;
            }

            IMemberDefinition memberDefinition = callee.Resolve();
            if (memberDefinition == null)
                return null;

            declaringType = memberDefinition.DeclaringType;

            // Check the method for an obsolete attribute
            CustomAttribute obsolete = null;
            if (memberDefinition.HasCustomAttributes)
                obsolete = CheckAttributes(memberDefinition, cache, ref declaringType, out obsoleteName);

            // Check all the declaring types too (walk parent hierarchy)
            if (obsolete == null)
                obsolete = CheckDeclaringTypes(memberDefinition, cache, ref declaringType, out obsoleteName);

            return obsolete;
        }

        private CustomAttribute CheckAttributes(IMemberDefinition attributeProvider, AnalysisCache cache, ref TypeDefinition declaringType, out string obsoleteName)
        {
            // Check the cache first, to avoid repeated queries
            if (cache.Cache.TryGetValue(attributeProvider, out var attributeProviderCacheData))
            {
                obsoleteName = attributeProviderCacheData.ObsoleteName;
                declaringType = attributeProviderCacheData.DeclaringType;
                return attributeProviderCacheData.ObsoleteAttribute;
            }

            // This will get overwritten later if a nested attribute is obsolete, but add it here to prevent infinite recursion
            cache.Cache[attributeProvider] = new AnalysisCache.CacheData();

            foreach (var attribute in attributeProvider.CustomAttributes)
            {
                if (attribute.AttributeType.FullName.GetHashCode() == k_ObsoleteAttributeHashCode)
                {
                    // Ignore the compiler-generated Obsolete attribute for 'ref struct'
                    if (attribute.ConstructorArguments.Count == 0 || (string)attribute.ConstructorArguments[0].Value != "Types with embedded references are not supported in this version of your compiler.")
                    {
                        obsoleteName = attributeProvider.Name;
                        cache.Cache[attributeProvider] = new AnalysisCache.CacheData() { ObsoleteAttribute = attribute, ObsoleteName = obsoleteName, DeclaringType = declaringType };
                        return attribute;
                    }
                }

                var typeDefinition = attribute.AttributeType.Resolve();
                if (typeDefinition != null && typeDefinition.HasCustomAttributes)
                {
                    var obsolete = CheckAttributes(typeDefinition, cache, ref declaringType, out obsoleteName);
                    if (obsolete != null)
                    {
                        obsoleteName = typeDefinition.Name;
                        cache.Cache[attributeProvider] = new AnalysisCache.CacheData() { ObsoleteAttribute = attribute, ObsoleteName = obsoleteName, DeclaringType = declaringType };
                        return obsolete;
                    }
                }
            }

            obsoleteName = string.Empty;
            return null;
        }

        private CustomAttribute CheckConstraints(TypeDefinition typeDefinition, AnalysisCache cache, ref TypeDefinition declaringType, out string obsoleteName)
        {
            foreach (var genericParameter in typeDefinition.GenericParameters)
            {
                if (genericParameter.HasConstraints)
                {
                    foreach (GenericParameterConstraint constraint in genericParameter.Constraints)
                    {
                        var constraintTypeDefinition = constraint.ConstraintType.Resolve();
                        if (constraintTypeDefinition != null && constraintTypeDefinition.HasCustomAttributes)
                        {
                            var obsolete = CheckAttributes(constraintTypeDefinition, cache, ref declaringType, out obsoleteName);
                            if (obsolete != null)
                                return obsolete;
                        }
                    }
                }
            }

            obsoleteName = string.Empty;
            return null;
        }

        private CustomAttribute CheckDeclaringTypes(IMemberDefinition memberDefinition, AnalysisCache cache, ref TypeDefinition declaringType, out string obsoleteName)
        {
            declaringType = memberDefinition.DeclaringType;
            if (declaringType != null)
            {
                // Check if there is a property setter or getter that is obsolete
                Mono.Cecil.PropertyDefinition targetProperty = FindPropertyForMethod(declaringType.Properties, memberDefinition);
                if (targetProperty != null && targetProperty.HasCustomAttributes)
                {
                    var obsolete = CheckAttributes(targetProperty, cache, ref declaringType, out obsoleteName);
                    if (obsolete != null || declaringType == null)
                    {
                        if (obsolete != null)
                            cache.Cache[memberDefinition] = new AnalysisCache.CacheData() { ObsoleteAttribute = obsolete, ObsoleteName = obsoleteName, DeclaringType = declaringType };
                        return obsolete;
                    }
                }

                // Perhaps the entire class is marked as Obsolete
                if (declaringType.HasCustomAttributes)
                {
                    var obsolete = CheckAttributes(declaringType, cache, ref declaringType, out obsoleteName);
                    if (obsolete != null || declaringType == null)
                    {
                        if (obsolete != null)
                            cache.Cache[memberDefinition] = new AnalysisCache.CacheData() { ObsoleteAttribute = obsolete, ObsoleteName = obsoleteName, DeclaringType = declaringType };
                        return obsolete;
                    }
                }

                // Perhaps its generic constraints are marked as Obsolete
                if (declaringType.HasGenericParameters)
                {
                    var obsolete = CheckConstraints(declaringType, cache, ref declaringType, out obsoleteName);
                    if (obsolete != null || declaringType == null)
                    {
                        if (obsolete != null)
                            cache.Cache[memberDefinition] = new AnalysisCache.CacheData() { ObsoleteAttribute = obsolete, ObsoleteName = obsoleteName, DeclaringType = declaringType };
                        return obsolete;
                    }
                }

                // Recurse
                {
                    var obsolete = CheckDeclaringTypes(declaringType, cache, ref declaringType, out obsoleteName);
                    if (obsolete != null)
                    {
                        cache.Cache[memberDefinition] = new AnalysisCache.CacheData() { ObsoleteAttribute = obsolete, ObsoleteName = obsoleteName, DeclaringType = declaringType };
                        return obsolete;
                    }
                }
            }

            obsoleteName = string.Empty;
            return null;                
        }

        private Mono.Cecil.PropertyDefinition FindPropertyForMethod(Collection<Mono.Cecil.PropertyDefinition> properties, IMemberDefinition memberDefinition)
        {
            foreach (var property in properties)
            {
                if (property.SetMethod == memberDefinition || property.GetMethod == memberDefinition)
                    return property;
            }

            return null;
        }
    }
}
