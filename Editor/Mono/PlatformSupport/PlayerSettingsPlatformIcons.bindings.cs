// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine.Bindings;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

namespace UnityEditor
{
    internal struct PlatformIconStruct
    {
        public int m_Width;
        public int m_Height;
        public int m_Kind;
        public string m_SubKind;
        public Texture2D[] m_Textures;
    }

    public class PlatformIcon
    {
        internal List<Texture2D> m_Textures;

        private PlatformIconKind m_Kind;

        private int m_MaxLayerCount;
        private int m_MinLayerCount;
        private string m_Description;

        private string m_iconSubKind;
        private int m_Width;
        private int m_Height;

        public int layerCount
        {
            get
            {
                return m_Textures.Count;
            }
            set
            {
                value = value > m_MaxLayerCount ? m_MaxLayerCount : value;
                value = value < m_MinLayerCount ? m_MinLayerCount : value;

                if (value < m_Textures.Count)
                    m_Textures.RemoveRange(value, m_Textures.Count - 1);
                else if (value > m_Textures.Count)
                    m_Textures.AddRange(new Texture2D[value - m_Textures.Count]);
            }
        }

        public int maxLayerCount  { get { return m_MaxLayerCount; } private set { m_MaxLayerCount = value; } }
        public int minLayerCount  { get { return m_MinLayerCount; } private set { m_MinLayerCount = value; } }
        internal string description  { get { return m_Description; } private set { m_Description = value; } }

        internal string iconSubKind { get { return m_iconSubKind; } private set { m_iconSubKind = value; } }
        public int width { get { return m_Width; } private set { m_Width = value; } }
        public int height { get { return m_Height; } private set { m_Height = value; } }

        public PlatformIconKind kind
        {
            get { return m_Kind; }
            private set
            {
                m_Kind = value;
                // m_KindValue = value.kind;
            }
        }
        internal PlatformIconStruct GetPlatformIconStruct()
        {
            PlatformIconStruct platformIconStruct = new PlatformIconStruct();
            platformIconStruct.m_Textures = m_Textures.ToArray();
            platformIconStruct.m_Width = m_Width;
            platformIconStruct.m_Height = m_Height;
            platformIconStruct.m_Kind = m_Kind.kind;
            platformIconStruct.m_SubKind = m_iconSubKind;

            return platformIconStruct;
        }

        internal bool IsEmpty()
        {
            return m_Textures.Count(t => t != null) == 0;
        }

        internal static PlatformIcon[] GetRequiredPlatformIconsByType(IPlatformIconProvider platformIcons, PlatformIconKind kind)
        {
            Dictionary<PlatformIconKind, PlatformIcon[]> requiredIcons = platformIcons.GetRequiredPlatformIcons();

            if (kind != PlatformIconKind.Any)
                return requiredIcons[kind];

            return requiredIcons.Values.SelectMany(i => i).ToArray();
        }

        internal PlatformIcon(int width, int height, int minLayerCount, int maxLayerCount, string iconSubKind, string description, PlatformIconKind kind)
        {
            this.width = width;
            this.height = height;
            this.iconSubKind = iconSubKind;
            this.description = description;
            this.minLayerCount = minLayerCount;
            this.maxLayerCount = maxLayerCount;
            this.kind = kind;

            m_Textures = new List<Texture2D>();
        }

        public Texture2D GetTexture(int layer = 0)
        {
            if (layer < 0 || layer >= m_MaxLayerCount)
                throw new ArgumentOutOfRangeException(string.Format("Attempting to retrieve icon layer {0}, while the icon only contains {1} layers!", layer, layerCount));
            else if (layer < layerCount)
                return m_Textures[layer];
            return null;
        }

        public Texture2D[] GetTextures()
        {
            return m_Textures.ToArray();
        }

        internal Texture2D[] GetPreviewTextures()
        {
            Texture2D[] previewTextures = new Texture2D[maxLayerCount];

            for (int i = 0; i < maxLayerCount; i++)
            {
                previewTextures[i] = PlayerSettings.GetPlatformIconAtSize(m_Kind.platform, m_Width, m_Height, m_Kind.kind, m_iconSubKind, i);
            }

            return previewTextures;
        }

