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


[UsedByNativeCode]
public sealed partial class AnimatorControllerParameter
{
    public string                             name
        {
            get { return m_Name; }
            set {   m_Name = value;     }

        }
    
    
    public int                                nameHash
        {
            get { return Animator.StringToHash(m_Name); }
        }
    
    
    public AnimatorControllerParameterType    type                            {   get { return m_Type; }                      set {  m_Type = value; } }
    public float                              defaultFloat                    {   get { return m_DefaultFloat; }              set {  m_DefaultFloat = value; } }
    public int                                defaultInt                      {   get { return m_DefaultInt; }                set {  m_DefaultInt = value; }   }
    public bool                               defaultBool                     {   get { return m_DefaultBool; }               set {  m_DefaultBool = value; }  }
    
    
            internal string                                 m_Name = "";
            internal AnimatorControllerParameterType        m_Type;
            internal float                                  m_DefaultFloat;
            internal int                                    m_DefaultInt;
            internal bool                                   m_DefaultBool;
    
    public override bool Equals(object o)
        {
            AnimatorControllerParameter other = o as AnimatorControllerParameter;
            return other != null && m_Name == other.m_Name && m_Type == other.m_Type && m_DefaultFloat == other.m_DefaultFloat && m_DefaultInt == other.m_DefaultInt && m_DefaultBool == other.m_DefaultBool;
        }
    
    public override int GetHashCode()
        {
            return name.GetHashCode();
        }
    
    
}


}
