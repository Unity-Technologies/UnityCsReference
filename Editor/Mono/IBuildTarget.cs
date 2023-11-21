// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal interface IBuildTarget
    {
        // The DesktopPluginImporterExtension.OnPlatformSettingsGUI method and the PlayerSettingsEditor.GraphicsAPIsGUI method
        // use this property to determine what name to display for a build target.  This is often the same as the TargetName
        // property but is different for several targets, such as "Embedded Linux" instead of "EmbeddedLinux".
        string DisplayName { get; }

        // The BuildTargetConverter.TryConvertToRuntimePlatform method uses this property to convert a build target into a
        // runtime platform.  This method is used only for testing.
        RuntimePlatform RuntimePlatform { get; }

        // This property contains the canonical name of the build target.  Many methods throughout the editor code base make
        // use of this property.
        string TargetName { get; }

        // This property is used as a suffix for il2cpp profiles. Should a more suited place for it exist, move it
        string RootSystemType { get; }

        // This function allows to retrieve BuildPlatformProperties derived from IPlatformProperties as quick access
        IBuildPlatformProperties BuildPlatformProperties { get; }

        // This function allows to retrieve GraphicsPlatformProperties derived from IPlatformProperties as quick access
        IGraphicsPlatformProperties GraphicsPlatformProperties { get; }

        // This function allows to retrieve PlayerConnectionPlatformProperties derived from IPlatformProperties as quick access
        IPlayerConnectionPlatformProperties PlayerConnectionPlatformProperties { get; }

        // This function allows to retrieve IconPlatformProperties derived from IPlatformProperties as quick access
        IIconPlatformProperties IconPlatformProperties { get; }

        // This function allows to retrieve UIPlatformProperties derived from IPlatformProperties as quick access
        IUIPlatformProperties UIPlatformProperties { get; }

        // This function allows to retrieve AudioPlatformProperties derived from IPlatformProperties as quick access
        IAudioPlatformProperties AudioPlatformProperties { get; }

        // This function allows to retrieve AudioPlatformProperties derived from IPlatformProperties as quick access
        IVRPlatformProperties VRPlatformProperties { get; }

        // This function allows to retrieve properties of a give type, derived from IPlatformProperties
        // if they are available.
        bool TryGetProperties<T>(out T properties) where T: IPlatformProperties;

        // This functions allows to access the old BuildTarget enum value for a build target when instantiated.
        int GetLegacyId { get; }
    }
}
