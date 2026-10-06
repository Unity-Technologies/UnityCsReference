// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    ///<summary>Describes how physics materials of the colliding objects are combined.
    ///
    ///The friction force as well as the residual bounce impulse are applied symmetrically to both of the colliders in contact, so each pair of overlapping colliders must have a single set of friction and bouciness settings. However, one can set arbitrary physics materials to any colliders. For that particular reason, there is a mechanism that allows the combination of two different sets of properties that correspond to each of the colliders in contact into one set to be used in the solver.
    ///
    ///Specifying Average, Maximum, Minimum or Multiply as the physics material combine mode, you directly set the function that is used to combine the settings corresponding to the two overlapping colliders into one set of settings that can be used to apply the material effect.
    ///
    ///Note that there is a special case when the two overlapping colliders have physics materials with different combine modes set. In this particular case, the function that has the highest priority is used. The priority order is as follows: Average &lt; Minimum &lt; Multiply &lt; Maximum. For example, if one material has Average set but the other one has Maximum, then the combine function to be used is Maximum, since it has higher priority.</summary>
    ///<seealso cref="PhysicsMaterial.frictionCombine" />
    ///<seealso cref="PhysicsMaterial.bounceCombine" />
    public enum PhysicsMaterialCombine
    {
        ///<summary>Averages the friction/bounce of the two colliding materials.</summary>
        Average = 0,
        ///<summary>Multiplies the friction/bounce of the two colliding materials.</summary>
        Multiply,
        ///<summary>Uses the smaller friction/bounce of the two colliding materials.</summary>
        Minimum,
        ///<summary>Uses the larger friction/bounce of the two colliding materials.</summary>
        Maximum
    }

    ///<summary>Physics material describes how to handle colliding objects (friction, bounciness).</summary>
    ///<seealso cref="Collider" />
    [global::UnityEngine.NativeClass("PhysicsMaterial", PersistentTypeId = 134)]
    [NativeHeader("Modules/Physics/PhysicsMaterial.h")]
    public class PhysicsMaterial : UnityEngine.Object
    {
        internal PhysicsMaterial(global::UnityEngine.EntityId id) : base(id) {}
        ///<summary>Creates a new material.</summary>
        ///<remarks>Note that although this function lets you create a new physics material from a script, it is generally easier to create and assign the material from the editor.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Collider coll;
        ///
        ///    void Start()
        ///    {
        ///        coll = GetComponent<Collider>();
        ///
        ///        PhysicsMaterial material = new PhysicsMaterial();
        ///        material.dynamicFriction = 1;
        ///        coll.material = material;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public PhysicsMaterial() { SetEntityIdFromConstructor(Internal_CreateDynamicsMaterial("DynamicMaterial")); }
        ///<summary>Creates a new material named <c>name</c>.</summary>
        public PhysicsMaterial(string name) { SetEntityIdFromConstructor(Internal_CreateDynamicsMaterial(name)); }
        extern private static EntityId Internal_CreateDynamicsMaterial(string name);

        ///<summary>How bouncy is the surface? A value of 0 will not bounce. A value of 1 will bounce without any loss of energy.</summary>
        extern public float bounciness { get; set; }
        ///<summary>The friction used when already moving.  This value is usually between 0 and 1.</summary>
        ///<remarks>A value of 0 feels like ice, 1 feels like rubber.</remarks>
        extern public float dynamicFriction { get; set; }
        ///<summary>The friction coefficient used when an object is lying on a surface.</summary>
        ///<remarks>Must be greater than or equal to zero. Natural materials will usually have a friction coefficient between 0 (no friction at all, like slippery ice) and 1 (full friction, like rubber). Values larger than 1 are possible, and may be realistic for sticky materials.</remarks>
        extern public float staticFriction { get; set; }
        ///<summary>Determines how the friction is combined.</summary>
        ///<remarks>Traditionally friction properties are dependent on the combination of the two materials in contact.
        ///This is however impractical in a game. Instead you can use the combine mode to tune how the friction values of two materials are combined.</remarks>
        extern public PhysicsMaterialCombine frictionCombine { get; set; }
        ///<summary>Determines how the bounciness is combined.</summary>
        ///<remarks>Traditionally bounciness properties are dependent on the combination of the two materials in contact.
        ///This is however impractical in a game. Instead you can use the combine mode to tune how the bounciness values of two materials are combined.</remarks>
        extern public PhysicsMaterialCombine bounceCombine { get; set; }
    }
}
