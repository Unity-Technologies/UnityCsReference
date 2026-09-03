// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Makes a field for entering Rotate.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    [UxmlElement(visibility = LibraryVisibility.Hidden)]
    internal partial class RotateField : BaseField<Rotate>, IValueField<Rotate>
    {
        public new static readonly string ussClassName = "unity-rotate-field";
        internal static readonly UniqueStyleString styleFieldUssClassNameUnique = new(ussClassName);

        public static readonly string foldoutUssClassName = ussClassName + "__foldout";
        static readonly UniqueStyleString foldoutUssClassNameUnique = new(foldoutUssClassName);

        AngleField m_AngleField;
        Vector3Field m_AxisField;

        public AngleField angleField => m_AngleField;
        public Vector3Field axisField => m_AxisField;

        BaseFieldMouseDragger m_Dragger;

        public RotateField() : this(null) { }

        public RotateField(string label) : base(label, null)
        {
            AddToClassList(styleFieldUssClassNameUnique);

            var foldout = new Foldout { text = "Rotate", value = false, viewDataKey = "unity-rotate-foldout" };
            foldout.AddToClassList(foldoutUssClassNameUnique);
            foldout.toggleOnLabelClick = false;

            m_AngleField = new AngleField("Angle");
            m_AxisField = new Vector3Field("Axis");

            m_AngleField.AddToClassList(alignedFieldUssClassNameUnique);
            m_AxisField.AddToClassList(alignedFieldUssClassNameUnique);

            foldout.Add(m_AngleField);
            foldout.Add(m_AxisField);
            visualInput.Add(foldout);

            m_AngleField.RegisterValueChangedCallback(e =>
            {
                UpdateRotateField();
                e.StopPropagation();
            });

            m_AxisField.RegisterValueChangedCallback(e =>
            {
                UpdateRotateField();
                e.StopPropagation();
            });

            AddLabelDragger(foldout);
            SetValueWithoutNotify(Rotate.Initial());
        }

        public override void SetValueWithoutNotify(Rotate rotate)
        {
            base.SetValueWithoutNotify(rotate);
            m_AngleField.SetValueWithoutNotify(value.angle);
            m_AxisField.SetValueWithoutNotify(value.axis);
        }

        void AddLabelDragger(Foldout foldout)
        {
            var dragZone = foldout.Q(className: Toggle.textUssClassName);
            if (dragZone == null)
                return;

            m_Dragger = new FieldMouseDragger<Rotate>(this);
            m_Dragger.SetDragZone(dragZone);
            dragZone.EnableInClassList(labelDraggerVariantUssClassNameUnique, true);
        }

        public void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, Rotate startValue)
        {
            m_AngleField.ApplyInputDeviceDelta(delta, speed, startValue.angle);
        }

        public void StartDragging() => m_AngleField.StartDragging();

        public void StopDragging() => m_AngleField.StopDragging();

        void UpdateRotateField()
        {
            value = new Rotate(m_AngleField.value, m_AxisField.value);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
