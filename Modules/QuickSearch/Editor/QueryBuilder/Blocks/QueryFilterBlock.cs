// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Search
{
    enum QueryBlockFormat
    {
        Default = 0,
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

    class QueryFilterBlock : QueryBlock
    {
        public static readonly string[] ops = new[] { "=", "<", "<=", ">=", ">" };

        static class InPlaceStyles
        {
            public static readonly GUIStyle objectField = Utils.FromUSS("quick-search-builder-in-place-objectfield");
            public static readonly GUIStyle objectFieldButton = Utils.FromUSS("quick-search-builder-in-place-objectfield-button");
            public static readonly Color toggleTint = new Color(1, 1, 1, 0.6f);
        }

        private int m_InPlaceEditorId;
        public string id { get; private set; }
        public object formatValue { get; set; }
        public QueryBlockFormat format { get; set; }
        public Type formatType { get; set; }
        public QueryMarker marker { get; set; }
        public bool property { get; set; }

        public override bool formatNames => true;
        public override bool wantsEvents => HasInPlaceEditor();
        public override bool canOpenEditorOnValueClicked => !HasInPlaceEditor();

        protected QueryFilterBlock(IQuerySource source, in string id, in string op, in string value)
            : base(source)
        {
            this.id = id;
            this.op = op;
            this.value = UnQuoteString(value) ?? string.Empty;

            if (id.Length > 0 && id[0] == '#')
            {
                property = true;
                name = ObjectNames.NicifyVariableName(id.TrimStart('#').Replace("m_", "").Replace(".", ""));
            }
            else
                name = id.ToLowerInvariant();

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
        }

        private string UnQuoteString(string value)
        {
            if (value != null && value.Length > 2 && value[0] == '"' && value[value.Length-1] == '"')
                return value.Substring(1, value.Length - 2);
            return value;
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
                    source.Apply();
                }
            }
        }

        public override string ToString()
        {
            return $"{id}{op}{FormatStringValue(value)}";
        }

        public override IBlockEditor OpenEditor(in Rect rect)
        {
            var screenRect = new Rect(rect.position + context.searchView.position.position, rect.size);
            switch (format)
            {
                case QueryBlockFormat.Expression: return QueryExpressionBlockEditor.Open(screenRect, this);
                case QueryBlockFormat.Number: return QueryNumberBlockEditor.Open(screenRect, this);
                case QueryBlockFormat.Default: return QueryTextBlockEditor.Open(screenRect, this);
                case QueryBlockFormat.Vector2: return QueryVectorBlockEditor.Open(screenRect, this, 2);
                case QueryBlockFormat.Vector3: return QueryVectorBlockEditor.Open(screenRect, this, 3);
                case QueryBlockFormat.Vector4: return QueryVectorBlockEditor.Open(screenRect, this, 4);
                case QueryBlockFormat.Enum: return QuerySelector.Open(rect, this);
                case QueryBlockFormat.Color:
                    GUIUtility.keyboardControl = m_InPlaceEditorId;
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

        public override IEnumerable<SearchProposition> FetchPropositions()
        {
            if (format == QueryBlockFormat.Enum)
            {
                if (formatValue is Enum ve)
                {
                    foreach (Enum v in ve.GetType().GetEnumValues())
                        yield return new SearchProposition(category: null, label: ObjectNames.NicifyVariableName(v.ToString()), data: v);

                    foreach (Enum v in SearchUtils.FindTypes<Enum>(ve.GetType().Name).SelectMany(t => t.GetEnumValues().OfType<Enum>()))
                    {
                        var vv = Convert.ToInt32(v);
                        var label = $"{ObjectNames.NicifyVariableName(v.ToString())} ({vv})";
                        yield return new SearchProposition(category: $"More/{v.GetType().FullName.Replace("UnityEngine.", "").Replace(".", "/")}",
                            label: label, help: v.GetType().FullName, data: v, priority: vv);
                    }
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

        public override Rect Layout(in Vector2 at, in float availableSpace)
        {
            if (!HasInPlaceEditor())
                return base.Layout(at, availableSpace);

            var labelStyle = Styles.QueryBuilder.label;
            var nameContent = labelStyle.CreateContent(name);
            var editorWidth = GetInPlaceEditorWidth();
            var blockWidth = nameContent.width + editorWidth + labelStyle.margin.horizontal * 2f + blockExtraPadding;
            return GetRect(at, blockWidth, blockHeight);
        }

        protected override void Draw(in Rect blockRect, in Vector2 mousePosition)
        {
            if (!HasInPlaceEditor())
            {
                base.Draw(blockRect, mousePosition);
                return;
            }

            var labelStyle = Styles.QueryBuilder.label;
            var nameContent = labelStyle.CreateContent(name);

            if (Event.current.type == EventType.Repaint)
                DrawBackground(blockRect, mousePosition);

            var nameRect = DrawName(blockRect, mousePosition, nameContent);
            EditorGUI.BeginChangeCheck();
            var newValue = DrawInPlaceEditor(nameRect, blockRect, mousePosition);
            if (EditorGUI.EndChangeCheck())
                SetValue(newValue);

            if (Event.current.type == EventType.Repaint)
                DrawBorders(blockRect, mousePosition);
        }

        protected override Rect DrawSeparator(in Rect at)
        {
            var sepRect = new Rect(at.xMax, at.yMin + 1f, 1f, Mathf.Ceil(at.height - 1f));
            var opRect = new Rect(at.xMax - 6f, at.yMin - 1f, 11f, Mathf.Ceil(at.height - 1f));
            if (string.Equals(op, ">=", StringComparison.Ordinal))
                Styles.QueryBuilder.label.Draw(opRect, "\u2265", false, false, false, false);
            else if (string.Equals(op, "<=", StringComparison.Ordinal))
                Styles.QueryBuilder.label.Draw(opRect, "\u2264", false, false, false, false);
            else if (string.Equals(op, ">", StringComparison.Ordinal))
                Styles.QueryBuilder.label.Draw(opRect, "\u003E", false, false, false, false);
            else if (string.Equals(op, "<", StringComparison.Ordinal))
                Styles.QueryBuilder.label.Draw(opRect, "\u003C", false, false, false, false);
            else
                return base.DrawSeparator(at);

            return sepRect;
        }

        protected override Color GetBackgroundColor()
        {
            return (property ? QueryColors.property : QueryColors.filter);
        }

        protected override void AddContextualMenuItems(GenericMenu menu)
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
        }

        private void SetEnumType(in Type type)
        {
            format = QueryBlockFormat.Enum;
            formatType = type;
            var enums = type.GetEnumValues();
            if (enums.Length > 0)
                SetValue(enums.GetValue(0));
        }

        private bool TryGetExpression(in string text, out string expression, out int start, out int end)
        {
            end = start = -1;
            expression = null;
            if (text == null && text.Length <= 2)
                return false;

            start = text.IndexOfAny(new[] { '{', '[' });
            if (start == -1)
                return false;

            end = text.IndexOfAny(new[] { '}', ']' });
            if (end != text.Length-1)
                return false;

            expression = text.Substring(start + 1, end - start - 1);
            return true;
        }

        public QueryBuilder CreateExpressionBuilder(in string expression)
        {
            var embeddedBuilder = new QueryBuilder(expression) { drawBackground = false, @readonly = true };
            foreach (var b in embeddedBuilder.blocks)
            {
                b.@readonly = true;
                b.disableHovering = true;
            }
            return embeddedBuilder;
        }

        private bool ParseValue(in string value)
        {
            if (TryGetExpression(value, out var expression, out _, out _))
            {
                format = QueryBlockFormat.Expression;
                formatValue = CreateExpressionBuilder(expression);
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
                else if(dimension == 4)
                    format = QueryBlockFormat.Vector4;
                this.value = Utils.ToString(v4);
                this.formatValue = v4;
            }
            else if (!string.IsNullOrEmpty(value) && TryParseObjectValue(value, out var objValue))
            {
                format = QueryBlockFormat.Object;
                formatValue = objValue;
                formatType = formatType ?? objValue?.GetType();
            }
            else
            {
                format = QueryBlockFormat.Default;
                formatValue = value ?? string.Empty;
                return false;
            }

            return true;
        }

        private static bool TryParseObjectValue(in string value, out UnityEngine.Object objValue)
        {
            objValue = null;
            if (string.Equals("none", value, StringComparison.OrdinalIgnoreCase))
                return true;

            if (value.StartsWith("GlobalObjectId", StringComparison.Ordinal) && GlobalObjectId.TryParse(value, out var gid))
            {
                objValue = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                return objValue != null;
            }

            var guid = AssetDatabase.AssetPathToGUID(value);
            if (!string.IsNullOrEmpty(guid))
            {
                objValue = AssetDatabase.LoadMainAssetAtPath(value);
                return true;
            }

            return false;
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
                return $"{{{qb.searchText}}}";

            if (marker.valid || format == QueryBlockFormat.Enum)
                return $"<${format.ToString().ToLowerInvariant()}:{formatValue?.ToString() ?? sv}{GetFormatMarkerArgs()}$>";

            return EscapeLiteralString(sv);
        }

        private object FormatObjectValue()
        {
            if (formatValue == null && formatType != null)
                return $"<$object:none,{formatType.Name}$>";
            return '"' + this.value + '"';
        }

        private string GetFormatMarkerArgs()
        {
            if ((format == QueryBlockFormat.Enum || format == QueryBlockFormat.Object) && formatType != null)
                return $",{formatType.Name}";
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
                formatValue = CreateExpressionBuilder(value);
            }
            source.Repaint();
        }

        private bool HasInPlaceEditor()
        {
            switch (format)
            {
                case QueryBlockFormat.Object:
                case QueryBlockFormat.Color:
                case QueryBlockFormat.Toggle:
                case QueryBlockFormat.Expression:
                    return true;
            }

            return false;
        }

        private float GetInPlaceEditorWidth()
        {
            switch (format)
            {
                case QueryBlockFormat.Object: return 120f;
                case QueryBlockFormat.Color: return 20f;
                case QueryBlockFormat.Toggle: return 4f;
                case QueryBlockFormat.Expression:
                    if (formatValue is QueryBuilder qb)
                    {
                        qb.LayoutBlocks(10000f);
                        return qb.width;
                    }
                    break;
            }

            return 0f;
        }

        private object DrawInPlaceEditor(in Rect at, in Rect blockRect, in Vector2 mousePosition)
        {
            var x = at.xMax - 4f;
            switch (format)
            {
                case QueryBlockFormat.Object:
                    var editorRect = new Rect(x, blockRect.y + 1f, blockRect.width - (x - blockRect.xMin) - 6f, blockRect.height - 2f);
                    var objectFieldType = formatType ?? typeof(UnityEngine.Object);
                    var allowSceneObjects = typeof(Component).IsAssignableFrom(objectFieldType) || typeof(GameObject).IsAssignableFrom(objectFieldType);
                    var result = EditorGUI.ObjectField(editorRect, formatValue as UnityEngine.Object, objectFieldType, allowSceneObjects: allowSceneObjects, InPlaceStyles.objectField, InPlaceStyles.objectFieldButton);
                    m_InPlaceEditorId = EditorGUIUtility.s_LastControlID;
                    return result;
                case QueryBlockFormat.Color:
                    Color c = Color.black;
                    if (formatValue is Color fc)
                        c = fc;
                    editorRect = new Rect(x, blockRect.y + 2f, blockRect.width - (x - blockRect.xMin) - 8f, blockRect.height - 4f);
                    c = EditorGUI.ColorField(editorRect, c, showEyedropper: false, showAlpha: true);
                    m_InPlaceEditorId = EditorGUIUtility.s_LastControlID;
                    return c;
                case QueryBlockFormat.Toggle:
                    var b = false;
                    if (formatValue is bool fb)
                        b = fb;
                    editorRect = new Rect(x, blockRect.y + 2f, blockRect.width - (x - blockRect.xMin) - 8f, blockRect.height - 4f);
                    var oldColor = GUI.color;
                    GUI.color = InPlaceStyles.toggleTint;
                    var value = EditorGUI.Toggle(editorRect, b, EditorStyles.toggle);
                    GUI.color = oldColor;
                    return value;

                case QueryBlockFormat.Expression:
                    if (formatValue is QueryBuilder qb)
                    {
                        valueRect = new Rect(x - 5f, blockRect.yMin - 5f, qb.width + 24f, qb.height);
                        qb.Draw(Event.current, valueRect, createLayout: false);
                        DrawArrow(blockRect, mousePosition, editor != null ? QueryContent.UpArrow : QueryContent.DownArrow);
                        return qb;
                    }
                    break;
            }

            return null;
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
                this.value = Utils.ToString(new Vector4(v2.x, v2.y, float.NaN, float.NaN));
            }
            else if (value is Vector3 v3)
            {
                format = QueryBlockFormat.Vector3;
                this.value = Utils.ToString(v3);
            }
            else if (value is Vector4 v4)
            {
                this.value = Utils.ToString(v4, format == QueryBlockFormat.Vector2 ? 2 : (format == QueryBlockFormat.Vector3 ? 3 : 4));
            }
            else
            {
                this.value = formatValue?.ToString() ?? string.Empty;
            }
            source.Apply();
        }
    }
}
