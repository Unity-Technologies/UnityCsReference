// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using GraphicsDeviceType = UnityEngine.Rendering.GraphicsDeviceType;
using UnityEngine.Rendering;

namespace UnityEditor;

internal interface IGraphicsPlatformProperties : IPlatformProperties
{
    // The QualitySettingsEditor.OnInspectorGUI method uses this property to determine whether or not to display a help
    // box about ignoring the vertical sync count property.
    bool IgnoresVSyncCount => false;

    // The Modules.ModuleManager.ShouldShowMultiDisplayOption method uses this property to determine whether or not client
    // code will show a multi-display option.  There are about half a dozen such clients in the editor code base.
    bool HasMultiDisplayOption => false;

    // The TextureImporterInspector uses this property to determine whether or not the default texture importer is using ETC. 
    bool IsETCUsedAsDefaultTextureImporter(TextureCompressionFormat defaultTexCompressionFormat) => defaultTexCompressionFormat == TextureCompressionFormat.ETC;

    // The PlayerSettings uses this property to determine whether or not the graphics jobs are experimental
    bool AreGraphicsJobsExperimental => false;

    // The PlayerSettingsEditor.OtherSectionRenderingGUI method uses this property to determine whether or not to display unsupportedMSAAFallback
    bool HasUnsupportedMSAAFallback => false;
}
