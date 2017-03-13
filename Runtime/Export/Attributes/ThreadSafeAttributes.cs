// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    internal class ThreadAndSerializationSafeAttribute : System.Attribute
    {
        public ThreadAndSerializationSafeAttribute()
        {
        }
    }

    // TODO(rb): This class is temporary, and should be removed when we throw on all threading
    //           errors in bindings.
    internal class ThreadAndSerializationUnsafeThrowExceptionAttribute : System.Attribute
    {
        public ThreadAndSerializationUnsafeThrowExceptionAttribute()
        {
        }
    }
}
