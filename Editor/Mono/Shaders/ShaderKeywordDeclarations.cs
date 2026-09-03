// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEditor.Shaders
{
    // Bound to the native KeywordOverrideRestrictionFlags: which override types are not available for a
    // keyword declaration. A declaration without restrictions can be overridden to any override type.
    [Flags]
    internal enum ShaderKeywordOverrideRestriction
    {
        None = 0,
        NoDynamicBranch = 1 << 0,
    }

    // Information about how a specific keyword tuple is being used in the project. The data is aggregated
    // over multiple instances of directives and shaders declaring this specific keyword tuple.
    internal struct ShaderKeywordDeclarationInfo
    {
        public string[] keywords;

        // Bit per ShaderVariantGenerationMode the declaration is declared with. There can be several,
        // since different shaders can declare the same keywords with different directives.
        [NativeName("variantGenerationModeMask")]
        public int declaredVariantGenerationModeMask;

        // Restrictions hit by at least one instance of this declaration
        public ShaderKeywordOverrideRestriction restrictionsInAnyInstance;

        // Restrictions hit by every instance of this declaration
        public ShaderKeywordOverrideRestriction restrictionsInEveryInstance;

        public bool IsRestrictedInAnyInstance(ShaderKeywordOverrideRestriction restriction)
        {
            return (restrictionsInAnyInstance & restriction) != 0;
        }

        public bool IsRestrictedInEveryInstance(ShaderKeywordOverrideRestriction restriction)
        {
            return (restrictionsInEveryInstance & restriction) != 0;
        }

        public bool IsDeclaredAs(ShaderBuildSettings.ShaderVariantGenerationMode variantGenerationMode)
        {
            return (declaredVariantGenerationModeMask & (1 << (int)variantGenerationMode)) != 0;
        }
    }

    [NativeHeader("Editor/Src/Shaders/ShaderKeywordDeclarations.h")]
    internal static class ShaderKeywordDeclarations
    {
        // Gathers the declarations of every shader, compute shader and ray tracing shader in the
        // project. Shaders that aren't imported yet are imported, which can take a while.
        [FreeFunction("GatherProjectShaderKeywordDeclarations")]
        public static extern ShaderKeywordDeclarationInfo[] GatherFromProject();

        [FreeFunction("GatherShaderKeywordDeclarations")]
        public static extern ShaderKeywordDeclarationInfo[] GatherFromShader([NotNull] Shader shader);

        [FreeFunction("GatherComputeShaderKeywordDeclarations")]
        public static extern ShaderKeywordDeclarationInfo[] GatherFromComputeShader([NotNull] ComputeShader shader);

        [FreeFunction("GatherRayTracingShaderKeywordDeclarations")]
        public static extern ShaderKeywordDeclarationInfo[] GatherFromRayTracingShader([NotNull] RayTracingShader shader);
    }
}
