// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

// Allow internal visibility to other assemblies
[assembly: InternalsVisibleTo("Unity.TextCore")]
[assembly: InternalsVisibleTo("Unity.TextMeshPro")]
[assembly: InternalsVisibleTo("Unity.TextCore.FontEngine")]
[assembly: InternalsVisibleTo("Unity.TextCore.FontEngine.Tools")]
[assembly: InternalsVisibleTo("UnityEngine.TextCoreTextEngineModule")]
[assembly: InternalsVisibleTo("UnityEditor.TextCoreTextEngineModule")]
[assembly: InternalsVisibleTo("Unity.TextCore.Editor")]
[assembly: InternalsVisibleTo("Unity.TextMeshPro.Editor")]

// Make internal visible for testing
[assembly: InternalsVisibleTo("Unity.FontEngine.Tests")]
[assembly: InternalsVisibleTo("Unity.TextCore.Tests")]
[assembly: InternalsVisibleTo("Unity.TextCore.FontEngine.Tests")]
[assembly: InternalsVisibleTo("Unity.FontEngine.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.TextCore.Editor.Tests")]

// Make internal visible to UIElements module.
[assembly: InternalsVisibleTo("UnityEngine.UIElementsModule")]
[assembly: InternalsVisibleTo("Unity.UIElements")]
[assembly: InternalsVisibleTo("Unity.UIElements.Tests")]
[assembly: InternalsVisibleTo("Unity.UIElements.Editor")]
[assembly: InternalsVisibleTo("Unity.UIElements.EditorTests")]
