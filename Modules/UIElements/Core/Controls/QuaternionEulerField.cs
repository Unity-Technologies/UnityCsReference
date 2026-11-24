// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A <see cref="Quaternion"/> field that edits the rotation as X, Y and Z euler angles, in degrees. As in the Transform inspector, angles that were not typed in the field are shown between -180 and 180. For more information, refer to [[wiki:UIE-uxm-element-QuaternionEulerField|UXML element QuaternionEulerField]].
    /// </summary>
    [UxmlElement(libraryPath = "Numeric Fields")]
    [Icon("UIToolkit/Icons/QuaternionEulerField.png")]
    public partial class QuaternionEulerField : BaseCompositeField<Quaternion, FloatField, float>
    {
        const float k_SameRotationTolerance = 1e-5f;
        const float k_ZeroAngleTolerance = 1e-6f;

        // Kept separately from the value so that edits through a gimbal-locked rotation keep the typed angles.
        Vector3 m_EulerAngles;
        Vector3 m_EditedEulerAngles;
        bool m_HasEditStart;
        Quaternion m_EditStartRotation;
        Vector3 m_EditStartEulerAngles;
        FieldDescription[] m_FieldDescriptions;

        internal override FieldDescription[] DescribeFields()
        {
            return m_FieldDescriptions ??= new[]
            {
                new FieldDescription("X", "unity-x-input", q => EulerAnglesOf(q).x, (ref Quaternion q, float v) => q = WithEulerAngle(q, 0, v)),
                new FieldDescription("Y", "unity-y-input", q => EulerAnglesOf(q).y, (ref Quaternion q, float v) => q = WithEulerAngle(q, 1, v)),
                new FieldDescription("Z", "unity-z-input", q => EulerAnglesOf(q).z, (ref Quaternion q, float v) => q = WithEulerAngle(q, 2, v)),
            };
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-quaternion-euler-field";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        internal new static readonly UniqueStyleString labelUssClassNameUnique = new(labelUssClassName);

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        internal new static readonly UniqueStyleString inputUssClassNameUnique = new(inputUssClassName);

        /// <summary>
        /// Initializes and returns an instance of QuaternionEulerField.
        /// </summary>
        public QuaternionEulerField()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of QuaternionEulerField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public QuaternionEulerField(string label)
            : base(label, 3)
        {
            AddToClassList(ussClassNameUnique);
            labelElement.AddToClassList(labelUssClassNameUnique);
            visualInput.AddToClassList(inputUssClassNameUnique);

            SetValueWithoutNotify(Quaternion.identity);
        }

        /// <summary>
        /// Sets the value without sending a <see cref="ChangeEvent{T}"/>. The displayed euler angles only change when the new value is a different rotation.
        /// </summary>
        /// <param name="newValue">The new rotation.</param>
        public override void SetValueWithoutNotify(Quaternion newValue)
        {
            base.SetValueWithoutNotify(newValue);
            m_EulerAngles = EulerAnglesOf(rawValue);
            if (!rawValue.Equals(Quaternion.Euler(m_EditedEulerAngles)))
            {
                m_EditedEulerAngles = m_EulerAngles;
                m_HasEditStart = false;
            }
        }

        Vector3 EulerAnglesOf(Quaternion rotation)
        {
            if (rotation.Equals(Quaternion.Euler(m_EditedEulerAngles)))
                return m_EditedEulerAngles;
            if (IsSameRotation(Quaternion.Euler(m_EulerAngles), rotation))
                return m_EulerAngles;
            if (IsSameRotation(Quaternion.Euler(m_EditedEulerAngles), rotation))
                return m_EditedEulerAngles;
            return GetInspectorEulerAngles(rotation);
        }

        // As in the Transform inspector: of the two euler triples for a rotation, the one closest to zero, in [-180, 180].
        internal static Vector3 GetInspectorEulerAngles(Quaternion rotation)
        {
            var eulerAngles = WrapEulerAngles(rotation.eulerAngles);
            var alternate = WrapEulerAngles(new Vector3(-180f - eulerAngles.x, eulerAngles.y + 180f, eulerAngles.z + 180f));
            return alternate.sqrMagnitude < eulerAngles.sqrMagnitude ? alternate : eulerAngles;
        }

        // A focused edit passes the rotation it started from on every keystroke, which the cached angles stop matching.
        Quaternion WithEulerAngle(Quaternion rotation, int axis, float angle)
        {
            if (!IsFinite(angle))
                return rotation;
            if (!m_HasEditStart || !rotation.Equals(m_EditStartRotation))
            {
                m_EditStartRotation = rotation;
                m_EditStartEulerAngles = EulerAnglesOf(rotation);
                m_HasEditStart = true;
            }
            m_EditedEulerAngles = m_EditStartEulerAngles;
            m_EditedEulerAngles[axis] = angle;
            return Quaternion.Euler(m_EditedEulerAngles);
        }

        // Quaternion.Angle and == treat rotations less than about 0.16 degrees apart as equal.
        static bool IsSameRotation(Quaternion a, Quaternion b)
        {
            var sign = Quaternion.Dot(a, b) < 0f ? -1f : 1f;
            var delta = new Vector4(a.x - sign * b.x, a.y - sign * b.y, a.z - sign * b.z, a.w - sign * b.w);
            return delta.sqrMagnitude <= k_SameRotationTolerance * k_SameRotationTolerance;
        }

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        static Vector3 WrapEulerAngles(Vector3 eulerAngles)
        {
            return new Vector3(WrapAngle(eulerAngles.x), WrapAngle(eulerAngles.y), WrapAngle(eulerAngles.z));
        }

        static float WrapAngle(float angle)
        {
            angle -= 360f * Mathf.Round(angle / 360f);
            return Mathf.Abs(angle) < k_ZeroAngleTolerance ? 0f : angle;
        }
    }
}
