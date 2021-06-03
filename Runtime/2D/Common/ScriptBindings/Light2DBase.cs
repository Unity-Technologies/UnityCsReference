// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.U2D
{
    public abstract class Light2DBase : MonoBehaviour
    {
    }

    /// <summary>
    /// This has to match Light2D.LightType from the Graphics repository
    /// </summary>
    internal enum Light2DType
    {
        Parametric = 0,
        Freeform = 1,
        Sprite = 2,
        Point = 3,
        Global = 4
    }
}
