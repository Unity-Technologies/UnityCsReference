// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Runtime/Animation/HumanDescription.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct SkeletonBone
    {
        [NativeName("m_Name")]
        public string     name;
        [NativeName("m_ParentName")]
        internal string   parentName;

        [NativeName("m_Position")]
        public Vector3    position;

        [NativeName("m_Rotation")]
        public Quaternion rotation;

        [NativeName("m_Scale")]
        public Vector3    scale;

        [Obsolete("transformModified is no longer used and has been deprecated.", true)]
        public int transformModified { get { return 0; } set {} }
    }

    [NativeHeader("Runtime/Animation/ScriptBindings/AvatarBuilder.bindings.h")]
    [NativeHeader("Runtime/Animation/HumanDescription.h")]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions.Custom, "MonoHumanLimit")]
    public struct HumanLimit
    {
        Vector3 m_Min;
        Vector3 m_Max;
        Vector3 m_Center;
        float   m_AxisLength;
        int     m_UseDefaultValues;

        public bool     useDefaultValues { get { return m_UseDefaultValues != 0; } set { m_UseDefaultValues = value ? 1 : 0; } }
        public Vector3  min { get { return m_Min; } set { m_Min = value; } }
        public Vector3  max { get { return m_Max; } set { m_Max = value; } }
        public Vector3  center { get { return m_Center; } set { m_Center = value; } }
        public float    axisLength { get { return m_AxisLength; } set { m_AxisLength = value; } }
    }

    [NativeHeader("Runtime/Animation/HumanDescription.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct HumanBone
    {
        string              m_BoneName;
        string              m_HumanName;

        [NativeName("m_Limit")]
        public HumanLimit   limit;

        public string   boneName { get { return m_BoneName; } set { m_BoneName = value; } }
        public string   humanName { get { return m_HumanName; } set { m_HumanName = value; } }
    }

    [NativeHeader("Runtime/Animation/ScriptBindings/AvatarBuilder.bindings.h")]
    [NativeHeader("Runtime/Animation/HumanDescription.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct HumanDescription
    {
        [NativeName("m_Human")]
        public HumanBone[]      human;
        [NativeName("m_Skeleton")]
        public SkeletonBone[]   skeleton;

        internal float  m_ArmTwist;
        internal float  m_ForeArmTwist;
        internal float  m_UpperLegTwist;
        internal float  m_LegTwist;
        internal float  m_ArmStretch;
        internal float  m_LegStretch;
        internal float  m_FeetSpacing;

        internal string  m_RootMotionBoneName;
        internal Quaternion m_RootMotionBoneRotation;

        internal bool   m_HasTranslationDoF;

        internal bool   m_HasExtraRoot;
        internal bool   m_SkeletonHasParents;

        public float    upperArmTwist { get { return m_ArmTwist; } set { m_ArmTwist = value; }   }
        public float    lowerArmTwist { get { return m_ForeArmTwist; } set { m_ForeArmTwist = value; }   }
        public float    upperLegTwist { get { return m_UpperLegTwist; } set { m_UpperLegTwist = value; }     }
        public float    lowerLegTwist { get { return m_LegTwist; } set { m_LegTwist = value; }   }
        public float    armStretch { get { return m_ArmStretch; } set { m_ArmStretch = value; }  }
        public float    legStretch { get { return m_LegStretch; } set { m_LegStretch = value; }  }
        public float    feetSpacing { get { return m_FeetSpacing; } set { m_FeetSpacing = value; }   }
        public bool     hasTranslationDoF { get { return m_HasTranslationDoF; } set { m_HasTranslationDoF = value; }}
    }

    [NativeHeader("Runtime/Animation/ScriptBindings/AvatarBuilder.bindings.h")]
    public class AvatarBuilder
    {
        public static Avatar BuildHumanAvatar(GameObject go, HumanDescription humanDescription)
        {
            if (go == null)
                throw new NullReferenceException();
            return BuildHumanAvatarInternal(go, humanDescription);
        }

        [FreeFunction("AvatarBuilderBindings::BuildHumanAvatar")]
        extern private static Avatar BuildHumanAvatarInternal(GameObject go, HumanDescription humanDescription);
        [FreeFunction("AvatarBuilderBindings::BuildGenericAvatar")]
        extern public static Avatar BuildGenericAvatar([NotNull] GameObject go, [NotNull] string rootMotionTransformName);
    }
}
