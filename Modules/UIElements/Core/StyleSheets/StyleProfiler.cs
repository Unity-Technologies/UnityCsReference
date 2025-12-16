// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements.StyleSheets;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal interface IStyleProfiler
{
    void BeginMatchingStyleSheet(StyleSheet styleSheet);
    void BeginMatchingElement(VisualElement element);
    void BeginMatchingSelector(StyleComplexSelector complexSelector);
    void EndMatchingSelector(StyleComplexSelector complexSelector, bool match, bool passedAncestorFilter);
    void EndMatchingStyleSheet(StyleSheet styleSheet);
}

static class StyleProfilerStorage<TProfilerType> where TProfilerType : struct, IStyleProfiler
{
    static TProfilerType s_Instance;

    // Caution: only call this using ref InstanceByRef to avoid copying the struct
    public static ref TProfilerType InstanceByRef => ref s_Instance;
}

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
struct NoOpStyleProfiler : IStyleProfiler
{
    public void BeginMatchingStyleSheet(StyleSheet styleSheet)
    {
    }

    public void BeginMatchingElement(VisualElement element)
    {
    }

    public void BeginMatchingSelector(StyleComplexSelector complexSelector)
    {
    }

    public void EndMatchingSelector(StyleComplexSelector complexSelector, bool match, bool passedAncestorFilter)
    {
    }

    public void EndMatchingStyleSheet(StyleSheet styleSheet)
    {
    }
}
