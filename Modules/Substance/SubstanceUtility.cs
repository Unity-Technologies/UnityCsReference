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
    [Obsolete("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.", true)]
    public enum ProceduralProcessorUsage
    {
        Unsupported = 0,
        One = 1,
        Half = 2,
        All = 3
    }

    [Obsolete("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.", true)]
    public enum ProceduralCacheSize
    {
        Tiny = 0,
        Medium = 1,
        Heavy = 2,
        NoLimit = 3,
        None = 4
    }

    [Obsolete("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.", true)]
    public enum ProceduralLoadingBehavior
    {
        DoNothing = 0,
        Generate = 1,
        BakeAndKeep = 2,
        BakeAndDiscard = 3,
        Cache = 4,
        DoNothingAndCache = 5
    }

    [Obsolete("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.", true)]
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

    [Obsolete("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.", true)]
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
    [Obsolete("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.", true)]
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

    [Obsolete("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.", true)]
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
            throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.");
        }

        public bool HasProceduralProperty(string inputName)
        {
            throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.");
        }

        public bool GetProceduralBoolean(string inputName)
        {
            throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.");
        }

        public bool IsProceduralPropertyVisible(string inputName)
        {
            throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.");
        }

        public void SetProceduralBoolean(string inputName, bool value)
        {
            FeatureRemoved();
        }

        public float GetProceduralFloat(string inputName)
        {
            throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.");
        }

        public void SetProceduralFloat(string inputName, float value)
        {
            FeatureRemoved();
        }

        public Vector4 GetProceduralVector(string inputName)
        {
            throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.");
        }

        public void SetProceduralVector(string inputName, Vector4 value)
        {
            FeatureRemoved();
        }

        public Color GetProceduralColor(string inputName)
        {
            throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.");
        }

        public void SetProceduralColor(string inputName, Color value)
        {
            FeatureRemoved();
        }

        public int GetProceduralEnum(string inputName)
        {
            throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.");
        }

        public void SetProceduralEnum(string inputName, int value)
        {
            FeatureRemoved();
        }

        public Texture2D GetProceduralTexture(string inputName)
        {
            throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.");
        }

        public void SetProceduralTexture(string inputName, Texture2D value)
        {
            FeatureRemoved();
        }

        public string GetProceduralString(string inputName)
        {
            throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.");
        }

        public void SetProceduralString(string inputName, string value)
        {
            FeatureRemoved();
        }

        public bool IsProceduralPropertyCached(string inputName)
        {
            throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.");
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
            get { throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store."); }
            set { FeatureRemoved(); }
        }


        public int animationUpdateRate
        {
            get { throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store."); }
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
            get { throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store."); }
        }

        public static void StopRebuilds()
        {
            FeatureRemoved();
        }

        public bool isCachedDataAvailable
        {
            get { throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store."); }
        }

        public bool isLoadTimeGenerated
        {
            get { throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store."); }
            set { FeatureRemoved(); }
        }

        public ProceduralLoadingBehavior loadingBehavior
        {
            get { throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store."); }
        }

        public static bool isSupported
        {
            get { throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store."); }
        }

        public static ProceduralProcessorUsage substanceProcessorUsage
        {
            get { throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store."); }
            set { FeatureRemoved(); }
        }

        public string preset
        {
            get { throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store."); }
            set { FeatureRemoved(); }
        }

        public Texture[] GetGeneratedTextures()
        {
            throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.");
        }

        public ProceduralTexture GetGeneratedTexture(string textureName)
        {
            throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.");
        }

        public bool isReadable
        {
            get { throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store."); }
            set { FeatureRemoved(); }
        }

        public void FreezeAndReleaseSourceData()
        {
            FeatureRemoved();
        }

        public bool isFrozen
        {
            get { throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store."); }
        }
    }

    [Obsolete("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.", true)]
    [ExcludeFromPreset]
    public sealed partial class ProceduralTexture : Texture
    {
        private ProceduralTexture()
        {
            throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.");
        }

        public ProceduralOutputType GetProceduralOutputType()
        {
            throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.");
        }

        internal ProceduralMaterial GetProceduralMaterial()
        {
            throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.");
        }

        public bool hasAlpha
        {
            get { throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store."); }
        }

        public TextureFormat format
        {
            get { throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store."); }
        }

        public Color32[] GetPixels32(int x, int y, int blockWidth, int blockHeight)
        {
            throw new Exception("Built-in support for Substance Designer materials has been removed from Unity. To continue using Substance Designer materials, you will need to install Allegorithmic's external importer from the Asset Store.");
        }
    }

}
