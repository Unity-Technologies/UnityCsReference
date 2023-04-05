// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class QueryBuilder : IQuerySource
    {
        private QueryAddNewBlock m_AddBlock;
        private QueryTextFieldBlock m_TextBlock;
        private ISearchField m_SearchField;
        private bool m_ReadOnly;

        private string m_SearchText;
        private readonly SearchContext m_Context;
        private readonly QueryEngine<QueryBlock> m_QueryEngine;

        public bool @readonly
        {
            get
            {
                return m_ReadOnly;
            }
            set
            {
                m_ReadOnly = value;
                if (blocks != null)
                {
                    foreach (var b in blocks)
                        b.@readonly = value;
                }
            }
        }

        public List<QueryBlock> blocks { get; private set; }
        public List<QueryError> errors { get; private set; }

        public SearchContext context => m_Context;
        public ISearchView searchView => m_Context?.searchView;
        public string searchText
        {
            get => m_SearchText ?? m_Context?.searchText;
            set
            {
                m_SearchText = value;
            }
        }
        public string wordText
        {
            get
            {
                return m_TextBlock?.value ?? string.Empty;
            }

            set
            {
                if (m_TextBlock == null)
                    return;
                m_TextBlock.value = value;
                Apply();
            }
        }

        internal QueryTextFieldBlock textBlock => m_TextBlock;

        public bool hasOwnText => m_SearchText != null;

        public bool valid => errors.Count == 0;

        public QueryBlock currentBlock => selectedBlocks.FirstOrDefault();
        public IEnumerable<QueryBlock> selectedBlocks => EnumerateBlocks().Where(b => b.selected);

        protected QueryBuilder()
        {
            errors = new List<QueryError>();
            blocks = new List<QueryBlock>();
            var opts = new QueryValidationOptions() { validateSyntaxOnly = true };
            m_QueryEngine = new QueryEngine<QueryBlock>(opts);
            m_QueryEngine.AddQuoteDelimiter(new QueryTextDelimiter("<$", "$>"));
            m_QueryEngine.AddQuoteDelimiter(new QueryTextDelimiter("[", "]"));
            m_QueryEngine.AddFilter(new Regex("(#[\\w.]+)"));
        }

        public QueryBuilder(SearchContext searchContext, ISearchField searchField = null)
            : this()
        {
            m_Context = searchContext;
            m_SearchField = searchField;
            Build();
        }

        public QueryBuilder(string searchText)
            : this()
        {
            m_SearchText = searchText;
            Build();
        }

        IEnumerable<QueryBlock> IQuerySource.EnumerateBlocks()
        {
            foreach (var b in blocks)
                yield return b;
        }

        public IEnumerable<QueryBlock> EnumerateBlocks()
        {
            foreach (var b in blocks)
                yield return b;

            if (m_AddBlock != null)
                yield return m_AddBlock;
            if (m_TextBlock != null)
                yield return m_TextBlock;
        }

        public void Repaint()
        {
            searchView?.Repaint();
        }

        public void SetSelection(IEnumerable<int> selectedBlockIndexes)
        {
            foreach(var toUnselect in selectedBlocks)
            {
                toUnselect.selected = false;
            }

            foreach(var toSelectIndex in selectedBlockIndexes)
            {
                if (toSelectIndex >=0 && toSelectIndex < blocks.Count())
                {
                    blocks[toSelectIndex].selected = true;
                }
            }
        }

        public void SetSelection(int selectedBlockIndex)
        {
            SetSelection(new[] { selectedBlockIndex });
        }

        public void AddToSelection(int selectedBlockIndex)
        {
            if (selectedBlockIndex >= 0 && selectedBlockIndex < blocks.Count())
                blocks[selectedBlockIndex].selected = true;
        }

        internal int GetBlockIndex(QueryBlock b)
        {
            return blocks.IndexOf(b);
        }

        internal void SetSourceData(QueryBlock source, string sourceDataIdentifier)
        {
            if (source != null && source.draggable)
                DragAndDrop.SetGenericData(sourceDataIdentifier, source);
        }

        internal void SetTargetData(Vector2 mousePosition, QueryBlock source, string targetDataIdentifier)
        {
            var sourceIndex = GetBlockIndex(source);
            for (int i = 0; i < blocks.Count; i++)
            {
                if (!blocks[i].drawRect.Contains(mousePosition))
                    continue;

                if (i != sourceIndex && blocks[i].draggable)
                {
                    DragAndDrop.SetGenericData(targetDataIdentifier, blocks[i]);
                    break;
                }
                else if (i == sourceIndex)
                {
                    DragAndDrop.SetGenericData(targetDataIdentifier, null);
                    break;
                }
                else if (i != sourceIndex && !blocks[i].draggable)
                {
                    var nextTargetIndex = i + 1;
                    if (nextTargetIndex == sourceIndex || nextTargetIndex > blocks.Count - 1)
                    {
                        DragAndDrop.SetGenericData(targetDataIdentifier, null);
                        break;
                    }

                    DragAndDrop.SetGenericData(targetDataIdentifier, blocks[nextTargetIndex]);
                    break;
                }
            }
        }

        internal void DropBlock(QueryBlock source, QueryBlock target)
        {
            if (source == null || target == null)
                return;

            var targetIndex = GetBlockIndex(target);
            blocks.Remove(source);
            blocks.Insert(targetIndex, source);
            Apply();
        }

        public string BuildQuery()
        {
            var query = new StringBuilder();
            BuildQuery(query, EnumerateBlocks().Where(b => !b.disabled));
            return Utils.Simplify(query.ToString());
        }

        private static void BuildQuery(StringBuilder query, IEnumerable<QueryBlock> blocks)
        {
            foreach (var c in blocks)
            {
                var s = c.ToString();
                if (string.IsNullOrEmpty(s))
                    continue;
                if (query.Length > 0)
                    query.Append(' ');
                if (c.excluded)
                    query.Append('-');
                query.Append(s);
            }
        }

        public bool Build()
        {
            errors.Clear();

            var newBlocks = new List<QueryBlock>();
            if (!string.IsNullOrEmpty(searchText))
            {
                string searchQuery;
                if (context != null)
                {
                    if (!string.IsNullOrEmpty(context.filterId))
                        newBlocks.Add(new QueryAreaBlock(this, context.providers.First()));
                    searchQuery = context.rawSearchQuery;
                }
                else
                {
                    var baseExpressionProviders = SearchService.GetActiveProviders().Where(p => char.IsLetter(p.filterId[0]));
                    searchQuery = SearchUtils.ParseSearchText(searchText, baseExpressionProviders, out var filteredProvider);
                    if (filteredProvider != null)
                    {
                        newBlocks.Add(new QueryAreaBlock(this, filteredProvider));
                    }
                }

                SearchExpression rootExpression = null;
                try
                {
                    rootExpression = SearchExpression.Parse(searchText);
                }
                catch(SearchExpressionParseException)
                {

                }

                if (rootExpression != null && rootExpression.types.HasAny(SearchExpressionType.Function))
                {
                    newBlocks.Add(new QueryExpressionBlock(this, rootExpression));
                    m_SearchField = null;
                }
                else
                {
                    var query = m_QueryEngine.ParseQuery(searchQuery);
                    if (HasFlag(SearchFlags.ShowErrorsWithResults) && !query.valid)
                        errors.AddRange(query.errors);

                    var rootNode = query.queryGraph.root;
                    if (rootNode != null)
                        ParseNode(rootNode, newBlocks);
                }
            }

            if (m_SearchField != null)
            {
                m_AddBlock = new QueryAddNewBlock(this);
                m_TextBlock = new QueryTextFieldBlock(this, m_SearchField);

                // Move ending word blocks into text field block
                var wordText = "";
                for (int w = newBlocks.Count - 1; w >= 0; --w)
                {
                    if (newBlocks[w].GetType() != typeof(QueryWordBlock))
                        break;

                    var wordBlock = newBlocks[w] as QueryWordBlock;
                    if (!wordBlock.explicitQuotes && !wordBlock.excluded && newBlocks.Remove(wordBlock))
                        wordText = (wordBlock.value + " " + wordText).Trim();
                }
                if (!string.IsNullOrEmpty(wordText))
                    m_TextBlock.value = wordText;
            }

            blocks.Clear();
            blocks.AddRange(newBlocks);
            return errors.Count == 0;
        }

        private bool HasFlag(SearchFlags flag)
        {
            return context != null && context.options.HasAny(flag);
        }

        private IList<QueryBlock> Build(string searchText)
        {
            var newBlocks = new List<QueryBlock>();
            var searchQuery = searchText;

            var query = m_QueryEngine.ParseQuery(searchQuery);
            var rootNode = query.queryGraph.root;
            if (rootNode == null)
                return null;

            ParseNode(rootNode, newBlocks, exclude: false);

            return newBlocks;
        }

        private void ParseNode(in IQueryNode node, List<QueryBlock> blocks, bool exclude = false)
        {
            if (!node.leaf)
                ParseNode(node.children[0], blocks, node.type == QueryNodeType.Not);

            var newBlock = CreateBlock(node);
            if (newBlock != null)
            {
                if (exclude)
                    newBlock.excluded = exclude;
                blocks.Add(newBlock);
            }

            if (!node.leaf && node.children.Count > 1)
            {
                foreach (var c in node.children.Skip(1))
                    ParseNode(c, blocks);
            }
        }

        private QueryBlock CreateBlock(in IQueryNode node)
        {
            if (node.type == QueryNodeType.Search && node is SearchNode sn)
                return new QueryWordBlock(this, sn);

            if ((node.type == QueryNodeType.Filter || node.type == QueryNodeType.FilterIn) && node is FilterNode fn)
            {
                var block = QueryListBlockAttribute.CreateBlock(fn.filterId.ToLower(), fn.operatorId.ToLower(), this, fn.rawFilterValueStringView.ToString());
                if (block != null)
                    return block;
                return new QueryFilterBlock(this, fn);
            }

            if (node.type == QueryNodeType.NestedQuery &&
                (node.parent == null || (node.parent.type != QueryNodeType.Aggregator && node.parent.type != QueryNodeType.FilterIn)) &&
                node is NestedQueryNode nqn)
                return new QueryWordBlock(this, nqn.rawNestedQueryStringView.ToString());

            if (node.type == QueryNodeType.Toggle && node is ToggleNode tn)
                return new QueryToggleBlock(this, tn.identifier);

            if (node.type == QueryNodeType.Aggregator &&
                (node.parent == null || node.parent.type != QueryNodeType.FilterIn) &&
                !node.leaf && node.children[0].type == QueryNodeType.NestedQuery &&
                node is AggregatorNode an && node.children[0] is NestedQueryNode nq)
                return new QueryWordBlock(this, $"{an.tokenStringView}{nq.rawNestedQueryStringView}");

            if (node.type == QueryNodeType.Or)
                return new QueryAndOrBlock(this, $"or");

            if (node.type == QueryNodeType.And && !string.IsNullOrEmpty(node.token.text))
                return new QueryAndOrBlock(this, $"and");

            if (HasFlag(SearchFlags.Debug))
                Debug.LogWarning($"TODO: Failed to parse block {node.identifier} ({node.type})");
            return null;
        }

        QueryBlock IQuerySource.AddProposition(in SearchProposition searchProposition) => AddProposition(searchProposition);
        internal QueryBlock AddProposition(in SearchProposition searchProposition)
        {
            SetSelection(-1);

            QueryBlock insertAt = EnumerateBlocks().FirstOrDefault(b => b.editor != null);

            if (searchProposition.data is SearchProvider provider)
                return InsertBlock(insertAt, new QueryAreaBlock(this, provider));
            if (searchProposition.data is QueryBlock block)
                return InsertBlock(insertAt, block);

            if (searchProposition.type != null && typeof(QueryListBlock).IsAssignableFrom(searchProposition.type))
            {
                var newBlock = QueryListBlockAttribute.CreateBlock(searchProposition.type, this, searchProposition.data?.ToString());
                if (newBlock == null)
                    return InsertBlock(insertAt, searchProposition.replacement);
                return InsertBlock(insertAt, newBlock);
            }

            if (searchProposition.type != null && typeof(QueryBlock).IsAssignableFrom(searchProposition.type))
            {
                var newBlock = (QueryBlock)Activator.CreateInstance(searchProposition.type, new object[] { this, searchProposition.data });
                return InsertBlock(insertAt, newBlock);
            }

            return InsertBlock(insertAt, searchProposition.replacement);
        }

        public void Apply()
        {
            var queryString = BuildQuery();
            if (HasFlag(SearchFlags.Debug))
                Debug.Log($"Apply query: {searchText} > {queryString}");
            SetSearchText(queryString);
        }

        QueryBlock IQuerySource.AddBlock(string text) => AddBlock(text);
        internal QueryBlock AddBlock(string text)
        {
            var newBlocks = Build(text);
            if (newBlocks == null || newBlocks.Count == 0)
                return null;

            blocks.AddRange(newBlocks);
            Apply();
            return newBlocks.FirstOrDefault();
        }

        internal QueryBlock InsertBlock(QueryBlock insertAfter, string text)
        {
            var newBlocks = Build(text);
            if (newBlocks == null || newBlocks.Count == 0)
                return null;

            foreach (var nb in newBlocks)
                InsertBlock(insertAfter, nb);

            return newBlocks.FirstOrDefault();
        }

        QueryBlock IQuerySource.AddBlock(QueryBlock newBlock) => AddBlock(newBlock);
        internal QueryBlock AddBlock(QueryBlock newBlock)
        {
            return InsertBlock(null, newBlock);
        }

        internal QueryBlock InsertBlock(QueryBlock insertAfter, QueryBlock newBlock)
        {
            if (insertAfter == null)
            {
                blocks.Add(newBlock);
            }
            else
            {
                var insertAt = blocks.IndexOf(insertAfter);
                if (insertAt < 0)
                    insertAt = ~insertAt;
                blocks.Insert(insertAt+1, newBlock);
            }
            Apply();
            if (context?.searchView != null)
                Dispatcher.Emit(SearchEvent.RefreshBuilder, new SearchEventPayload(context.searchView.state));
            return newBlock;
        }

        void IQuerySource.RemoveBlock(in QueryBlock block) => RemoveBlock(block);
        internal void RemoveBlock(in QueryBlock block)
        {
            var currentIndex = currentBlock == block ? GetBlockIndex(block) : -1;
            blocks.Remove(block);
            if (currentIndex != -1 && currentIndex < blocks.Count())
            {
                SetSelection(currentIndex);
            }
            Apply();
        }

        void IQuerySource.BlockActivated(in QueryBlock block)
        {
            if (block == m_TextBlock)
                SetSelection(-1);
            else
            {
                var index = GetBlockIndex(block);
                SetSelection(index);
            }
        }

        private void SetSearchText(string text)
        {
            text = Utils.Simplify(text);
            if (searchView != null)
                searchView.SetSearchText(text, TextCursorPlacement.None);
            else if (context != null)
                context.searchText = text;
            searchText = text;
        }

        static bool HasCharacterModifier(in Event evt)
        {
            if (evt.modifiers == EventModifiers.None)
                return false;

            if (evt.modifiers == EventModifiers.FunctionKey)
                return false;

            return true;
        }

        // TODO: Remove me
        public bool HandleKeyEvent(in Event evt)
        {
            if (@readonly || context == null || evt.type != EventType.KeyDown)
                return false;

            if (Utils.IsEditingTextField() && GUIUtility.keyboardControl != m_TextBlock?.GetSearchField().controlID)
                return false;

            var te = m_TextBlock?.GetSearchField();
            var cursorAtBeginning = te?.cursorIndex == 0;
            var cursorAtEnd = te?.cursorIndex == te?.text?.Length;
            var controlPresed = evt.modifiers.HasAny(EventModifiers.Command) || evt.modifiers.HasAny(EventModifiers.Control);

            if (evt.keyCode == KeyCode.Home && cursorAtBeginning)
            {
                var cb = currentBlock;
                if (cb != null)
                {
                    SetSelection(0);
                    evt.Use();
                    return true;
                }
            }
            else if (evt.keyCode == KeyCode.LeftArrow && cursorAtBeginning && !HasCharacterModifier(evt))
            {
                var currentIndex = GetBlockIndex(currentBlock);
                var toSelectIndex = -1;
                if (m_TextBlock != null && currentIndex == -1)
                {
                    // Focus is in the textfield:
                    if (te != null && te.cursorIndex == 0)
                    {
                        toSelectIndex = blocks.Count() - 1;
                    }
                }
                else if (currentIndex != 0)
                {
                    toSelectIndex = currentIndex - 1;
                }

                if (toSelectIndex != -1)
                {
                    SetSelection(toSelectIndex);
                    evt.Use();
                    return true;
                }
            }
            else if (evt.keyCode == KeyCode.RightArrow && !HasCharacterModifier(evt))
            {
                var currentIndex = GetBlockIndex(currentBlock);
                if (currentIndex != -1)
                {
                    if (m_TextBlock != null && currentIndex + 1 == blocks.Count)
                    {
                        // Put focus back in the textfield:
                        m_TextBlock.GetSearchField()?.Focus();
                    }
                    
                    SetSelection(currentIndex + 1);
                    evt.Use();
                    return true;
                }
                else
                {
                    m_TextBlock.GetSearchField()?.Focus();
                }
            }
            else if (evt.keyCode == KeyCode.Backspace || evt.keyCode == KeyCode.Delete)
            {
                QueryBlock toRemoveBlock = currentBlock;
                if (toRemoveBlock != null && !toRemoveBlock.@readonly)
                {
                    toRemoveBlock.Delete();
                    evt.Use();
                    return true;
                }
            }
            else if (controlPresed && evt.keyCode == KeyCode.D)
            {
                var cb = currentBlock;
                if (cb != null)
                {
                    var potentialBlocks = Build(cb.ToString());
                    if (potentialBlocks != null && potentialBlocks.Count() > 0)
                    {
                        foreach (var b in potentialBlocks)
                        {
                            AddBlock(b);
                        }
                        evt.Use();
                        return true;
                    }
                }
            }
            else if (m_TextBlock != null && controlPresed && evt.keyCode == KeyCode.Space)
            {
                var potentialBlocks = Build(m_TextBlock.ToString());
                if (potentialBlocks != null && potentialBlocks.Count() > 0)
                {
                    foreach (var b in potentialBlocks)
                        AddBlock(b);
                    m_TextBlock.value = string.Empty;
                    evt.Use();
                    return true;
                }
            }
            else if (!controlPresed)
                SetSelection(-1);

            return false;
        }

        bool IQuerySource.SwapBlock(QueryBlock bl, QueryBlock br)
        {
            var il = blocks.IndexOf(bl);
            var ir = blocks.IndexOf(br);
            if (il == -1 || ir == -1)
                return false;

            blocks.RemoveAt(il);
            blocks.Insert(ir, bl);
            Apply();
            return true;
        }

        public bool HandleGlobalKeyDown(KeyDownEvent evt)
        {
            var te = m_TextBlock?.GetSearchField();
            if (evt.keyCode == KeyCode.Tab)
            {
                var cb = currentBlock;
                var currentIndex = GetBlockIndex(currentBlock);
                if (m_TextBlock != null && currentIndex == -1)
                {
                    // Focus is in the textfield:
                    if (m_TextBlock.value == "" ||
                        (te != null && (te.cursorIndex == 0 || te.text[te.cursorIndex - 1] == ' ')))
                    {
                        m_AddBlock.OpenEditor(m_AddBlock.drawRect);
                        return true;
                    }
                }
                else
                {
                    cb.OpenEditor(cb.drawRect);
                    return true;
                }
            }
            else if (evt.keyCode == KeyCode.LeftArrow)
            {
                var currentIndex = GetBlockIndex(currentBlock);
                if (m_TextBlock != null && currentIndex == -1 && te != null && te.cursorIndex == 0 && blocks.Count > 0)
                {
                    SetSelection(blocks.Count - 1);
                    return true;
                }
            }

            return false;
        }
    }
}
