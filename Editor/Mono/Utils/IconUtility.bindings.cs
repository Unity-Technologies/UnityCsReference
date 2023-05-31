// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Utils
{
    [Flags]
    enum ImageFlags
    {
        NoFlags = 0,
        WarnIfCompressed = 1 << 0,
        WarnIfScalingUp = 1 << 1,
        UseAlpha = 1 << 2,
        UseIndexed = 1 << 3,
        ClearAlpha = 1 << 4
    }

    struct ImageDescription
    {
        public string description;
        public string destFileName;
        public string subKind;
        public int width;  // zero means that the source image should not be resized
        public int height;
        public ImageFlags flags;
        public int kind;
        public bool hasLayers;

        public ImageDescription(string desc, string fileName, int width, int height, int kind, bool hasLayers, string subkind, ImageFlags flags = ImageFlags.NoFlags)
        {
            this.description = desc;
            this.destFileName = fileName;
            this.width = width;
            this.height = height;
            this.flags = flags;
            this.kind = kind;
            this.hasLayers = hasLayers;
            this.subKind = subkind;
        }

        public ImageDescription(string desc, string fileName, int width, int height, int kind, string subkind, ImageFlags flags = ImageFlags.NoFlags)
            : this(desc, fileName, width, height, kind, false, subkind, flags)
        {
        }

        public ImageDescription(string desc, string fileName, int width, int height, ImageFlags flags = ImageFlags.NoFlags)
            : this(desc, fileName, width, height, 0, "", flags)
        {
        }

        public ImageDescription(string desc, string fileName, ImageFlags flags = ImageFlags.NoFlags)
            : this(desc, fileName, 0, 0, flags)
        {
        }
    }

    [NativeHeader("Editor/Src/Commands/IconUtility.h")]
    static class IconUtility
    {
        [FreeFunction]
        extern public static bool AddIconToWindowsExecutable(string path);

        [FreeFunction]
        extern public static bool SaveIcoForPlatform(string path, BuildTargetGroup buildTargetGroup, Vector2Int[] iconSizes);

        [FreeFunction]
        extern public static void SaveTextureToFile(string path, Texture2D texture, uint fileType);

        [FreeFunction]
        extern static void ExportTextureToImageFile(Texture2D texture, int resx, int resy, string path,
            bool use_alpha, bool clear_alpha, bool use_indexed);

        static void ExportImageToPath(string imagePath, Texture2D texture, ImageDescription desc)
        {
            if (texture == null)
                return;
            if (desc.flags.HasFlag(ImageFlags.WarnIfCompressed))
            {
                if (GraphicsFormatUtility.IsCompressedFormat(texture.format))
                    Debug.LogWarning($"Compressed texture {texture.name} is used as {desc.description}. This might compromise visual quality of the final image. Uncompressed format might be considered as better import option.");
            }

            bool useAlpha = desc.flags.HasFlag(ImageFlags.UseAlpha);
            bool clearAlpha = desc.flags.HasFlag(ImageFlags.ClearAlpha);
            bool useIndexed = desc.flags.HasFlag(ImageFlags.UseIndexed);

            var dstWidth = desc.width;
            var dstHeight = desc.height;

            if (dstWidth != 0 && dstHeight != 0)
            {
                if (desc.flags.HasFlag(ImageFlags.WarnIfScalingUp))
                {
                    int srcresx = texture.width;
                    int srcresy = texture.height;
                    if (srcresx < desc.width || srcresy < desc.height)
                    {
                        Debug.LogWarning($"Texture {texture.name} ({desc.description}) has resolution ({srcresx} x {srcresy}) lower than expected ({desc.width} x {desc.height}). "
                            + "This might compromise visual quality of the final image. "
                            + "Please consider using a native size texture and appropriate import options (NPOT)."
                        );
                    }
                }
            }
            else
            {
                dstWidth = texture.width;
                dstHeight = texture.height;
            }

            ExportTextureToImageFile(texture, dstWidth, dstHeight, imagePath, useAlpha, clearAlpha, useIndexed);
        }

        public static void ExportImage(string basePath, Texture2D texture, ImageDescription desc)
        {
            ExportImageToPath(Path.Combine(basePath, desc.destFileName), texture, desc);
        }

        // Same as ExportImage, just that destFileName is a template string, into which
        // index is inserted. destFileName must contain "{0}", which identifies the
        // place for insertion
        static void ExportImageNumbered(string basePath, Texture2D texture, ImageDescription desc, uint number)
        {
            if (!desc.destFileName.Contains("{0}"))
                Debug.LogError($"The icon {desc.destFileName} can't be used with ExportImageNumbered");

            string destFileName = string.Format(desc.destFileName, number);
            ExportImageToPath(Path.Combine(basePath, destFileName), texture, desc);
        }

        public static void ExportIcon(string basePath, string platform, ImageDescription desc, int layerCount = 0)
        {
            if (!desc.hasLayers)
                layerCount = -1;
            Texture2D[] textures = PlayerSettings.GetIconsForPlatformByKind(layerCount, platform, desc.kind, desc.subKind, desc.width, desc.height);
            if (desc.hasLayers == true)
            {
                for (uint i = 0; i < textures.Length; ++i)
                    ExportImageNumbered(basePath, textures[i], desc, i);
            }
            else
            {
                ExportImage(basePath, textures[0], desc);
            }
        }

        [FreeFunction]
        public static extern bool WriteOSXIcon(string path);
    }
}
