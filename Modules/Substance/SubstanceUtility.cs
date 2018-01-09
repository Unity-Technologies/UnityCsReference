// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

/*
using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;
*/
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;

namespace UnityEngine
{
    [Obsolete("ProceduralProcessorUsage is deprecated and no longer available.", true)]
    public enum ProceduralProcessorUsage
    {
        Unsupported = 0,
        One = 1,
        Half = 2,
        All = 3
    }

    [Obsolete("ProceduralCacheSize is deprecated and no longer available.", true)]
    public enum ProceduralCacheSize
    {
        Tiny = 0,
        Medium = 1,
        Heavy = 2,
        NoLimit = 3,
        None = 4
    }

    [Obsolete("ProceduralLoadingBehavior is deprecated and no longer available.", true)]
    public enum ProceduralLoadingBehavior
    {
        DoNothing = 0,
        Generate = 1,
        BakeAndKeep = 2,
        BakeAndDiscard = 3,
        Cache = 4,
        DoNothingAndCache = 5
    }

    [Obsolete("ProceduralPropertyType is deprecated and no longer available.", true)]
    public enum ProceduralPropertyType
    {
        Boolean = 0,
        Float = 1,
        Vector2 = 2,
        Vector3 = 3,
        Vector4 = 4,
        Color3 = 5,
        Color4 = 6,
        Enum = 7,
        Texture = 8,
        String = 9
    }

    [Obsolete("ProceduralOutputType is deprecated and no longer available.", true)]
    public enum ProceduralOutputType
    {
        Unknown = 0,
        Diffuse = 1,
        Normal = 2,
        Height = 3,
        Emissive = 4,
        Specular = 5,
        Opacity = 6,
        Smoothness = 7,
        AmbientOcclusion = 8,
        DetailMask = 9,
        Metallic = 10,
        Roughness = 11
    }

    [StructLayout(LayoutKind.Sequential)]
    [Obsolete("ProceduralPropertyDescription is deprecated and no longer available.", true)]
    public sealed partial class ProceduralPropertyDescription
    {
        public string name;
        public string label;
        public string group;
        public ProceduralPropertyType type;
        public bool hasRange;
        public float minimum;
        public float maximum;
        public float step;
        public string[] enumOptions;
        public string[] componentLabels;
    }

    [Obsolete("ProceduralMaterial is deprecated and no longer available.", true)]
    [ExcludeFromPreset]
    public sealed partial class ProceduralMaterial : Material
    {
        internal ProceduralMaterial()
            : base((Material)null)
        {
            FeatureRemoved();
        }

        public ProceduralPropertyDescription[] GetProceduralPropertyDescriptions()
        {
            throw new Exception("ProceduralMaterial is deprecated and no longer available.");
        }

        public bool HasProceduralProperty(string inputName)
        {
            throw new Exception("ProceduralMaterial is deprecated and no longer available.");
        }

        public bool GetProceduralBoolean(string inputName)
        {
            throw new Exception("ProceduralMaterial is deprecated and no longer available.");
        }

        public bool IsProceduralPropertyVisible(string inputName)
        {
            throw new Exception("ProceduralMaterial is deprecated and no longer available.");
        }

        public void SetProceduralBoolean(string inputName, bool value)
        {
            FeatureRemoved();
        }

        public float GetProceduralFloat(string inputName)
        {
            throw new Exception("ProceduralMaterial is deprecated and no longer available.");
        }

        public void SetProceduralFloat(string inputName, float value)
        {
            FeatureRemoved();
        }

        public Vector4 GetProceduralVector(string inputName)
        {
            throw new Exception("ProceduralMaterial is deprecated and no longer available.");
        }

        public void SetProceduralVector(string inputName, Vector4 value)
        {
            FeatureRemoved();
        }

        public Color GetProceduralColor(string inputName)
        {
            throw new Exception("ProceduralMaterial is deprecated and no longer available.");
        }

        public void SetProceduralColor(string inputName, Color value)
        {
            FeatureRemoved();
        }

        public int GetProceduralEnum(string inputName)
        {
            throw new Exception("ProceduralMaterial is deprecated and no longer available.");
        }

        public void SetProceduralEnum(string inputName, int value)
        {
            FeatureRemoved();
        }

