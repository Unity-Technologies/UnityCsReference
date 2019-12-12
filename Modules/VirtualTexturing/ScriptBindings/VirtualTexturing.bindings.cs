// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Experimental.Rendering
{
    [NativeHeader("Modules/VirtualTexturing/Public/VirtualTexturingDebugHandle.h")]
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    public struct VirtualTexturingDebugHandle
    {
        public long handle; //Handle number as exposed outside of module
        public string group; //Group of this handle (currently tile set)
        public string name; //Name of this handle
        public int numLayers; //Number of layers
        public Material material; //Material to initialize with gpu data. If null this is skipped.
    }

    [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
    [StaticAccessor("VirtualTexturing", StaticAccessorType.DoubleColon)]
    public static class VirtualTexturing
    {
        extern public static bool debugTilesEnabled { get; set; }
        extern public static bool resolvingEnabled { get; set; }

        extern public static void UpdateSystem();

        public const int AllMips = int.MaxValue;
        extern public static void RequestRegion(Material mat, int stackNameId, Rect r, int mipMap, int numMips);

        extern public static string[] GetTexturesInTileset(string tileSetPathName);

        [NativeConditional("UNITY_EDITOR")]
        extern public static bool ValidateTextureStack(Texture[] textures, out string errorMessage);
    }

    [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
    [StaticAccessor("VirtualTexturingDebugging", StaticAccessorType.DoubleColon)]
    public static class VirtualTexturingDebugging
    {
        extern public static int GetNumHandles();
        extern public static void GrabHandleInfo([Out] out VirtualTexturingDebugHandle debugHandle, int index);
        extern public static string GetInfoDump();
    }

    [NativeHeader("Modules/VirtualTexturing/Public/VirtualTextureResolver.h")]
    [StructLayout(LayoutKind.Sequential)]
    public class VirtualTextureResolver : IDisposable
    {
        internal IntPtr m_Ptr;

        public VirtualTextureResolver()
        {
            if (SystemInfo.supportsAsyncGPUReadback == false)
            {
                Debug.LogError("VirtualTextureResolver requires async gpu readback");
            }
            m_Ptr = InitNative();
        }

        ~VirtualTextureResolver()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // we don't have any managed references, so 'disposing' part of
            // standard IDisposable pattern does not apply

            // Release native resources
            if (m_Ptr != IntPtr.Zero)
            {
                Flush_Internal();
                ReleaseNative(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        private static extern IntPtr InitNative();

        [NativeMethod(IsThreadSafe = true)]
        private static extern void ReleaseNative(IntPtr ptr);

        extern void Flush_Internal();
        extern  void Init_Internal(int width, int height);

        public uint CurrentWidth { get; private set; } = 0;
        public uint CurrentHeight { get; private set; } = 0;

        public void Init(uint width, uint height)
        {
            // When in the editor, it is possible that we have both the game and editor view rendering at the same time
            // When they are different resolutions, we rescale the resolver twice each frame. This is obviously not good.
            // As the SRP seems to be shared between editor/game mode, just use 'worst' case resolution
            if (CurrentWidth < width || CurrentHeight < height)
            {
                Flush_Internal();

                CurrentWidth = width;
                CurrentHeight = height;

                Init_Internal((int)width, (int)height);
            }
        }

        public void Process(CommandBuffer cmd, RenderTargetIdentifier rt)
        {
            Process(cmd, rt, 0, CurrentWidth, 0, CurrentHeight, 0, 0);
        }

        public void Process(CommandBuffer cmd, RenderTargetIdentifier rt, uint x, uint width, uint y, uint height, uint mip, uint slice)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException("cmd");
            }
            cmd.ProcessVTFeedback(rt, m_Ptr, (int)slice, (int)x, (int)width, (int)y, (int)height, (int)mip);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Interfaces/IVirtualTexturingManager.h")]
    public struct ProceduralTextureStackCreationParams
    {
        public const int MaxNumLayers = 4;
        public const int MaxRequestsPerFrameSupported = 0x0fff;

        public uint width, height;
        public uint maxRequestsPerFrame;
        public uint tilesize;
        public TextureFormat[] layers;
        internal uint borderSize;

        internal void Validate()
        {
            if (width == 0 || height == 0 || tilesize == 0)
            {
                throw new ArgumentException($"Zero sized dimensions are invalid (width: {width}, height: {height}, tilesize {tilesize}");
            }
            if (layers == null || layers.Length > MaxNumLayers)
            {
                throw new ArgumentException($"layers is either invalid or has to many layers (maxNumLayers: {MaxNumLayers})");
            }
            TextureFormat[] supportedFormats = { TextureFormat.RGBA32, TextureFormat.RGBAFloat, TextureFormat.RG16 };
            for (int i = 0; i < layers.Length; ++i)
            {
                bool valid = false;
                for (int j = 0; j < supportedFormats.Length; ++j)
                {
                    if (layers[i] == supportedFormats[j])
                    {
                        valid = true;
                        break;
                    }
                }
                if (valid == false)
                {
                    throw new ArgumentException($"Invalid textureformat on layer: {i}. Supported formats are: {supportedFormats}");
                }
            }
            if (maxRequestsPerFrame > MaxRequestsPerFrameSupported || maxRequestsPerFrame == 0)
            {
                throw new ArgumentException($"Invalid requests per frame (MaxRequestsPerFrameSupported: ]0, {MaxRequestsPerFrameSupported}])");
            }
        }
    }

    [NativeHeader("Modules/VirtualTexturing/ScriptBindings/VirtualTexturing.bindings.h")]
    [StaticAccessor("VirtualTexturing::Procedural", StaticAccessorType.DoubleColon)]
    internal static class ProceduralVirtualTexturing
    {
        extern internal static ulong Create(ProceduralTextureStackCreationParams p);
        extern internal static void Destroy(ulong handle);

        extern internal static int GetActiveRequests(ulong handle, IntPtr requests);
        extern internal static void MarkRequestsFinished(ulong handle, IntPtr requests, int count);

        extern internal static void BindToMaterialPropertyBlock(ulong handle, MaterialPropertyBlock material, string name);
        extern internal static void BindToMaterial(ulong handle, Material material, string name);

        extern public static void RequestRegion(ulong handle, Rect r, int mipMap, int numMips);
        extern public static void InvalidateRegion(ulong handle, Rect r, int mipMap, int numMips);
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct ProceduralTextureStackRequestLayer /// KEEP IN SYNC WITH IVirtualTexturingManager.h
    {
        public int destX, destY;
        public int enabled;
        public RenderTargetIdentifier dest;
    }

    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    public struct ProceduralTextureStackRequest /// KEEP IN SYNC WITH IVirtualTexturingManager.h
    {
        internal int id;

        public int level;
        public int x, y;
        public int width, height;
        public int numLayers;

        ProceduralTextureStackRequestLayer layer0;
        ProceduralTextureStackRequestLayer layer1;
        ProceduralTextureStackRequestLayer layer2;
        ProceduralTextureStackRequestLayer layer3;
        public ProceduralTextureStackRequestLayer GetLayer(int index)
        {
            switch (index)
            {
                case 0:
                    return layer0;
                case 1:
                    return layer1;
                case 2:
                    return layer2;
                case 3:
                    return layer3;
            }
            throw new IndexOutOfRangeException();
        }
    }

    public class ProceduralRequestList : IDisposable
    {
        NativeArray<ProceduralTextureStackRequest> requests;
        ProceduralTextureStack owner;

        internal ProceduralRequestList(ProceduralTextureStackCreationParams creationParams, ProceduralTextureStack _owner)
        {
            requests = new NativeArray<ProceduralTextureStackRequest>((int)creationParams.maxRequestsPerFrame, Allocator.Persistent);
            owner = _owner;
        }

        public void Dispose()
        {
            requests.Dispose();
        }

        public int Length { get; private set; } = 0;
        public ProceduralTextureStackRequest this[int index]
        {
            get
            {
                if (index >= Length)
                {
                    throw new IndexOutOfRangeException();
                }
                return requests[index];
            }
        }

        unsafe public void MarkAsFinished(int count = -1)
        {
            int actualCount = count == -1 ? Length : Math.Min(count, Length);
            ProceduralVirtualTexturing.MarkRequestsFinished(owner.handle, (IntPtr)requests.GetUnsafeReadOnlyPtr(), actualCount);
            Length = 0;
        }

        unsafe internal void Sync()
        {
            Length = ProceduralVirtualTexturing.GetActiveRequests(owner.handle, (IntPtr)requests.GetUnsafePtr());
        }
    }

    public sealed class ProceduralTextureStack : IDisposable
    {
        ProceduralRequestList requests;

        public ProceduralRequestList GetActiveRequests()
        {
            requests.Sync();
            return requests;
        }

        internal readonly ulong handle;
        public readonly static uint borderSize = 8;

        string name;

        public ProceduralTextureStack(string _name, ProceduralTextureStackCreationParams creationParams)
        {
            name = _name;
            creationParams.borderSize = borderSize;
            creationParams.Validate();
            requests = new ProceduralRequestList(creationParams, this);
            handle = ProceduralVirtualTexturing.Create(creationParams);
        }

        public void Dispose()
        {
            requests.Dispose();
            ProceduralVirtualTexturing.Destroy(handle);
        }

        public void BindToMaterialPropertyBlock(MaterialPropertyBlock mpb)
        {
            if (mpb == null)
            {
                throw new ArgumentNullException("mbp");
            }
            ProceduralVirtualTexturing.BindToMaterialPropertyBlock(handle, mpb, name);
        }

        public void BindToMaterial(Material mat)
        {
            if (mat == null)
            {
                throw new ArgumentNullException("mat");
            }
            ProceduralVirtualTexturing.BindToMaterial(handle, mat, name);
        }

        public const int AllMips = int.MaxValue;

        public void RequestRegion(Rect r, int mipMap, int numMips)
        {
            ProceduralVirtualTexturing.RequestRegion(handle, r, mipMap, numMips);
        }

        public void InvalidateRegion(Rect r, int mipMap, int numMips)
        {
            ProceduralVirtualTexturing.InvalidateRegion(handle, r, mipMap, numMips);
        }
    }

    [StaticAccessor("VirtualTexturing::Procedural", StaticAccessorType.DoubleColon)]
    public static class ProceduralTextureStackRequestLayerUtil
    {
        public extern static int GetWidth(this ProceduralTextureStackRequestLayer layer);
        public extern static int GetHeight(this ProceduralTextureStackRequestLayer layer);
    }
}