        public void SetTexture(Texture2D texture, int layer = 0)
        {
            if (layer < 0 || layer >= maxLayerCount)
            {
                throw new ArgumentOutOfRangeException(string.Format("Attempting to set icon layer {0}, while icon only supports {1} layers!", layer, maxLayerCount));
            }
            else if (layer > m_Textures.Count - 1)
            {
                for (int i = m_Textures.Count; i <= layer; i++)
                    m_Textures.Add(null);
            }

            m_Textures[layer] = texture;
        }

        public void SetTextures(params Texture2D[] textures)
        {
            if (textures == null || textures.Length == 0 || textures.Count(t => t != null) == 0)
            {
                m_Textures.Clear();
                return;
            }
            else if (textures.Length > maxLayerCount || textures.Length < minLayerCount)
            {
                throw new InvalidOperationException(string.Format("Attempting to assign an incorrect amount of layers to an PlatformIcon, trying to assign {0} textures while the Icon requires atleast {1} but no more than {2} layers",
                        textures.Length,
                        minLayerCount,
                        maxLayerCount
                        )
                    );
            }

            m_Textures = textures.ToList();
        }

        public override string ToString() { return string.Format("({0}x{1}) {2}", width, height, description); }
    }

    public class PlatformIconKind
    {
        internal static readonly PlatformIconKind Any = new PlatformIconKind(-1, "Any", BuildTargetGroup.Unknown);

        internal int kind { get; private set; }
        internal string platform { get; private set; }
        internal string[] customLayerLabels { get; private set; }
        private string kindString { get; set; }

        internal PlatformIconKind(int kind, string kindString, BuildTargetGroup platform, string[] customLayerLabels = null)
        {
            this.kind = kind;
            this.platform = PlayerSettings.GetPlatformName(platform);
            this.kindString = kindString;
            this.customLayerLabels = customLayerLabels;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            return kind == ((PlatformIconKind)obj).kind;
        }

        public override int GetHashCode()
        {
            return kind.GetHashCode();
        }

        public override string ToString() { return kindString; }
    }

    internal interface IPlatformIconProvider
    {
        Dictionary<PlatformIconKind, PlatformIcon[]> GetRequiredPlatformIcons();
        PlatformIconKind GetPlatformIconKindFromEnumValue(IconKind kind);
    }


    [NativeHeader("Runtime/Misc/PlayerSettings.h")]
    public partial class PlayerSettings : UnityEngine.Object
    {
        internal static Dictionary<BuildTargetGroup, IPlatformIconProvider> platformIconProviders = new Dictionary<BuildTargetGroup, IPlatformIconProvider>();

        internal static IPlatformIconProvider GetPlatformIconProvider(BuildTargetGroup platform)
        {
            if (!platformIconProviders.ContainsKey(platform))
                return null;
            return platformIconProviders[platform];
        }

        internal static bool HasPlatformIconProvider(BuildTargetGroup platform)
        {
            return platformIconProviders.ContainsKey(platform);
        }

        internal static void RegisterPlatformIconProvider(BuildTargetGroup platform, IPlatformIconProvider platformIconProvider)
        {
            if (platformIconProviders.ContainsKey(platform))
                return;
            platformIconProviders[platform] = platformIconProvider;
        }

