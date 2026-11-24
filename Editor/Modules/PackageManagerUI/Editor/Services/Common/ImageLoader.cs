// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IImageLoader : IService
    {
        void LoadImageAsync(ImageSource source, int maxWidth, int maxHeight, Action<Texture2D> onLoaded);
    }

    internal readonly struct ImageSource
    {
        public string path { get; }
        public bool isLocal { get; }

        private ImageSource(string path, bool isLocal)
        {
            this.path = path;
            this.isLocal = isLocal;
        }

        public string uri => isLocal ? ImageLoader.GetFileUri(path) : path;

        public static ImageSource FromUrl(string url) => new ImageSource(url, false);
        public static ImageSource FromFilePath(string filePath) => new ImageSource(filePath, true);
    }

    internal class ImageLoader : BaseService<IImageLoader>, IImageLoader
    {
        private readonly HashSet<string> m_PendingWrites = new();

        private readonly IInternalEditorUtilityProxy m_InternalEditorUtilityProxy;
        private readonly IIOProxy m_IOProxy;
        private readonly IHttpClientFactory m_HttpClientFactory;
        private readonly IImageProcessorProxy m_ImageProcessor;

        public ImageLoader(IInternalEditorUtilityProxy internalEditorUtilityProxy,
            IIOProxy ioProxy,
            IHttpClientFactory httpClientFactory,
            IImageProcessorProxy imageProcessor)
        {
            m_InternalEditorUtilityProxy = RegisterDependency(internalEditorUtilityProxy);
            m_IOProxy = RegisterDependency(ioProxy);
            m_HttpClientFactory = RegisterDependency(httpClientFactory);
            m_ImageProcessor = RegisterDependency(imageProcessor);
        }

        private string m_CacheRoot;
        private string cacheRoot => m_CacheRoot ??= IOUtils.PathsCombine(m_InternalEditorUtilityProxy.userAppDataFolder, "Package Manager", "Cache", "Images");

        // We put the max size in the key so that callers asking for different max sizes get separate entries, and old entries are ignored if a caller's max size changes
        private string GetCachePath(string key, int maxWidth, int maxHeight) => IOUtils.PathsCombine(cacheRoot, Hash128.Compute($"{key}|{maxWidth}x{maxHeight}").ToString());

        // The internal modifier is used (instead of private) to give our test project access to these properties/methods
        internal string GetRemoteCachePath(string url, int maxWidth, int maxHeight) => GetCachePath(url, maxWidth, maxHeight);

        // The internal modifier is used (instead of private) to give our test project access to these properties/methods
        internal string GetLocalCachePath(string filePath, int maxWidth, int maxHeight) => GetCachePath($"{m_IOProxy.GetLogicalPath(filePath)}|{m_IOProxy.GetFileSize(filePath)}|{m_IOProxy.GetLastWriteTimeUtc(filePath).Ticks}", maxWidth, maxHeight);

        // The internal modifier is used (instead of private) to give our test project access to these properties/methods
        internal static string GetFileUri(string filePath) => new Uri(Path.GetFullPath(filePath)).AbsoluteUri;

        public void LoadImageAsync(ImageSource source, int maxWidth, int maxHeight, Action<Texture2D> onLoaded)
        {
            if (string.IsNullOrEmpty(source.path) || (source.isLocal && !m_IOProxy.FileExists(source.path)))
            {
                onLoaded?.Invoke(null);
                return;
            }

            var cachePath = source.isLocal ? GetLocalCachePath(source.path, maxWidth, maxHeight) : GetRemoteCachePath(source.path, maxWidth, maxHeight);
            LoadFromCacheOrOriginal(cachePath, source, maxWidth, maxHeight, onLoaded);
        }

        // The internal modifier is used (instead of private) to give our test project access to these properties/methods
        internal static bool ShouldShrinkImage(int width, int height, int maxWidth, int maxHeight, out int shrunkWidth, out int shrunkHeight)
        {
            var ratio = Mathf.Min((float)maxWidth / width, (float)maxHeight / height);
            if (ratio >= 1f)
            {
                shrunkWidth = width;
                shrunkHeight = height;
                return false;
            }

            shrunkWidth = Mathf.Max(1, Mathf.RoundToInt(width * ratio));
            shrunkHeight = Mathf.Max(1, Mathf.RoundToInt(height * ratio));
            return true;
        }

        private void LoadFromCacheOrOriginal(string cachePath, ImageSource original, int maxWidth, int maxHeight, Action<Texture2D> onLoaded)
        {
            if (!m_IOProxy.FileExists(cachePath))
            {
                LoadOriginalAndUpdateCache(cachePath, original, maxWidth, maxHeight, onLoaded);
                return;
            }

            if (m_IOProxy.GetFileSize(cachePath) == 0)
            {
                // When a local image already fits, we write an empty marker instead of a copy because the original is already on disk.
                // An empty marker means we load the original directly.
                if (original.isLocal)
                    m_HttpClientFactory.LoadTextureAsync(original.uri, (texture, _) => onLoaded?.Invoke(PrepareTexture(texture)));
                else
                    LoadOriginalAndUpdateCache(cachePath, original, maxWidth, maxHeight, onLoaded);
                return;
            }

            m_HttpClientFactory.LoadTextureAsync(GetFileUri(cachePath), (texture, _) =>
            {
                if (texture != null)
                {
                    onLoaded?.Invoke(PrepareTexture(texture));
                    return;
                }

                try
                {
                    TryDeleteCacheFile(cachePath);
                    LoadOriginalAndUpdateCache(cachePath, original, maxWidth, maxHeight, onLoaded);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Package Manager Window] Unable to load image: {e}");
                    onLoaded?.Invoke(null);
                }
            });
        }

        private void LoadOriginalAndUpdateCache(string cachePath, ImageSource original, int maxWidth, int maxHeight, Action<Texture2D> onLoaded)
        {
            m_HttpClientFactory.LoadTextureAsync(original.uri, (texture, originalBytes) =>
            {
                Texture2D result = null;
                if (texture != null)
                {
                    try
                    {
                        result = FitToMaxSizeAndCache(cachePath, original, maxWidth, maxHeight, texture, originalBytes);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Package Manager Window] Unable to process image: {e}");
                        DestroyTexture(texture);
                    }
                }

                // We call onLoaded outside the try block so that an exception thrown by the caller does not make us call it a second time
                onLoaded?.Invoke(PrepareTexture(result));
            });
        }

        private Texture2D FitToMaxSizeAndCache(string cachePath, ImageSource original, int maxWidth, int maxHeight, Texture2D texture, byte[] originalBytes)
        {
            if (!ShouldShrinkImage(texture.width, texture.height, maxWidth, maxHeight, out var shrunkWidth, out var shrunkHeight))
            {
                var bytesToCache = original.isLocal ? Array.Empty<byte>() : originalBytes;
                if (bytesToCache != null)
                    WriteEntry(cachePath, bytesToCache);
                return texture;
            }

            var keepAlpha = GraphicsFormatUtility.HasAlphaChannel(texture.format);
            var shrunk = m_ImageProcessor.Resize(texture, shrunkWidth, shrunkHeight);
            if (shrunk == null)
                return texture;

            UnityEngine.Object.DestroyImmediate(texture);

            // We request the encode before handing the texture over so that the caller can safely destroy it right away
            RequestEncodeAndWriteEntry(cachePath, shrunk, keepAlpha);
            return shrunk;
        }

        // We swallow failures here so that an image that cannot be cached is still displayed
        private void RequestEncodeAndWriteEntry(string cachePath, Texture2D texture, bool keepAlpha)
        {
            if (!m_PendingWrites.Add(cachePath))
                return;

            try
            {
                m_ImageProcessor.EncodeAsync(texture, keepAlpha, encodedBytes =>
                {
                    m_PendingWrites.Remove(cachePath);
                    if (encodedBytes != null && encodedBytes.Length > 0)
                        WriteEntry(cachePath, encodedBytes);
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[Package Manager Window] Unable to save image: {e}");
                m_PendingWrites.Remove(cachePath);
            }
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture != null)
                UnityEngine.Object.DestroyImmediate(texture);
        }

        private static Texture2D PrepareTexture(Texture2D texture)
        {
            if (texture != null)
                texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        private void WriteEntry(string cachePath, byte[] bytes)
        {
            // We write to a temp file and then move it so that other Editors sharing this cache, or a crash mid-write, never leave a truncated entry that reads as a marker or a broken image
            var tempPath = $"{cachePath}.{Guid.NewGuid():N}.tmp";
            try
            {
                var directory = IOUtils.GetParentDirectory(cachePath);
                if (!m_IOProxy.DirectoryExists(directory))
                    m_IOProxy.CreateDirectory(directory);

                m_IOProxy.FileWriteAllBytes(tempPath, bytes);
                m_IOProxy.DeleteIfExists(cachePath);
                m_IOProxy.Move(tempPath, cachePath);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.Log($"[Package Manager Window] Unable to save image: {e.Message}");
                TryDeleteCacheFile(tempPath);
            }
        }

        private void TryDeleteCacheFile(string path)
        {
            try
            {
                m_IOProxy.DeleteIfExists(path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.Log($"[Package Manager Window] Unable to delete image: {e.Message}");
            }
        }
    }
}
