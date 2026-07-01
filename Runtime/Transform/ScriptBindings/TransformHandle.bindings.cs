// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [Serializable]
    [NativeClass("TransformHandle")]
    public unsafe struct TransformHandle : IEquatable<TransformHandle>, IComparable<TransformHandle>
    {
        internal IntPtr pTransformData;
        [SerializeField]
        internal EntityId id;

        public static TransformHandle None => default;
        public override bool Equals(object obj) => obj is TransformHandle other && Equals(other);
        public bool Equals(TransformHandle other) => id == other.id;
        public int CompareTo(TransformHandle other) => id.CompareTo(other.id);
        public static bool operator ==(TransformHandle lhs, TransformHandle rhs)
        {
            return lhs.id == rhs.id && lhs.pTransformData == rhs.pTransformData;
        }
        public static bool operator !=(TransformHandle lhs, TransformHandle rhs)
        {
            return lhs.id != rhs.id || lhs.pTransformData != rhs.pTransformData;
        }

        public DirectChildrenEnumerable DirectChildren => new DirectChildrenEnumerable(this);

        public DirectChildrenEnumerator GetDirectChildrenEnumerator()
        {
            return new DirectChildrenEnumerator(this);
        }

        public SubhierarchyEnumerable Subhierarchy => new SubhierarchyEnumerable(this);

        public SubhierarchyEnumerator GetSubhierarchyEnumerator()
        {
            return new SubhierarchyEnumerator(this);
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        private static void AssertHandleIsValid(TransformHandle handle)
        {
            if (!Resources.EntityIdIsValid(handle.id))
            {
                if (handle.id == EntityId.None)
                    throw new NullReferenceException($"The TransformHandle object is null. It may not have been properly initialized, or may refer to an object which has been destroyed. TransformHandles should only be obtained through a valid GameObject or Component.");
                else
                    throw new MissingReferenceException($"The target of this TransformHandle (id='{handle.id}') is not a valid object. The corresponding object may have been destroyed.");
            }
        }

        public Vector3 position
        {
            get
            {
                AssertHandleIsValid(this);
                Internal_GetPosition(out var p);
                return p;
            }
            set
            {
                AssertHandleIsValid(this);
                Internal_SetPosition(value);
            }
        }
        [NativeMethod(Name = "GetPosition")]
        private extern void Internal_GetPosition(out Vector3 p);
        [NativeMethod(Name = "SetPosition")]
        private extern void Internal_SetPosition(Vector3 p);

        public Quaternion rotation
        {
            get
            {
                AssertHandleIsValid(this);
                Internal_GetRotation(out var q);
                return q;
            }
            set
            {
                AssertHandleIsValid(this);
                Internal_SetRotation(value);
            }
        }
        [NativeMethod(Name = "GetRotation")]
        private extern void Internal_GetRotation(out Quaternion r);
        [NativeMethod(Name = "SetRotation")]
        private extern void Internal_SetRotation(Quaternion r);

        // The global scale of the object (RO).
        public Vector3 lossyScale
        {
            get
            {
                AssertHandleIsValid(this);
                Internal_GetWorldScaleLossy(out var s);
                return s;
            }
        }
        [NativeMethod(Name = "GetWorldScaleLossy")]
        private extern void Internal_GetWorldScaleLossy(out Vector3 s);

        public Vector3 localPosition
        {
            get
            {
                AssertHandleIsValid(this);
                Internal_GetLocalPosition(out var p);
                return p;
            }
            set
            {
                AssertHandleIsValid(this);
                Internal_SetLocalPosition(value);
            }
        }
        [NativeMethod(Name = "GetLocalPosition")]
        private extern void Internal_GetLocalPosition(out Vector3 r);
        [NativeMethod(Name = "SetLocalPosition")]
        private extern void Internal_SetLocalPosition(Vector3 r);

        public Quaternion localRotation
        {
            get
            {
                AssertHandleIsValid(this);
                Internal_GetLocalRotation(out var q);
                return q;
            }
            set
            {
                AssertHandleIsValid(this);
                Internal_SetLocalRotation(value);
            }
        }
        [NativeMethod(Name = "GetLocalRotation")]
        private extern void Internal_GetLocalRotation(out Quaternion r);
        [NativeMethod(Name = "SetLocalRotation")]
        private extern void Internal_SetLocalRotation(Quaternion r);

        public Vector3 localScale
        {
            get
            {
                AssertHandleIsValid(this);
                Internal_GetLocalScale(out var s);
                return s;
            }
            set
            {
                AssertHandleIsValid(this);
                Internal_SetLocalScale(value);
            }
        }
        [NativeMethod(Name = "GetLocalScale")]
        private extern void Internal_GetLocalScale(out Vector3 r);
        [NativeMethod(Name = "SetLocalScale")]
        private extern void Internal_SetLocalScale(Vector3 r);

        // The rotation as Euler angles in degrees.
        public Vector3 eulerAngles { get { return rotation.eulerAngles; } set { rotation = Quaternion.Euler(value); } }

        // The rotation as Euler angles in degrees relative to the parent transform's rotation.
        public Vector3 localEulerAngles { get { return localRotation.eulerAngles; } set { localRotation = Quaternion.Euler(value); } }

        // The red axis of the transform in world space.
        public Vector3 right { get { return rotation * Vector3.right; } set { rotation = Quaternion.FromToRotation(Vector3.right, value); } }

        // The green axis of the transform in world space.
        public Vector3 up { get { return rotation * Vector3.up; } set { rotation = Quaternion.FromToRotation(Vector3.up, value); } }

        // The blue axis of the transform in world space.
        public Vector3 forward { get { return rotation * Vector3.forward; } set { rotation = Quaternion.LookRotation(value); } }

        // Matrix that transforms a point from world space into local space (RO).
        public Matrix4x4 worldToLocalMatrix
        {
            get
            {
                AssertHandleIsValid(this);
                Internal_GetWorldToLocalMatrix(out var m);
                return m;
            }
        }
        [NativeMethod(Name = "GetWorldToLocalMatrix")]
        private extern void Internal_GetWorldToLocalMatrix(out Matrix4x4 m);

        // Matrix that transforms a point from local space into world space (RO).
        public Matrix4x4 localToWorldMatrix
        {
            get
            {
                AssertHandleIsValid(this);
                Internal_GetLocalToWorldMatrix(out var m);
                return m;
            }
        }
        [NativeMethod(Name = "GetLocalToWorldMatrix")]
        private extern void Internal_GetLocalToWorldMatrix(out Matrix4x4 m);

        public bool IsValid()
        {
            // AssertHandleIsValid() isn't needed here; the whole point of this method is to be a non-fatal validity test
            return Internal_IsValid();
        }
        [NativeMethod(Name = "IsValid")]
        private extern bool Internal_IsValid();

        public TransformHandle root
        {
            get
            {
                AssertHandleIsValid(this);
                Internal_GetRoot(out var rootHandle);
                return rootHandle;
            }
        }
        [NativeMethod(Name = "Internal_GetRoot")]
        private extern void Internal_GetRoot(out TransformHandle outRootHandle);

        public TransformHandle parent
        {
            get
            {
                AssertHandleIsValid(this);
                Internal_TryGetParent(out var parentHandle);
                return parentHandle;
            }
            set
            {
                AssertHandleIsValid(this);
                // Preserve behavior of Transform.SetParent(): if newParent is invalid but non-null, quietly force it to null.
                TransformHandle newParent = value;
                if (newParent != TransformHandle.None && !newParent.IsValid())
                    newParent = TransformHandle.None;
                Internal_SetParent(newParent, worldPositionStays: true);
            }
        }
        [NativeMethod(Name = "TryGetParent")]
        private extern bool Internal_TryGetParent(out TransformHandle parentHandle);

        public void SetParent(TransformHandle p)
        {
            AssertHandleIsValid(this);
            // Preserve behavior of Transform.SetParent(): if newParent is invalid but non-null, quietly force it to null.
            if (p != TransformHandle.None && !p.IsValid())
                p = TransformHandle.None;
            Internal_SetParent(p, worldPositionStays: true);
        }
        public void SetParent(TransformHandle parent, bool worldPositionStays)
        {
            AssertHandleIsValid(this);
            // Preserve behavior of Transform.SetParent(): if newParent is invalid but non-null, quietly force it to null.
            if (parent != TransformHandle.None && !parent.IsValid())
                parent = TransformHandle.None;
            Internal_SetParent(parent, worldPositionStays);
        }
        [NativeMethod(Name = "SetParent_Internal")]
        private extern void Internal_SetParent(TransformHandle parent, bool worldPositionStays);

        public TransformHandle GetChild(int index)
        {
            AssertHandleIsValid(this);
            Internal_GetChild(index, out var childHandle);
            return childHandle;
        }
        [NativeMethod(Name = "Internal_GetChild")]
        private extern void Internal_GetChild(int index, out TransformHandle outChildHandle);

        [NativeMethod(Name = "Internal_GetDeepChildCount")]
        private extern int Internal_GetDeepChildCount();

        [NativeMethod(Name = "Internal_GetNextDeepChildIndex")]
        private extern int Internal_GetNextDeepChildIndex(int currentIndex, out TransformHandle nextChildHandle);

        [NativeMethod(Name = "Internal_GetHierarchyVersion")]
        private extern int Internal_GetHierarchyVersion();


        public bool HasParent()
        {
            AssertHandleIsValid(this);
            return Internal_HasParent();
        }
        [NativeMethod(Name = "HasParent")]
        private extern bool Internal_HasParent();

        public bool IsChildOf(TransformHandle parent)
        {
            AssertHandleIsValid(this);
            if (parent != TransformHandle.None)
                AssertHandleIsValid(parent);
            return Internal_IsChildOf(parent);
        }
        [NativeMethod(Name = "IsChildOrSameAsOther")]
        private extern bool Internal_IsChildOf(TransformHandle parent);

        public int childCount
        {
            get
            {
                AssertHandleIsValid(this);
                return Internal_GetChildrenCount();
            }
        }
        [NativeMethod(Name = "GetChildrenCount")]
        private extern int Internal_GetChildrenCount();


        public void DetachChildren()
        {
            AssertHandleIsValid(this);
            Internal_DetachChildren();
        }
        [NativeMethod(Name = "DetachChildren")]
        private extern void Internal_DetachChildren();

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            AssertHandleIsValid(this);
            Internal_SetPositionAndRotation(position, rotation);
        }
        [NativeMethod(Name = "SetPositionAndRotation")]
        private extern void Internal_SetPositionAndRotation(Vector3 position, Quaternion rotation);

        public void SetLocalPositionAndRotation(Vector3 localPosition, Quaternion localRotation)
        {
            AssertHandleIsValid(this);
            Internal_SetLocalPositionAndRotation(localPosition, localRotation);
        }
        [NativeMethod(Name = "SetLocalPositionAndRotation")]
        private extern void Internal_SetLocalPositionAndRotation(Vector3 localPosition, Quaternion localRotation);

        public void GetPositionAndRotation(out Vector3 position, out Quaternion rotation)
        {
            AssertHandleIsValid(this);
            Internal_GetPositionAndRotation(out position, out rotation);
        }
        [NativeMethod(Name = "GetPositionAndRotation")]
        private extern void Internal_GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

        public void GetLocalPositionAndRotation(out Vector3 localPosition, out Quaternion localRotation)
        {
            AssertHandleIsValid(this);
            Internal_GetLocalPositionAndRotation(out localPosition, out localRotation);
        }
        [NativeMethod(Name = "GetLocalPositionAndRotation")]
        private extern void Internal_GetLocalPositionAndRotation(out Vector3 localPosition, out Quaternion localRotation);

        public void Translate(Vector3 translation, [UnityEngine.Internal.DefaultValue("Space.Self")] Space relativeTo)
        {
            if (relativeTo == Space.World)
                position += translation;
            else
                position += TransformDirection(translation);
        }

        public void Translate(Vector3 translation)
        {
            Translate(translation, Space.Self);
        }

        public void Translate(float x, float y, float z, [UnityEngine.Internal.DefaultValue("Space.Self")] Space relativeTo)
        {
            Translate(new Vector3(x, y, z), relativeTo);
        }

        public void Translate(float x, float y, float z)
        {
            Translate(new Vector3(x, y, z), Space.Self);
        }

        public void Translate(Vector3 translation, TransformHandle relativeTo)
        {
            if (relativeTo != TransformHandle.None)
                position += relativeTo.TransformDirection(translation);
            else
                position += translation;
        }

        public void Translate(float x, float y, float z, TransformHandle relativeTo)
        {
            Translate(new Vector3(x, y, z), relativeTo);
        }

        // Applies a rotation of /eulerAngles.z/ degrees around the z axis, /eulerAngles.x/ degrees around the x axis, and /eulerAngles.y/ degrees around the y axis (in that order).
        public void Rotate(Vector3 eulers, [UnityEngine.Internal.DefaultValue("Space.Self")] Space relativeTo)
        {
            Quaternion eulerRot = Quaternion.Euler(eulers.x, eulers.y, eulers.z);
            if (relativeTo == Space.Self)
                localRotation = localRotation * eulerRot;
            else
            {
                rotation = rotation * (Quaternion.Inverse(rotation) * eulerRot * rotation);
            }
        }

        public void Rotate(Vector3 eulers)
        {
            Rotate(eulers, Space.Self);
        }

        // Applies a rotation of /zAngle/ degrees around the z axis, /xAngle/ degrees around the x axis, and /yAngle/ degrees around the y axis (in that order).
        public void Rotate(float xAngle, float yAngle, float zAngle, [UnityEngine.Internal.DefaultValue("Space.Self")] Space relativeTo)
        {
            Rotate(new Vector3(xAngle, yAngle, zAngle), relativeTo);
        }

        public void Rotate(float xAngle, float yAngle, float zAngle)
        {
            Rotate(new Vector3(xAngle, yAngle, zAngle), Space.Self);
        }

        // Rotates the transform around /axis/ by /angle/ degrees.
        public void Rotate(Vector3 axis, float angle, [UnityEngine.Internal.DefaultValue("Space.Self")] Space relativeTo)
        {
            AssertHandleIsValid(this);
            if (relativeTo == Space.Self)
                Internal_RotateAround(TransformDirection(axis), angle * Mathf.Deg2Rad);
            else
                Internal_RotateAround(axis, angle * Mathf.Deg2Rad);
        }
        // Rotates the transform about /axis/ passing through /point/ in world coordinates by /angle/ degrees.
        public void RotateAround(Vector3 point, Vector3 axis, float angle)
        {
            Vector3 worldPos = position;
            Quaternion q = Quaternion.AngleAxis(angle, axis);
            Vector3 dif = worldPos - point;
            dif = q * dif;
            worldPos = point + dif;
            position = worldPos;
            //AssertHandleIsValid() is not needed here; it's already handled by the position property accesses above.
            Internal_RotateAround(axis, angle * Mathf.Deg2Rad);
        }
        [NativeMethod(Name = "RotateAround")]
        private extern void Internal_RotateAround(Vector3 worldAxis, float rad);

        public void Rotate(Vector3 axis, float angle)
        {
            Rotate(axis, angle, Space.Self);
        }

        // Rotates the transform so the forward vector points at /target/'s current position.
        public void LookAt(TransformHandle target, [UnityEngine.Internal.DefaultValue("Vector3.up")] Vector3 worldUp)
        {
            if (target != TransformHandle.None)
            {
                AssertHandleIsValid(this);
                AssertHandleIsValid(target);
                Internal_LookAt(target.position, worldUp);
            }
        }
        public void LookAt(TransformHandle target)
        {
            if (target != TransformHandle.None)
            {
                AssertHandleIsValid(this);
                AssertHandleIsValid(target);
                Internal_LookAt(target.position, Vector3.up);
            }
        }
        // Rotates the transform so the forward vector points at /worldPosition/.
        public void LookAt(Vector3 worldPosition, [UnityEngine.Internal.DefaultValue("Vector3.up")] Vector3 worldUp)
        {
            AssertHandleIsValid(this);
            Internal_LookAt(worldPosition, worldUp);
        }
        public void LookAt(Vector3 worldPosition)
        {
            AssertHandleIsValid(this);
            Internal_LookAt(worldPosition, Vector3.up);
        }
        [NativeMethod(Name = "LookAt")]
        private extern void Internal_LookAt(Vector3 worldPosition, Vector3 worldUp);

        // Transforms the position /x/, /y/, /z/ from local space to world space.
        public Vector3 TransformPoint(float x, float y, float z)
        {
            AssertHandleIsValid(this);
            return Internal_TransformPoint(new Vector3(x, y, z));
        }
        public Vector3 TransformPoint(Vector3 point)
        {
            AssertHandleIsValid(this);
            return Internal_TransformPoint(point);
        }
        [NativeMethod(Name = "TransformPoint")]
        private extern Vector3 Internal_TransformPoint(Vector3 point);

        // Transforms multiple points from local space to world space
        public unsafe void TransformPoints(ReadOnlySpan<Vector3> positions, Span<Vector3> transformedPositions)
        {
            if (positions.Length != transformedPositions.Length)
                throw new InvalidOperationException($"Both spans passed to Transform.TransformPoints() must be the same length");
            AssertHandleIsValid(this);
            Internal_TransformPoints(positions, transformedPositions);
        }
        public unsafe void TransformPoints(Span<Vector3> positions)
        {
            AssertHandleIsValid(this);
            Internal_TransformPoints(positions, positions);
        }
        [NativeMethod(Name = "TransformPoints")]
        private extern void Internal_TransformPoints(ReadOnlySpan<Vector3> points, Span<Vector3> transformedPoints);

        // Transforms direction /x/, /y/, /z/ from local space to world space.
        public Vector3 TransformDirection(float x, float y, float z)
        {
            AssertHandleIsValid(this);
            return Internal_TransformDirection(new Vector3(x, y, z));
        }
        public Vector3 TransformDirection(Vector3 direction)
        {
            AssertHandleIsValid(this);
            return Internal_TransformDirection(direction);
        }
        [NativeMethod(Name = "TransformDirection")]
        private extern Vector3 Internal_TransformDirection(Vector3 direction);

        // Transform multiple direction vectors from local space to world space.
        public unsafe void TransformDirections(ReadOnlySpan<Vector3> directions, Span<Vector3> transformedDirections)
        {
            if (directions.Length != transformedDirections.Length)
                throw new InvalidOperationException($"Both spans passed to Transform.TransformDirections() must be the same length");
            AssertHandleIsValid(this);
            Internal_TransformDirections(directions, transformedDirections);
        }

        public unsafe void TransformDirections(Span<Vector3> directions)
        {
            AssertHandleIsValid(this);
            Internal_TransformDirections(directions, directions);
        }
        [NativeMethod(Name = "TransformDirections")]
        private extern void Internal_TransformDirections(ReadOnlySpan<Vector3> directions, Span<Vector3> transformedDirections);

        // Transforms vector /x/, /y/, /z/ from local space to world space.
        public Vector3 TransformVector(float x, float y, float z)
        {
            return Internal_TransformVector(new Vector3(x, y, z));
        }
        public Vector3 TransformVector(Vector3 vector)
        {
            return Internal_TransformVector(vector);
        }
        [NativeMethod(Name = "TransformVector")]
        private extern Vector3 Internal_TransformVector(Vector3 vector);

        // Transforms multiple vectors from local space to world space
        public unsafe void TransformVectors(ReadOnlySpan<Vector3> vectors, Span<Vector3> transformedVectors)
        {
            if (vectors.Length != transformedVectors.Length)
                throw new InvalidOperationException($"Both spans passed to Transform.TransformVectors() must be the same length");
            AssertHandleIsValid(this);
            Internal_TransformVectors(vectors, transformedVectors);
        }
        public unsafe void TransformVectors(Span<Vector3> vectors)
        {
            AssertHandleIsValid(this);
            Internal_TransformVectors(vectors, vectors);
        }
        [NativeMethod(Name = "TransformVectors")]
        private extern void Internal_TransformVectors(ReadOnlySpan<Vector3> vectors, Span<Vector3> transformedVectors);

        // Transforms the position /x/, /y/, /z/ from world space to local space. The opposite of TransformHandle.TransformPoint.
        public Vector3 InverseTransformPoint(float x, float y, float z)
        {
            AssertHandleIsValid(this);
            return Internal_InverseTransformPoint(new Vector3(x, y, z));
        }
        public Vector3 InverseTransformPoint(Vector3 point)
        {
            AssertHandleIsValid(this);
            return Internal_InverseTransformPoint(point);
        }
        [NativeMethod(Name = "InverseTransformPoint")]
        private extern Vector3 Internal_InverseTransformPoint(Vector3 point);

        public unsafe void InverseTransformPoints(ReadOnlySpan<Vector3> positions, Span<Vector3> transformedPositions)
        {
            if (positions.Length != transformedPositions.Length)
                throw new InvalidOperationException($"Both spans passed to Transform.InverseTransformPoints() must be the same length");
            AssertHandleIsValid(this);
            Internal_InverseTransformPoints(positions, transformedPositions);
        }
        public unsafe void InverseTransformPoints(Span<Vector3> positions)
        {
            AssertHandleIsValid(this);
            Internal_InverseTransformPoints(positions, positions);
        }
        [NativeMethod(Name = "InverseTransformPoints")]
        private extern void Internal_InverseTransformPoints(ReadOnlySpan<Vector3> points, Span<Vector3> transformedPoints);

        // Transforms the direction /x/, /y/, /z/ from world space to local space. The opposite of TransformHandle.TransformDirection.
        public Vector3 InverseTransformDirection(float x, float y, float z)
        {
            AssertHandleIsValid(this);
            return Internal_InverseTransformDirection(new Vector3(x, y, z));
        }
        public Vector3 InverseTransformDirection(Vector3 direction)
        {
            AssertHandleIsValid(this);
            return Internal_InverseTransformDirection(direction);
        }
        [NativeMethod(Name = "InverseTransformDirection")]
        private extern Vector3 Internal_InverseTransformDirection(Vector3 direction);

        // Transform multiple directions from world space to local space. The opposite of TransformHandle.TransformDirections.
        public unsafe void InverseTransformDirections(ReadOnlySpan<Vector3> directions, Span<Vector3> transformedDirections)
        {
            if (directions.Length != transformedDirections.Length)
                throw new InvalidOperationException($"Both spans passed to Transform.InverseTransformDirections() must be the same length");
            AssertHandleIsValid(this);
            Internal_InverseTransformDirections(directions, transformedDirections);
        }
        public unsafe void InverseTransformDirections(Span<Vector3> directions)
        {
            AssertHandleIsValid(this);
            Internal_InverseTransformDirections(directions, directions);
        }
        [NativeMethod(Name = "InverseTransformDirections")]
        private extern void Internal_InverseTransformDirections(ReadOnlySpan<Vector3> directions, Span<Vector3> transformedDirections);

        // Transforms the vector /x/, /y/, /z/ from world space to local space. The opposite of Transform.TransformVector.
        public Vector3 InverseTransformVector(float x, float y, float z)
        {
            AssertHandleIsValid(this);
            return Internal_InverseTransformVector(new Vector3(x, y, z));
        }
        public Vector3 InverseTransformVector(Vector3 vector)
        {
            AssertHandleIsValid(this);
            return Internal_InverseTransformVector(vector);
        }
        [NativeMethod(Name = "InverseTransformVector")]
        private extern Vector3 Internal_InverseTransformVector(Vector3 vector);

        // Transforms multiple vectors from world space to local space. The opposite of Transform.TransformVector.
        public unsafe void InverseTransformVectors(ReadOnlySpan<Vector3> vectors, Span<Vector3> transformedVectors)
        {
            if (vectors.Length != transformedVectors.Length)
                throw new InvalidOperationException($"Both spans passed to Transform.InverseTransformVectors() must be the same length");
            AssertHandleIsValid(this);
            Internal_InverseTransformVectors(vectors, transformedVectors);
        }
        public unsafe void InverseTransformVectors(Span<Vector3> vectors)
        {
            AssertHandleIsValid(this);
            Internal_InverseTransformVectors(vectors, vectors);
        }
        [NativeMethod(Name = "InverseTransformVectors")]
        private extern void Internal_InverseTransformVectors(ReadOnlySpan<Vector3> vectors, Span<Vector3> transformedVectors);


        public int hierarchyCapacity
        {
            get
            {
                AssertHandleIsValid(this);
                return Internal_GetHierarchyCapacity();
            }
            set
            {
                AssertHandleIsValid(this);
                Internal_SetHierarchyCapacity(value);
            }
        }
        [NativeMethod("GetHierarchyCapacity")]
        private extern int Internal_GetHierarchyCapacity();
        [NativeMethod("SetHierarchyCapacity")]
        private extern void Internal_SetHierarchyCapacity(int value);


        public int hierarchyCount
        {
            get
            {
                AssertHandleIsValid(this);
                return Internal_GetHierarchyCount();
            }
        }
        [NativeMethod("GetHierarchyCount")]
        private extern int Internal_GetHierarchyCount();

        public struct DirectChildrenEnumerable : IEnumerable<TransformHandle>
        {
            private TransformHandle Root;

            public DirectChildrenEnumerable(TransformHandle root)
            {
                this.Root = root;
            }

            public IEnumerator<TransformHandle> GetEnumerator()
            {
                return new DirectChildrenEnumerator(Root);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public struct DirectChildrenEnumerator : IEnumerator<TransformHandle>
        {
            private TransformHandle parent;
            private int currentIndex;

            internal DirectChildrenEnumerator(TransformHandle parent)
            {
                this.parent = parent;
                this.currentIndex = -1;
            }

            object IEnumerator.Current => Current;
            public TransformHandle Current => this.parent.GetChild(this.currentIndex);

            public bool MoveNext()
            {
                return ++this.currentIndex < this.parent.childCount;
            }

            public void Reset()
            {
                this.currentIndex = -1;
            }

            public void Dispose()
            { }
        }

        public struct SubhierarchyEnumerable : IEnumerable<TransformHandle>
        {
            private TransformHandle root;

            public SubhierarchyEnumerable(TransformHandle root)
            {
                this.root = root;
            }

            public IEnumerator<TransformHandle> GetEnumerator()
            {
                return new SubhierarchyEnumerator(root);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public struct SubhierarchyEnumerator : IEnumerator<TransformHandle>
        {
            private TransformHandle root;
            private int currentIndex;
            private int remaining;
            private int expectedVersion;
            private TransformHandle current;

            internal SubhierarchyEnumerator(TransformHandle root)
            {
                this.root = root;
                this.currentIndex = -1;
                this.remaining = -1;
                this.expectedVersion = 0;
                this.current = TransformHandle.None;
            }

            object IEnumerator.Current => Current;
            public TransformHandle Current => this.current;

            public bool MoveNext()
            {
                AssertHandleIsValid(this.root);

                if (this.remaining < 0)
                {
                    this.remaining = this.root.Internal_GetDeepChildCount();
                    this.expectedVersion = this.root.Internal_GetHierarchyVersion();
                    if (this.remaining == 0)
                        return false;
                    this.current = this.root;
                    this.remaining--;
                    return true;
                }

                if (this.remaining == 0)
                    return false;

                if (this.root.Internal_GetHierarchyVersion() != this.expectedVersion)
                    throw new InvalidOperationException("TransformHandle hierarchy was modified during Subhierarchy enumeration.");

                this.currentIndex = this.root.Internal_GetNextDeepChildIndex(this.currentIndex, out this.current);
                this.remaining--;
                return true;
            }

            public void Reset()
            {
                this.currentIndex = -1;
                this.remaining = -1;
                this.expectedVersion = 0;
                this.current = TransformHandle.None;
            }

            public void Dispose()
            { }
        }
    }
}
