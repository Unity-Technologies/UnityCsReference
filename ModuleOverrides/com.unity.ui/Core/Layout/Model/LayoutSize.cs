// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEngine.UIElements.Layout;

[StructLayout(LayoutKind.Sequential)]
struct LayoutSize
{
    public float width;
    public float height;

    public LayoutSize(float width, float height)
    {
        this.width = width;
        this.height = height;
    }
}
