// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    public enum RuntimeInitializeLoadType
    {
        AfterSceneLoad  = 0,
        BeforeSceneLoad
    };

    [System.AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RuntimeInitializeOnLoadMethodAttribute : Scripting.PreserveAttribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { this.loadType = RuntimeInitializeLoadType.AfterSceneLoad; }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { this.loadType = loadType; }

        public RuntimeInitializeLoadType loadType { get; private set; }
    }
}
