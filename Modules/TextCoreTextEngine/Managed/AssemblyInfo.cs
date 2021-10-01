// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

// Allow internal visibility to the following assemblies
[assembly: InternalsVisibleTo("Unity.TextMeshPro")]
[assembly: InternalsVisibleTo("Unity.FontEngine")]
[assembly: InternalsVisibleTo("Unity.TextCore.FontEngine")]
[assembly: InternalsVisibleTo("Unity.TextCore.FontEngine.Tools")]
[assembly: InternalsVisibleTo("UnityEngine.TextCoreModule")]
[assembly: InternalsVisibleTo("UnityEngine.TextCoreTextModule")]

// Used internally for testing and a few enterprise users.
[assembly: InternalsVisibleTo("UnityEngine.TextCore.Tools")]

[assembly: InternalsVisibleTo("Unity.TextCore.Editor")]
[assembly: InternalsVisibleTo("UnityEditor.TextCoreTextModule")]
[assembly: InternalsVisibleTo("Unity.TextMeshPro.Editor")]

[assembly: InternalsVisibleTo("Unity.TextMeshPro.Tests")]
[assembly: InternalsVisibleTo("Unity.TextCore.Tests")]
[assembly: InternalsVisibleTo("Unity.FontEngine.Tests")]
[assembly: InternalsVisibleTo("Unity.UIElements.Tests")]
[assembly: InternalsVisibleTo("Unity.UIElements.PlayModeTests")]

// Make internal visible to UIElements module.
[assembly: InternalsVisibleTo("UnityEngine.UIElementsModule")]
[assembly: InternalsVisibleTo("Unity.UIElements")]
[assembly: InternalsVisibleTo("Unity.UIElements.Text")]

[assembly: InternalsVisibleTo("UnityEditor.TextCoreTextEngineModule")]
[assembly: InternalsVisibleTo("Unity.TextCore.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.TextMeshPro.Editor")]
[assembly: InternalsVisibleTo("Unity.TextMeshPro.Editor.Tests")]
