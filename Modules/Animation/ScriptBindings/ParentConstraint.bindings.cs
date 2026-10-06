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
    ///<summary>Constrains the transformation of a GameObject based on the position and rotation of one or more sources.</summary>
    ///<remarks>Use this constraint to move and rotate a GameObject based on the position and rotation of other sources. For example, you can use this constraint to place a sword in the hand of a character. You can also adjust the weight of each source to influence whether the constrained GameObject follows the position and rotation of one source over another source.
    ///
    ///Refer to [Parent Constraint Component](xref:class-ParentConstraint) for more details.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/ParentConstraint.cs}]]></code>
    ///</example>
    ///<seealso cref="ConstraintSource" />
    ///<seealso cref="LookAtConstraint" />
    ///<seealso cref="AimConstraint" />
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Animation/Constraints/ParentConstraint.h")]
    [NativeHeader("Modules/Animation/Constraints/Constraint.bindings.h")]
    [NativeClass("ParentConstraint", PersistentTypeId = 0x69B45D86)]
    public sealed partial class ParentConstraint : Behaviour, IConstraint, IConstraintInternal
    {
        internal ParentConstraint(global::UnityEngine.EntityId id) : base(id) {}
        ParentConstraint()
        {
            SetEntityIdFromConstructor(Internal_Create());
        }

        private static extern EntityId Internal_Create();

        ///<summary>The weight of the constraint component.</summary>
        public extern float weight { get; set; }

        ///<summary>Activates or deactivates the constraint.</summary>
        public extern bool constraintActive { get; set; }
        ///<summary>Locks the offsets and position (translation and rotation) at rest.</summary>
        ///<remarks>In Edit mode, unlocks the constraint to update its offsets. In Play mode, the constraint is always locked.</remarks>
        public extern bool locked { get; set; }

        ///<summary>The number of sources set on the component (read-only).</summary>
        public int sourceCount { get { return GetSourceCountInternal(this); } }
        [FreeFunction("ConstraintBindings::GetSourceCount")]
        private static extern int GetSourceCountInternal([NotNull] ParentConstraint self);

        ///<summary>The position of the object in local space, used when the sources have a total weight of 0.</summary>
        public extern Vector3 translationAtRest { get; set; }
        ///<summary>The rotation used when the sources have a total weight of 0.</summary>
        public extern Vector3 rotationAtRest { get; set; }

        ///<summary>The translation offsets from the constrained orientation.</summary>
        ///<remarks>The translation offsets are relative to the source position local space.</remarks>
        public extern Vector3[] translationOffsets { get; set; }
        ///<summary>The rotation offsets from the constrained orientation.</summary>
        ///<remarks>The rotation offsets are relative to the source orientations.</remarks>
        public extern Vector3[] rotationOffsets { get; set; }

        ///<summary>The translation axes affected by the ParentConstraint.</summary>
        ///<remarks>Use this property to restrict the translation of the constrained object on a particular axis.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine.Animations;
        ///
        ///public class ConstraintAxis
        ///{
        ///    public void ConstrainOnlyOnXY(ParentConstraint component)
        ///    {
        ///        component.translationAxis = Axis.X | Axis.Y;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern Axis translationAxis { get; set; }
        ///<summary>The rotation axes affected by the ParentConstraint.</summary>
        ///<remarks>Use this property to restrict the rotation of the constrained object on a particular axis.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine.Animations;
        ///
        ///public class ConstraintAxis
        ///{
        ///    public void ConstrainOnlyOnXY(ParentConstraint component)
        ///    {
        ///        component.rotationAxis = Axis.X | Axis.Y;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern Axis rotationAxis { get; set; }

        ///<summary>Gets the rotation offset associated with a source by index.</summary>
        ///<remarks>Throws InvalidOperationException, if the list of sources is empty. Throws ArgumentOutOfRangeException, if the index is invalid.</remarks>
        ///<param name="index">The index of the constraint source.</param>
        ///<returns>The translation offset.</returns>
        public Vector3 GetTranslationOffset(int index)
        {
            ValidateSourceIndex(index);
            return GetTranslationOffsetInternal(index);
        }

        ///<summary>Sets the translation offset associated with a source by index.</summary>
        ///<remarks>Throws InvalidOperationException, if the list of sources is empty. Throws ArgumentOutOfRangeException, if the index is invalid.</remarks>
        ///<param name="index">The index of the constraint source.</param>
        ///<param name="value">The new translation offset.</param>
        public void SetTranslationOffset(int index, Vector3 value)
        {
            ValidateSourceIndex(index);
            SetTranslationOffsetInternal(index, value);
        }

        [NativeName("GetTranslationOffset")]
        private extern Vector3 GetTranslationOffsetInternal(int index);
        [NativeName("SetTranslationOffset")]
        private extern void SetTranslationOffsetInternal(int index, Vector3 value);

        ///<summary>Gets the rotation offset associated with a source by index.</summary>
        ///<remarks>Throws InvalidOperationException, if the list of sources is empty. Throws ArgumentOutOfRangeException, if the index is invalid.</remarks>
        ///<param name="index">The index of the constraint source.</param>
        ///<returns>The rotation offset, as Euler angles.</returns>
        public Vector3 GetRotationOffset(int index)
        {
            ValidateSourceIndex(index);
            return GetRotationOffsetInternal(index);
        }

        ///<summary>Sets the rotation offset associated with a source by index.</summary>
        ///<remarks>Throws InvalidOperationException, if the list of sources is empty. Throws ArgumentOutOfRangeException, if the index is invalid.</remarks>
        ///<param name="index">The index of the constraint source.</param>
        ///<param name="value">The new rotation offset.</param>
        public void SetRotationOffset(int index, Vector3 value)
        {
            ValidateSourceIndex(index);
            SetRotationOffsetInternal(index, value);
        }

        [NativeName("GetRotationOffset")]
        private extern Vector3 GetRotationOffsetInternal(int index);
        [NativeName("SetRotationOffset")]
        private extern void SetRotationOffsetInternal(int index, Vector3 value);

        private void ValidateSourceIndex(int index)
        {
            if (sourceCount == 0)
            {
                throw new InvalidOperationException("The ParentConstraint component has no sources.");
            }

            if (index < 0 || index >= sourceCount)
            {
                throw new ArgumentOutOfRangeException("index", string.Format("Constraint source index {0} is out of bounds (0-{1}).", index, sourceCount));
            }
        }

        ///<summary>Gets the list of sources.</summary>
        ///<remarks>Throws ArgumentNullException, if the list of sources is null.</remarks>
        ///<param name="sources">The list of sources filled by the component.</param>
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
        private static extern void SetSourcesInternal([NotNull] ParentConstraint self, [In] List<ConstraintSource> sources);

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
        ///<remarks>Throws InvalidOperationException, if the list of sources is empty. Throws ArgumentOutOfRangeException, if the index is invalid.</remarks>
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

        extern void ActivateAndPreserveOffset();
        extern void ActivateWithZeroOffset();
        extern void UserUpdateOffset();

        void IConstraintInternal.ActivateAndPreserveOffset()
        {
            ActivateAndPreserveOffset();
        }

        void IConstraintInternal.ActivateWithZeroOffset()
        {
            ActivateWithZeroOffset();
        }

        void IConstraintInternal.UserUpdateOffset()
        {
            UserUpdateOffset();
        }

        Transform IConstraintInternal.transform
        {
            get { return this.transform; }
        }
    }
}
