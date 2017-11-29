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
    [NativeHeader("Runtime/Animation/Constraints/AimConstraint.h")]
    [NativeHeader("Runtime/Animation/Constraints/Constraint.bindings.h")]
    public sealed partial class AimConstraint : Behaviour, IConstraint, IConstraintInternal
    {
        public enum WorldUpType
        {
            SceneUp,
            ObjectUp,
            ObjectRotationUp,
            Vector,
            None
        }

        AimConstraint()
        {
            Internal_Create(this);
        }

        private static extern void Internal_Create([Writable] AimConstraint self);

        public extern float weight { get; set; }

        public extern bool constraintActive { get; set; }
        public extern bool locked { get; set; }

        public extern Vector3 rotationAtRest { get; set; }

        public extern Vector3 rotationOffset { get; set; }
        public extern Axis rotationAxis { get; set; }

        public extern Vector3 aimVector { get; set; }
        public extern Vector3 upVector { get; set; }
        public extern Vector3 worldUpVector { get; set; }
        public extern Transform worldUpObject { get; set; }
        public extern WorldUpType worldUpType { get; set; }

        public int sourceCount { get { return GetSourceCountInternal(this); } }
        [FreeFunction("ConstraintBindings::GetSourceCount")]
        private static extern int GetSourceCountInternal(AimConstraint self);

        [FreeFunction(Name = "ConstraintBindings::GetSources", HasExplicitThis = true)]
        public extern void GetSources([NotNull] List<ConstraintSource> sources);

        public void SetSources(List<ConstraintSource> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            SetSourcesInternal(this, sources);
        }

        [FreeFunction("ConstraintBindings::SetSources")]
        private static extern void SetSourcesInternal(AimConstraint self, List<ConstraintSource> sources);

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
