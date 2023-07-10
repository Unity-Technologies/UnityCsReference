// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    internal abstract class UIElementsBridge
    {
        public abstract void SetWantsMouseJumping(int value);
    }

    internal class RuntimeUIElementsBridge : UIElementsBridge
    {
        public override void SetWantsMouseJumping(int value)
        {
        }
    }
}
