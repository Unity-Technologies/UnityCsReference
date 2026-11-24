// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IImageProcessorProxy : IService
    {
        Texture2D Resize(Texture2D source, int width, int height);
        void EncodeAsync(Texture2D texture, bool keepAlpha, Action<byte[]> onEncoded);
    }

    [ExcludeFromCodeCoverage]
    internal class ImageProcessorProxy : BaseService<IImageProcessorProxy>, IImageProcessorProxy
    {
        private const int k_JpgQuality = 90;

        public Texture2D Resize(Texture2D source, int width, int height)
        {
            // We check this first because Graphics.ConvertTexture logs an error on devices that cannot copy a render texture into a texture.
            // Returning null makes the caller show the original image instead.
            if ((SystemInfo.copyTextureSupport & CopyTextureSupport.RTToTexture) == 0)
                return null;

            // Bilinear filtering samples only the 2x2 source pixels nearest to each output pixel, so a single blit that shrinks by more than half ignores most of the source and looks jagged and noisy.
            // We halve step by step so that every source pixel contributes to the result.
            var current = source;
            while (current.width > width || current.height > height)
            {
                var next = new Texture2D(Mathf.Max(width, current.width / 2), Mathf.Max(height, current.height / 2), TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                // ConvertTexture only writes the GPU copy, so drop the CPU copy instead of keeping stale pixels around
                next.Apply(false, true);

                var converted = Graphics.ConvertTexture(current, next);
                if (current != source)
                    UnityEngine.Object.DestroyImmediate(current);
                if (!converted)
                {
                    UnityEngine.Object.DestroyImmediate(next);
                    return null;
                }
                current = next;
            }
            return current == source ? null : current;
        }

        public void EncodeAsync(Texture2D texture, bool keepAlpha, Action<byte[]> onEncoded)
        {
            // Without async readback we skip caching rather than stall the main thread on a synchronous readback.
            // The image is still shown, it is just shrunk again the next time it is loaded.
            if (!SystemInfo.supportsAsyncGPUReadback)
            {
                onEncoded?.Invoke(null);
                return;
            }

            var width = (uint)texture.width;
            var height = (uint)texture.height;
            var format = texture.graphicsFormat;
            AsyncGPUReadback.Request(texture, 0, request =>
            {
                if (request.hasError)
                {
                    onEncoded?.Invoke(null);
                    return;
                }

                var pixels = request.GetData<byte>().ToArray();
                RunOnWorkerThread(() => keepAlpha
                    ? ImageConversion.EncodeArrayToPNG(pixels, format, width, height)
                    : ImageConversion.EncodeArrayToJPG(pixels, format, width, height, 0, k_JpgQuality), onEncoded);
            });
        }

        private static void RunOnWorkerThread<T>(Func<T> work, Action<T> onDone)
        {
            var mainThreadContext = SynchronizationContext.Current;
            Task.Run(() =>
            {
                var result = default(T);
                try
                {
                    result = work();
                }
                catch (Exception e)
                {
                    Debug.Log($"[Package Manager Window] Unable to process image: {e.Message}");
                }

                mainThreadContext.Post(_ => onDone?.Invoke(result), null);
            });
        }
    }
}
