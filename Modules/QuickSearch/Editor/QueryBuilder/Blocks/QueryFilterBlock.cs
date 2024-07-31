// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    enum QueryBlockFormat
    {
        Text = 0,
        Toggle,
        Number,
        Object,
        Color,
        Enum,
        Vector2,
        Vector3,
        Vector4,
        Expression
    }

    class QueryFilterBlock : QueryBlock, IQueryExpressionBlock
    {
        public static readonly string[] ops = new[] { "=", "<", "<=", ">=", ">" };

        private VisualElement m_InPlaceEditorElement;

        public string id { get; private set; }
        public object formatValue { get; set; }
        public string formatParam { get; set; }
        public QueryBlockFormat format { get; set; }
        public Type formatType { get; set; }
        public QueryMarker marker { get; set; }
        public bool property { get; set; }

        public QueryBuilder builder => formatValue as QueryBuilder;
        internal override bool formatNames => true;
        internal override bool wantsEvents => HasInPlaceEditor();
        internal override bool canOpenEditorOnValueClicked => !HasInPlaceEditor();

        QueryBuilder IQueryExpressionBlock.builder => builder;
        IQuerySource IQueryExpressionBlock.source => source;
        IBlockEditor IQueryExpressionBlock.editor => editor;

        protected QueryFilterBlock(IQuerySource source, in string id, in string op, in string value)
            : base(source)
        {
            this.id = id;
            this.op = op;
            this.value = UnQuoteString(value) ?? string.Empty;

            UpdateName();
            marker = default;
            formatValue = null;
            formatType = null;

            if (!string.IsNullOrEmpty(this.value))
            {
                if (!ParseValue(this.value))
                {
                    if (QueryMarker.TryParse(this.value.GetStringView(), out var marker))
                        ParseMarker(marker);
                }
            }
        }

        public QueryFilterBlock(IQuerySource source, FilterNode node)
            : this(source, node.filterId, node.operatorId, node.rawFilterValueStringView.ToString())
        {
            if (!string.IsNullOrEmpty(node.paramValue))
            {
                formatParam = node.paramValue;
                UpdateName();
            }
        }

        private string NiceName(in string name)
        {
            return ObjectNames.NicifyVariableName(name.TrimStart('#').Replace("m_", "").Replace(".", ""));
        }

        private string UnQuoteString(string value)
        {
            if (value != null && value.Length > 2 && value[0] == '"' && value[value.Length-1] == '"')
                return value.Substring(1, value.Length - 2);
            return value;
        }

        public void ApplyExpression(string searchText)
        {
            format = QueryBlockFormat.Expression;
            formatValue = ExpressionBlock.Create(searchText);
        }

        public override void Apply(in SearchProposition searchProposition)
        {
            if (format == QueryBlockFormat.Enum)
            {
                if (searchProposition.data is Enum e)
                    SetValue(e);
                else if (searchProposition.type?.IsEnum == true)
                {
                    SetEnumType(searchProposition.type);
                    ApplyChanges();
                }
            }
        }

        public override string ToString()
        {
            return $"{FormatFilterName()}{op}{FormatStringValue(value)}";
        }

        private string FormatFilterName()
        {
            if (formatParam != null)
                return $"{id}({formatParam})";
            return id;
        }

        internal override IBlockEditor OpenEditor(in Rect rect)
        {
            var screenRect = new Rect((parent != null ? worldBound.position : rect.position) + context.searchView.position.position, rect.size);
            if (parent != null)
                screenRect.y -= screenRect.height;
            switch (format)
            {
                case QueryBlockFormat.Expression: return QueryExpressionBlockEditor.Open(screenRect, this);
                case QueryBlockFormat.Number: return QueryNumberBlockEditor.Open(screenRect, this);
                case QueryBlockFormat.Vector2: return QueryVectorBlockEditor.Open(screenRect, this, 2);
                case QueryBlockFormat.Vector3: return QueryVectorBlockEditor.Open(screenRect, this, 3);
                case QueryBlockFormat.Vector4: return QueryVectorBlockEditor.Open(screenRect, this, 4);
                case QueryBlockFormat.Enum: return QuerySelector.Open(rect, this);
                case QueryBlockFormat.Color:
                    Color c = Color.black;
                    if (formatValue is Color fc)
                        c = fc;
                    ColorPicker.Show(GUIView.current, c, true, false);
                    return null;
                case QueryBlockFormat.Toggle:
                    var v = (bool)formatValue;
                    SetValue(!v);
                    return null;
            }

            return null;
        }

        internal override IEnumerable<SearchProposition> FetchPropositions()
        {
            if (format == QueryBlockFormat.Enum)
            {
                if (formatValue is Enum ve)
                {
                    foreach (Enum v in ve.GetType().GetEnumValues())
                        yield return new SearchProposition(category: null, label: ObjectNames.NicifyVariableName(v.ToString()), data: v);
                }
                else
                {
                    foreach (var e in TypeCache.GetTypesDerivedFrom<Enum>())
                    {
                        if (!e.IsVisible)
                            continue;
                        var category = e.FullName;
                        var cpos = category.LastIndexOf('.');
                        if (cpos != -1)
                            category = category.Substring(0, cpos);
                        category = category.Replace(".", "/");
                        yield return new SearchProposition(category: category, label: ObjectNames.NicifyVariableName(e.Name), type: e);
                    }
                }
            }
        }

        internal override void CreateBlockElement(VisualElement container)
        {
            AddLabel(container, name);

            if (string.Equals(op, ">=", StringComparison.Ordinal))
                AddLabel(container, "\u2265");
            else if (string.Equals(op, "<=", StringComparison.Ordinal))
                AddLabel(container, "\u2264");
            else if (string.Equals(op, ">", StringComparison.Ordinal))
                AddLabel(container, "\u003E");
            else if (string.Equals(op, "<", StringComparison.Ordinal))
                AddLabel(container, "\u003C");
            else if (!HasInPlaceEditor())
                AddSeparator(container);

            if (!HasInPlaceEditor())
            {
                AddLabel(container, GetValueLabel());
                AddOpenEditorArrow(container);
            }
            else
            {
                if (m_InPlaceEditorElement != null)
                    UpdateInPlaceEditorValue();
                else
                    m_InPlaceEditorElement = CreateInPlaceEditorElement();
                if (m_InPlaceEditorElement != null)
                    container.Add(m_InPlaceEditorElement);
            }
        }

        internal string GetValueLabel()
        {
            if (formatValue == null)
                return value;

            switch (format)
            {
                case QueryBlockFormat.Vector2:
                {
                    var v4 = (Vector4)formatValue;
                    var v2 = new Vector2(v4.x, v4.y);
                    return v2.ToString();
                }
                case QueryBlockFormat.Vector3:
                {
                    var v4 = (Vector4)formatValue;
                    var v3 = new Vector3(v4.x, v4.y, v4.z);
                    return v3.ToString();
                }
                default: return formatValue.ToString();
            }
        }

        private VisualElement CreateInPlaceEditorElement()
        {
            switch (format)
            {
                case QueryBlockFormat.Text:
                    var textField = new TextField() { value = Convert.ToString(formatValue) };
                    textField.RegisterCallback<ChangeEvent<string>>(evt => SetValue(evt.newValue));
                    return textField;

                case QueryBlockFormat.Object:
                    var objectFieldType = formatType ?? typeof(UnityEngine.Object);
                    var allowSceneObjects = typeof(Component).IsAssignableFrom(objectFieldType) || typeof(GameObject).IsAssignableFrom(objectFieldType);
                    var objField = new UIElements.ObjectField()
                    {
                        allowSceneObjects = allowSceneObjects,
                        objectType = objectFieldType,
                        value = formatValue as UnityEngine.Object
                    };
                    objField.RegisterCallback<ChangeEvent<UnityEngine.Object>>(evt => SetValue(evt.newValue));
                    return objField;

                case QueryBlockFormat.Color:
                    Color c = Color.black;
                    if (formatValue is Color fc)
                        c = fc;
                    var colorField = new UIElements.ColorField() { value = c, showAlpha = true, showEyeDropper = false };
                    colorField.RegisterCallback<ChangeEvent<Color>>(evt => SetValue(evt.newValue));
                    return colorField;

                case QueryBlockFormat.Toggle:
                    var b = false;
                    if (formatValue is bool fb)
                        b = fb;
                    var toggle = new Toggle() { value = b };
                    toggle.RegisterCallback<ChangeEvent<bool>>(evt => SetValue(evt.newValue));
                    return toggle;

                 case QueryBlockFormat.Expression:
                    var expressionContainer = new VisualElement();
                    foreach (var bh in builder.EnumerateBlocks())
                        expressionContainer.Add(bh.CreateGUI());
                    return expressionContainer;

                default:
                    Debug.LogError($"Cannot create GUI for {GetType().Name} with {format}");
                    break;
            }

            return null;
        }

        private void UpdateInPlaceEditorValue()
        {
            switch (format)
            {
                case QueryBlockFormat.Text:
                    if (m_InPlaceEditorElement is TextField tf && Convert.ToString(formatValue) is string v && !string.Equals(v, tf.value, StringComparison.Ordinal))
                        tf.SetValueWithoutNotify(v);
                    break;
                case QueryBlockFormat.Object:
                    if (m_InPlaceEditorElement is UIElements.ObjectField objField && (formatValue as UnityEngine.Object) != objField.value)
                        objField.SetValueWithoutNotify(formatValue as UnityEngine.Object);
                    break;

                case QueryBlockFormat.Color:
                    Color c = Color.black;
                    if (formatValue is Color fc)
                        c = fc;
                    if (m_InPlaceEditorElement is UIElements.ColorField colorField && c != colorField.value)
                        colorField.SetValueWithoutNotify(c);
                    break;

                case QueryBlockFormat.Toggle:

                    var b = false;
                    if (formatValue is bool fb)
                        b = fb;
                    if (m_InPlaceEditorElement is Toggle toggleField && b != toggleField.value)
                        toggleField.SetValueWithoutNotify(b);
                    break;

                  case QueryBlockFormat.Expression:
                      // For expressions, the formatValue is the builder, so recreate the blocks in the container.
                      var expressionContainer = m_InPlaceEditorElement;
                      expressionContainer.Clear();
                      foreach (var bh in builder.EnumerateBlocks())
                          expressionContainer.Add(bh.CreateGUI());
                      break;

                default:
                    throw new NotImplementedException($"Failed to update GUI value ({this}) for {GetType().Name} with {format}");
            }
        }

        internal override Color GetBackgroundColor()
        {
            return (property ? QueryColors.property : QueryColors.filter);
        }

        internal override void AddContextualMenuItems(GenericMenu menu)
        {
            foreach (var _e in Enum.GetValues(typeof(QueryBlockFormat)))
            {
                var e = (QueryBlockFormat)_e;
                menu.AddItem(EditorGUIUtility.TrTextContent($"Format/{ObjectNames.NicifyVariableName(e.ToString())}"), format == e, () => SetFormat(e));
            }

            menu.AddItem(EditorGUIUtility.TrTextContent($"Operator/Equal (=)"), string.Equals(op, "=", StringComparison.Ordinal), () => SetOperator("="));
            menu.AddItem(EditorGUIUtility.TrTextContent($"Operator/Contains (:)"), string.Equals(op, ":", StringComparison.Ordinal), () => SetOperator(":"));
            menu.AddItem(EditorGUIUtility.TrTextContent($"Operator/Less Than or Equal (<=)"), string.Equals(op, "<=", StringComparison.Ordinal), () => SetOperator("<="));
            menu.AddItem(EditorGUIUtility.TrTextContent($"Operator/Greater Than or Equal (>=)"), string.Equals(op, ">=", StringComparison.Ordinal), () => SetOperator(">="));
            menu.AddItem(EditorGUIUtility.TrTextContent($"Operator/Less Than (<)"), string.Equals(op, "<", StringComparison.Ordinal), () => SetOperator("<"));
            menu.AddItem(EditorGUIUtility.TrTextContent($"Operator/Greater Than (>)"), string.Equals(op, ">", StringComparison.Ordinal), () => SetOperator(">"));

            if (formatParam != null)
                menu.AddItem(EditorGUIUtility.TrTextContent($"Edit Parameter..."), false, () => EditParameter());
        }

        private void EditParameter()
        {
            var screenRect = new Rect(drawRect.position + context.searchView.position.position, drawRect.size);
            editor = QueryParamBlockEditor.Open(screenRect, this);
        }

        public void UpdateName()
        {
            property = false;
            if (formatParam != null)
            {
                property = true;
                name = $"<b>{id}</b>(<i>{NiceName(formatParam)}</i>)";
            }
            else
            {
                if (id.Length > 0 && id[0] == '#')
                {
                    property = true;
                    name = NiceName(id);
                }
                else
                    name = id.ToLowerInvariant();
            }
        }

        private void SetEnumType(in Type type)
        {
            format = QueryBlockFormat.Enum;
            formatType = type;
            var enums = type.GetEnumValues();
            if (enums.Length > 0)
                SetValue(enums.GetValue(0));
        }

        private bool TryGetExpression(in string text, out SearchExpression expression)
        {
            expression = null;
            if (string.IsNullOrEmpty(text) || text.Length <= 2)
                return false;

            var start = text.IndexOfAny(new[] { '{', '[' });
            if (start == -1)
                return false;

            var end = text.LastIndexOfAny(new[] { '}', ']' });
            if (end == -1)
                return false;

            expression = SearchExpression.Parse(text, SearchExpressionParserFlags.None);
            if (expression == null)
                return false;

            if (!expression.types.HasAny(SearchExpressionType.Iterable | SearchExpressionType.Function))
                return false;

            return true;
        }

        private bool ParseValue(in string value)
        {
            if (TryGetExpression(value, out var expression))
            {
                ApplyExpression(expression.outerText.ToString());
            }
            else if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
            {
                format = QueryBlockFormat.Toggle;
                formatValue = true;
            }
            else if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
            {
                format = QueryBlockFormat.Toggle;
                formatValue = false;
            }
            else if (Utils.TryParse(value, out float number))
            {
                format = QueryBlockFormat.Number;
                formatValue = number;
            }
            else if (!string.IsNullOrEmpty(value) && value[0] == '#' && ColorUtility.TryParseHtmlString(value, out var c))
            {
                format = QueryBlockFormat.Color;
                formatValue = c;
            }
            else if (Utils.TryParseVectorValue(value, out var v4, out var dimension))
            {
                if (dimension == 2)
                    format = QueryBlockFormat.Vector2;
                else if (dimension == 3)
                    format = QueryBlockFormat.Vector3;
                else if (dimension == 4)
                    format = QueryBlockFormat.Vector4;
                this.value = Utils.ToString(v4, dimension);
                this.formatValue = v4;
            }
            else if (Utils.TryParseObjectValue(value, out var objValue))
            {
                format = QueryBlockFormat.Object;
                formatValue = objValue;
                formatType = formatType ?? objValue?.GetType();
            }
            else
            {
                format = QueryBlockFormat.Text;
                formatValue = value ?? string.Empty;
                return false;
            }

            return true;
        }

        private void ParseMarker(in QueryMarker marker)
        {
            this.marker = marker;
            formatValue = marker.value;
            value = marker.value?.ToString();

            foreach (QueryBlockFormat e in Enum.GetValues(typeof(QueryBlockFormat)))
            {
                if (!string.Equals(e.ToString(), marker.type, StringComparison.OrdinalIgnoreCase))
                    continue;

                format = e;
                break;
            }

            switch (format)
            {
                case QueryBlockFormat.Enum:
                    {
                        formatType = ParseMarkerType<Enum>(marker, 1);
                        if (formatType == null)
                            break;
                        foreach (var e in Enum.GetValues(formatType))
                        {
                            if (string.Equals(e.ToString(), marker.value?.ToString(), StringComparison.OrdinalIgnoreCase))
                            {
                                formatValue = e;
                                break;
                            }
                            else if (!(formatValue is Enum))
                                formatValue = e;
                        }

                        break;
                    }

                case QueryBlockFormat.Object:
                    {
                        formatType = ParseMarkerType<UnityEngine.Object>(marker, 1);
                        if (formatType == null)
                            break;
                        if (marker.value is string assetPath)
                            ParseValue(assetPath);
                        break;
                    }

                default:
                    if (marker.value is string s)
                        ParseValue(s);
                    break;
            }
        }

        private static Type ParseMarkerType<T>(in QueryMarker marker, int typeArgIndex = 1)
        {
            var typeString = marker.EvaluateArgs().Skip(typeArgIndex).FirstOrDefault()?.ToString();
            if (string.IsNullOrEmpty(typeString))
                return null;
            return SearchUtils.FindType<T>(typeString);
        }

        private object FormatStringValue(in string sv)
        {
            if (format == QueryBlockFormat.Object)
                return FormatObjectValue();

            if (format == QueryBlockFormat.Expression && formatValue is QueryBuilder qb)
                return qb.searchText;

            if (marker.valid || format == QueryBlockFormat.Enum)
                return $"<${format.ToString().ToLowerInvariant()}:{formatValue?.ToString() ?? sv}{GetFormatMarkerArgs()}$>";

            return EscapeLiteralString(sv);
        }

        private object FormatObjectValue()
        {
            if (formatValue == null && formatType != null)
                return $"<$object:none,{formatType.FullName}$>";
            return '"' + this.value + '"';
        }

        private string GetFormatMarkerArgs()
        {
            if ((format == QueryBlockFormat.Enum || format == QueryBlockFormat.Object) && formatType != null)
                return $",{formatType.FullName}";
            return string.Empty;
        }

        private void SetFormat(in QueryBlockFormat format)
        {
            this.format = format;
            if (format == QueryBlockFormat.Vector2 || format == QueryBlockFormat.Vector3 || format == QueryBlockFormat.Vector4)
            {
                if (Utils.TryParseVectorValue(value, out var v4, out _))
                    SetValue(v4);
                else
                    formatValue = new Vector4(float.NaN, float.NaN, float.NaN, float.NaN);
            }
            else if (format == QueryBlockFormat.Expression)
            {
                formatValue = ExpressionBlock.Create(value);
            }
            ApplyChanges();
        }

        private bool HasInPlaceEditor()
        {
            switch (format)
            {
                case QueryBlockFormat.Text:
                case QueryBlockFormat.Object:
                case QueryBlockFormat.Color:
                case QueryBlockFormat.Toggle:
                case QueryBlockFormat.Expression:
                    return true;
            }

            return false;
        }

        public void SetValue(object value)
        {
            formatValue = value;
            if (value is Color c)
            {
                this.op = "=";
                this.value = $"#{ColorUtility.ToHtmlStringRGB(c).ToLowerInvariant()}";
            }
            else if (value is UnityEngine.Object obj)
            {
                this.value = SearchUtils.GetObjectPath(obj) ?? string.Empty;
            }
            else if (value is Enum e)
            {
                op = "=";
                this.value = e.ToString();
            }
            else if (value is Vector2 v2)
            {
                format = QueryBlockFormat.Vector2;
                var v4 = new Vector4(v2.x, v2.y, float.NaN, float.NaN);
                formatValue = v4; // The editor expects a Vector4
                this.value = Utils.ToString(v4, 2);
            }
            else if (value is Vector3 v3)
            {
                format = QueryBlockFormat.Vector3;
                var v4 = new Vector4(v3.x, v3.y, v3.z, float.NaN);
                formatValue = v4; // The editor expects a Vector4
                this.value = Utils.ToString(v4, 3);
            }
            else if (value is Vector4 v4)
            {
                this.value = Utils.ToString(v4, format == QueryBlockFormat.Vector2 ? 2 : (format == QueryBlockFormat.Vector3 ? 3 : 4));
            }
            else
            {
                this.value = formatValue?.ToString() ?? string.Empty;
            }
            ApplyChanges();
        }

        void IQueryExpressionBlock.CloseEditor()
        {
            editor = null;
        }

        void IQueryExpressionBlock.ApplyExpression(string searchText)
        {
            builder.searchText = searchText;
            builder.Build();
            ApplyChanges();
        }

        void IQueryExpressionBlock.ApplyChanges()
        {
            ApplyChanges();
        }
    }
}