        // Loops through 'requiredIconSlots' and fills it with icons that are already serialized.
        public static PlatformIcon[] GetPlatformIcons(BuildTargetGroup platform, PlatformIconKind kind)
        {
            IPlatformIconProvider platformIconProvider = GetPlatformIconProvider(platform);
            if (platformIconProvider == null)
                return new PlatformIcon[] {};

            string platformName = PlayerSettings.GetPlatformName(platform);

            PlatformIconStruct[] serializedIcons  = GetPlatformIconsInternal(platformName, kind.kind);
            PlatformIcon[] icons = PlatformIcon.GetRequiredPlatformIconsByType(platformIconProvider, kind);

            if (serializedIcons.Length <= 0)
            {
                ImportLegacyIcons(platform, kind, icons);
                SetPlatformIcons(platform, kind, icons);
            }

            if (serializedIcons.Length <= 0)
            {
                foreach (var icon in icons)
                    if (icon.IsEmpty())
                        icon.SetTextures(null);
            }
            else
            {
                foreach (PlatformIcon icon in icons)
                {
                    foreach (PlatformIconStruct serializedIcon in serializedIcons)
                    {
                        int requiredKind = kind.Equals(PlatformIconKind.Any) ? serializedIcon.m_Kind : kind.kind;
                        if (icon.kind.kind == requiredKind && icon.iconSubKind == serializedIcon.m_SubKind)
                        {
                            if (icon.width == serializedIcon.m_Width && icon.height == serializedIcon.m_Height)
                            {
                                Texture2D[] serializedTextures =
                                    serializedIcon.m_Textures.Take(icon.maxLayerCount).ToArray();
                                Texture2D[] textures = new Texture2D[serializedTextures.Length > icon.minLayerCount
                                                                     ? serializedTextures.Length
                                                                     : icon.minLayerCount];

                                for (int i = 0; i < serializedTextures.Length; i++)
                                    textures[i] = serializedTextures[i];

                                icon.SetTextures(textures);
                                break;
                            }
                        }
                    }
                }
            }
            return icons;
        }

        public static void SetPlatformIcons(BuildTargetGroup platform, PlatformIconKind kind, PlatformIcon[] icons)
        {
            string platformName = GetPlatformName(platform);
            IPlatformIconProvider platformIconProvider = GetPlatformIconProvider(platform);
            if (platformIconProvider == null)
                return;

            int requiredIconCount = PlatformIcon.GetRequiredPlatformIconsByType(platformIconProvider, kind).Length;

            PlatformIconStruct[] iconStructs;
            if (icons == null)
                iconStructs = new PlatformIconStruct[0];
            else if (requiredIconCount != icons.Length)
            {
                throw new InvalidOperationException(string.Format("Attempting to set an incorrect number of icons for {0} {1} kind, it requires {2} icons but trying to assign {3}.", platform.ToString(),
                        kind.ToString(),
                        requiredIconCount,
                        icons.Length)
                    );
            }
            else
            {
                iconStructs = icons.Select(
                        i => i.GetPlatformIconStruct()
                        ).ToArray<PlatformIconStruct>();
            }

            SetPlatformIconsInternal(platformName, iconStructs, kind.kind);
        }

        public static PlatformIconKind[] GetSupportedIconKindsForPlatform(BuildTargetGroup platform)
        {
            IPlatformIconProvider platformIconProvider = GetPlatformIconProvider(platform);

            if (platformIconProvider == null)
                return new PlatformIconKind[] {};

            return platformIconProvider.GetRequiredPlatformIcons().Keys.ToArray();
        }

        internal static int GetNonEmptyPlatformIconCount(PlatformIcon[] icons)
        {
            return icons.Count(i => !i.IsEmpty());
        }

        internal static int GetValidPlatformIconCount(PlatformIcon[] icons)
        {
            return icons.Count(
                i => i.GetTextures().Count(t => t != null) >= i.minLayerCount && i.layerCount <= i.maxLayerCount
                );
        }

        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        [NativeMethod(Name = "SetIconsForPlatform")]
        extern internal static void SetPlatformIconsInternal(string platform, PlatformIconStruct[] icons, int kind);

        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        [NativeMethod(Name = "GetIconsForPlatform")]
        extern internal static PlatformIconStruct[] GetPlatformIconsInternal(string platform, int kind);

        // Get the texture that will be used as the display icon at a specified size for the specified platform.
        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        extern internal static Texture2D GetPlatformIconAtSize(string platform, int width, int height, int kind, string subKind = "", int layer = 0);

        internal static void ClearSetIconsForPlatform(BuildTargetGroup target)
        {
            SetPlatformIcons(target, PlatformIconKind.Any, null);
        }

