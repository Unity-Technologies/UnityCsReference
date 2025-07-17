// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    abstract class QueryBlockEditor<T, B> : EditorWindow, IBlockEditor where B : QueryBlock
    {
        const string k_BlockEditorUssClassName = "search-query-block-editor";
        const string k_PopupWindowUssClassName = "unity-popup-window-root";

        protected VisualElement m_ContentUI;

        public T value;
        public object[] args { get; set; }
        public EditorWindow window => this;
        public B block { get; protected set; }

        public void CreateGUI()
        {
            if (block == null)
            {
                Close();
                return;
            }

            SearchElement.AppendStyleSheets(rootVisualElement);
            rootVisualElement.AddToClassList(k_PopupWindowUssClassName);
            rootVisualElement.AddToClassList(k_BlockEditorUssClassName);
            rootVisualElement.RegisterCallback<KeyDownEvent>(HandleKeyboardEvent, TrickleDown.TrickleDown);

            m_ContentUI = CreateUI();
            m_ContentUI.RegisterCallback<AttachToPanelEvent>(OnContentAttachToPanel);
            rootVisualElement.Add(m_ContentUI);
        }

        void HandleKeyboardEvent(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape || evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Return)
            {
                evt.StopPropagation();
                Close();
            }
        }

        public void OnDisable()
        {
            block?.CloseEditor();
        }

        protected abstract VisualElement CreateUI();

        protected virtual void OnContentAttachToPanel(AttachToPanelEvent evt)
        {}

        protected virtual void Apply(in T value)
        {
            if (block is QueryFilterBlock filterBlock)
            {
                filterBlock.formatValue = value;
                filterBlock.UpdateName();
            }
            block.ApplyChanges();
        }

        protected IBlockEditor Show(in B block, in Rect rect, in float width = 400f, in float height = 0f)
        {
            this.block = block;
            minSize = Vector2.zero;
            maxSize = new Vector2(width, height == 0f ? EditorGUI.kSingleLineHeight * 1.5f : height);

            var popupRect = new Rect(new Vector2(rect.x, rect.yMax), maxSize);
            var windowRect = new Rect(new Vector2(rect.x, rect.yMax + rect.height), maxSize);
            ShowAsDropDown(popupRect, maxSize);
            position = windowRect;
            m_Parent.window.m_DontSaveToLayout = true;
            return this;
        }
    }

    class QueryTextBlockEditor : QueryBlockEditor<string, QueryBlock>
    {
        public static IBlockEditor Open(in Rect rect, QueryBlock block)
        {
            var w = CreateInstance<QueryTextBlockEditor>();
            w.value = block.value;
            return w.Show(block, rect, 200f);
        }

        protected override VisualElement CreateUI()
        {
            var textField = new TextField();
            textField.value = value;
            textField.RegisterValueChangedCallback(evt =>
            {
                Apply(evt.newValue);
            });
            return textField;
        }

        protected override void OnContentAttachToPanel(AttachToPanelEvent evt)
        {
            m_ContentUI.Focus();
        }

        protected override void Apply(in string value)
        {
            block.value = value;
            base.Apply(value);
        }
    }

    class QueryParamBlockEditor : QueryBlockEditor<string, QueryFilterBlock>
    {
        public static IBlockEditor Open(in Rect rect, QueryFilterBlock block)
        {
            var w = CreateInstance<QueryParamBlockEditor>();
            w.value = block.formatParam;
            return w.Show(block, rect, 200f);
        }

        protected override VisualElement CreateUI()
        {
            var textField = new TextField();
            textField.value = value;
            textField.RegisterValueChangedCallback(evt =>
            {
                Apply(evt.newValue);
            });
            return textField;
        }

        protected override void OnContentAttachToPanel(AttachToPanelEvent evt)
        {
            m_ContentUI.Focus();
        }

        protected override void Apply(in string value)
        {
            block.formatParam = value;
            if (block is QueryFilterBlock filterBlock)
            {
                filterBlock.UpdateName();
            }
            block.ApplyChanges();
        }
    }

    abstract class QueryFilterBlockEditor<T> : QueryBlockEditor<T, QueryFilterBlock>
    {
        const string k_BaseUssClassName = "search-query-block-filter-editor";
        static readonly string k_OperatorSelectorUssClassName = k_BaseUssClassName.WithUssElement("operator-selector");

        protected VisualElement m_FilterEditor;

        protected override VisualElement CreateUI()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;

            var operatorField = new PopupField<string>(QueryFilterBlock.ops, block.op);
            operatorField.AddToClassList(k_OperatorSelectorUssClassName);
            operatorField.RegisterValueChangedCallback(evt =>
            {
                block.SetOperator(evt.newValue);
            });
            row.Add(operatorField);

            m_FilterEditor = CreateFilterEditor();
            row.Add(m_FilterEditor);

            return row;
        }

        protected abstract VisualElement CreateFilterEditor();
    }

    abstract class QueryNumericalCompositeFieldEditor<T> : QueryFilterBlockEditor<T>
    {
        VisualElement m_CompositeField;

        protected override VisualElement CreateUI()
        {
            var row = base.CreateUI();
            row.AddToClassList(BaseCompositeField<float, FloatField, float>.ussClassName);
            return row;
        }

        protected override VisualElement CreateFilterEditor()
        {
            m_CompositeField = CreateAndPopulateCompositeField();
            m_CompositeField.delegatesFocus = true;
            return m_CompositeField;
        }

        protected override void OnContentAttachToPanel(AttachToPanelEvent evt)
        {
            var focusable = m_CompositeField.GetFocusDelegate();
            focusable?.Focus();
        }

        protected abstract void PopulateCompositeField(VisualElement compositeField);

        VisualElement CreateAndPopulateCompositeField()
        {
            var inputContainer = CreateCompositeField();
            PopulateCompositeField(inputContainer);
            return inputContainer;
        }

        public static VisualElement CreateCompositeField()
        {
            var inputContainer = new VisualElement();
            inputContainer.AddToClassList(BaseCompositeField<Vector4, FloatField, float>.inputUssClassName);
            return inputContainer;
        }

        public static FloatField CreateCompositeFieldComponent(string label, float value, Action<float> onChangedCallback, bool allowUnsetThroughMouse = true)
        {
            var field = new FloatField(label);
            field.AddToClassList(BaseCompositeField<Vector4, FloatField, float>.fieldUssClassName);
            field.value = value;
            if (float.IsNaN(value))
                field.showMixedValue = true;

            if (allowUnsetThroughMouse)
            {
                field.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == (int)MouseButton.MiddleMouse)
                    {
                        field.value = float.NaN;
                        field.showMixedValue = true;
                        evt.StopPropagation();
                    }
                }, TrickleDown.TrickleDown);
            }

            field.RegisterValueChangedCallback(evt =>
            {
                onChangedCallback(evt.newValue);
            });
            return field;
        }
    }

    class QueryNumberBlockEditor : QueryNumericalCompositeFieldEditor<float>
    {
        public static IBlockEditor Open(in Rect rect, QueryFilterBlock block)
        {
            var w = CreateInstance<QueryNumberBlockEditor>();
            w.value = Convert.ToSingle(block.formatValue);
            w.block = block;
            return w.Show(block, rect, 150f);
        }

        protected override void PopulateCompositeField(VisualElement compositeField)
        {
            var floatField = CreateCompositeFieldComponent("Value", value, newValue =>
            {
                Apply(newValue);
            });
            compositeField.Add(floatField);
        }

        protected override void Apply(in float value)
        {
            block.value = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            base.Apply(value);
        }
    }

    class QueryVectorBlockEditor : QueryNumericalCompositeFieldEditor<Vector4>
    {
        int dimension { get; set; }

        public static IBlockEditor Open(in Rect rect, QueryFilterBlock block, int dimension)
        {
            var w = CreateInstance<QueryVectorBlockEditor>();
            w.block = block;
            w.value = (Vector4)block.formatValue;
            w.dimension = dimension;
            var width = dimension * 80f + 30f;
            return w.Show(block, rect, width);
        }

        protected override void PopulateCompositeField(VisualElement compositeField)
        {
            if (dimension >= 2)
            {
                compositeField.Add(CreateCompositeFieldComponent("X", value.x, v =>
                {
                    value.x = v;
                    Apply(value);
                }));
                compositeField.Add(CreateCompositeFieldComponent("Y", value.y, v =>
                {
                    value.y = v;
                    Apply(value);
                }));
            }

            if (dimension >= 3)
                compositeField.Add(CreateCompositeFieldComponent("Z", value.z, v =>
                {
                    value.z = v;
                    Apply(value);
                }));

            if (dimension >= 4)
                compositeField.Add(CreateCompositeFieldComponent("W", value.w, v =>
                {
                    value.w = v;
                    Apply(value);
                }));
        }

        protected override void Apply(in Vector4 value)
        {
            block.SetValue(value);
        }
    }

    class QueryRangeBlockEditor : QueryNumericalCompositeFieldEditor<PropertyRange>
    {
        public static IBlockEditor Open(in Rect rect, QueryFilterBlock block)
        {
            var w = CreateInstance<QueryRangeBlockEditor>();
            w.block = block;
            w.value = (PropertyRange)block.formatValue;
            var width = 2 * 80f + 30f;
            return w.Show(block, rect, width);
        }

        protected override void PopulateCompositeField(VisualElement compositeField)
        {
            compositeField.Add(CreateCompositeFieldComponent("Min", value.min, v =>
            {
                value = new PropertyRange(v, value.max);
                Apply(value);
            }));
            compositeField.Add(CreateCompositeFieldComponent("Max", value.max, v =>
            {
                value = new PropertyRange(value.min, v);
                Apply(value);
            }));
        }

        protected override void Apply(in PropertyRange value)
        {
            block.SetValue(value);
        }
    }

    abstract class QueryFilterMarkerBlockEditor<T> : QueryFilterBlockEditor<T>
    {
        protected QueryMarker m_Marker;

        protected virtual void UpdateBlockMarker()
        {
            block.UpdateMarker(m_Marker);
            block.ApplyChanges();
        }

        protected TArg EvaluateArgument<TArg>(QueryMarkerArgument argument, TArg defaultValue)
        {
            var evaluatedArg = argument.Evaluate();
            // Return the first evaluated arg (we do not support many evaluated args per raw arg).
            foreach (var o in evaluatedArg)
            {
                if (o is TArg oAsTArg)
                    return oAsTArg;
            }
            return defaultValue;
        }
    }

    class QueryBoundNumberBlockEditor : QueryFilterMarkerBlockEditor<float>
    {
        float m_Min;
        float m_Max;

        Slider m_ValueSlider;
        FloatField m_ValueField;

        public static IBlockEditor Open(in Rect rect, QueryFilterBlock block)
        {
            if (!block.marker.valid || block.marker.args.Length != 3)
                throw new ArgumentException("Invalid bound number marker", nameof(block.marker));
            var w = CreateInstance<QueryBoundNumberBlockEditor>();
            w.block = block;
            w.value = Convert.ToSingle(block.formatValue);
            w.m_Marker = block.marker;
            return w.Show(block, rect, 400f, EditorGUI.kSingleLineHeight * 3f);
        }

        protected override VisualElement CreateFilterEditor()
        {
            var minArg = m_Marker.args[1];
            var maxArg = m_Marker.args[2];

            // Numbers are stored as double in arguments
            m_Min = (float)EvaluateArgument<double>(minArg, value);
            m_Max = (float)EvaluateArgument<double>(maxArg, value);

            var editorContainer = new VisualElement();
            editorContainer.AddToClassList("search-query-block-bound-number-editor");
            m_ValueSlider = new Slider("Value", m_Min, m_Max);
            m_ValueSlider.value = value;
            m_ValueSlider.RegisterValueChangedCallback(evt =>
            {
                value = evt.newValue;
                m_ValueField.SetValueWithoutNotify(value);
                Apply(evt.newValue);
            });
            editorContainer.Add(m_ValueSlider);

            var fieldContainer = QueryNumericalCompositeFieldEditor<float>.CreateCompositeField();
            m_ValueField = QueryNumericalCompositeFieldEditor<float>.CreateCompositeFieldComponent("Value", value, v =>
            {
                if (v > m_Max)
                {
                    m_ValueField.SetValueWithoutNotify(m_Max);
                    v = m_Max;
                }
                else if (v < m_Min)
                {
                    m_ValueField.SetValueWithoutNotify(m_Min);
                    v = m_Min;
                }

                m_ValueSlider.value = v;
            }, false);
            fieldContainer.Add(m_ValueField);
            fieldContainer.Add(QueryNumericalCompositeFieldEditor<float>.CreateCompositeFieldComponent("Min", m_Min, v =>
            {
                m_Min = v;
                m_ValueSlider.lowValue = v;
                UpdateBlockMarker();
            }, false));
            fieldContainer.Add(QueryNumericalCompositeFieldEditor<float>.CreateCompositeFieldComponent("Max", m_Max, v =>
            {
                m_Max = v;
                m_ValueSlider.highValue = v;
                UpdateBlockMarker();
            }, false));

            editorContainer.Add(fieldContainer);

            return editorContainer;
        }

        protected override void Apply(in float value)
        {
            block.value = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            base.Apply(value);
        }

        protected override void OnContentAttachToPanel(AttachToPanelEvent evt)
        {
            m_ValueField.Focus();
        }

        protected override void UpdateBlockMarker()
        {
            if (QueryMarker.TryParse($"<$number:{value},{m_Min},{m_Max}$>", out var newMarker))
            {
                m_Marker = newMarker;
            }
            base.UpdateBlockMarker();
        }
    }

    class QueryBoundRangeBlockEditor : QueryFilterMarkerBlockEditor<PropertyRange>
    {
        float m_Min;
        float m_Max;

        MinMaxSlider m_RangeSlider;
        FloatField m_RangeMinField;
        FloatField m_RangeMaxField;

        public static IBlockEditor Open(in Rect rect, QueryFilterBlock block)
        {
            if (!block.marker.valid || block.marker.args.Length != 3)
                throw new ArgumentException("Invalid bound range marker", nameof(block.marker));
            var w = CreateInstance<QueryBoundRangeBlockEditor>();
            w.block = block;
            w.value = (PropertyRange)block.formatValue;
            w.m_Marker = block.marker;
            return w.Show(block, rect, 600f, EditorGUI.kSingleLineHeight * 3f);
        }

        protected override VisualElement CreateFilterEditor()
        {
            var minArg = m_Marker.args[1];
            var maxArg = m_Marker.args[2];

            // Numbers are stored as double in arguments
            m_Min = (float)EvaluateArgument<double>(minArg, value.min);
            m_Max = (float)EvaluateArgument<double>(maxArg, value.max);

            var editorContainer = new VisualElement();
            editorContainer.AddToClassList("search-query-block-bound-range-editor");
            m_RangeSlider = new MinMaxSlider("Range", value.min, value.max, m_Min, m_Max);
            m_RangeSlider.RegisterValueChangedCallback(evt =>
            {
                value = new PropertyRange(evt.newValue.x, evt.newValue.y);
                m_RangeMinField.SetValueWithoutNotify(value.min);
                m_RangeMaxField.SetValueWithoutNotify(value.max);
                Apply(value);
            });
            editorContainer.Add(m_RangeSlider);

            var fieldContainer = QueryNumericalCompositeFieldEditor<float>.CreateCompositeField();
            m_RangeMinField = QueryNumericalCompositeFieldEditor<float>.CreateCompositeFieldComponent("Range Min", value.min, v =>
            {
                if (v > m_Max)
                {
                    m_RangeMinField.SetValueWithoutNotify(m_Max);
                    v = m_Max;
                }
                else if (v < m_Min)
                {
                    m_RangeMinField.SetValueWithoutNotify(m_Min);
                    v = m_Min;
                }

                m_RangeSlider.value = new Vector2(v, value.max);
            }, false);
            fieldContainer.Add(m_RangeMinField);

            m_RangeMaxField = QueryNumericalCompositeFieldEditor<float>.CreateCompositeFieldComponent("Range Max", value.max, v =>
            {
                if (v > m_Max)
                {
                    m_RangeMaxField.SetValueWithoutNotify(m_Max);
                    v = m_Max;
                }
                else if (v < m_Min)
                {
                    m_RangeMaxField.SetValueWithoutNotify(m_Min);
                    v = m_Min;
                }

                m_RangeSlider.value = new Vector2(value.min, v);
            }, false);
            fieldContainer.Add(m_RangeMaxField);

            fieldContainer.Add(QueryNumericalCompositeFieldEditor<float>.CreateCompositeFieldComponent("Lower Limit", m_Min, v =>
            {
                m_Min = v;
                m_RangeSlider.lowLimit = v;
                UpdateBlockMarker();
            }, false));
            fieldContainer.Add(QueryNumericalCompositeFieldEditor<float>.CreateCompositeFieldComponent("Upper Limit", m_Max, v =>
            {
                m_Max = v;
                m_RangeSlider.highLimit = v;
                UpdateBlockMarker();
            }, false));

            editorContainer.Add(fieldContainer);

            return editorContainer;
        }

        protected override void Apply(in PropertyRange value)
        {
            block.SetValue(value);
        }

        protected override void OnContentAttachToPanel(AttachToPanelEvent evt)
        {
            m_RangeMinField.Focus();
        }

        protected override void UpdateBlockMarker()
        {
            if (QueryMarker.TryParse($"<$range:{value},{m_Min},{m_Max}$>", out var newMarker))
            {
                m_Marker = newMarker;
            }
            base.UpdateBlockMarker();
        }
    }
}
