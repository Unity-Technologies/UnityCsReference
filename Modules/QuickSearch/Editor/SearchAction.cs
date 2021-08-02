// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace UnityEditor.Search
{
    /// <summary>
    /// Define an action that can be applied on SearchItem of a specific provider type.
    /// </summary>
    [DebuggerDisplay("{displayName} - {id}")]
    public class SearchAction
    {
        /// <summary>
        /// Default constructor to build a search action.
        /// </summary>
        /// <param name="providerId">Provider Id that supports this action.</param>
        /// <param name="id">Action unique id.</param>
        /// <param name="content">Display information when displaying the action in the Action Menu</param>
        public SearchAction(string providerId, string id, GUIContent content)
        {
            this.providerId = providerId;
            this.id = id;
            this.content = content;
            handler = null;
            execute = null;
            enabled = (a) => true;
        }

        /// <summary>
        /// Default constructor to build a search action.
        /// </summary>
        /// <param name="providerId">Provider Id that supports this action.</param>
        /// <param name="id">Action unique id.</param>
        /// <param name="content">Display information when displaying the action in the Action Menu</param>
        /// <param name="handler">Handler that will execute the action.</param>
        public SearchAction(string providerId, string id, GUIContent content, Action<SearchItem[]> handler)
            : this(providerId, id, content)
        {
            execute = handler;
        }

        /// <summary>
        /// Default constructor to build a search action.
        /// </summary>
        /// <param name="providerId">Provider Id that supports this action.</param>
        /// <param name="id">Action unique id.</param>
        /// <param name="content">Display information when displaying the action in the Action Menu</param>
        /// <param name="handler">Handler that will execute the action.</param>
        public SearchAction(string providerId, string id, GUIContent content, Action<SearchItem> handler)
            : this(providerId, id, content)
        {
            this.handler = handler;
        }

        /// <summary>
        /// Extended constructor to build a search action.
        /// </summary>
        /// <param name="providerId">Provider Id that supports this action.</param>
        /// <param name="name">Label name when displaying the action in the Action Menu</param>
        /// <param name="icon">Icon when displaying the action in the Action Menu</param>
        /// <param name="tooltip">Tooltip assocoated with the when displayed in the Action Menu</param>
        /// <param name="handler">Handler that will execute the action.</param>
        public SearchAction(string providerId, string name, Texture2D icon, string tooltip, Action<SearchItem[]> handler)
            : this(providerId, name, new GUIContent(SearchUtils.ToPascalWithSpaces(name), icon, tooltip ?? name), handler)
        {
        }

        /// <summary>
        /// Extended constructor to build a search action.
        /// </summary>
        /// <param name="providerId">Provider Id that supports this action.</param>
        /// <param name="name">Label name when displaying the action in the Action Menu</param>
        /// <param name="icon">Icon when displaying the action in the Action Menu</param>
        /// <param name="tooltip">Tooltip assocoated with the when displayed in the Action Menu</param>
        /// <param name="handler">Handler that will execute the action.</param>
        public SearchAction(string providerId, string name, Texture2D icon, string tooltip, Action<SearchItem> handler)
            : this(providerId, name, new GUIContent(SearchUtils.ToPascalWithSpaces(name), icon, tooltip ?? name), handler)
        {
        }

        internal SearchAction(string providerId, string name, Texture2D icon, string tooltip, Action<SearchItem> handler, Func<IReadOnlyCollection<SearchItem>, bool> enabledHandler)
            : this(providerId, name, icon, tooltip, handler)
        {
            enabled = enabledHandler;
        }

        internal SearchAction(string name, string label, Action<SearchItem> execute)
            : this(name, label, execute, null)
        {
        }

        internal SearchAction(string name, string label, Action<SearchItem> execute, Func<SearchItem, bool> enabled)
            : this(string.Empty, name, new GUIContent(label))
        {
            handler = execute;
            if (enabled != null)
                this.enabled = (items) => items.All(e => enabled(e));
        }

        /// <summary>
        /// Extended constructor to build a search action.
        /// </summary>
        /// <param name="providerId">Provider Id that supports this action.</param>
        /// <param name="name">Label name when displaying the action in the Action Menu</param>
        /// <param name="icon">Icon when displaying the action in the Action Menu</param>
        /// <param name="tooltip">Tooltip associated with the when displayed in the Action Menu</param>
        public SearchAction(string providerId, string name, Texture2D icon = null, string tooltip = null)
            : this(providerId, name, new GUIContent(SearchUtils.ToPascalWithSpaces(name), icon, tooltip ?? name))
        {
        }

        /// <summary>
        /// Action unique identifier.
        /// </summary>
        public string id { get; private set; }

        /// <summary>
        /// Name used to display
        /// </summary>
        public string displayName => content.tooltip;

        /// <summary>
        /// Indicates if the search view should be closed after the action execution.
        /// </summary>
        public bool closeWindowAfterExecution = true;

        /// <summary>
        /// Unique (for a given provider) id of the action
        /// </summary>
        internal string providerId;

        /// <summary>
        /// GUI content used to display the action in the search view.
        /// </summary>
        internal GUIContent content;

        /// <summary>
        /// Callback used to check if the action is enabled based on the current context.
        /// </summary>
        public Func<IReadOnlyCollection<SearchItem>, bool> enabled;

        /// <summary>
        /// Execute a action on a set of items.
        /// </summary>
        public Action<SearchItem[]> execute;

        /// <summary>
        /// This handler is used for action that do not support multi selection.
        /// </summary>
        public Action<SearchItem> handler;
    }
}
