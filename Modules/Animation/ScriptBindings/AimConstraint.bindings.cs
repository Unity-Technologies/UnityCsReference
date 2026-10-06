// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine.Animations
{
    ///<summary>Constrains the orientation of a GameObject based on the position of one or more sources.</summary>
    ///<remarks>Use this constraint to aim a GameObject towards one or more sources. For example, when you want to aim a turret towards many targets. You can also adjust the weight of each source to influence whether the constrained GameObject aims more towards one source over another source. 
    ///
    ///Make sure you use <c>AimConstraint</c> properties to align the orientation of the constraint. Otherwise, the constrained GameObject might aim in the wrong direction.
    ///
    ///Refer to [Aim Constraint Component](xref:class-AimConstraint) for more details.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/AimConstraint.cs}]]></code>
    ///</example>
    ///<seealso cref="ConstraintSource" />
    ///<seealso cref="LookAtConstraint" />
    ///<seealso cref="ParentConstraint" />
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Animation/Constraints/AimConstraint.h")]
    [NativeHeader("Modules/Animation/Constraints/Constraint.bindings.h")]
    [NativeClass("AimConstraint", PersistentTypeId = 0x35606F27)]
    public sealed partial class AimConstraint : Behaviour, IConstraint, IConstraintInternal
    {
        internal AimConstraint(global::UnityEngine.EntityId id) : base(id) {}
        ///<summary>Specifies how the world up vector used by the aim constraint is defined.</summary>
        public enum WorldUpType
        {
            ///<summary>Uses and defines the world up vector as the Unity Scene up vector (the Y axis).</summary>
            SceneUp,
            ///<summary>Uses and defines the world up vector as a vector from the constrained object, in the direction of the up object.</summary>
            ///<remarks>When using this world up type, the up vector of the constrained object tries to aim towards the origin of the up object.
            ///
            ///If the up object is not set, the world up vector points towards the origin of the Scene.</remarks>
            ObjectUp,
            ///<summary>Uses and defines the world up vector as relative to the local space of the object.</summary>
            ///<remarks>If the object is not set, the world up vector is defined relative to the Scene up vector.</remarks>
            ObjectRotationUp,
            ///<summary>Uses and defines the world up vector as a vector specified by the user.</summary>
            Vector,
            ///<summary>Neither defines nor uses a world up vector.</summary>
            None
        }

        AimConstraint()
        {
            SetEntityIdFromConstructor(Internal_Create());
        }

        private static extern EntityId Internal_Create();

        ///<summary>The weight of the constraint component.</summary>
        public extern float weight { get; set; }

        ///<summary>Activates or deactivates the constraint.</summary>
        public extern bool constraintActive { get; set; }
        ///<summary>Locks the offset and rotation at rest.</summary>
        ///<remarks>In Edit mode, unlocks the constraint to update its offsets. In Play mode, the constraint is always locked.</remarks>
        public extern bool locked { get; set; }

        ///<summary>The rotation used when the sources have a total weight of 0.</summary>
        public extern Vector3 rotationAtRest { get; set; }

        ///<summary>Represents an offset from the constrained orientation.</summary>
        public extern Vector3 rotationOffset { get; set; }
        ///<summary>The axes affected by the AimConstraint.</summary>
        ///<remarks>Use this property to restrict the effect of the constraint on a particular axis.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine.Animations;
        ///
        ///public class ConstraintAxis
        ///{
        ///    public void ConstrainOnlyOnXY(AimConstraint component)
        ///    {
        ///        component.rotationAxis = Axis.X | Axis.Y;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern Axis rotationAxis { get; set; }

        ///<summary>The axis towards which the constrained object orients.</summary>
        public extern Vector3 aimVector { get; set; }
        ///<summary>The up vector.</summary>
        ///<remarks>The up vector controls the rotation of the constrained object about the aim vector. It is defined in the constrained object local space.</remarks>
        public extern Vector3 upVector { get; set; }
        ///<summary>The world up Vector used when the world up type is <see cref="AimConstraint.WorldUpType.Vector" /> or <see cref="AimConstraint.WorldUpType.ObjectRotationUp" />.</summary>
        public extern Vector3 worldUpVector { get; set; }
        ///<summary>The world up object, used to calculate the world up vector when the world up Type is <see cref="AimConstraint.WorldUpType.ObjectUp" /> or <see cref="AimConstraint.WorldUpType.ObjectRotationUp" />.</summary>
        public extern Transform worldUpObject { get; set; }
        ///<summary>The type of the world up vector.</summary>
        ///<seealso cref="AimConstraint.WorldUpType" />
        public extern WorldUpType worldUpType { get; set; }

        ///<summary>The number of sources set on the component (read-only).</summary>
        public int sourceCount { get { return GetSourceCountInternal(this); } }
        [FreeFunction("ConstraintBindings::GetSourceCount")]
        private static extern int GetSourceCountInternal([NotNull] AimConstraint self);

        ///<summary>Gets the list of sources.</summary>
        ///<remarks>Throws ArgumentNullException, if the list of sources is null.</remarks>
        ///<param name="sources">The list of sources to be filled by the component.</param>
        [FreeFunction(Name = "ConstraintBindings::GetSources", HasExplicitThis = true)]
        public extern void GetSources([NotNull][Out] List<ConstraintSource> sources);

        ///<summary>Sets the list of sources on the component.</summary>
        ///<remarks>Throws ArgumentNullException, if the list of sources is null.</remarks>
        ///<param name="sources">The list of sources to set.</param>
        public void SetSources(List<ConstraintSource> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            SetSourcesInternal(this, sources);
        }

        [FreeFunction("ConstraintBindings::SetSources", ThrowsException = true)]
        private static extern void SetSourcesInternal([NotNull] AimConstraint self, [In] List<ConstraintSource> sources);

        ///<summary>Adds a constraint source.</summary>
        ///<param name="source">The source object and its weight.</param>
        ///<returns>Returns the index of the added source.</returns>
        public extern int AddSource(ConstraintSource source);

        ///<summary>Removes a source from the component.</summary>
        ///<remarks>Throws InvalidOperationException, if the list of sources is empty. Throws ArgumentOutOfRangeException, if the index is invalid.</remarks>
        ///<param name="index">The index of the source to remove.</param>
        public void RemoveSource(int index)
        {
            ValidateSourceIndex(index);
            RemoveSourceInternal(index);
        }

        [NativeName("RemoveSource")]
        private extern void RemoveSourceInternal(int index);

        ///<summary>Gets a constraint source by index.</summary>
        ///<remarks>Throws ArgumentOutOfRangeException, if the index is invalid.</remarks>
        ///<param name="index">The index of the source.</param>
        ///<returns>The source object and its weight.</returns>
        public ConstraintSource GetSource(int index)
        {
            ValidateSourceIndex(index);
            return GetSourceInternal(index);
        }

        [NativeName("GetSource")]
        private extern ConstraintSource GetSourceInternal(int index);

        ///<summary>Sets a source at a specified index.</summary>
        ///<remarks>Throws ArgumentOutOfRangeException, if the index is invalid.</remarks>
        ///<param name="index">The index of the source to set.</param>
        ///<param name="source">The source object and its weight.</param>
        public void SetSource(int index, ConstraintSource source)
        {
            ValidateSourceIndex(index);
            SetSourceInternal(index, source);
        }

        [NativeName("SetSource")]
        private extern void SetSourceInternal(int index, ConstraintSource source);

        private void ValidateSourceIndex(int index)
        {
            if (sourceCount == 0)
            {
                throw new InvalidOperationException("The AimConstraint component has no sources.");
            }

            if (index < 0 || index >= sourceCount)
            {
                throw new ArgumentOutOfRangeException("index", string.Format("Constraint source index {0} is out of bounds (0-{1}).", index, sourceCount));
            }
        }

        extern void ActivateAndPreserveOffset();
        extern void ActivateWithZeroOffset();
        extern void UserUpdateOffset();

        void IConstraintInternal.ActivateAndPreserveOffset()
        {
            this.ActivateAndPreserveOffset();
        }

        void IConstraintInternal.ActivateWithZeroOffset()
        {
            this.ActivateWithZeroOffset();
        }

        void IConstraintInternal.UserUpdateOffset()
        {
            this.UserUpdateOffset();
        }

        Transform IConstraintInternal.transform
        {
            get { return this.transform; }
        }
    }
}
