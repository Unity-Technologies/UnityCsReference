// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm = System.ComponentModel;
using uei = UnityEngine.Internal;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;

using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor.Animations;
using System.Runtime.InteropServices;
using System;
using UnityEngine.Bindings;

namespace UnityEditor
{
    public enum ClipAnimationMaskType
    {
        CreateFromThisModel = 0,

        CopyFromOther = 1,
        None = 3
    }

    [UsedByNativeCode]
    [NativeType(CodegenOptions = CodegenOptions.Custom, Header = "Runtime/Animation/AvatarMask.h", IntermediateScriptingStructName = "MonoTransformMaskElement")]
    [NativeHeader("Editor/Src/AssetPipeline/ModelImporting/ModelImporter.bindings.h")]
    internal partial struct TransformMaskElement
    {
        public string path;
        public float weight;
    }

    [UsedByNativeCode]
    [NativeType(CodegenOptions = CodegenOptions.Custom, IntermediateScriptingStructName = "MonoClipAnimationInfoCurve")]
    [NativeHeader("Editor/Src/AssetPipeline/ModelImporting/ModelImporter.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct ClipAnimationInfoCurve
    {
        public string name;
        public AnimationCurve curve;
    }

    [UsedByNativeCode]
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom, IntermediateScriptingStructName = "MonoClipAnimationInfo")]
    [NativeHeader("Editor/Src/AssetPipeline/ModelImporting/ModelImporter.bindings.h")]
    public sealed partial class ModelImporterClipAnimation
    {
        string m_TakeName;
        string m_Name;
        float m_FirstFrame;
        float m_LastFrame;
        WrapMode m_WrapMode;
        int m_Loop;

        float m_OrientationOffsetY;
        float m_Level;
        float m_CycleOffset;
        float m_AdditiveReferencePoseFrame;

        int m_HasAdditiveReferencePose;
        int m_LoopTime;
        int m_LoopBlend;
        int m_LoopBlendOrientation;
        int m_LoopBlendPositionY;
        int m_LoopBlendPositionXZ;
        int m_KeepOriginalOrientation;
        int m_KeepOriginalPositionY;
        int m_KeepOriginalPositionXZ;
        int m_HeightFromFeet;
        int m_Mirror;
        int m_MaskType = 3;
        AvatarMask m_MaskSource;

        int[] m_BodyMask;
        AnimationEvent[] m_AnimationEvents;
        ClipAnimationInfoCurve[] m_AdditionnalCurves;
        TransformMaskElement[] m_TransformMask;

        bool m_MaskNeedsUpdating;

        public string takeName { get { return m_TakeName; } set { m_TakeName = value; } }
        public string name { get { return m_Name; } set { m_Name = value; } }
        public float firstFrame { get { return m_FirstFrame; } set { m_FirstFrame = value; } }
        public float lastFrame { get { return m_LastFrame; } set { m_LastFrame = value; } }

        public WrapMode wrapMode { get { return m_WrapMode; } set { m_WrapMode = value; } }

        public bool loop { get { return m_Loop != 0; } set { m_Loop = value ? 1 : 0; } }

        public float rotationOffset { get { return m_OrientationOffsetY; } set { m_OrientationOffsetY = value; } }

        public float heightOffset { get { return m_Level; } set { m_Level = value; } }

        public float cycleOffset { get { return m_CycleOffset; } set { m_CycleOffset = value; } }

        public bool loopTime { get { return m_LoopTime != 0; } set { m_LoopTime = value ? 1 : 0; } }

        public bool loopPose { get { return m_LoopBlend != 0; } set { m_LoopBlend = value ? 1 : 0; } }

        public bool lockRootRotation { get { return m_LoopBlendOrientation != 0; } set { m_LoopBlendOrientation = value ? 1 : 0; } }

        public bool lockRootHeightY { get { return m_LoopBlendPositionY != 0; } set { m_LoopBlendPositionY = value ? 1 : 0; } }

