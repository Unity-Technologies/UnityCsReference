// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEditor.LowLevelPhysics2D
{
    /// <undoc/>
    internal readonly struct PhysicsEditor
    {
        /// <undoc/>
        public static void ReadProjectSettings() => PhysicsGlobal_ReadProjectSettings();

        /// <undoc/>
        public static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelSettings2D lowLevelSettings => PhysicsGlobal_GetPhysicsLowLevelSettings() as UnityEngine.LowLevelPhysics2D.PhysicsLowLevelSettings2D;
    }
}

