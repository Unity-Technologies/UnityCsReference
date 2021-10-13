// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEngine.Animations
{
    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Field)]
    public class DiscreteEvaluationAttribute : Attribute
    {
    }

    internal static class DiscreteEvaluationAttributeUtilities
    {
        public static int ConvertFloatToDiscreteInt(float f)
        {
            unsafe
            {
                float* fp = &f;
                int* i = (int*)fp;
                return *i;
            }
        }

        public static float ConvertDiscreteIntToFloat(int f)
        {
            unsafe
            {
                int* fp = &f;
                float* i = (float*)fp;
                return *i;
            }
        }
    }
}
