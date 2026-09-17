// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using Unity.Scripting.LifecycleManagement;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    // Grid-aware display value field: an all-text strip (Flex / Grid / None) when the CSS Grid flag is on,
    // the icon strip (flex, none) with no Grid button when off, rebuilt live when the flag changes.
    [UxmlElement]
    internal partial class DisplayStyleToggleField : BaseField<DisplayStyle>
    {
        static readonly string s_UssClassName = "unity-toggle-button-group_display-style-field";
        static readonly string s_TextModifierClassName = "unity-toggle-button-group_display-style-field--text";

        // Immutable button descriptor table (name/enum/label/tooltip); holds no reloadable references.
        [NoAutoStaticsCleanup]
        static readonly (string name, DisplayStyle value, string label, string tooltip)[] k_Buttons =
        {
            ("flex", DisplayStyle.Flex, "Flex", "Turns the element into a flexible container for aligning and distributing items."),
            ("grid", DisplayStyle.Grid, "Grid", "Lays the element's children out on a CSS Grid of rows and columns."),
            ("none", DisplayStyle.None, "None", "Hides the element in the container. This might have an impact on the layout."),
        };

        readonly ToggleButtonGroup m_ToggleButtonGroup;

        // A rebuild shifts the selection and dispatches a change; that is not a user edit, so ignore it.
        bool m_Rebuilding;

        public ToggleButtonGroup toggleButtonGroup => m_ToggleButtonGroup;

        public DisplayStyleToggleField() : this(null) {}

        public DisplayStyleToggleField(string label) : base(label, new ToggleButtonGroup())
        {
            m_ToggleButtonGroup = visualInput as ToggleButtonGroup;
            m_ToggleButtonGroup.AddToClassList(s_UssClassName);
            m_ToggleButtonGroup.isMultipleSelection = false;
            // Empty selection: never auto-select on rebuild (a spurious change), and allow nothing selected
            // when the value is not in the strip (grid while the flag is off).
            m_ToggleButtonGroup.allowEmptySelection = true;

            Rebuild();
            m_ToggleButtonGroup.RegisterValueChangedCallback(OnStripChange);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                UIToolkitProjectSettings.onEnableGridLayoutChanged -= OnFlagChanged;
                UIToolkitProjectSettings.onEnableGridLayoutChanged += OnFlagChanged;
                // Resync: the flag may have changed while this field was detached (and unsubscribed).
                OnFlagChanged(false);
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
                UIToolkitProjectSettings.onEnableGridLayoutChanged -= OnFlagChanged);
        }

        void OnFlagChanged(bool _)
        {
            Rebuild();
            SetValueWithoutNotify(value);
        }

        void Rebuild()
        {
            m_Rebuilding = true;
            try
            {
                RebuildButtons();
            }
            finally
            {
                m_Rebuilding = false;
            }
        }

        void RebuildButtons()
        {
            for (var b = m_ToggleButtonGroup.GetButton(0); b != null; b = m_ToggleButtonGroup.GetButton(0))
                b.RemoveFromHierarchy();

            var gridOn = UIToolkitProjectSettings.enableGridLayout;
            if (gridOn)
            {
                // All-text strip in None / Flex / Grid order; the flex/none icons are suppressed by the modifier class.
                foreach (var (name, _, label, tooltip) in k_Buttons)
                    m_ToggleButtonGroup.Add(new Button { name = name, text = label, tooltip = tooltip });
            }
            else
            {
                // Original icon strip (flex, none); Grid is not shown while the feature is off.
                AddIconButton("flex");
                AddIconButton("none");
            }

            void AddIconButton(string buttonName)
            {
                foreach (var (name, _, _, tooltip) in k_Buttons)
                    if (name == buttonName)
                    {
                        m_ToggleButtonGroup.Add(new Button { name = name, tooltip = tooltip });
                        return;
                    }
            }
            m_ToggleButtonGroup.EnableInClassList(s_TextModifierClassName, gridOn);
        }

        public override void SetValueWithoutNotify(DisplayStyle newValue)
        {
            base.SetValueWithoutNotify(newValue);
            var state = new ToggleButtonGroupState(0, 64);
            for (var i = 0; m_ToggleButtonGroup.GetButton(i) is { } button; ++i)
            {
                if (button.name == NameOf(newValue))
                {
                    state[i] = true;
                    break;
                }
            }
            m_ToggleButtonGroup.SetValueWithoutNotify(state);
        }

        void OnStripChange(ChangeEvent<ToggleButtonGroupState> evt)
        {
            if (m_Rebuilding)
                return;

            var selected = evt.newValue.GetActiveOptions(stackalloc int[evt.newValue.length]);
            if (selected.IsEmpty)
                return;

            var button = m_ToggleButtonGroup.GetButton(selected[0]);
            if (button == null)
                return;

            foreach (var (name, enumValue, _, _) in k_Buttons)
            {
                if (name == button.name)
                {
                    base.value = enumValue;
                    break;
                }
            }
        }

        static string NameOf(DisplayStyle value)
        {
            foreach (var (name, enumValue, _, _) in k_Buttons)
                if (enumValue == value)
                    return name;
            return "flex";
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
