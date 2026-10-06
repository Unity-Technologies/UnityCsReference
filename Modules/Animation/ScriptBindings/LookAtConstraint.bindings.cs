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
    ///<remarks>This constraint is similar to <see cref="Animations.AimConstraint" /> but is meant for constraining a <see cref="Camera" />. Use this constraint to point a <see cref="Camera" /> towards other sources. You can also adjust the weight of each source to influence whether the <see cref="Camera" /> points more towards one source over another source. 
    ///
    ///Make sure you use <c>LookAtConstraint</c> properties to align the orientation of the constraint. Otherwise, the constrained <see cref="Camera" /> might point in the wrong direction.
    ///
    ///Refer to [Look At Constraint Component](xref:class-LookAtConstraint) for more details.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/LookAtConstraint.cs}]]></code>
    ///</example>
    ///<seealso cref="ConstraintSource" />
    ///<seealso cref="AimConstraint" />
    ///<seealso cref="ParentConstraint" />
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Animation/Constraints/LookAtConstraint.h")]
    [NativeHeader("Modules/Animation/Constraints/Constraint.bindings.h")]
    [NativeClass("LookAtConstraint", PersistentTypeId = 0x4683850F)]
    public sealed partial class LookAtConstraint : Behaviour, IConstraint, IConstraintInternal
    {
        internal LookAtConstraint(global::UnityEngine.EntityId id) : base(id) {}
        LookAtConstraint()
        {
            SetEntityIdFromConstructor(Internal_Create());
        }

        private static extern EntityId Internal_Create();

        ///<summary>The weight of the constraint component.</summary>
        public extern float weight { get; set; }

        ///<summary>The rotation angle along the z axis of the object. The constraint uses this property to calculate the world up vector when <see cref="P:UnityEngine.Animations.LookAtConstraint.useUpObject" /> is false.</summary>
        public extern float roll { get; set; }

        ///<summary>Activates or deactivates the constraint.</summary>
        public extern bool constraintActive { get; set; }
        ///<summary>Locks the offset and rotation at rest.</summary>
        ///<remarks>In Edit mode, unlocks the constraint to update its offsets. In Play mode, the constraint is always locked.</remarks>
        public extern bool locked { get; set; }

        ///<summary>The rotation used when the sources have a total weight of 0.</summary>
        public extern Vector3 rotationAtRest { get; set; }

        ///<summary>Represents an offset from the constrained orientation.</summary>
        public extern Vector3 rotationOffset { get; set; }

        ///<summary>The world up object, used to calculate the world up vector when <see cref="P:UnityEngine.Animations.LookAtConstraint.useUpObject" /> is true.</summary>
        public extern Transform worldUpObject { get; set; }

        ///<summary>Determines how the up vector is calculated.</summary>
        ///<remarks>When set to true, the constraint uses <see cref="Animations.LookAtConstraint.worldUpObject" /> to calculate the up vector.
        ///                When set to false, the constraint uses the world's Y axis rotated by <see cref="Animations.LookAtConstraint.roll" /> as the up vector.</remarks>
        public extern bool useUpObject { get; set; }

        ///<summary>The number of sources set on the component (RO).</summary>
        public int sourceCount { get { return GetSourceCountInternal(this); } }
        [FreeFunction("ConstraintBindings::GetSourceCount")]
        private static extern int GetSourceCountInternal([NotNull] LookAtConstraint self);

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
        private static extern void SetSourcesInternal([NotNull] LookAtConstraint self, [In] List<ConstraintSource> sources);

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
        ///<returns>Returns the source object and its weight.</returns>
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
                throw new InvalidOperationException("The LookAtConstraint component has no sources.");
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
