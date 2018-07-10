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
    [NativeHeader("Runtime/Animation/Constraints/LookAtConstraint.h")]
    [NativeHeader("Runtime/Animation/Constraints/Constraint.bindings.h")]
    public sealed partial class LookAtConstraint : Behaviour, IConstraint, IConstraintInternal
    {
        LookAtConstraint()
        {
            Internal_Create(this);
        }

        private static extern void Internal_Create([Writable] LookAtConstraint self);

        public extern float weight { get; set; }

        public extern float roll { get; set; }

        public extern bool constraintActive { get; set; }
        public extern bool locked { get; set; }

        public extern Vector3 rotationAtRest { get; set; }

        public extern Vector3 rotationOffset { get; set; }

        public extern Transform worldUpObject { get; set; }

        public extern bool useUpObject { get; set; }

        public int sourceCount { get { return GetSourceCountInternal(this); } }
        [FreeFunction("ConstraintBindings::GetSourceCount")]
        private static extern int GetSourceCountInternal([NotNull] LookAtConstraint self);

        [FreeFunction(Name = "ConstraintBindings::GetSources", HasExplicitThis = true)]
        public extern void GetSources([NotNull] List<ConstraintSource> sources);

        public void SetSources(List<ConstraintSource> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            SetSourcesInternal(this, sources);
        }

        [FreeFunction("ConstraintBindings::SetSources")]
        private static extern void SetSourcesInternal([NotNull] LookAtConstraint self, List<ConstraintSource> sources);

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
