// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.InstructionAnalyzers
{
    class ObsoleteAttributeAnalyzer : CodeModuleInstructionAnalyzer
    {
        static readonly int k_ObsoleteAttributeHashCode = "System.ObsoleteAttribute".GetHashCode();

        internal const string PAC0194 = nameof(PAC0194);
        internal const string PAC0195 = nameof(PAC0195);
        internal const string PAC0196 = nameof(PAC0196);
        internal const string PAC0197 = nameof(PAC0197);
        internal const string PAC0198 = nameof(PAC0198);

        internal static readonly Descriptor k_ObsoleteAttributeIssueDescriptor = new Descriptor
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

        static readonly Descriptor k_ObsoleteAutoUpgradeIssueDescriptor = new Descriptor
            (
            PAC0195,
            "Code will become Obsolete",
            Areas.CPU | Areas.Upgrade,
            "This code will become obsolete in a future version of Unity. Unity can automatically upgrade this code in the new version for you.",
            "Unity can update the code automatically after upgrading."
            )
        {
            DefaultSeverity = Severity.Minor
        };

        static readonly Descriptor k_ObsoleteWarningUpgradeIssueDescriptor = new Descriptor
            (
            PAC0196,
            "Code will become Obsolete",
            Areas.CPU | Areas.Upgrade,
            "This code will become obsolete in a future version of Unity. This issue is a warning, and will not prevent compilation in the new version.",
            "Fix the code after upgrading."
            )
        {
            DefaultSeverity = Severity.Moderate
        };

        static readonly Descriptor k_ObsoleteErrorUpgradeIssueDescriptor = new Descriptor
            (
            PAC0197,
            "Code will become Obsolete",
            Areas.CPU | Areas.Upgrade,
            "This code will become obsolete in a future version of Unity. This issue will produce a compiler error in the new version.",
            "Fix the code after upgrading."
            )
        {
            DefaultSeverity = Severity.Major
        };

        static readonly Descriptor k_ObsoleteRemovedUpgradeIssueDescriptor = new Descriptor
            (
            PAC0198,
            "Code will be removed",
            Areas.CPU | Areas.Upgrade,
            "This code has been removed in a future version of Unity. This issue will prevent compilation in the new version.",
            "Fix the code after upgrading."
            )
        {
            DefaultSeverity = Severity.Major
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

        public override IReadOnlyList<OpCode> opCodes => m_OpCodes;

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_ObsoleteAttributeIssueDescriptor);
            registerDescriptor(k_ObsoleteAutoUpgradeIssueDescriptor);
            registerDescriptor(k_ObsoleteWarningUpgradeIssueDescriptor);
            registerDescriptor(k_ObsoleteErrorUpgradeIssueDescriptor);
            registerDescriptor(k_ObsoleteRemovedUpgradeIssueDescriptor);
        }

        internal override object OnAnalyzeAssembly()
        {
            return new AnalysisCache();
        }

        internal override ReportItemBuilder OnAnalyzeMethodBody(MethodAnalysisContext context)
        {
            var cache = (AnalysisCache)context.AssemblyUserData;

            // If the method being analyzed is obsolete, do not worry about it calling other Obsolete things
            var obsolete = FindObsoleteAttribute(context.MethodDefinition, cache, out var _, out var _);
            cache.ObsoleteMethod = (obsolete != null);

            return null;
        }

        public override IEnumerable<ReportItemBuilder> Analyze(InstructionAnalysisContext context)
        {
            var cache = (AnalysisCache)context.AssemblyUserData;

            // Now check the instruction
            if (context.Instruction.Operand is MemberReference callee)
            {
                // If the method being analyzed is obsolete, do not worry about it calling other Obsolete things
                if (cache.ObsoleteMethod == false)
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

                        string msg, recommendation;
                        if (arguments.Count > 0)
                        {
                            recommendation = (string)arguments[0].Value;
                            msg = $"'{obsoleteName}' is obsolete: '{recommendation}'";
                        }
                        else
                        {
                            recommendation = string.Empty;
                            msg = $"'{obsoleteName}' is obsolete.";
                        }

                        yield return context.CreateIssue(IssueCategory.Code, k_ObsoleteAttributeIssueDescriptor.Id)
                            .WithSeverity(error ? Severity.Error : Severity.Warning)
                            .WithDescription(msg)
                            .WithUpgradeProperties(Application.unityVersion, null, recommendation);
                    }
                }

                // Check for obsoletion in future Unity versions
                if (context.Instruction.OpCode == OpCodes.Call || context.Instruction.OpCode == OpCodes.Callvirt)
                {
                    if (ObsoleteLibrary.HasAnyUpgradeVersions)
                    {
                        string fullName = BuildObsoleteLookupKey((MethodReference)callee);

                        if (ObsoleteLibrary.LibraryDictionary.TryGetValue(fullName, out var reportItem))
                        {
                            var currentVersion = Utility.VersionToInt(Application.unityVersion);

                            bool autoUpgradable = reportItem.GetCustomPropertyBool(ObsoleteApiProperty.AutoUpgradable);
                            var removedIn = reportItem.GetCustomProperty(ObsoleteApiProperty.RemovedIn);
                            var recommendation = reportItem.GetCustomProperty(ObsoleteApiProperty.Recommendation);

                            if (autoUpgradable)
                            {
                                var obsoleteSince = reportItem.GetCustomProperty(ObsoleteApiProperty.ObsoleteSince);
                                if (Utility.VersionToInt(obsoleteSince) > currentVersion)
                                {
                                    yield return new ReportItemBuilder(IssueCategory.Code, k_ObsoleteAutoUpgradeIssueDescriptor.Id, $"'{reportItem.Description}' will be automatically upgraded", reportItem)
                                        .WithUpgradeProperties(obsoleteSince, removedIn, recommendation);
                                }
                            }
                            else
                            {
                                var warningSince = reportItem.GetCustomProperty(ObsoleteApiProperty.WarningSince);
                                var errorSince = reportItem.GetCustomProperty(ObsoleteApiProperty.ErrorSince);

                                if (!string.IsNullOrEmpty(warningSince) && Utility.VersionToInt(warningSince) > currentVersion)
                                {
                                    yield return new ReportItemBuilder(IssueCategory.Code, k_ObsoleteWarningUpgradeIssueDescriptor.Id, $"'{reportItem.Description}' obsoletion warning in {warningSince}", reportItem)
                                        .WithUpgradeProperties(warningSince, errorSince ?? removedIn, recommendation);
                                }

                                if (!string.IsNullOrEmpty(errorSince) && Utility.VersionToInt(errorSince) > currentVersion)
                                {
                                    yield return new ReportItemBuilder(IssueCategory.Code, k_ObsoleteErrorUpgradeIssueDescriptor.Id, $"'{reportItem.Description}' obsoletion error in {errorSince}", reportItem)
                                        .WithUpgradeProperties(errorSince, removedIn, recommendation);
                                }
                            }

                            if (!string.IsNullOrEmpty(removedIn) && Utility.VersionToInt(removedIn) > currentVersion)
                            {
                                yield return new ReportItemBuilder(IssueCategory.Code, k_ObsoleteRemovedUpgradeIssueDescriptor.Id, $"'{reportItem.Description}' will be removed in {removedIn}", reportItem)
                                    .WithUpgradeProperties(removedIn, null, recommendation);
                            }
                        }
                    }
                }
            }
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

        [ThreadStatic, NoAutoStaticsCleanup]
        static StringBuilder ts_KeyBuilder;

        internal static string BuildObsoleteLookupKey(MethodReference callee)
        {
            ParameterTypeSpec[] paramSpecs = null;
            if (callee.HasParameters)
            {
                var parameters = callee.Parameters;
                paramSpecs = new ParameterTypeSpec[parameters.Count];
                for (int i = 0; i < parameters.Count; i++)
                    paramSpecs[i] = ParameterTypeSpec.FromCecil(parameters[i].ParameterType);
            }

            // Reduce a constructed generic (ComponentLookup`1<MyComponent>) to its open type; the database records declaring types without their type arguments.
            var declaringType = callee.DeclaringType;
            while (declaringType is TypeSpecification specification)
                declaringType = specification.ElementType;

            return BuildObsoleteLookupKey(declaringType.FullName, callee.Name, paramSpecs);
        }

        // Builds the lookup key in the format used by ObsoleteDatabase.json:
        //  - Property getters: "Namespace.Type.PropertyName" (no parentheses)
        //  - Methods:          "Namespace.Type.MethodName(ParamType1,ParamType2,...)"
        // Parameter types use C# language keywords for primitives (int, string, ...),
        // short type names for everything else, "[]" for arrays, and "<T, U>" for generic args.
        internal static string BuildObsoleteLookupKey(string declaringTypeFullName, string methodName, IReadOnlyList<ParameterTypeSpec> parameters)
        {
            bool isPropertyGetter = methodName.StartsWith("get_", StringComparison.Ordinal);
            if (isPropertyGetter)
                methodName = methodName.Substring("get_".Length);

            var sb = ts_KeyBuilder ??= new StringBuilder();
            sb.Clear();
            AppendDeclaringTypeName(sb, declaringTypeFullName);
            sb.Append('.');
            sb.Append(methodName);

            if (!isPropertyGetter)
            {
                sb.Append('(');
                if (parameters != null)
                {
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        if (i > 0)
                            sb.Append(',');
                        AppendParameterTypeName(sb, parameters[i]);
                    }
                }
                sb.Append(')');
            }

            return sb.ToString();
        }

        // The database spells declaring types as open types with '.' between every segment, both for namespaces and
        // for nested types ("Unity.Entities.ComponentLookup", "UnityEditor.CameraEditor.Settings"). Cecil keeps the
        // generic arity suffix and separates nested types with '/'.
        static void AppendDeclaringTypeName(StringBuilder sb, string cecilFullName)
        {
            bool inArity = false;
            for (int i = 0; i < cecilFullName.Length; i++)
            {
                var c = cecilFullName[i];
                if (c == '`')
                {
                    inArity = true;
                }
                else if (c == '/' || c == '.')
                {
                    inArity = false;
                    sb.Append('.');
                }
                else if (!inArity)
                {
                    sb.Append(c);
                }
            }
        }

        static void AppendParameterTypeName(StringBuilder sb, ParameterTypeSpec type)
        {
            switch (type.TypeKind)
            {
                case ParameterTypeSpec.Kind.Array:
                    AppendParameterTypeName(sb, type.Element);
                    sb.Append("[]");
                    return;

                case ParameterTypeSpec.Kind.Pointer:
                    AppendParameterTypeName(sb, type.Element);
                    sb.Append('*');
                    return;

                case ParameterTypeSpec.Kind.ByReference:
                    AppendParameterTypeName(sb, type.Element);
                    return;

                case ParameterTypeSpec.Kind.GenericInstance:
                    AppendGenericInstanceTypeName(sb, type);
                    return;

                case ParameterTypeSpec.Kind.Simple:
                    var keyword = GetCSharpKeyword(type.FullName);
                    if (keyword != null)
                        sb.Append(keyword);
                    else
                        AppendUnqualifiedTypeName(sb, type.Name);
                    return;
            }
        }

        static void AppendUnqualifiedTypeName(StringBuilder sb, string name)
        {
            // Strip generic arity suffix (e.g. "Dictionary`2" -> "Dictionary")
            var tickIndex = name.IndexOf('`');
            if (tickIndex >= 0)
                sb.Append(name, 0, tickIndex);
            else
                sb.Append(name);
        }

        // The database keeps a nested type's qualifier only when that qualifier is generic
        // ("NativeArray<int>.ReadOnly", "CoreEditorDrawer<T>.IDrawer"), and strips it otherwise because a
        // non-generic qualifier cannot be told apart from a namespace in the sources it is scraped from.
        // Cecil reports the whole nested chain and hoists the enclosing type's arguments onto the innermost
        // type, so the arguments belong on the outermost segment and the nested names trail behind it.
        static void AppendGenericInstanceTypeName(StringBuilder sb, ParameterTypeSpec type)
        {
            var fullName = type.FullName;
            var outerEnd = fullName.IndexOf('/');
            if (outerEnd < 0)
                outerEnd = fullName.Length;

            AppendTypeNameSegment(sb, fullName, fullName.LastIndexOf('.', outerEnd - 1) + 1, outerEnd);

            sb.Append('<');
            var args = type.GenericArguments;
            for (int i = 0; i < args.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                AppendParameterTypeName(sb, args[i]);
            }
            sb.Append('>');

            for (int segmentStart = outerEnd; segmentStart < fullName.Length;)
            {
                var segmentEnd = fullName.IndexOf('/', segmentStart + 1);
                if (segmentEnd < 0)
                    segmentEnd = fullName.Length;

                sb.Append('.');
                AppendTypeNameSegment(sb, fullName, segmentStart + 1, segmentEnd);
                segmentStart = segmentEnd;
            }
        }

        static void AppendTypeNameSegment(StringBuilder sb, string fullName, int start, int end)
        {
            var tickIndex = fullName.IndexOf('`', start);
            if (tickIndex >= 0 && tickIndex < end)
                end = tickIndex;

            sb.Append(fullName, start, end - start);
        }

        // Roslyn can do this automatically via SpecialType. Use it when we migrate!
        static string GetCSharpKeyword(string fullName)
        {
            switch (fullName)
            {
                case "System.Boolean": return "bool";
                case "System.Byte": return "byte";
                case "System.SByte": return "sbyte";
                case "System.Int16": return "short";
                case "System.UInt16": return "ushort";
                case "System.Int32": return "int";
                case "System.UInt32": return "uint";
                case "System.Int64": return "long";
                case "System.UInt64": return "ulong";
                case "System.Single": return "float";
                case "System.Double": return "double";
                case "System.Decimal": return "decimal";
                case "System.Char": return "char";
                case "System.String": return "string";
                case "System.Object": return "object";
                case "System.Void": return "void";
                default: return null;
            }
        }
    }
}
