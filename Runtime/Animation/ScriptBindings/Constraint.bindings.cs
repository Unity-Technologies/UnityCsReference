// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Animations
{
    [NativeType("Runtime/Animation/Constraints/ConstraintEnums.h")]
    [Flags]
    public enum Axis
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4
    }

    [System.Serializable]
    [NativeType(CodegenOptions = CodegenOptions.Custom, Header = "Runtime/Animation/Constraints/ConstraintSource.h", IntermediateScriptingStructName = "MonoConstraintSource")]
    [NativeHeader("Runtime/Animation/Constraints/Constraint.bindings.h")]
    [UsedByNativeCode]
    public struct ConstraintSource
    {
        [NativeName("sourceTransform")]
        private Transform m_SourceTransform;
        [NativeName("weight")]
        private float m_Weight;

        public Transform sourceTransform { get { return m_SourceTransform; } set { m_SourceTransform = value; } }
        public float weight { get { return m_Weight; } set { m_Weight = value; } }
    }

    public interface IConstraint
    {
        float weight { get; set; }

        bool constraintActive { get; set; }
        bool locked { get; set; }

        int sourceCount { get; }

        int AddSource(ConstraintSource source);
        void RemoveSource(int index);
        ConstraintSource GetSource(int index);
        void SetSource(int index, ConstraintSource source);

        void GetSources(List<ConstraintSource> sources);
        void SetSources(List<ConstraintSource> sources);
    }

    internal interface IConstraintInternal
    {
        void ActivateAndPreserveOffset();
        void ActivateWithZeroOffset();
        void UserUpdateOffset();
        Transform transform { get; }
    }

    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Runtime/Animation/Constraints/PositionConstraint.h")]
    [NativeHeader("Runtime/Animation/Constraints/Constraint.bindings.h")]
    public sealed partial class PositionConstraint : Behaviour, IConstraint, IConstraintInternal
    {
        PositionConstraint()
        {
            Internal_Create(this);
        }

        private static extern void Internal_Create([Writable] PositionConstraint self);

        public extern float weight { get; set; }

        public extern Vector3 translationAtRest { get; set; }

        public extern Vector3 translationOffset { get; set; }

        public extern Axis translationAxis { get; set; }

        public extern bool constraintActive { get; set; }
        public extern bool locked { get; set; }

        public int sourceCount { get { return GetSourceCountInternal(this); } }
        [FreeFunction("ConstraintBindings::GetSourceCount")]
        private static extern int GetSourceCountInternal(PositionConstraint self);

        [FreeFunction(Name = "ConstraintBindings::GetSources", HasExplicitThis = true)]
        public extern void GetSources([NotNull] List<ConstraintSource> sources);

        public void SetSources(List<ConstraintSource> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            SetSourcesInternal(this, sources);
        }

        [FreeFunction("ConstraintBindings::SetSources")]
        private static extern void SetSourcesInternal(PositionConstraint self, List<ConstraintSource> sources);

        public extern int AddSource(ConstraintSource source);

        public void RemoveSource(int index)
        {
            ValidateSourceIndex(index);
            RemoveSourceInternal(index);
        }

        [NativeName("RemoveSource")]
        private extern void RemoveSourceInternal(int index);

        public ConstraintSource GetSource(int index)
        {
            ValidateSourceIndex(index);
            return GetSourceInternal(index);
        }

        [NativeName("GetSource")]
        private extern ConstraintSource GetSourceInternal(int index);

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

    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Runtime/Animation/Constraints/RotationConstraint.h")]
    [NativeHeader("Runtime/Animation/Constraints/Constraint.bindings.h")]
    public sealed partial class RotationConstraint : Behaviour, IConstraint, IConstraintInternal
    {
        RotationConstraint()
        {
            Internal_Create(this);
        }

        private static extern void Internal_Create([Writable] RotationConstraint self);

        public extern float weight { get; set; }

        public extern Vector3 rotationAtRest { get; set; }

        public extern Vector3 rotationOffset { get; set; }

        public extern Axis rotationAxis { get; set; }

        public extern bool constraintActive { get; set; }
        public extern bool locked { get; set; }

        public int sourceCount { get { return GetSourceCountInternal(this); } }
        [FreeFunction("ConstraintBindings::GetSourceCount")]
        private static extern int GetSourceCountInternal(RotationConstraint self);

        [FreeFunction(Name = "ConstraintBindings::GetSources", HasExplicitThis = true)]
        public extern void GetSources([NotNull] List<ConstraintSource> sources);

        public void SetSources(List<ConstraintSource> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            SetSourcesInternal(this, sources);
        }

        [FreeFunction("ConstraintBindings::SetSources")]
        private static extern void SetSourcesInternal(RotationConstraint self, List<ConstraintSource> sources);

        public extern int AddSource(ConstraintSource source);

        public void RemoveSource(int index)
        {
            ValidateSourceIndex(index);
            RemoveSourceInternal(index);
        }

        [NativeName("RemoveSource")]
        private extern void RemoveSourceInternal(int index);

        public ConstraintSource GetSource(int index)
        {
            ValidateSourceIndex(index);
            return GetSourceInternal(index);
        }

        [NativeName("GetSource")]
        private extern ConstraintSource GetSourceInternal(int index);

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

    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Runtime/Animation/Constraints/ScaleConstraint.h")]
    [NativeHeader("Runtime/Animation/Constraints/Constraint.bindings.h")]
    public sealed partial class ScaleConstraint : Behaviour, IConstraint, IConstraintInternal
    {
        ScaleConstraint()
        {
            Internal_Create(this);
        }

        private static extern void Internal_Create([Writable] ScaleConstraint self);

        public extern float weight { get; set; }

        public extern Vector3 scaleAtRest { get; set; }

        public extern Vector3 scaleOffset { get; set; }

        public extern Axis scalingAxis { get; set; }

        public extern bool constraintActive { get; set; }
        public extern bool locked { get; set; }

        public int sourceCount { get { return GetSourceCountInternal(this); } }
        [FreeFunction("ConstraintBindings::GetSourceCount")]
        private static extern int GetSourceCountInternal(ScaleConstraint self);

        [FreeFunction(Name = "ConstraintBindings::GetSources", HasExplicitThis = true)]
        public extern void GetSources([NotNull] List<ConstraintSource> sources);

        public void SetSources(List<ConstraintSource> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            SetSourcesInternal(this, sources);
        }

        [FreeFunction("ConstraintBindings::SetSources")]
        private static extern void SetSourcesInternal(ScaleConstraint self, List<ConstraintSource> sources);

        public extern int AddSource(ConstraintSource source);

        public void RemoveSource(int index)
        {
            ValidateSourceIndex(index);
            RemoveSourceInternal(index);
        }

        [NativeName("RemoveSource")]
        private extern void RemoveSourceInternal(int index);

        public ConstraintSource GetSource(int index)
        {
            ValidateSourceIndex(index);
            return GetSourceInternal(index);
        }

        [NativeName("GetSource")]
        private extern ConstraintSource GetSourceInternal(int index);

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
