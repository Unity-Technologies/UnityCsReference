// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <undoc/>
    internal readonly struct PhysicsEditorOnly
    {
        /// <undoc/>
        public static void ReadProjectSettings() => PhysicsGlobal_ReadProjectSettings();

        /// <undoc/>
        public static PhysicsCoreSettings2D physicsSettings => PhysicsGlobal_GetPhysicsCoreSettings() as PhysicsCoreSettings2D;
    }
}

