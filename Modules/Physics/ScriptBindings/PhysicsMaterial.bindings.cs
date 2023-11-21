// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    public enum PhysicsMaterialCombine
    {
        Average = 0,
        Multiply,
        Minimum,
        Maximum
    }

    [NativeHeader("Modules/Physics/PhysicsMaterial.h")]
    public class PhysicsMaterial : UnityEngine.Object
    {
        public PhysicsMaterial() { Internal_CreateDynamicsMaterial(this, "DynamicMaterial"); }
        public PhysicsMaterial(string name) { Internal_CreateDynamicsMaterial(this, name); }
        extern private static void Internal_CreateDynamicsMaterial([Writable] PhysicsMaterial mat, string name);

        extern public float bounciness { get; set; }
        extern public float dynamicFriction { get; set; }
        extern public float staticFriction { get; set; }
        extern public PhysicsMaterialCombine frictionCombine { get; set; }
        extern public PhysicsMaterialCombine bounceCombine { get; set; }
    }
}
