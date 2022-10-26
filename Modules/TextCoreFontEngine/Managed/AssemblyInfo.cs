// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

// Allow internal visibility for testing purposes.
[assembly: InternalsVisibleTo("Unity.TextCore")]
[assembly: InternalsVisibleTo("Unity.TextMeshPro")]
[assembly: InternalsVisibleTo("Unity.TextCore.FontEngine")]
[assembly: InternalsVisibleTo("Unity.TextCore.FontEngine.Tools")]
[assembly: InternalsVisibleTo("UnityEngine.TextCoreTextEngineModule")]

[assembly: InternalsVisibleTo("Unity.TextCore.Editor")]
[assembly: InternalsVisibleTo("Unity.TextMeshPro.Editor")]
[assembly: InternalsVisibleTo("Unity.TextMeshPro.Editor.Tests")]
[assembly: InternalsVisibleTo("UnityEditor.TextCoreTextEngineModule")]

// Make internal visible to various tests.
[assembly: InternalsVisibleTo("Unity.FontEngine.Tests")]
[assembly: InternalsVisibleTo("Unity.TextCore.Tests")]
[assembly: InternalsVisibleTo("Unity.TextCore.FontEngine.Tests")]
[assembly: InternalsVisibleTo("Unity.FontEngine.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.TextCore.Editor.Tests")]
