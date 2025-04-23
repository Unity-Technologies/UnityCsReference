// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Modules
{
    internal interface IDerivedBuildTargetExtensions
    {
        ICompilationExtension CompilationExtension { get; }
        IBuildPostprocessor BuildPostprocessor { get; }
        IDerivedBuildTarget DerivedBuildTarget { get; }
        IBuildProfileExtension CreateBuildProfileExtension();
        ISettingEditorExtension CreateSettingEditorExtension();
    }
}