        public bool lockRootPositionXZ { get { return m_LoopBlendPositionXZ != 0; } set { m_LoopBlendPositionXZ = value ? 1 : 0; } }

        public bool keepOriginalOrientation { get { return m_KeepOriginalOrientation != 0; } set { m_KeepOriginalOrientation = value ? 1 : 0; } }

        public bool keepOriginalPositionY { get { return m_KeepOriginalPositionY != 0; } set { m_KeepOriginalPositionY = value ? 1 : 0; } }

        public bool keepOriginalPositionXZ { get { return m_KeepOriginalPositionXZ != 0; } set { m_KeepOriginalPositionXZ = value ? 1 : 0; } }

        public bool heightFromFeet { get { return m_HeightFromFeet != 0; } set { m_HeightFromFeet = value ? 1 : 0; } }

        public bool mirror { get { return m_Mirror != 0; } set { m_Mirror = value ? 1 : 0; } }

        public ClipAnimationMaskType maskType { get { return (ClipAnimationMaskType)m_MaskType; } set { m_MaskType = (int)value; } }

        public AvatarMask maskSource { get { return m_MaskSource; } set { m_MaskSource = value; } }

        public AnimationEvent[] events { get { return m_AnimationEvents; } set { m_AnimationEvents = value; } }
        public ClipAnimationInfoCurve[] curves { get { return m_AdditionnalCurves; } set { m_AdditionnalCurves = value; } }

        public bool maskNeedsUpdating { get { return m_MaskNeedsUpdating; } }

        public float additiveReferencePoseFrame { get { return m_AdditiveReferencePoseFrame; } set { m_AdditiveReferencePoseFrame = value; } }
        public bool hasAdditiveReferencePose { get { return m_HasAdditiveReferencePose != 0; } set { m_HasAdditiveReferencePose = value ? 1 : 0; } }

        public void ConfigureMaskFromClip(ref AvatarMask mask)
        {
            mask.transformCount = this.m_TransformMask.Length;
            for (int i = 0; i < mask.transformCount; i++)
            {
                mask.SetTransformPath(i, this.m_TransformMask[i].path);
                mask.SetTransformActive(i, this.m_TransformMask[i].weight > 0f);
            }
            for (int i = 0; i < this.m_BodyMask.Length; i++)
            {
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, m_BodyMask[i] != 0);
            }
        }