        public Texture2D GetProceduralTexture(string inputName)
        {
            throw new Exception("ProceduralMaterial is deprecated and no longer available.");
        }

        public void SetProceduralTexture(string inputName, Texture2D value)
        {
            FeatureRemoved();
        }

        public string GetProceduralString(string inputName)
        {
            throw new Exception("ProceduralMaterial is deprecated and no longer available.");
        }

        public void SetProceduralString(string inputName, string value)
        {
            FeatureRemoved();
        }

        public bool IsProceduralPropertyCached(string inputName)
        {
            throw new Exception("ProceduralMaterial is deprecated and no longer available.");
        }

        public void CacheProceduralProperty(string inputName, bool value)
        {
            FeatureRemoved();
        }

        public void ClearCache()
        {
            FeatureRemoved();
        }

        public ProceduralCacheSize cacheSize
        {
            get { throw new Exception("ProceduralMaterial is deprecated and no longer available."); }
            set { FeatureRemoved(); }
        }


        public int animationUpdateRate
        {
            get { throw new Exception("ProceduralMaterial is deprecated and no longer available."); }
            set { FeatureRemoved(); }
        }

        public void RebuildTextures()
        {
            FeatureRemoved();
        }

        public void RebuildTexturesImmediately()
        {
            FeatureRemoved();
        }

        public bool isProcessing
        {
            get { throw new Exception("ProceduralMaterial is deprecated and no longer available."); }
        }

        public static void StopRebuilds()
        {
            FeatureRemoved();
        }

        public bool isCachedDataAvailable
        {
            get { throw new Exception("ProceduralMaterial is deprecated and no longer available."); }
        }

        public bool isLoadTimeGenerated
        {
            get { throw new Exception("ProceduralMaterial is deprecated and no longer available."); }
            set { FeatureRemoved(); }
        }

        public ProceduralLoadingBehavior loadingBehavior
        {
            get { throw new Exception("ProceduralMaterial is deprecated and no longer available."); }
        }

        public static bool isSupported
        {
            get { throw new Exception("ProceduralMaterial is deprecated and no longer available."); }
        }

        public static ProceduralProcessorUsage substanceProcessorUsage
        {
            get { throw new Exception("ProceduralMaterial is deprecated and no longer available."); }
            set { FeatureRemoved(); }
        }

        public string preset
        {
            get { throw new Exception("ProceduralMaterial is deprecated and no longer available."); }
            set { FeatureRemoved(); }
        }

        public Texture[] GetGeneratedTextures()
        {
            throw new Exception("ProceduralMaterial is deprecated and no longer available.");
        }

        public ProceduralTexture GetGeneratedTexture(string textureName)
        {
            throw new Exception("ProceduralMaterial is deprecated and no longer available.");
        }

        public bool isReadable
        {
            get { throw new Exception("ProceduralMaterial is deprecated and no longer available."); }
            set { FeatureRemoved(); }
        }

        public void FreezeAndReleaseSourceData()
        {
            FeatureRemoved();
        }

        public bool isFrozen
        {
            get { throw new Exception("ProceduralMaterial is deprecated and no longer available."); }
        }
    }

    [Obsolete("ProceduralTexture is deprecated and no longer available.", true)]
    [ExcludeFromPreset]
    public sealed partial class ProceduralTexture : Texture
    {
        private ProceduralTexture()
        {
            throw new Exception("ProceduralTexture is deprecated and no longer available.");
        }

        public ProceduralOutputType GetProceduralOutputType()
        {
            throw new Exception("ProceduralTexture is deprecated and no longer available.");
        }

        internal ProceduralMaterial GetProceduralMaterial()
        {
            throw new Exception("ProceduralTexture is deprecated and no longer available.");
        }

        public bool hasAlpha
        {
            get { throw new Exception("ProceduralTexture is deprecated and no longer available."); }
        }

        public TextureFormat format
        {
            get { throw new Exception("ProceduralTexture is deprecated and no longer available."); }
        }

        public Color32[] GetPixels32(int x, int y, int blockWidth, int blockHeight)
        {
            throw new Exception("ProceduralTexture is deprecated and no longer available.");
        }
    }

}
