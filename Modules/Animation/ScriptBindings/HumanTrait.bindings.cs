// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;

namespace UnityEngine
{
    ///<summary>Details of all the human bone and muscle types defined by Mecanim.</summary>
    [NativeHeader("Modules/Animation/HumanTrait.h")]
    public class HumanTrait
    {
        // Number of muscles
        ///<summary>The number of human muscle types defined by Mecanim.</summary>
        ///<seealso cref="HumanTrait.MuscleName" />
        extern public static int MuscleCount
        {
            get;
        }

        internal static int GetBoneIndexFromMono(int humanId)
        {
            if (humanId < 0 || humanId >= BoneCount)
                throw new ArgumentOutOfRangeException(nameof(humanId));
            return Internal_GetBoneIndexFromMono(humanId);
        }

        [NativeMethod("GetBoneIndexFromMono")]
        extern static int Internal_GetBoneIndexFromMono(int humanId);

        extern internal static int GetBoneIndexToMono(int boneIndex);

        // Muscle's name
        ///<summary>Array of the names of all human muscle types defined by Mecanim.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        string[] muscleName = HumanTrait.MuscleName;
        ///        for (int i = 0; i < HumanTrait.BoneCount; ++i)
        ///        {
        ///            Debug.Log(muscleName[i]);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public static string[] MuscleName
        {
            [NativeMethod("GetMuscleNames")]
            get;
        }

        // Number of bones
        ///<summary>The number of human bone types defined by Mecanim.</summary>
        ///<seealso cref="HumanTrait.BoneName" />
        extern public static int BoneCount
        {
            get;
        }

        // Bone's name
        ///<summary>Array of the names of all human bone types defined by Mecanim.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        string[] boneName = HumanTrait.BoneName;
        ///        for (int i = 0; i < HumanTrait.BoneCount; ++i)
        ///        {
        ///            Debug.Log(boneName[i]);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public static string[] BoneName
        {
            [NativeMethod("MonoBoneNames")]
            get;
        }

        // Return muscle index linked to bone i, dofIndex allow you to choose between X, Y and Z muscle's axis
        ///<summary>Obtain the muscle index for a particular bone index and "degree of freedom".</summary>
        ///<remarks>The indexing order of the bones is the same as that of the <see cref="BoneName" /> array.</remarks>
        ///<param name="i">Bone index.</param>
        ///<param name="dofIndex">Number representing a "degree of freedom": 0 for X-Axis, 1 for Y-Axis, 2 for Z-Axis.</param>
        ///<seealso cref="HumanTrait.BoneName" />
        ///<seealso cref="HumanTrait.BoneCount" />
        ///<seealso cref="HumanTrait.MuscleName" />
        ///<seealso cref="HumanTrait.MuscleCount" />
        public static int MuscleFromBone(int i, int dofIndex)
        {
            return Internal_MuscleFromBone(GetBoneIndexFromMono(i), dofIndex);
        }

        [NativeMethod("MuscleFromBone")]
        extern static int Internal_MuscleFromBone(int i, int dofIndex);

        // Return bone index linked to muscle i
        ///<summary>Return the bone to which a particular muscle is connected.</summary>
        ///<remarks>The bone and muscle indices used by this function are the same as those of the <see cref="BoneName" /> and <see cref="MuscleName" /> arrays respectively.</remarks>
        ///<param name="i">Muscle index.</param>
        static public int BoneFromMuscle(int i)
        {
            return GetBoneIndexToMono(Internal_BoneFromMuscle(i));
        }

        [NativeMethod("BoneFromMuscle")]
        extern static int Internal_BoneFromMuscle(int i);

