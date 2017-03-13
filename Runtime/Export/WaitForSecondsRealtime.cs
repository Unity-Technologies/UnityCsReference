// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    public class WaitForSecondsRealtime : CustomYieldInstruction
    {
        private float waitTime;

        public override bool keepWaiting
        {
            get { return Time.realtimeSinceStartup < waitTime; }
        }

        public WaitForSecondsRealtime(float time)
        {
            waitTime = Time.realtimeSinceStartup + time;
        }
    }
}