        // Old API methods, will be made obsolete when the new API is implemented for all platforms,
        // currently it functions as a wrapper for the new API for all platforms that support it (iOS, Android & tvOS).
        public static void SetIconsForTargetGroup(BuildTargetGroup platform, Texture2D[] icons, IconKind kind)
        {
            if (platform == BuildTargetGroup.iOS || platform == BuildTargetGroup.tvOS || platform == BuildTargetGroup.Android)
            {
                IPlatformIconProvider iconProvider = GetPlatformIconProvider(platform);

                if (iconProvider == null)
                    return;

                PlatformIconKind platformIconKind = iconProvider.GetPlatformIconKindFromEnumValue(kind);

                PlatformIcon[] platformIcons = GetPlatformIcons(platform, platformIconKind);

                for (var i = 0; i < icons.Length; i++)
                    platformIcons[i].SetTexture(icons[i], 0);

                SetPlatformIcons(platform, platformIconKind, platformIcons);
            }
            else
                SetIconsForPlatform(GetPlatformName(platform), icons, kind);
        }

        // Assign a list of icons for the specified platform.
        public static void SetIconsForTargetGroup(BuildTargetGroup platform, Texture2D[] icons)
        {
            SetIconsForTargetGroup(platform, icons, IconKind.Any);
        }

        // Returns the list of assigned icons for the specified platform of a specific kind.
        public static Texture2D[] GetIconsForTargetGroup(BuildTargetGroup platform, IconKind kind)
        {
            if (platform == BuildTargetGroup.iOS || platform == BuildTargetGroup.tvOS || platform == BuildTargetGroup.Android)
            {
                IPlatformIconProvider iconProvider = GetPlatformIconProvider(platform);

                if (iconProvider == null)
                    return new Texture2D[] {};

                PlatformIconKind platformIconKind = iconProvider.GetPlatformIconKindFromEnumValue(kind);

                return GetPlatformIcons(platform, platformIconKind).Select(t => t.GetTexture(0)).ToArray();
            }
            else
            {
                Texture2D[] icons = GetIconsForPlatform(GetPlatformName(platform), kind);
                return icons;
            }
        }

        internal static void ImportLegacyIcons(string platform, PlatformIconKind kind, PlatformIcon[] platformIcons)
        {
            if (!Enum.IsDefined(typeof(IconKind), kind.kind))
                return;

            IconKind iconKind = (IconKind)kind.kind;

            Texture2D[] legacyIcons = GetIconsForPlatform(platform, iconKind);
            int[] legacyIconWidths = GetIconWidthsForPlatform(platform, iconKind);
            int[] legacyIconHeights  = GetIconHeightsForPlatform(platform, iconKind);

            for (var i = 0; i < legacyIcons.Length; i++)
            {
                List<PlatformIcon> selectedIcons = new List<PlatformIcon>();
                foreach (var icon in platformIcons)
                {
                    if (icon.width == legacyIconWidths[i] && icon.height == legacyIconHeights[i])
                    {
                        selectedIcons.Add(icon);
                    }
                }
                foreach (var selectedIcon in selectedIcons)
                    selectedIcon.SetTextures(legacyIcons[i]);
            }
        }

        internal static void ImportLegacyIcons(BuildTargetGroup platform, PlatformIconKind kind, PlatformIcon[] platformIcons)
        {
            ImportLegacyIcons(GetPlatformName(platform), kind, platformIcons);
        }

        // Returns the list of assigned icons for the specified platform.
        public static Texture2D[] GetIconsForTargetGroup(BuildTargetGroup platform)
        {
            return GetIconsForTargetGroup(platform, IconKind.Any);
        }

        // Returns a list of icon sizes for the specified platform of a specific kind.
        public static int[] GetIconSizesForTargetGroup(BuildTargetGroup platform, IconKind kind)
        {
            if (platform == BuildTargetGroup.iOS || platform == BuildTargetGroup.tvOS || platform == BuildTargetGroup.Android)
            {
                IPlatformIconProvider iconProvider = GetPlatformIconProvider(platform);

                if (iconProvider == null)
                    return new int[] {};

                PlatformIconKind platformIconKind = iconProvider.GetPlatformIconKindFromEnumValue(kind);

                return GetPlatformIcons(platform, platformIconKind).Select(s => s.width).ToArray();
            }
            else
                return GetIconWidthsForPlatform(GetPlatformName(platform), kind);
        }

        // Returns a list of icon sizes for the specified platform.
        public static int[] GetIconSizesForTargetGroup(BuildTargetGroup platform)
        {
            return GetIconSizesForTargetGroup(platform, IconKind.Any);
        }
    }
}