        // Return true if bone i is a required bone.
        ///<summary>Is the bone a member of the minimal set of bones that Mecanim requires for a human model?</summary>
        ///<remarks>The indexing order of the bones is the same as that used for the <see cref="BoneName" /> array.</remarks>
        ///<param name="i">Index of the bone to test.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        string[] boneName = HumanTrait.BoneName;
        ///        for (int i = 0; i < HumanTrait.BoneCount; ++i)
        ///        {
        ///            if (HumanTrait.RequiredBone(i))
        ///                Debug.Log(boneName[i]);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool RequiredBone(int i)
        {
            return Internal_RequiredBone(GetBoneIndexFromMono(i));
        }

        [NativeMethod("RequiredBone")]
        extern static bool Internal_RequiredBone(int i);

        // Number of required bones.
        ///<summary>The number of bone types that are required by Mecanim for any human model.</summary>
        extern public static int RequiredBoneCount
        {
            [NativeMethod("RequiredBoneCount")]
            get;
        }

        // Return default minimum values for muscle.
        ///<summary>Get the default minimum value of rotation for a muscle in degrees.</summary>
        ///<remarks>
        ///  <para>The default minimum applies to all three axes of rotation for the muscle. The indexing order for the muscles is the same as that of the <see cref="MuscleName" /> array.</para>
        ///  <para />
        ///</remarks>
        ///<param name="i">Muscle index.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        string[] muscleName = HumanTrait.MuscleName;
        ///        for (int i = 0; i < HumanTrait.BoneCount; ++i)
        ///        {
        ///            Debug.Log(muscleName[i] + " min: " + HumanTrait.GetMuscleDefaultMin(i) + " max: " + HumanTrait.GetMuscleDefaultMax(i));
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="HumanLimit.min" />
        ///<seealso cref="HumanTrait.GetMuscleDefaultMax" />
        extern public static float GetMuscleDefaultMin(int i);

        // Return default maximum values for muscle.
        ///<summary>Get the default maximum value of rotation for a muscle in degrees.</summary>
        ///<remarks>
        ///  <para>The default maximum applies to all three axes of rotation for the muscle. The indexing order for the muscles is the same as that of the <see cref="MuscleName" /> array.</para>
        ///  <para />
        ///</remarks>
        ///<param name="i">Muscle index.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        string[] muscleName = HumanTrait.MuscleName;
        ///        for (int i = 0; i < HumanTrait.BoneCount; ++i)
        ///        {
        ///            Debug.Log(muscleName[i] + " min: " + HumanTrait.GetMuscleDefaultMin(i) + " max: " + HumanTrait.GetMuscleDefaultMax(i));
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="HumanLimit.max" />
        ///<seealso cref="HumanTrait.GetMuscleDefaultMin" />
        extern public static float GetMuscleDefaultMax(int i);

        // Return bone hierarchy mass
        ///<summary>Gets the bone hierarchy mass.</summary>
        ///<remarks>The default bone mass is used to compute the mass center. The default bone mass is an approximation based on the weight of a human with normal proportions.</remarks>
        ///<param name="i">The humanoid bone index.</param>
        ///<returns>The bone hierarchy mass.</returns>
        ///<seealso cref="Animator.bodyPosition" />
        ///<seealso cref="P:UnityEngine.Animations.AnimationHumanStream.bodyPosition" />
        ///<seealso cref="P:UnityEngine.Animations.AnimationHumanStream.bodyLocalPosition" />
        static public float GetBoneDefaultHierarchyMass(int i)
        {
            return Internal_GetBoneHierarchyMass(GetBoneIndexFromMono(i));
        }

        // Return parent human bone id
        ///<summary>Returns parent humanoid bone index of a bone.</summary>
        ///<param name="i">Humanoid bone index to get parent from.</param>
        ///<returns>Humanoid bone index of parent.</returns>
        static public int GetParentBone(int i)
        {
            int parentIndex = Internal_GetParent(GetBoneIndexFromMono(i));
            return parentIndex != -1 ? GetBoneIndexToMono(parentIndex) : -1;
        }

        [NativeMethod("GetBoneHierarchyMass")]
        extern static float Internal_GetBoneHierarchyMass(int i);

        [NativeMethod("GetParent")]
        extern static int Internal_GetParent(int i);
    }
}
