// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor
{
    enum BackgroundPositionMode
    {
        Invalid,
        Horizontal,
        Vertical
    };

    [UsedImplicitly]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class BackgroundPositionStyleField : BaseField<BackgroundPosition>
    {
        [Serializable]
        public new class UxmlSerializedData : BaseField<BackgroundPosition>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<BackgroundPosition>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[] { new(nameof(mode), "mode") }, true);
            }

            public override object CreateInstance() => new BackgroundPositionStyleField();

#pragma warning disable 649
            [SerializeField]
            BackgroundPosition.Axis mode;
            [SerializeField, UxmlIgnore, HideInInspector]
            UxmlAttributeFlags mode_UxmlAttributeFlags;
#pragma warning restore 649

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);
                var e = (BackgroundPositionStyleField)obj;
                if (ShouldWriteAttributeValue(mode_UxmlAttributeFlags))
                {
                    e.mode = mode;
                }
            }
        }

        static readonly string FieldClassName = "unity-background-position-style-field";
        static readonly string UxmlPath = "UIToolkitAuthoring/Inspector/Controls/BackgroundPositionStyleField.uxml";
        static readonly string IconPath =  "UIToolkit/Icons";
        public static readonly string BackgroundPositionFieldName = "position";
        public static readonly string BackgroundPositionAlignFieldName = "align";
        public static readonly string InspectorContainerClassName = "unity-ui-inspector__container";

        static readonly string BackgroundPositionXLeftTooltip = "Aligns the background image to the left side.";
        static readonly string BackgroundPositionXCenterTooltip = "Centers the background image horizontally.";
        static readonly string BackgroundPositionXRightTooltip = "Aligns the background image to the right side.";
        static readonly string BackgroundPositionYTopTooltip = "Aligns the background image to the top edge.";
        static readonly string BackgroundPositionYCenterTooltip = "Centers the background image vertically.";
        static readonly string BackgroundPositionYBottomTooltip = "Aligns the background image to the bottom edge";

        LengthField m_BackgroundPositionField;
        ToggleButtonGroup m_BackgroundPositionAlign;

        BackgroundPosition.Axis m_Mode;

        public BackgroundPosition.Axis mode
        {
            get => m_Mode;
            set
            {
                if (m_Mode == value)
                    return;
                m_Mode = value;

                OnModeChanged();
            }
        }

        public BackgroundPositionStyleField() : base(null)
        {
            AddToClassList(InspectorContainerClassName);
            AddToClassList(FieldClassName);

            var template = EditorGUIUtility.Load(UxmlPath) as VisualTreeAsset;
            template.CloneTree(this);

            m_BackgroundPositionField = this.Q<LengthField>(BackgroundPositionFieldName);
            m_BackgroundPositionAlign = this.Q<ToggleButtonGroup>(BackgroundPositionAlignFieldName);

            m_BackgroundPositionField.RegisterValueChangedCallback(e =>
            {
                UpdateBackgroundPositionField();
                e.StopPropagation();
            });

            m_BackgroundPositionAlign.RegisterValueChangedCallback(e =>
            {
                UpdateBackgroundPositionField();
                e.StopPropagation();
            });

            value = new BackgroundPosition(BackgroundPositionKeyword.Center);
            OnModeChanged();
        }

        public override void SetValueWithoutNotify(BackgroundPosition newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            var toggleButtonGroupState = new ToggleButtonGroupState(0u, 3);

            int index = 0;

            switch (value.keyword)
            {
                case BackgroundPositionKeyword.Left:
                case BackgroundPositionKeyword.Top:
                    index = 0;
                    break;
                case BackgroundPositionKeyword.Center:
                    index = 1;
                    break;
                case BackgroundPositionKeyword.Right:
                case BackgroundPositionKeyword.Bottom:
                    index = 2;
                    break;
            }

            toggleButtonGroupState[index] = true;
            m_BackgroundPositionAlign.SetValueWithoutNotify(toggleButtonGroupState);

            m_BackgroundPositionField.SetEnabled(index != 1);
            m_BackgroundPositionField.SetValueWithoutNotify(value.offset);
        }

        void UpdateBackgroundPositionField()
        {
            // Rebuild value from sub fields
            var newPosition = new BackgroundPosition();

            var valueAlign = m_BackgroundPositionAlign.value;
            var selectedAlign = valueAlign.GetActiveOptions(stackalloc int[valueAlign.length]);
            int indexAlign = selectedAlign[0];


            if (indexAlign == 0) newPosition.keyword = (m_Mode == BackgroundPosition.Axis.Horizontal) ? BackgroundPositionKeyword.Left : BackgroundPositionKeyword.Top;
            else if (indexAlign == 1) newPosition.keyword = BackgroundPositionKeyword.Center;
            else newPosition.keyword = (m_Mode == BackgroundPosition.Axis.Horizontal) ? BackgroundPositionKeyword.Right : BackgroundPositionKeyword.Bottom;

            newPosition.offset = m_BackgroundPositionField.value;

            value = newPosition;
        }

        void OnModeChanged()
        {
            var icons = new List<Background>();
            var tooltips = new List<string>();

            if (m_Mode == BackgroundPosition.Axis.Horizontal)
            {
                icons.Add(UIResources.LoadIcon($"{IconPath}/background-position-x-left.png"));
                icons.Add(UIResources.LoadIcon($"{IconPath}/background-position-x-center.png"));
                icons.Add(UIResources.LoadIcon($"{IconPath}/background-position-x-right.png"));

                tooltips.Add(BackgroundPositionXLeftTooltip);
                tooltips.Add(BackgroundPositionXCenterTooltip);
                tooltips.Add(BackgroundPositionXRightTooltip);
            }
            else
            {
                icons.Add(UIResources.LoadIcon($"{IconPath}/background-position-y-top.png"));
                icons.Add(UIResources.LoadIcon($"{IconPath}/background-position-y-center.png"));
                icons.Add(UIResources.LoadIcon($"{IconPath}/background-position-y-bottom.png"));

                tooltips.Add(BackgroundPositionYTopTooltip);
                tooltips.Add(BackgroundPositionYCenterTooltip);
                tooltips.Add(BackgroundPositionYBottomTooltip);
            }

            m_BackgroundPositionAlign.Clear();

            for (var i = 0; i < icons.Count; ++i)
            {
                var icon = icons[i];
                var tooltip = tooltips[i];
                var button = new Button() { iconImage = icon, tooltip = tooltip };
                m_BackgroundPositionAlign.Add(button);
            }

            var toggleButtonGroupState = new ToggleButtonGroupState(2u, 3);
            m_BackgroundPositionAlign.SetValueWithoutNotify(toggleButtonGroupState);
        }
    }
}
