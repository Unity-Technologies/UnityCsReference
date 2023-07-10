// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEngine.UIElements;

unsafe struct VisualNodePropertyData
{
    public void* Ptr;
}

[StructLayout(LayoutKind.Sequential)]
unsafe struct VisualNodeProperty<T> where T : unmanaged
{
    readonly VisualNodePropertyData* m_Data;

    internal VisualNodeProperty(VisualNodePropertyData* data)
        => m_Data = data;

    public ref T this[VisualNodeHandle handle]
    {
        get
        {
            Debug.Assert(handle.Id > 0);
            return ref ((T*)m_Data->Ptr)[handle.Id - 1];
        }
    }
}
