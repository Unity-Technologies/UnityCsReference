// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;

namespace UnityEngine
{


[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct SkeletonBone
{
    public string     name;
    internal string   parentName;
    
    
    public Vector3    position;
    public Quaternion rotation;
    public Vector3    scale;
    
    
    [Obsolete("transformModified is no longer used and has been deprecated.", true)]
            public int transformModified { get { return 0; } set {} }
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct HumanLimit
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

[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct HumanBone
{
    
            string              m_BoneName;
            string              m_HumanName;
            public HumanLimit   limit;
    
            public string   boneName { get { return m_BoneName; } set { m_BoneName = value; } }
            public string   humanName { get { return m_HumanName; } set { m_HumanName = value; } }
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct HumanDescription
{
    
            public HumanBone[]      human;
            public SkeletonBone[]   skeleton;
    
            internal float  m_ArmTwist;
            internal float  m_ForeArmTwist;
            internal float  m_UpperLegTwist;
            internal float  m_LegTwist;
            internal float  m_ArmStretch;
            internal float  m_LegStretch;
            internal float  m_FeetSpacing;
    
            internal bool   m_HasTranslationDoF;
    
            public float    upperArmTwist { get { return m_ArmTwist; } set { m_ArmTwist = value; }   }
            public float    lowerArmTwist { get { return m_ForeArmTwist; } set { m_ForeArmTwist = value; }   }
            public float    upperLegTwist { get { return m_UpperLegTwist; } set { m_UpperLegTwist = value; }     }
            public float    lowerLegTwist { get { return m_LegTwist; } set { m_LegTwist = value; }   }
            public float    armStretch { get { return m_ArmStretch; } set { m_ArmStretch = value; }  }
            public float    legStretch { get { return m_LegStretch; } set { m_LegStretch = value; }  }
            public float    feetSpacing { get { return m_FeetSpacing; } set { m_FeetSpacing = value; }   }
            public bool     hasTranslationDoF { get { return m_HasTranslationDoF; } set { m_HasTranslationDoF = value; }}
}

public sealed partial class AvatarBuilder
{
    public static Avatar BuildHumanAvatar(GameObject go, HumanDescription humanDescription)
        {

            if (go == null)
                throw new NullReferenceException();

            return BuildHumanAvatarMono(go, humanDescription);
        }
    
    
    private static Avatar BuildHumanAvatarMono (GameObject go, HumanDescription monoHumanDescription) {
        return INTERNAL_CALL_BuildHumanAvatarMono ( go, ref monoHumanDescription );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Avatar INTERNAL_CALL_BuildHumanAvatarMono (GameObject go, ref HumanDescription monoHumanDescription);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Avatar BuildGenericAvatar (GameObject go, string rootMotionTransformName) ;

}

}
