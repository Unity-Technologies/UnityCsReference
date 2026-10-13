// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Modules
{
    internal interface IDerivedBuildTargetExtensions
    {
        /// <summary>
        /// What this derived platform adds to its base platform's compilation extension.
        /// </summary>
        ICompilationExtension AdditionalCompilationExtension { get; }

        /// <summary>
        /// This derived platform's complete compilation extension.
        /// </summary>
        ICompilationExtension CreateCompilationExtension(ICompilationExtension baseCompilationExtension)
            => CompositeCompilationExtension.Combine(baseCompilationExtension, AdditionalCompilationExtension);

        IBuildPostprocessor BuildPostprocessor { get; }
        IDerivedBuildTarget DerivedBuildTarget { get; }
        IBuildProfileExtension CreateBuildProfileExtension();
        ISettingEditorExtension CreateSettingEditorExtension();
    }
}
