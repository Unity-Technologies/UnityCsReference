// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor
{
    // Silly helper to get proper deltatime inside events
    internal struct TimeHelper
    {
        public float deltaTime;
        long lastTime;

        public void Begin()
        {
            lastTime = System.DateTime.Now.Ticks;
        }

        public float Update()
        {
            deltaTime = (System.DateTime.Now.Ticks - lastTime) / 10000000.0f;
            lastTime = System.DateTime.Now.Ticks;
            return deltaTime;
        }
    }
}
