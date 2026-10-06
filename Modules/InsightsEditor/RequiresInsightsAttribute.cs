// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEditor.InsightsEditor;

/// <summary>
/// Describes a package's requirement for Insights data collection.
/// </summary>
/// <remarks>
/// The Editor resolves one instance of this struct for every package that declares
/// <see cref="RequiresInsightsAttribute"/>, using the name and version of the package that owns
/// the attributed assembly. The Editor records the resolved requirements in the project settings
/// so the runtime can report them when it requests its collection configuration.
/// </remarks>
[VisibleToOtherModules]
[NativeHeader("Modules/Insights/CollectionRequirement.h")]
internal struct CollectionRequirement
{
    /// <summary>
    /// Name of the package that requires Insights data collection, for example "com.unity.purchasing".
    /// </summary>
    public string packageName;

    /// <summary>
    /// Semantic version of the package, for example "5.1.0".
    /// </summary>
    public string semVer;
}

// Public so packages can apply it, but deliberately absent from the Script Reference.
/// <summary>
/// Declares that the package owning the attributed assembly requires Insights data collection.
/// </summary>
/// <remarks>
/// Apply this attribute to an assembly that belongs to a package to declare that the package
/// requires Insights data collection. The Editor discovers the declarations on every domain
/// reload and player build, and identifies the declaring package by the name and version of the
/// installed package that owns the attributed assembly. A declared requirement keeps the
/// Insights module in player builds and enables data collection for the declaring package, even
/// when the Engine Diagnostics project setting is disabled.
/// The attribute is ignored, with a warning, on assemblies that do not belong to a package.
/// When several assemblies of the same package carry the attribute, the package declares a
/// single requirement. Declared requirements have no effect on platforms where Insights data
/// collection is not supported, so declarations do not need to be guarded by platform scripting
/// defines.
/// This attribute is available in every Editor. A package that also compiles against Editor
/// versions without this attribute can guard the declaration with an assembly definition
/// version define on the "Unity" resource, as shown in the example.
/// </remarks>
/// <example nocheck="true">
/// <code lang="cs"><![CDATA[
/// // AssemblyInfo.cs in an Editor assembly of the package. The UNITY_INSIGHTS_REQUIREMENTS_API
/// // define comes from the assembly definition's versionDefines, so the declaration compiles
/// // only in Editor versions that provide the attribute:
/// //   "versionDefines": [
/// //       { "name": "Unity", "expression": "6000.7.0a7", "define": "UNITY_INSIGHTS_REQUIREMENTS_API" }
/// //   ]
/// #if UNITY_INSIGHTS_REQUIREMENTS_API
/// [assembly: UnityEditor.InsightsEditor.RequiresInsights]
/// #endif
/// ]]></code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly)]
[ExcludeFromDocs]
public sealed class RequiresInsightsAttribute : Attribute
{
}