        public void ConfigureClipFromMask(AvatarMask mask)
        {
            this.m_TransformMask = new TransformMaskElement[mask.transformCount];
            for (int i = 0; i < mask.transformCount; i++)
            {
                m_TransformMask[i].path = mask.GetTransformPath(i);
                m_TransformMask[i].weight = mask.GetTransformActive(i) ? 1f : 0f;
            }
            m_BodyMask = new int[(int)AvatarMaskBodyPart.LastBodyPart];

            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                m_BodyMask[i] = mask.GetHumanoidBodyPartActive((AvatarMaskBodyPart)i) ? 1 : 0;
            }
        }

        public override bool Equals(object o)
        {
            ModelImporterClipAnimation other = o as ModelImporterClipAnimation;
            return other != null && takeName == other.takeName && name == other.name && firstFrame == other.firstFrame && lastFrame == other.lastFrame && m_WrapMode == other.m_WrapMode && m_Loop == other.m_Loop &&
                loopPose == other.loopPose && lockRootRotation == other.lockRootRotation && lockRootHeightY == other.lockRootHeightY && lockRootPositionXZ == other.lockRootPositionXZ &&
                mirror == other.mirror && maskType == other.maskType && maskSource == other.maskSource && additiveReferencePoseFrame == other.additiveReferencePoseFrame && hasAdditiveReferencePose == other.hasAdditiveReferencePose;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }
    }

    [System.Obsolete("Use ModelImporterMaterialName, ModelImporter.materialName and ModelImporter.importMaterials instead")]
    public enum ModelImporterGenerateMaterials
    {
        [System.Obsolete("Use ModelImporter.importMaterials=false instead")]
        None = 0,

        [System.Obsolete("Use ModelImporter.importMaterials=true and ModelImporter.materialName=ModelImporterMaterialName.BasedOnTextureName instead")]
        PerTexture = 1,

        [System.Obsolete("Use ModelImporter.importMaterials=true and ModelImporter.materialName=ModelImporterMaterialName.BasedOnModelNameAndMaterialName instead")]
        PerSourceMaterial = 2,
    }

    [NativeType(Header = "Editor/Src/AssetPipeline/ModelImporting/ModelImporter.h")]
    public enum ModelImporterMaterialName
    {
        BasedOnTextureName = 0,

        BasedOnMaterialName = 1,

        BasedOnModelNameAndMaterialName = 2,

        [System.Obsolete("You should use ModelImporterMaterialName.BasedOnTextureName instead, because it it less complicated and behaves in more consistent way.")]
        BasedOnTextureName_Or_ModelNameAndMaterialName = 3,
    }

    [NativeType(Header = "Editor/Src/AssetPipeline/ModelImporting/ModelImporter.h")]
    public enum ModelImporterMaterialSearch
    {
        Local = 0,

        RecursiveUp = 1,

        Everywhere = 2,
    }

    [NativeType(Header = "Editor/Src/AssetPipeline/ModelImporting/ModelImporter.h")]
    public enum ModelImporterMaterialLocation
    {
        External = 0,
        InPrefab = 1
    };

    public enum ModelImporterTangentSpaceMode
    {
        [System.Obsolete("Use ModelImporterNormals.Import instead")]
        Import = 0,
        [System.Obsolete("Use ModelImporterNormals.Calculate instead")]
        Calculate = 1,
        [System.Obsolete("Use ModelImporterNormals.None instead")]
        None = 2,
    }

    [NativeType(Header = "Editor/Src/AssetPipeline/ModelImporting/ImportMesh.h")]
    public enum ModelImporterNormals
    {
        Import = 0,

        Calculate = 1,

        None = 2,
    }

    [NativeType(Header = "Editor/Src/AssetPipeline/ModelImporting/ImportMesh.h")]
    public enum ModelImporterNormalCalculationMode
    {
        Unweighted_Legacy,

        Unweighted,
        AreaWeighted,
        AngleWeighted,
        AreaAndAngleWeighted
    }

    [NativeType(Header = "Editor/Src/AssetPipeline/ModelImporting/ImportMesh.h")]
    public enum ModelImporterTangents
    {
        Import = 0,

        CalculateLegacy = 1,

        CalculateLegacyWithSplitTangents = 4,
        CalculateMikk = 3,

        None = 2,
    }

    public enum ModelImporterMeshCompression
    {
        Off = 0,

        Low = 1,

        Medium = 2,

        High = 3,
    }

    public enum ModelImporterIndexFormat
    {
        Auto = 0,
        UInt16 = 1,
        UInt32 = 2,
    }

    [NativeType(Header = "Editor/Src/AssetPipeline/ModelImporting/ModelImporter.h")]
    public enum ModelImporterAnimationCompression
    {
        Off = 0,

        KeyframeReduction = 1,

        KeyframeReductionAndCompression = 2,

        Optimal = 3
    }

    [NativeType(Header = "Editor/Src/AssetPipeline/ModelImporting/ModelImporter.h")]
    public enum ModelImporterGenerateAnimations
    {
        None = 0,

        GenerateAnimations = 4,

        InRoot = 3,

        InOriginalRoots = 1,

        InNodes = 2
    }

    [NativeType(Header = "Editor/Src/AssetPipeline/ModelImporting/ModelImporter.h")]
    public enum ModelImporterAnimationType
    {
        None = 0,

        Legacy = 1,

        Generic = 2,

        Human = 3
    }

    [NativeType(Header = "Editor/Src/AssetPipeline/ModelImporting/ModelImporter.h")]
    public enum ModelImporterHumanoidOversampling
    {
        X1 = 1,

        X2 = 2,

        X4 = 4,

        X8 = 8
    }

    [UsedByNativeCode]
    [NativeType(Header = "Editor/Src/Animation/HumanTemplate.h")]
    public sealed partial class HumanTemplate : Object
    {
        public HumanTemplate()
        {
            Internal_Create(this);
        }

        extern private static void Internal_Create([Writable] HumanTemplate self);

        extern public void Insert(string name, string templateName);

        extern public string Find(string name);

        extern public void ClearTemplate();
    }

    [NativeType(Header = "Editor/Src/AssetPipeline/ModelImporting/ModelImporter.h")]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    [UsedByNativeCode]
    public partial struct TakeInfo
    {
        public string name;
        public string defaultClipName;
        public float startTime;
        public float stopTime;
        public float bakeStartTime;
        public float bakeStopTime;
        public float sampleRate;
    }

    [NativeType(Header = "Editor/Src/AssetPipeline/ModelImporting/ModelImporter.h")]
    [NativeHeader("Editor/Src/AssetPipeline/ModelImporting/ModelImporter.bindings.h")]
    public partial class ModelImporter : AssetImporter
    {
        [System.Obsolete("Use importMaterials, materialName and materialSearch instead")]
        public extern ModelImporterGenerateMaterials generateMaterials
        {
            get;
            set;
        }

        public extern bool importMaterials
        {
            get;
            set;
        }

        public extern ModelImporterMaterialName materialName
        {
            get;
            set;
        }

        public extern ModelImporterMaterialSearch materialSearch
        {
            get;
            set;
        }

        public extern ModelImporterMaterialLocation materialLocation { get; set; }

        internal extern SourceAssetIdentifier[] sourceMaterials
        {
            [FreeFunction(Name = "ModelImporterBindings::GetSourceMaterials", HasExplicitThis = true)]
            get;
        }

        public extern float globalScale
        {
            get;
            set;
        }

        public extern bool isUseFileUnitsSupported
        {
            [NativeMethod("IsUseFileUnitsSupported")]
            get;
        }

        public extern bool importVisibility
        {
            get;
            set;
        }

        public extern bool useFileUnits
        {
            get;
            set;
        }

        public extern bool useFileScale
        {
            get;
            set;
        }

        [System.Obsolete("Use useFileScale instead")]
        public bool isFileScaleUsed
        {
            get { return useFileScale; }
        }

        public extern bool importBlendShapes
        {
            get;
            set;
        }

        public extern bool importCameras
        {
            get;
            set;
        }

        public extern bool importLights
        {
            get;
            set;
        }

        public extern bool addCollider
        {
            [NativeMethod("GetAddColliders")]
            get;
            [NativeMethod("SetAddColliders")]
            set;
        }

        public extern float normalSmoothingAngle
        {
            get;
            set;
        }

        [System.Obsolete("Please use tangentImportMode instead")]
        public bool splitTangentsAcrossSeams
        {
            get
            {
                return importTangents == ModelImporterTangents.CalculateLegacyWithSplitTangents;
            }
            set
            {
                if (importTangents == ModelImporterTangents.CalculateLegacyWithSplitTangents && !value)
                    importTangents = ModelImporterTangents.CalculateLegacy;
                else if (importTangents == ModelImporterTangents.CalculateLegacy && value)
                    importTangents = ModelImporterTangents.CalculateLegacyWithSplitTangents;
            }
        }

        public extern bool swapUVChannels
        {
            get;
            set;
        }

        public extern bool weldVertices
        {
            get;
            set;
        }

        public extern bool keepQuads
        {
            get;
            set;
        }

        public extern ModelImporterIndexFormat indexFormat
        {
            get;
            set;
        }

        public extern bool preserveHierarchy
        {
            get;
            set;
        }

        public extern bool generateSecondaryUV
        {
            get;
            set;
        }

        public extern float secondaryUVAngleDistortion
        {
            get;
            set;
        }

        public extern float secondaryUVAreaDistortion
        {
            get;
            set;
        }

        public extern float secondaryUVHardAngle
        {
            get;
            set;
        }

        public extern float secondaryUVPackMargin
        {
            get;
            set;
        }

        public extern ModelImporterGenerateAnimations generateAnimations
        {
            [NativeMethod("GetLegacyGenerateAnimations")]
            get;
            [NativeMethod("SetLegacyGenerateAnimations")]
            set;
        }

        public extern TakeInfo[] importedTakeInfos
        {
            get;
        }

        public extern string[] transformPaths
        {
            get;
        }

        public string[] referencedClips
        {
            get { return INTERNAL_GetReferencedClips(this); }
        }
        [FreeFunction("ModelImporterBindings::GetReferencedClips")]
        private extern static string[] INTERNAL_GetReferencedClips(ModelImporter self);

        public extern bool isReadable
        {
            get;
            set;
        }

        public extern bool optimizeMesh
        {
            get;
            set;
        }

        [System.Obsolete("normalImportMode is deprecated. Use importNormals instead")]
        public ModelImporterTangentSpaceMode normalImportMode
        {
            get { return (ModelImporterTangentSpaceMode)importNormals; }
            set { importNormals = (ModelImporterNormals)value; }
        }

        [System.Obsolete("tangentImportMode is deprecated. Use importTangents instead")]
        public ModelImporterTangentSpaceMode tangentImportMode
        {
            get { return (ModelImporterTangentSpaceMode)importTangents; }
            set { importTangents = (ModelImporterTangents)value; }
        }

        public extern ModelImporterNormals importNormals
        {
            [NativeMethod("GetNormalImportMode")]
            get;
            [NativeMethod("SetNormalImportMode")]
            set;
        }

        public extern ModelImporterNormalCalculationMode normalCalculationMode
        {
            get;
            set;
        }

        public extern ModelImporterTangents importTangents
        {
            [NativeMethod("GetTangentImportMode")]
            get;
            [NativeMethod("SetTangentImportMode")]
            set;
        }

        public extern bool bakeIK
        {
            get;
            set;
        }

        public extern bool isBakeIKSupported
        {
            [NativeMethod("IsBakeIKSupported")]
            get;
        }

        [System.Obsolete("use resampleCurves instead.")]
        public extern bool resampleRotations
        {
            [NativeMethod("GetResampleCurves")]
            get;
            [NativeMethod("SetResampleCurves")]
            set;
        }

        public extern bool resampleCurves
        {
            get;
            set;
        }

        public extern bool isTangentImportSupported
        {
            [NativeMethod("IsTangentImportSupported")]
            get;
        }

        [System.Obsolete("Use animationCompression instead", true)]
        private bool reduceKeyframes { get { return false; } set {} }

        public ModelImporterMeshCompression meshCompression
        {
            get { return (ModelImporterMeshCompression)internal_meshCompression; }
            set { internal_meshCompression = (int)value; }
        }

        private extern int internal_meshCompression
        {
            [NativeMethod("GetMeshCompression")]
            get;
            [NativeMethod("SetMeshCompression")]
            set;
        }

        public extern bool importAnimation
        {
            get;
            set;
        }

        public extern bool optimizeGameObjects
        {
            get;
            set;
        }

        public string[] extraExposedTransformPaths
        {
            get { return GetExtraExposedTransformPaths(); }
            set { INTERNAL_set_extraExposedTransformPaths(this, value); }
        }

        private extern string[] GetExtraExposedTransformPaths();

        [FreeFunction("ModelImporterBindings::SetExtraExposedTransformPaths")]
        private extern static void INTERNAL_set_extraExposedTransformPaths([Writable] ModelImporter self, string[] value);

        public string[] extraUserProperties
        {
            get { return GetExtraUserProperties(); }
            set { INTERNAL_set_extraUserProperties(this, value); }
        }

        private extern string[] GetExtraUserProperties();

        [FreeFunction("ModelImporterBindings::SetExtraUserProperties")]
        private extern static void INTERNAL_set_extraUserProperties([Writable] ModelImporter self, string[] value);

        public extern ModelImporterAnimationCompression animationCompression
        {
            get;
            set;
        }

        public extern bool importAnimatedCustomProperties
        {
            get;
            set;
        }

        public extern float animationRotationError
        {
            get;
            set;
        }

        public extern float animationPositionError
        {
            get;
            set;
        }

        public extern float animationScaleError
        {
            get;
            set;
        }

        public extern WrapMode animationWrapMode
        {
            get;
            set;
        }

        public extern ModelImporterAnimationType animationType
        {
            get;
            set;
        }

        public extern ModelImporterHumanoidOversampling humanoidOversampling
        {
            get;
            set;
        }

        public extern string motionNodeName
        {
            get;
            set;
        }

        public Avatar sourceAvatar
        {
            get
            {
                return GetSourceAvatar();
            }
            set
            {
                Avatar avatar = value;
                if (value != null)
                {
                    ModelImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(value)) as ModelImporter;

                    if (importer != null)
                    {
                        humanDescription = importer.humanDescription;
                    }
                    else
                    {
                        Debug.LogError("Avatar must be from a ModelImporter, otherwise use ModelImporter.humanDescription");
                        avatar = null;
                    }
                }

                SetSourceAvatarInternal(this, avatar);
            }
        }

        private extern Avatar GetSourceAvatar();
        [FreeFunction("ModelImporterBindings::SetSourceAvatarInternal")]
        private extern static void SetSourceAvatarInternal(ModelImporter self, Avatar value);

        [System.Obsolete("splitAnimations has been deprecated please use clipAnimations instead.", true)]
        public bool splitAnimations
        {
            get { return clipAnimations.Length != 0; }
            set {}
        }

        public ModelImporterClipAnimation[] clipAnimations
        {
            get { return GetClipAnimations(this); }
            set { SetClipAnimations(this, value); }
        }
        [FreeFunction("ModelImporterBindings::GetClipAnimations")]
        private extern static ModelImporterClipAnimation[] GetClipAnimations(ModelImporter self);
        [FreeFunction("ModelImporterBindings::SetClipAnimations")]
        private extern static void SetClipAnimations([Writable] ModelImporter self, ModelImporterClipAnimation[] value);

        public ModelImporterClipAnimation[] defaultClipAnimations
        {
            get { return GetDefaultClipAnimations(this); }
        }
        [FreeFunction("ModelImporterBindings::GetDefaultClipAnimations")]
        private extern static ModelImporterClipAnimation[] GetDefaultClipAnimations(ModelImporter self);

        internal extern bool isAssetOlderOr42
        {
            [NativeMethod("IsAssetOlderOr42")]
            get;
        }

        [FreeFunction("ModelImporterBindings::UpdateTransformMask")]
        extern internal static void UpdateTransformMask([NotNull] AvatarMask mask, [NotNull] SerializedProperty serializedProperty);

        extern internal AnimationClip GetPreviewAnimationClipForTake(string takeName);

        extern internal string CalculateBestFittingPreviewGameObject();

        public void CreateDefaultMaskForClip(ModelImporterClipAnimation clip)
        {
            if (this.defaultClipAnimations.Length > 0)
            {
                var mask = new AvatarMask();
                this.defaultClipAnimations[0].ConfigureMaskFromClip(ref mask);
                clip.ConfigureClipFromMask(mask);
                DestroyImmediate(mask);
            }
            else
                Debug.LogError("Cannot create default mask because the current importer doesn't have any animation information");
        }

        [NativeName("ExtractTextures")]
        private extern bool ExtractTexturesInternal(string folderPath);

        public bool ExtractTextures(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
                throw new ArgumentException("The path cannot be empty", folderPath);

            return ExtractTexturesInternal(folderPath);
        }

        public extern bool SearchAndRemapMaterials(ModelImporterMaterialName nameOption, ModelImporterMaterialSearch searchOption);
    }
}
