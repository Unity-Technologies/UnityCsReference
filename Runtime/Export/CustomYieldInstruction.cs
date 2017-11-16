// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;

namespace UnityEngine
{
    public abstract class CustomYieldInstruction : IEnumerator
    {
        public abstract bool keepWaiting
        {
            get;
        }

        public object Current
        {
            get
            {
                return null;
            }
        }
        public bool   MoveNext() { return keepWaiting; }
        public void   Reset() {}
    }
}
