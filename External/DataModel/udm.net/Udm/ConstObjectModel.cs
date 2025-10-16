using System;
using System.Runtime.InteropServices;

using udm_const_object_model_ptr = System.IntPtr;

namespace Unity.DataModel
{
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ConstObjectModel
{
    public static implicit operator ConstObjectModel(ObjectModel objectModel)
    {
        return new ConstObjectModel
        {
            ObjectId = objectModel.ObjectId,
            Accessor = objectModel.Accessor
        };
    }

    internal ConstAccessor GetAccessor()
    {
        return Accessor;
    }
    
    internal Schema GetSchema()
    {
        return Accessor.Schema;
    }

    internal UdmObjectId GetObjectID()
    {
        return ObjectId;
    }

    internal bool IsValid()
    {
        unsafe
        {
            return Accessor.Data != IntPtr.Zero;
        }
    }

    internal void ThrowIfInvalid()
    {
        if (!IsValid())
            throw new InvalidOperationException("Trying to use an invalid ObjectModel");
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    internal UdmObjectId ObjectId;
    internal ConstAccessor Accessor;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
}
}
