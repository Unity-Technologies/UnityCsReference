// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Search
{
    abstract class QueryListBlock : QueryBlock
    {
        const float iconSize = 16f;

        public readonly string id;
        public readonly string category;
        protected string label;
        protected Texture2D icon;
        protected bool alwaysDrawLabel;

        public abstract IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags = SearchPropositionFlags.None);

        protected QueryListBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source)
        {
            this.id = id ?? attr.id;
            this.op = attr.op;
            this.name = attr.name;
            this.value = value;
            this.category = attr.category;
        }

        protected QueryListBlock(IQuerySource source, string id, string op, string value, string category = null)
            : base(source)
        {
            this.id = id;
            this.op = op;
            this.value = value;
            this.category = category;
        }

        protected string GetCategory(SearchPropositionFlags flags)
        {
            return flags.HasAny(SearchPropositionFlags.NoCategory) ? null : category;
        }

        public override IBlockEditor OpenEditor(in Rect rect)
        {
            return QuerySelector.Open(rect, this);
        }

        public override IEnumerable<SearchProposition> FetchPropositions()
        {
            return GetPropositions(SearchPropositionFlags.NoCategory);
        }

        public override void Apply(in SearchProposition searchProposition)
        {
            value = searchProposition.data?.ToString() ?? searchProposition.replacement;
            source.Apply();
        }

        protected override Color GetBackgroundColor()
        {
            return icon == null ? QueryColors.type : QueryColors.typeIcon;
        }

        protected SearchProposition CreateProposition(SearchPropositionFlags flags, string label, string data, string help = "", int score = 0)
        {
            return new SearchProposition(category: GetCategory(flags), label: label, help: help,
                    data: data, priority: score, icon: icon, type: GetType());
        }

        protected SearchProposition CreateProposition(SearchPropositionFlags flags, string label, string data, string help, Texture2D icon, int score = 0)
        {
            return new SearchProposition(category: GetCategory(flags), label: label, help: help,
                    data: data, priority: score, icon: icon, type: GetType());
        }

        public override Rect Layout(in Vector2 at, in float availableSpace)
        {
            if (!icon || alwaysDrawLabel)
                return base.Layout(at, availableSpace);

            var labelStyle = Styles.QueryBuilder.label;
            var valueContent = labelStyle.CreateContent(label ?? value);
            var blockWidth = iconSize + valueContent.width + labelStyle.margin.horizontal + blockExtraPadding + (@readonly ? 0 : QueryContent.DownArrow.width);
            return GetRect(at, blockWidth, blockHeight);
        }

        protected override void Draw(in Rect blockRect, in Vector2 mousePosition)
        {
            if (!icon || alwaysDrawLabel)
            {
                base.Draw(blockRect, mousePosition);
                return;
            }

            var labelStyle = Styles.QueryBuilder.label;
            var valueContent = labelStyle.CreateContent(label ?? value);

            DrawBackground(blockRect, mousePosition);

            var backgroundTextureRect = new Rect(blockRect.x + 1f, blockRect.y + 1f, 24f, blockRect.height - 2f);
            var iconBackgroundRadius = new Vector4(borderRadius, 0, 0, editor != null ? 0 : borderRadius);
            var backgroundTextureRect2 = backgroundTextureRect;
            if (selected)
                backgroundTextureRect2.xMin -= 1f;
            GUI.DrawTexture(backgroundTextureRect2, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 0f, QueryColors.textureBackgroundColor, Vector4.zero, iconBackgroundRadius);

            var valueRect = backgroundTextureRect;
            var textureRect = backgroundTextureRect;
            textureRect.x += 5f; textureRect.y += 1f; textureRect.width = iconSize; textureRect.height = iconSize;
            GUI.DrawTexture(textureRect, icon, ScaleMode.ScaleToFit, true);

            valueRect.x -= 4f;
            DrawValue(valueRect, blockRect, mousePosition, valueContent);

            DrawBorders(blockRect, mousePosition);
        }

        public override string ToString()
        {
            return $"{id}{op}{value}";
        }

        protected override void AddContextualMenuItems(GenericMenu menu)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent($"Operator/Equal (=)"), string.Equals(op, "=", StringComparison.Ordinal), () => SetOperator("="));
            menu.AddItem(EditorGUIUtility.TrTextContent($"Operator/Contains (:)"), string.Equals(op, ":", StringComparison.Ordinal), () => SetOperator(":"));
        }

        public virtual bool TryGetReplacement(string id, string type, ref Type blockType, out string replacement)
        {
            replacement = string.Empty;
            return false;
        }
    }

    class QueryListMarkerBlock : QueryListBlock
    {
        private QueryMarker m_Marker;

        public QueryListMarkerBlock(IQuerySource source, string id, string name, string op, QueryMarker value)
            : base(source, id, op, value.value as string)
        {
            m_Marker = value;
            this.name = name ?? id;
        }

        public QueryListMarkerBlock(IQuerySource source, string id, QueryMarker value, QueryListBlockAttribute attr)
            : base(source, id, value.value as string, attr)
        {
            m_Marker = value;
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags = SearchPropositionFlags.None)
        {
            var args = m_Marker.EvaluateArgs().ToArray();
            if (args.Length < 2)
                yield break;
            else
            {
                foreach (var choice in args.Skip(1))
                {
                    var choiceStr = (string)choice;
                    yield return new SearchProposition(category: null, label: ObjectNames.NicifyVariableName(choiceStr), replacement: choiceStr);
                }
            }
        }
    }

    [QueryListBlock("Types", "type", "t", ":")]
    class QueryTypeBlock : QueryListBlock
    {
        private Type type;

        public QueryTypeBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source, id, value, attr)
        {
            SetType(GetValueType(value));
        }

        private void SetType(in Type type)
        {
            this.type = type;
            if (this.type != null)
            {
                value = type.Name;
                label = ObjectNames.NicifyVariableName(type.Name);
                icon = SearchUtils.GetTypeIcon(type);
            }
        }

        private static Type GetValueType(string value)
        {
            return TypeCache.GetTypesDerivedFrom<UnityEngine.Object>().FirstOrDefault(t => string.Equals(t.Name, value, StringComparison.OrdinalIgnoreCase) || string.Equals(t.ToString(), value, StringComparison.OrdinalIgnoreCase));
        }

        public override void Apply(in SearchProposition searchProposition)
        {
            if (searchProposition.data is Type t)
            {
                SetType(t);
                source.Apply();
            }
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {
            yield return new SearchProposition(
                category: null,
                label: "Components",
                icon: EditorGUIUtility.LoadIcon("GameObject Icon"));

            var componentType = typeof(Component);
            var assetTypes = SearchUtils.FetchTypePropositions<UnityEngine.Object>().Where(p => !componentType.IsAssignableFrom((Type)p.data));
            var propositions = SearchUtils.FetchTypePropositions<Component>("Components").Concat(assetTypes);
            foreach (var p in propositions)
                yield return p;
        }
    }

    [QueryListBlock("Components", "component", "t", ":")]
    class QueryComponentBlock : QueryTypeBlock
    {
        public QueryComponentBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
             : base(source, id, value, attr)
        {
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {
            return SearchUtils.FetchTypePropositions<Component>("Components", GetType());
        }
    }

    [QueryListBlock("Labels", "label", "l", ":")]
    class QueryLabelBlock : QueryListBlock
    {
       public QueryLabelBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source, id, value, attr)
        {
            icon = Utils.LoadIcon("AssetLabelIcon");
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {
            foreach (var l in AssetDatabase.GetAllLabels())
            {
                yield return CreateProposition(flags, ObjectNames.NicifyVariableName(l.Key), l.Key);
            }
        }
    }

    [QueryListBlock("Tags", "tag", "tag")]
    class QueryTagBlock : QueryListBlock
    {
        public QueryTagBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source, id, value, attr)
        {
            icon = Utils.LoadIcon("AssetLabelIcon");
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {

            foreach (var t in InternalEditorUtility.tags)
            {
                yield return CreateProposition(flags, ObjectNames.NicifyVariableName(t), t);
            }
        }
    }

    [QueryListBlock("Layers", "layer", new[] { "layer", "#m_layer" })]
    class QueryLayerBlock : QueryListBlock
    {
        const int k_MaxLayerCount = 32;

        public QueryLayerBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source, id, value, attr)
        {
            icon = Utils.LoadIcon("GUILayer Icon");

            if (QueryMarker.TryParse(value, out var marker) && marker.valid && marker.args.Length >= 2)
                this.value = marker.args[1].rawText.ToString();
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {
            for (var i = 0; i < k_MaxLayerCount; i++)
            {
                var layerName = InternalEditorUtility.GetLayerName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    yield return CreateProposition(flags, ObjectNames.NicifyVariableName(layerName), layerName);
                }
            }
        }

        public override string ToString()
        {
            for (var i = 0; i < k_MaxLayerCount; i++)
            {
                var layerName = InternalEditorUtility.GetLayerName(i);
                if (layerName == value)
                    return $"{id}{op}{FormatValue(i, layerName)}";
            }

            return base.ToString();
        }

        public override bool TryGetReplacement(string id, string type, ref Type blockType, out string replacement)
        {
            replacement = $"{id}{op}{GetDefaultMarker()}";
            return true;
        }

        static string FormatValue(int layerIndex, string layerName)
        {
            return $"<$layer:{layerIndex}, {layerName}$>";
        }

        public static string GetDefaultMarker()
        {
            var layerName = InternalEditorUtility.GetLayerName(0);
            return FormatValue(0, layerName);
        }
    }

    [QueryListBlock("Prefabs", "prefab", "prefab", ":")]
    class QueryPrefabFilterBlock : QueryListBlock
    {
        public QueryPrefabFilterBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source, id, value, attr)
        {
            icon = Utils.LoadIcon("Prefab Icon");
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {
            yield return CreateProposition(flags, "Any", "any", "Search prefabs");
            yield return CreateProposition(flags, "Base", "Base", "Search base prefabs");
            yield return CreateProposition(flags, "Root", "root", "Search prefab roots");
            yield return CreateProposition(flags, "Top", "top", "Search top-level prefab root instances");
            yield return CreateProposition(flags, "Instance", "instance", "Search objects that are part of a prefab instance");
            yield return CreateProposition(flags, "Non asset", "nonasset", "Search prefab objects that are not part of an asset");
            yield return CreateProposition(flags, "Asset", "asset", "Search prefab objects that are part of an asset");
            yield return CreateProposition(flags, "Model", "model", "Search prefab objects that are part of a model");
            yield return CreateProposition(flags, "Regular", "regular", "Search regular prefab objects");
            yield return CreateProposition(flags, "Variant", "variant", "Search variant prefab objects");
            yield return CreateProposition(flags, "Modified", "modified", "Search modified prefab assets");
            yield return CreateProposition(flags, "Altered", "altered", "Search modified prefab instances");
        }
    }

    [QueryListBlock("Filters", "is", "is", ":")]
    class QueryIsFilterBlock : QueryListBlock
    {
        public QueryIsFilterBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source, id, value, attr)
        {
            icon = Utils.LoadIcon("Filter Icon");
            alwaysDrawLabel = true;
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {
            yield return CreateProposition(flags, "Child", "child", "Search object with a parent");
            yield return CreateProposition(flags, "Leaf", "leaf", "Search object without children");
            yield return CreateProposition(flags,  "Root", "root", "Search root objects");
            yield return CreateProposition(flags, "Visible", "visible", "Search view visible objects");
            yield return CreateProposition(flags, "Hidden", "hidden", "Search hierarchically hidden objects");
            yield return CreateProposition(flags, "Static", "static", "Search static objects");
            yield return CreateProposition(flags, "Prefab", "prefab", "Search prefab objects");
        }
    }
}
