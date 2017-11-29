// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Animations
{
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Runtime/Animation/Constraints/ParentConstraint.h")]
    [NativeHeader("Runtime/Animation/Constraints/Constraint.bindings.h")]
    public sealed partial class ParentConstraint : Behaviour, IConstraint, IConstraintInternal
    {
        ParentConstraint()
        {
            Internal_Create(this);
        }

        private static extern void Internal_Create([Writable] ParentConstraint self);

        public extern float weight { get; set; }

        public extern bool constraintActive { get; set; }
        public extern bool locked { get; set; }

        public int sourceCount { get { return GetSourceCountInternal(this); } }
        [FreeFunction("ConstraintBindings::GetSourceCount")]
        private static extern int GetSourceCountInternal(ParentConstraint self);

        public extern Vector3 translationAtRest { get; set; }
        public extern Vector3 rotationAtRest { get; set; }

        public extern Vector3[] translationOffsets { get; set; }
        public extern Vector3[] rotationOffsets { get; set; }

        public extern Axis translationAxis { get; set; }
        public extern Axis rotationAxis { get; set; }

        public Vector3 GetTranslationOffset(int index)
        {
            ValidateSourceIndex(index);
            return GetTranslationOffsetInternal(index);
        }

        public void SetTranslationOffset(int index, Vector3 value)
        {
            ValidateSourceIndex(index);
            SetTranslationOffsetInternal(index, value);
        }

        [NativeName("GetTranslationOffset")]
        private extern Vector3 GetTranslationOffsetInternal(int index);
        [NativeName("SetTranslationOffset")]
        private extern void SetTranslationOffsetInternal(int index, Vector3 value);

        public Vector3 GetRotationOffset(int index)
        {
            ValidateSourceIndex(index);
            return GetRotationOffsetInternal(index);
        }

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

        [FreeFunction(Name = "ConstraintBindings::GetSources", HasExplicitThis = true)]
        public extern void GetSources([NotNull] List<ConstraintSource> sources);

        public void SetSources(List<ConstraintSource> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            SetSourcesInternal(this, sources);
        }

        [FreeFunction("ConstraintBindings::SetSources")]
        private static extern void SetSourcesInternal(ParentConstraint self, List<ConstraintSource> sources);

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
