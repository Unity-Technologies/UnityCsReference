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
    ///<summary>Represents the axes used in 3D space.</summary>
    [NativeHeader("Modules/Animation/Constraints/ConstraintEnums.h")]
    [Flags]
    public enum Axis
    {
        ///<summary>Represents the case when no axis is specified.</summary>
        None = 0,
        ///<summary>Represents the X axis.</summary>
        X = 1,
        ///<summary>Represents the Y axis.</summary>
        Y = 2,
        ///<summary>Represents the Z axis.</summary>
        Z = 4
    }

    ///<summary>Represents a weighted position that can be used in a constraint.</summary>
    ///<remarks>Use this struct to provide a weighted position to a constraint that implements the <see cref="IConstraint" /> interface.
    ///You can use many constraint sources in a constraint. To adjust the effect these sources have on the constraint, set the <see cref="ConstraintSource.weight">weight</see> parameter.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/ConstraintSourceSwitcher.cs}]]></code>
    ///</example>
    ///<seealso cref="IConstraint" />
    ///<seealso cref="PositionConstraint" />
    ///<seealso cref="RotationConstraint" />
    ///<seealso cref="ScaleConstraint" />
    ///<seealso cref="AimConstraint" />
    ///<seealso cref="ParentConstraint" />
    [System.Serializable]
    [NativeHeader("Modules/Animation/Constraints/ConstraintSource.h")]
    [NativeHeader("Modules/Animation/Constraints/Constraint.bindings.h")]
    [UsedByNativeCode]
    public struct ConstraintSource
    {
        [NativeName("sourceTransform")]
        private Transform m_SourceTransform;
        [NativeName("weight")]
        private float m_Weight;

        ///<summary>The transform component of the source object.</summary>
        public Transform sourceTransform { get { return m_SourceTransform; } set { m_SourceTransform = value; } }
        ///<summary>The weight of the source in the evaluation of the constraint.</summary>
        public float weight { get { return m_Weight; } set { m_Weight = value; } }
    }

    ///<summary>The common interface for constraint components.</summary>
    public interface IConstraint
    {
        ///<summary>The weight of the constraint component.</summary>
        float weight { get; set; }

        ///<summary>Activate or deactivate the constraint.</summary>
        bool constraintActive { get; set; }
        ///<summary>Lock or unlock the offset and position at rest.</summary>
        ///<remarks>In Edit mode, unlock the constraint to update its offsets. In Play mode, the constraint is always locked.</remarks>
        bool locked { get; set; }

        ///<summary>Gets the number of sources currently set on the component.</summary>
        int sourceCount { get; }

        ///<summary>Add a constraint source.</summary>
        ///<param name="source">The source object and its weight.</param>
        ///<returns>Returns the index of the added source.</returns>
        int AddSource(ConstraintSource source);
        ///<summary>Removes a source from the component.</summary>
        ///<remarks>Throws InvalidOperationException, if the list of sources is empty. Throws ArgumentOutOfRangeException, if the index is invalid.</remarks>
        ///<param name="index">The index of the source to remove.</param>
        void RemoveSource(int index);
        ///<summary>Gets a constraint source by index.</summary>
        ///<remarks>Throws ArgumentOutOfRangeException, if the index is invalid.</remarks>
        ///<param name="index">The index of the source.</param>
        ///<returns>The source object and its weight.</returns>
        ConstraintSource GetSource(int index);
        ///<summary>Sets a source at a specified index.</summary>
        ///<remarks>Throws ArgumentOutOfRangeException, if the index is invalid.</remarks>
        ///<param name="index">The index of the source to set.</param>
        ///<param name="source">The source object and its weight.</param>
        void SetSource(int index, ConstraintSource source);

        ///<summary>Gets the list of sources.</summary>
        ///<remarks>Throws ArgumentNullException, if the list of sources is null.</remarks>
        ///<param name="sources">The list of sources to be filled by the component.</param>
        void GetSources(List<ConstraintSource> sources);
        ///<summary>Sets the list of sources on the component.</summary>
        ///<remarks>Throws ArgumentNullException, if the list of sources is null.</remarks>
        ///<param name="sources">The list of sources to set.</param>
        void SetSources(List<ConstraintSource> sources);
    }

    internal interface IConstraintInternal
    {
        void ActivateAndPreserveOffset();
        void ActivateWithZeroOffset();
        void UserUpdateOffset();
        Transform transform { get; }
    }

    ///<summary>Constrains the position of a GameObject based on the position of one or more sources.</summary>
    ///<remarks>Use this constraint to move a GameObject based on the position of other sources. For example, you can use this constraint to align collectibles with a moving player. You can also adjust the weight of each source to influence whether the constrained GameObject follows the position of one source over another source. 
    ///
    ///Refer to [Position Constraint Component](xref:class-PositionConstraint) for more details.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/PositionConstraint.cs}]]></code>
    ///</example>
    ///<seealso cref="ConstraintSource" />
    ///<seealso cref="RotationConstraint" />
    ///<seealso cref="ScaleConstraint" />
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Animation/Constraints/PositionConstraint.h")]
    [NativeHeader("Modules/Animation/Constraints/Constraint.bindings.h")]
    [NativeClass("PositionConstraint", PersistentTypeId = 0x6C61FB20)]
    public sealed partial class PositionConstraint : Behaviour, IConstraint, IConstraintInternal
    {
        internal PositionConstraint(global::UnityEngine.EntityId id) : base(id) {}
        PositionConstraint()
        {
            SetEntityIdFromConstructor(Internal_Create());
        }

        private static extern EntityId Internal_Create();

        ///<summary>The weight of the constraint component.</summary>
        public extern float weight { get; set; }

        ///<summary>The translation used when the sources have a total weight of 0.</summary>
        public extern Vector3 translationAtRest { get; set; }

        ///<summary>The offset from the constrained position.</summary>
        public extern Vector3 translationOffset { get; set; }

        ///<summary>The axes affected by the PositionConstraint.</summary>
        ///<remarks>Use this property to restrict the effect of the constraint on a particular axis.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine.Animations;
        ///
        ///public class ConstraintAxis
        ///{
        ///    public void ConstrainOnlyOnXY(PositionConstraint component)
        ///    {
        ///        component.translationAxis = Axis.X | Axis.Y;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern Axis translationAxis { get; set; }

        ///<summary>Activates or deactivates the constraint.</summary>
        public extern bool constraintActive { get; set; }
        ///<summary>Locks the offset and position at rest.</summary>
        ///<remarks>In Edit mode, unlocks the constraint to update its offsets. In Play mode, the constraint is always locked.</remarks>
        public extern bool locked { get; set; }

        ///<summary>The number of sources set on the component (read-only).</summary>
        public int sourceCount { get { return GetSourceCountInternal(this); } }
        [FreeFunction("ConstraintBindings::GetSourceCount")]
        private static extern int GetSourceCountInternal([NotNull] PositionConstraint self);

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
        private static extern void SetSourcesInternal([NotNull] PositionConstraint self, [In] List<ConstraintSource> sources);

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
                throw new InvalidOperationException("The PositionConstraint component has no sources.");
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

    ///<summary>Constrains the rotation of a GameObject based on the rotation of one or more sources.</summary>
    ///<remarks>Use this constraint to rotate a GameObject based on the rotation of other sources. For example, you can use this constraint to synchronize spinning gears in a machine. You can also adjust the weight of each source to influence whether the constrained GameObject follows the rotation of one source over another source.
    ///
    ///Refer to [Rotation Constraint Component](xref:class-RotationConstraint) for more details.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/RotationConstraint.cs}]]></code>
    ///</example>
    ///<seealso cref="ConstraintSource" />
    ///<seealso cref="ParentConstraint" />
    ///<seealso cref="LookAtConstraint" />
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Animation/Constraints/RotationConstraint.h")]
    [NativeHeader("Modules/Animation/Constraints/Constraint.bindings.h")]
    [NativeClass("RotationConstraint", PersistentTypeId = 0x6C61FB21)]
    public sealed partial class RotationConstraint : Behaviour, IConstraint, IConstraintInternal
    {
        internal RotationConstraint(global::UnityEngine.EntityId id) : base(id) {}
        RotationConstraint()
        {
            SetEntityIdFromConstructor(Internal_Create());
        }

        private static extern EntityId Internal_Create();

        ///<summary>The weight of the constraint component.</summary>
        public extern float weight { get; set; }

        ///<summary>The rotation used when the sources have a total weight of 0.</summary>
        public extern Vector3 rotationAtRest { get; set; }

        ///<summary>The offset from the constrained rotation.</summary>
        public extern Vector3 rotationOffset { get; set; }

        ///<summary>The axes affected by the RotationConstraint.</summary>
        ///<remarks>Use this property to restrict the effect of the constraint on a particular axis.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine.Animations;
        ///
        ///public class ConstraintAxis
        ///{
        ///    public void ConstrainOnlyOnXY(RotationConstraint component)
        ///    {
        ///        component.rotationAxis = Axis.X | Axis.Y;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern Axis rotationAxis { get; set; }

        ///<summary>Activates or deactivates the constraint.</summary>
        public extern bool constraintActive { get; set; }
        ///<summary>Locks the offset and rotation at rest.</summary>
        ///<remarks>In Edit mode, unlock the constraint to update its offsets. In Play mode, the constraint is always locked.</remarks>
        public extern bool locked { get; set; }

        ///<summary>The number of sources set on the component (read-only).</summary>
        public int sourceCount { get { return GetSourceCountInternal(this); } }
        [FreeFunction("ConstraintBindings::GetSourceCount")]
        private static extern int GetSourceCountInternal([NotNull] RotationConstraint self);

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
        private static extern void SetSourcesInternal([NotNull] RotationConstraint self, [In] List<ConstraintSource> sources);

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
                throw new InvalidOperationException("The RotationConstraint component has no sources.");
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

    ///<summary>Constrains the scale of a GameObject based on the scale of one or more sources.</summary>
    ///<remarks>Use this constraint to scale a GameObject based on the scale of other sources. For example, use this constraint to scale UI elements based on the scale of a viewport. You can also adjust the weight of each source to influence whether the constrained GameObject follows the scale of one source over another source.
    ///
    ///Refer to [Scale Constraint Component](xref:class-ScaleConstraint) for more details.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/ScaleConstraint.cs}]]></code>
    ///</example>
    ///<seealso cref="ConstraintSource" />
    ///<seealso cref="RotationConstraint" />
    ///<seealso cref="PositionConstraint" />
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Animation/Constraints/ScaleConstraint.h")]
    [NativeHeader("Modules/Animation/Constraints/Constraint.bindings.h")]
    [NativeClass("ScaleConstraint", PersistentTypeId = 0x6C61FB22)]
    public sealed partial class ScaleConstraint : Behaviour, IConstraint, IConstraintInternal
    {
        internal ScaleConstraint(global::UnityEngine.EntityId id) : base(id) {}
        ScaleConstraint()
        {
            SetEntityIdFromConstructor(Internal_Create());
        }

        private static extern EntityId Internal_Create();

        ///<summary>The weight of the constraint component.</summary>
        public extern float weight { get; set; }

        ///<summary>The scale used when the sources have a total weight of 0.</summary>
        public extern Vector3 scaleAtRest { get; set; }

        ///<summary>The offset from the constrained scale.</summary>
        public extern Vector3 scaleOffset { get; set; }

        ///<summary>The axes affected by the ScaleConstraint.</summary>
        ///<remarks>Use this property to restrict the effect of the constraint on a particular axis.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine.Animations;
        ///
        ///public class ConstraintAxis
        ///{
        ///    public void ConstrainOnlyOnXY(ScaleConstraint component)
        ///    {
        ///        component.scalingAxis = Axis.X | Axis.Y;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern Axis scalingAxis { get; set; }

        ///<summary>Activates or deactivates the constraint.</summary>
        public extern bool constraintActive { get; set; }
        ///<summary>Locks the offset and scale at rest.</summary>
        ///<remarks>In Edit mode, unlocks the constraint to update its offsets. In Play mode, the constraint is always locked.</remarks>
        public extern bool locked { get; set; }

        ///<summary>The number of sources set on the component (read-only).</summary>
        public int sourceCount { get { return GetSourceCountInternal(this); } }
        [FreeFunction("ConstraintBindings::GetSourceCount")]
        private static extern int GetSourceCountInternal([NotNull] ScaleConstraint self);

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
        private static extern void SetSourcesInternal([NotNull] ScaleConstraint self, [In] List<ConstraintSource> sources);

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
                throw new InvalidOperationException("The ScaleConstraint component has no sources.");
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
