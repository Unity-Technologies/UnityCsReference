// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

// Allow internal visibility for testing purposes.
[assembly: InternalsVisibleTo("Unity.TextCore")]
[assembly: InternalsVisibleTo("UnityEngine.TextCoreTextModule")]
[assembly: InternalsVisibleTo("Unity.TextCore.Editor")]
[assembly: InternalsVisibleTo("Unity.TextMeshPro")]
[assembly: InternalsVisibleTo("Unity.TextMeshPro.Editor")]

// Make internal visible to UIElements module.
//[assembly: InternalsVisibleTo("UnityEngine.UIElementsModule")]

// Make internal visible to various tests.
[assembly: InternalsVisibleTo("Unity.FontEngine.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.TextCore.Editor.Tests")]
