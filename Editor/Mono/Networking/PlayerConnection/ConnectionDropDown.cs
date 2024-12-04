// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Networking.PlayerConnection
{
    internal static class ConnectionUIHelper
    {
        private static string portPattern = @":\d{4,}";
        private static string ipPattern = @"@\d{1,3}.\d{1,3}.\d{1,3}.\d{1,3}";
        private static string localHostPattern = @" (Localhost prohibited)";
        public static readonly string kDevices = L10n.Tr("Devices");

        internal static class Content
        {
            public static string PlayerLogging = L10n.Tr("Player Logging");
            public static string FullLog = L10n.Tr("Full Log [Developer Mode Only]");
            public static string Logging = L10n.Tr("Logging");
        }

        public static string GetToolbarContent(string connectionName, GUIStyle style, int maxWidth)
        {
            var projectNameFromConnectionIdentifier = GetProjectNameFromConnectionIdentifier(ProfilerDriver.connectedProfiler);
            var s =  $"{GetPlayerNameFromIDString(GetIdString(connectionName))}{(string.IsNullOrEmpty(projectNameFromConnectionIdentifier) ? "" : $" - {projectNameFromConnectionIdentifier}")}";
            return TruncateString(s, style, maxWidth);
        }

        public static string TruncateString(string s, GUIStyle style, float maxwidth)
        {
            if (style.CalcSize(GUIContent.Temp(s)).x < maxwidth)
                return s;

            maxwidth -= style.CalcSize(GUIContent.Temp("...")).x;
            while (s.Length > 0 && style.CalcSize(GUIContent.Temp(s)).x > maxwidth)
            {
                s = s.Remove(s.Length - 1, 1);
            }

            return s + "...";
        }

        public static string GetPlayerNameFromId(int id)
        {
            return GetPlayerNameFromIDString(GetIdString(GeneralConnectionState.GetConnectionName(id)));
        }

        public static string GetIdString(string id)
        {
            //strip port info
            var portMatch = Regex.Match(id, portPattern);
            if (portMatch.Success)
            {
                id = id.Replace(portMatch.Value, "");
            }

            //strip ip info
            var ipMatch = Regex.Match(id, ipPattern);
            if (ipMatch.Success)
            {
                id = id.Replace(ipMatch.Value, "");
            }

            var removedWarning = false;
            if (id.Contains(localHostPattern))
            {
                id = id.Replace(localHostPattern, "");
                removedWarning = true;
            }

            id = id.Contains('(') ? id.Substring(id.IndexOf('(') + 1) : id;// trim the brackets
            id = id.EndsWith(")") ? id.Substring(0, id.Length - 1) : id; // trimend cant be used as the player id might end with )

            if (removedWarning)
                id += localHostPattern;
            return id;
        }

        public static string GetRuntimePlatformFromIDString(string id)
        {
            var s = id.Contains(',') ? id.Substring(0, id.IndexOf(',')) : "";

            return RuntimePlatform.TryParse(s, out RuntimePlatform result) ? result.ToString() : "";
        }

        public static string GetPlayerNameFromIDString(string id)
        {
            return id.Contains(',') ? id.Substring(id.IndexOf(',') + 1) : id;
        }

        public static string GetPlayerType(string connectionName)
        {
            return connectionName.Contains('(')
                ? connectionName.Substring(0, connectionName.IndexOf('('))
                : connectionName;
        }

        public static string GetProjectNameFromConnectionIdentifier(int connectionId)
        {
            return ProfilerDriver.GetProjectName(connectionId);
        }

        public static string GetIP(int connectionId)
        {
            return ProfilerDriver.GetConnectionIP(connectionId);
        }

        public static string GetPort(int connectionId)
        {
            var port = ProfilerDriver.GetConnectionPort(connectionId);
            return port == 0 ? "-" : port.ToString();
        }

        public static GUIContent GetIcon(string name)
        {
            var content = GetBuildTargetIcon(name);
            if (content == null)
            {
                content = name switch
                {
                    "Editor" => EditorGUIUtility.IconContent("d_SceneAsset Icon"),
                    "WindowsEditor" => EditorGUIUtility.IconContent("d_SceneAsset Icon"),
                    "WindowsPlayer" => EditorGUIUtility.IconContent("BuildSettings.Metro.Small"),
                    "Android" => EditorGUIUtility.IconContent("BuildSettings.Android.Small"),
                    "OSXPlayer" => EditorGUIUtility.IconContent("BuildSettings.Standalone.Small"),
                    "IPhonePlayer" => EditorGUIUtility.IconContent("BuildSettings.iPhone.Small"),
                    "WebGLPlayer" => EditorGUIUtility.IconContent("BuildSettings.WebGL.Small"),
                    "tvOS" => EditorGUIUtility.IconContent("BuildSettings.tvOS.Small"),
                    "Lumin" => EditorGUIUtility.IconContent("BuildSettings.Lumin.small"),
                    "LinuxPlayer" => EditorGUIUtility.IconContent("BuildSettings.EmbeddedLinux.Small"),
                    "WSAPlayerX86" => EditorGUIUtility.IconContent("BuildSettings.Metro.Small"),
                    "WSAPlayerX64" => EditorGUIUtility.IconContent("BuildSettings.Metro.Small"),
                    "WSAPlayerARM" => EditorGUIUtility.IconContent("BuildSettings.Metro.Small"),
                    "Switch" => EditorGUIUtility.IconContent("BuildSettings.Switch.Small"),
                    "Stadia" => EditorGUIUtility.IconContent("BuildSettings.Stadia.small"),
                    "EmbeddedLinuxArm64" => EditorGUIUtility.IconContent("BuildSettings.EmbeddedLinux.Small"),
                    "EmbeddedLinuxArm32" => EditorGUIUtility.IconContent("BuildSettings.EmbeddedLinux.Small"),
                    "EmbeddedLinuxX64" => EditorGUIUtility.IconContent("BuildSettings.EmbeddedLinux.Small"),
                    "EmbeddedLinuxX86" => EditorGUIUtility.IconContent("BuildSettings.EmbeddedLinux.Small"),
                    "QNXArm64" => EditorGUIUtility.IconContent("BuildSettings.QNX.Small"),
                    "QNXArm32" => EditorGUIUtility.IconContent("BuildSettings.QNX.Small"),
                    "QNXX64" => EditorGUIUtility.IconContent("BuildSettings.QNX.Small"),
                    "QNXX86" => EditorGUIUtility.IconContent("BuildSettings.QNX.Small"),
                    "PS4" => EditorGUIUtility.IconContent("BuildSettings.PS4.Small"),
                    "PS5" => EditorGUIUtility.IconContent("BuildSettings.PS5.Small"),
                    "Devices" => EditorGUIUtility.IconContent("BuildSettings.Standalone.Small"),
                    "<unknown>" => EditorGUIUtility.IconContent("BuildSettings.Broadcom"),
                    _ => EditorGUIUtility.IconContent("BuildSettings.Broadcom")
                };
            }

            return content;
        }

        private static GUIContent GetBuildTargetIcon(string name)
        {
            var target = BuildPipeline.GetBuildTargetByName(name);
            if (target == BuildTarget.NoTarget)
            {
                return null;
            }

            var namedBuildTarget = Build.NamedBuildTarget.FromActiveSettings(target);
            if (namedBuildTarget == null)
            {
                return null;
            }

            var buildPlatform = Build.BuildPlatforms.instance.BuildPlatformFromNamedBuildTarget(namedBuildTarget);
            if (buildPlatform == null || buildPlatform.smallIcon == null)
            {
                return null;
            }

            return new GUIContent(buildPlatform.smallIcon);
        }
    }

    internal class ConnectionDropDownItem : TreeViewItem
    {
        public Func<bool> m_Connected;
        public Action m_OnSelected;
        public string m_SubGroup;
        public bool m_Disabled;
        public string ProjectName;
        public float ProjectNameSize;
        public ConnectionMajorGroup m_TopLevelGroup;
        public int m_ConnectionId;
        internal string DisplayName;
        public float DisplayNameSize;
        internal string IP;
        internal float IPSize;
        internal string Port;
        internal float PortSize;
        internal bool OldConnectionFormat;
        public bool IsDevice;
        public GUIContent IconContent;

        static class Content
        {
            public static readonly string DirectConnection = L10n.Tr("Direct Connection");
        }

        internal enum ConnectionMajorGroup
        {
            Editor,
            Logging,
            Local,
            Remote,
            ConnectionsWithoutID,
            Direct,
            Unknown
        }

        internal static string[] ConnectionMajorGroupLabels =
        {
            "Editor",
            "Logging",
            "Local",
            "Remote",
            "Connections Without ID",
            "Direct Connection",
            "Unknown"
        };

        public ConnectionDropDownItem(string content, int connectionId, string subGroup, ConnectionMajorGroup topLevelGroup, Func<bool> connected, Action onSelected,
                                       bool isDevice = false, string iconString = null)
        {
            var idString = ConnectionUIHelper.GetIdString(content);
            DisplayName = subGroup == ConnectionUIHelper.kDevices ? content : ConnectionUIHelper.GetPlayerNameFromIDString(idString);
            DisplayNameSize = ConnectionDropDownStyles.sTVLine.CalcSize(GUIContent.Temp(DisplayName)).x;

            ProjectName = subGroup == ConnectionUIHelper.kDevices ? "" : ConnectionUIHelper.GetProjectNameFromConnectionIdentifier(connectionId);
            ProjectNameSize = ConnectionDropDownStyles.sTVLine.CalcSize(GUIContent.Temp(ProjectName)).x;

            IP = ConnectionUIHelper.GetIP(connectionId);
            IPSize = ConnectionDropDownStyles.sTVLine.CalcSize(GUIContent.Temp(IP)).x;

            Port = ConnectionUIHelper.GetPort(connectionId);
            PortSize = ConnectionDropDownStyles.sTVLine.CalcSize(GUIContent.Temp(Port)).x;

            m_ConnectionId = connectionId;
            m_Connected = connected;
            m_OnSelected = onSelected;
            m_SubGroup = string.IsNullOrEmpty(subGroup) ? ConnectionUIHelper.GetRuntimePlatformFromIDString(idString) : subGroup;
            m_TopLevelGroup = topLevelGroup == ConnectionMajorGroup.Unknown ? (m_SubGroup == Content.DirectConnection ? ConnectionMajorGroup.Direct : (ProfilerDriver.IsIdentifierOnLocalhost(connectionId) ? ConnectionMajorGroup.Local : ConnectionMajorGroup.Remote)) : topLevelGroup;

            if (m_TopLevelGroup == ConnectionMajorGroup.Direct)
            {
                DisplayName = content;
            }

            if (string.IsNullOrEmpty(m_SubGroup))
            {
                if (idString == "Editor" || idString == "Main Editor Process")
                {
                    m_TopLevelGroup = ConnectionMajorGroup.Editor;
                    m_SubGroup = "Editor";
                }
                else
                {
                    DisplayName = content;
                    m_TopLevelGroup = ConnectionMajorGroup.ConnectionsWithoutID;
                    OldConnectionFormat = true;
                }
            }

            displayName = DisplayName + ProjectName;
            id = GenerateID();
            IsDevice = isDevice;
            if (isDevice)
            {
                IconContent = ConnectionUIHelper.GetIcon(iconString);
            }
        }

        public int GenerateID()
        {
            return (DisplayName + ProjectName + IP + Port).GetHashCode();
        }

        public bool DisplayProjectName()
        {
            return !IsDevice && !OldConnectionFormat &&
                ConnectionMajorGroup.Direct != m_TopLevelGroup && ConnectionMajorGroup.Logging != m_TopLevelGroup;
        }

        public bool DisplayPort()
        {
            return !IsDevice && !OldConnectionFormat &&
                ConnectionMajorGroup.Direct != m_TopLevelGroup && ConnectionMajorGroup.Logging != m_TopLevelGroup;
        }

        public bool DisplayIP()
        {
            return !IsDevice && !OldConnectionFormat &&
                ConnectionMajorGroup.Direct != m_TopLevelGroup && ConnectionMajorGroup.Logging != m_TopLevelGroup;
        }
    }

    enum ConnectionDropDownColumns
    {
        DisplayName,
        ProjectName,
        IP,
        Port
    }

    internal class ConnectionDropDownMultiColumnHeader : MultiColumnHeader
    {
        public ConnectionDropDownMultiColumnHeader(MultiColumnHeaderState state) : base(state)
        {
        }

        protected override void AddColumnHeaderContextMenuItems(GenericMenu menu)
        {
            base.AddColumnHeaderContextMenuItems(menu);
            menu.menuItems.RemoveAt(0);
        }
    }

    internal class ConnectionTreeView : TreeView
    {
        public List<ConnectionDropDownItem> dropDownItems = new List<ConnectionDropDownItem>(0);

        public string search = "";
        public float DisplayNameIndent => 2 * depthIndentWidth + ConnectionDropDownStyles.ToggleRectWidth + (2 * ConnectionDropDownStyles.SeparatorVerticalPadding) + ConnectionDropDownStyles.SeparatorLineWidth;
        Action m_CloseWindow;

        public ConnectionTreeView(TreeViewState state, ConnectionDropDownMultiColumnHeader multiColumnHeader, Action closeWindow) : base(state, multiColumnHeader)
        {
            showAlternatingRowBackgrounds = false;
            baseIndent = 0;
            depthIndentWidth = foldoutWidth;
            rowHeight = 20f;
            m_CloseWindow = closeWindow;
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = -1, depth = -1, displayName = "Root" };
            foreach (var connectionType in dropDownItems.OrderBy(x => x.m_TopLevelGroup).GroupBy(x => x.m_TopLevelGroup))
            {
                if (connectionType.Key == ConnectionDropDownItem.ConnectionMajorGroup.Editor)
                {
                    foreach (var connectionDropDownItem in connectionType)
                    {
                        root.AddChild(connectionDropDownItem);
                    }
                    continue;
                }
                // connection major group
                var i = new TreeViewItem { displayName = ConnectionDropDownItem.ConnectionMajorGroupLabels[(int)connectionType.Key]};
                i.id = (i.displayName + root.displayName).GetHashCode();
                root.AddChild(i);
                foreach (var playerType in connectionType.Where(x => x.m_SubGroup != null).GroupBy(x => x.m_SubGroup).OrderBy(x => x.Key))
                {
                    // direct and preeasy id connections dont have any subgrouping
                    if (i.displayName == ConnectionDropDownItem.ConnectionMajorGroupLabels[(int)ConnectionDropDownItem.ConnectionMajorGroup.Direct] ||
                        i.displayName == ConnectionDropDownItem.ConnectionMajorGroupLabels[(int)ConnectionDropDownItem.ConnectionMajorGroup.ConnectionsWithoutID])
                    {
                        foreach (var player in playerType.OrderBy(x => x.DisplayName).Where(x => x.DisplayName.ToLower().Contains(search.ToLower())))
                            i.AddChild(player);
                        continue;
                    }

                    // if we match a top level group add all of its children and return
                    if (string.Equals(i.displayName.ToLower(), search.ToLower(), StringComparison.Ordinal))
                    {
                        AddChildren(playerType, i);
                        SetupDepthsFromParentsAndChildren(root);
                        return root;
                    }
                    // if we match a player type add all of its children and continue
                    if (string.Equals(playerType.Key.ToLower(), search.ToLower(), StringComparison.Ordinal))
                    {
                        AddChildren(playerType, i);
                        continue;
                    }

                    AddChildren(playerType, i, search);
                }

                if (!i.hasChildren)
                    root.children.Remove(i);
            }
            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        TreeViewItem AddHeaderItem(string name, TreeViewItem item)
        {
            if (name == item.displayName) return item;

            var newItem = new TreeViewItem { displayName = name, id = (name + item.parent.displayName).GetHashCode() };
            item.AddChild(newItem);
            return newItem;
        }

        void AddChildren(IGrouping<string, ConnectionDropDownItem> group, TreeViewItem treeViewItem, string filter = null)
        {
            if (filter == null)
            {
                var header = AddHeaderItem(group.Key, treeViewItem);
                foreach (var player in group.OrderBy(x => x.DisplayName))
                    header.AddChild(player);
            }
            else
            {
                var header = AddHeaderItem(group.Key, treeViewItem);
                bool addedChild = false;
                foreach (var player in group.Where(x => x.DisplayName.ToLower().Contains(filter.ToLower()))
                         .OrderBy(x => x.DisplayName))
                {
                    addedChild = true;
                    header.AddChild(player);
                }

                if (!addedChild && treeViewItem.hasChildren)
                    treeViewItem.children.Remove(treeViewItem.children.Last());
            }
        }

        protected override void SingleClickedItem(int id)
        {
            var t = dropDownItems.Find(x => x.id == id);
            if (t is ConnectionDropDownItem && !t.m_Disabled)
            {
                t.m_OnSelected?.Invoke();
                m_CloseWindow.Invoke();
                EditorGUIUtility.ExitGUI();
                return;
            }

            var item = FindItem(id, rootItem);
            controller.UserInputChangedExpandedState(item, FindRowOfItem(item), !IsExpanded(id));
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item;

            if (item is ConnectionDropDownItem cddi)
            {
                GUI.enabled = !cddi.m_Disabled;
                if (GUI.enabled && args.selected && Event.current.keyCode == KeyCode.Return)
                {
                    cddi.m_OnSelected?.Invoke();
                    Repaint();
                }
            }

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, (ConnectionDropDownColumns)args.GetColumn(i), ref args);
            }
            GUI.enabled = true;
        }

        void CellGUI(Rect cellRect, TreeViewItem item, ConnectionDropDownColumns column, ref RowGUIArgs args)
        {
            // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
            CenterRectUsingSingleLineHeight(ref cellRect);
            ConnectionDropDownItem cddi = null;
            if (item is ConnectionDropDownItem downItem)
                cddi = downItem;

            switch (column)
            {
                case ConnectionDropDownColumns.DisplayName:
                    if (cddi != null)
                    {//actual connections
                        var rect = cellRect;
                        rect.x += GetContentIndent(item) - foldoutWidth;

                        EditorGUI.BeginChangeCheck();
                        rect.width = ConnectionDropDownStyles.ToggleRectWidth;
                        var isConnected = cddi.m_Connected?.Invoke() ?? false;
                        GUI.Label(rect, (isConnected ? EditorGUIUtility.IconContent("Valid") : GUIContent.none));
                        rect.x += ConnectionDropDownStyles.ToggleRectWidth;
                        if (cddi.IsDevice)
                        {
                            EditorGUI.LabelField(new Rect(rect.x, rect.y, rowHeight, rowHeight), cddi.IconContent);
                            rect.x += rowHeight;
                        }
                        var textRect = cellRect;
                        textRect.x = rect.x;
                        textRect.width = cellRect.width - (textRect.x - cellRect.x);

                        GUI.Label(textRect, ConnectionUIHelper.TruncateString(cddi.DisplayName, ConnectionDropDownStyles.sTVLine, textRect.width));
                        if (EditorGUI.EndChangeCheck())
                        {
                            cddi.m_OnSelected.Invoke();
                        }
                    }
                    else
                    {
                        var r = cellRect;
                        if (item.depth <= 0)
                        {// major group headers
                            if (args.row != 0)
                                EditorGUI.DrawRect(new Rect(r.x, r.y, args.rowRect.width, 1f), ConnectionDropDownStyles.SeparatorColor);
                            r.x += GetContentIndent(item);
                            r.width = args.rowRect.width - GetContentIndent(item);
                            GUI.Label(r, item.displayName, EditorStyles.boldLabel);
                        }
                        else
                        {// sub group headers
                            r.x += GetContentIndent(item);
                            EditorGUI.LabelField(new Rect(r.x, r.y, rowHeight, rowHeight), ConnectionUIHelper.GetIcon(item.displayName));
                            GUI.Label(new Rect(r.x + rowHeight, r.y, r.width - rowHeight, r.height), item.displayName, EditorStyles.miniBoldLabel);
                        }
                    }
                    break;

                case ConnectionDropDownColumns.ProjectName:
                    if(item.depth > 1)
                    {
                        DrawVerticalSeparatorLine(cellRect);
                        GUI.Label(cellRect, ConnectionUIHelper.TruncateString(cddi.ProjectName, ConnectionDropDownStyles.sTVLine, cellRect.width));

                    }
                    break;
                case ConnectionDropDownColumns.IP:
                    if(item.depth > 1)
                    {
                        DrawVerticalSeparatorLine(cellRect);
                        GUI.Label(cellRect, ConnectionUIHelper.TruncateString(cddi.IP, ConnectionDropDownStyles.sTVLine, cellRect.width));
                    }
                    break;
                case ConnectionDropDownColumns.Port:
                    if(item.depth > 1)
                    {
                        DrawVerticalSeparatorLine(cellRect);
                        GUI.Label(cellRect, ConnectionUIHelper.TruncateString(cddi.Port, ConnectionDropDownStyles.sTVLine, cellRect.width));
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(column), column, null);
            }
        }


        private void DrawVerticalSeparatorLine(Rect cellRect)
        {
            EditorGUI.DrawRect(
                new Rect(cellRect.x - 4, cellRect.y + ConnectionDropDownStyles.SeparatorVerticalPadding, ConnectionDropDownStyles.SeparatorLineWidth,
                    cellRect.height - 2 * ConnectionDropDownStyles.SeparatorVerticalPadding), ConnectionDropDownStyles.SeparatorColor);
        }

        public float GetHeight()
        {
            if (dropDownItems.Count > 0)
                return totalHeight;

            return 0;
        }
    }

    internal class ConnectionDropDownStyles
    {
        internal static float searchFieldPadding = 12f;
        internal static float searchFieldVerticalSpacing = 12f;

        internal static GUIStyle sConnectionTrouble = "MenuItem";
        internal static GUIStyle sTVLine = "TV Line";

        internal static readonly Color SeparatorColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        internal static int ToggleRectWidth = 16;
        internal static float SeparatorLineWidth = 1f;
        internal static float SeparatorVerticalPadding = 2f;
        public static float troubleShootBtnPadding = 10f;
    }

    internal class ConnectionTreeViewWindow : PopupWindowContent
    {
        private ConnectionTreeView m_connectionTreeView;
        private SearchField m_SearchField;

        private ConnectionDropDownMultiColumnHeader multiColumnHeader;
        private IConnectionStateInternal state;
        private static MultiColumnHeaderState multiColumnHeaderState;
        private static TreeViewState treeViewState;
        private List<ConnectionDropDownItem> connectionItems;
        private static bool firstOpen = true;
        float loggingVerticalPadding = 3f;
        float searchToLoggingPadding = 8f;
        ConsoleWindow.ConsoleAttachToPlayerState consoleAttachToPlayerState;
        bool didFocus;
        Dictionary<int, bool> m_expandedState = new Dictionary<int, bool>();
        bool searching;

        public ConnectionTreeViewWindow(IConnectionStateInternal internalState, Rect rect)
        {
            state = internalState;
            state.AddItemsToTree(this, rect);
            if (multiColumnHeaderState == null)
            {
                treeViewState = new TreeViewState();
                multiColumnHeaderState = CreateDefaultMultiColumnHeaderState(100);
                multiColumnHeader = new ConnectionDropDownMultiColumnHeader(multiColumnHeaderState);
                m_connectionTreeView = new ConnectionTreeView(treeViewState, multiColumnHeader, ClosePopUp) { dropDownItems = connectionItems };
                SetMinColumnWidths();
                return;
            }

            multiColumnHeader = new ConnectionDropDownMultiColumnHeader(multiColumnHeaderState);
            m_connectionTreeView = new ConnectionTreeView(treeViewState, multiColumnHeader, ClosePopUp) { dropDownItems = connectionItems };
        }

        static class Content
        {
            private static GUIStyle style = MultiColumnHeader.DefaultStyles.columnHeader;
            public static GUIContent PlayerName = new GUIContent("Player Name");
            public static float PlayerNameMinWidth = 150;
            public static GUIContent ProjectName = new GUIContent("Product Name");
            public static float ProjectNameMinWidth = style.CalcSize(ProjectName).x;
            public static GUIContent IP = new GUIContent("IP");
            public static float IPMinWidth = style.CalcSize(GUIContent.Temp("00000")).x;
            public static GUIContent Port = new GUIContent("Port");
            public static float PortMinWidth = style.CalcSize(GUIContent.Temp("00000")).x;
            public static GUIContent TroubleShoot = new GUIContent("Troubleshoot Connection Issues");
        }

        void ClosePopUp()
        {
            this.editorWindow.Close();
        }

        public override void OnClose()
        {
            state = null;
        }

        internal void Reload()
        {
            if (m_connectionTreeView.dropDownItems.Count > 0)
            {
                m_connectionTreeView.Reload();
            }
        }

        public override void OnOpen()
        {
            Reload();

            if (firstOpen)
                m_connectionTreeView.ExpandAll();

            firstOpen = false;
            m_SearchField = new SearchField();
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUI.BeginChangeCheck();
            m_connectionTreeView.search = m_SearchField.OnGUI(new Rect(rect.x + ConnectionDropDownStyles.searchFieldPadding, rect.y + ConnectionDropDownStyles.searchFieldPadding, rect.width - (2 * ConnectionDropDownStyles.searchFieldPadding), EditorGUI.kSingleLineHeight), m_connectionTreeView.search);

            if (!didFocus && !m_SearchField.HasFocus())
            {
                m_SearchField.SetFocus();
                didFocus = true;
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (!string.IsNullOrEmpty(m_connectionTreeView.search))
                {
                    if (!searching)
                    {
                        var rows = m_connectionTreeView.GetRows();
                        m_expandedState.Clear();
                        foreach (var row in rows)
                        {
                            m_expandedState.Add(row.id, m_connectionTreeView.IsExpanded(row.id));
                        }
                    }
                    m_connectionTreeView.ExpandAll();
                    searching = true;
                }
                else
                {
                    if (searching)
                    {
                        foreach (var b in m_expandedState)
                        {
                            m_connectionTreeView.SetExpanded(b.Key, b.Value);
                        }
                        searching = false;
                    }

                }

                Reload();
            }
            rect.y += EditorGUI.kSingleLineHeight + ConnectionDropDownStyles.searchFieldVerticalSpacing;

            if (consoleAttachToPlayerState != null)
            {
                rect.y += searchToLoggingPadding;
                EditorGUI.BeginChangeCheck();
                GUI.Toggle(new Rect(rect.x + ConnectionDropDownStyles.searchFieldPadding, rect.y, rect.width, EditorGUI.kSingleLineHeight), consoleAttachToPlayerState.IsConnected(), ConnectionUIHelper.Content.PlayerLogging);
                if (EditorGUI.EndChangeCheck())
                {
                    consoleAttachToPlayerState.PlayerLoggingOptionSelected();
                }
                rect.y += EditorGUI.kSingleLineHeight + loggingVerticalPadding;
                GUI.enabled = consoleAttachToPlayerState.IsConnected();
                m_connectionTreeView.dropDownItems.ForEach(x => x.m_Disabled = !GUI.enabled);

                EditorGUI.BeginChangeCheck();
                GUI.Toggle(new Rect(rect.x + ConnectionDropDownStyles.searchFieldPadding, rect.y, rect.width, EditorGUI.kSingleLineHeight), consoleAttachToPlayerState.IsLoggingFullLog(), ConnectionUIHelper.Content.FullLog);
                if (EditorGUI.EndChangeCheck())
                {
                    consoleAttachToPlayerState.FullLogOptionSelected();
                }
                rect.y += EditorGUI.kSingleLineHeight + loggingVerticalPadding;
            }

            m_connectionTreeView?.OnGUI(new Rect(rect.x, rect.y, rect.width, (float)m_connectionTreeView?.totalHeight));
            rect.y += (float)m_connectionTreeView?.totalHeight;
            GUI.enabled = true;

            EditorGUI.DrawDelimiterLine(new Rect(rect.x,rect.y,rect.width,1f));
            rect.y += 1f;
            rect.y += ConnectionDropDownStyles.troubleShootBtnPadding / 2f;
            if (EditorGUI.Button(rect, Content.TroubleShoot, ConnectionDropDownStyles.sConnectionTrouble))
            {
                var help = Help.FindHelpNamed("profiler-profiling-applications");
                Help.BrowseURL(help);
            }

            if(Event.current.type == EventType.MouseMove)
                Event.current.Use();

        }

        private void SetMinColumnWidths()
        {
            foreach (var stateVisibleColumn in m_connectionTreeView.multiColumnHeader.state.visibleColumns)
            {
                m_connectionTreeView.multiColumnHeader.state.columns[stateVisibleColumn].width = GetLargestWidth(stateVisibleColumn);
            }
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth)
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = Content.PlayerName,
                    headerTextAlignment = TextAlignment.Left,
                    autoResize = false,
                    allowToggleVisibility = false,
                    minWidth = Content.PlayerNameMinWidth,
                    canSort = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = Content.ProjectName,
                    headerTextAlignment = TextAlignment.Left,
                    autoResize = false,
                    allowToggleVisibility = true,
                    minWidth = Content.ProjectNameMinWidth,
                    canSort = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = Content.IP,
                    headerTextAlignment = TextAlignment.Left,
                    autoResize = false,
                    allowToggleVisibility = true,
                    minWidth = Content.IPMinWidth,
                    canSort = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = Content.Port,
                    headerTextAlignment = TextAlignment.Left,
                    autoResize = false,
                    allowToggleVisibility = true,
                    minWidth = Content.PortMinWidth,
                    canSort = false
                }
            };

            var state = new MultiColumnHeaderState(columns);
            return state;
        }

        public void AddDisabledItem(ConnectionDropDownItem connectionDropDownItem)
        {
            connectionDropDownItem.m_Disabled = true;
            AddItem(connectionDropDownItem);
        }

        public void AddItem(ConnectionDropDownItem connectionDropDownItem)
        {
            // *begin-nonstandard-formatting*
            connectionItems ??= new List<ConnectionDropDownItem>();
            // *end-nonstandard-formatting*

            var dupes = connectionItems.FirstOrDefault(x => x.DisplayName == connectionDropDownItem.DisplayName && x.IP == connectionDropDownItem.IP && x.Port == connectionDropDownItem.Port);
            if (dupes != null)
                connectionItems.Remove(dupes);

            connectionItems.Add(connectionDropDownItem);
        }

        public bool HasItem(string name)
        {
            return connectionItems != null && connectionItems.Any(x => x.DisplayName == name);
        }

        public override Vector2 GetWindowSize()
        {
            Vector2 v = Vector2.zero;
            v.y +=  EditorGUI.kSingleLineHeight + ConnectionDropDownStyles.searchFieldVerticalSpacing; // search field
            v.x = Mathf.Max(200, GetTotalVisibleWidth());
            if (consoleAttachToPlayerState != null)
            {
                v.y += searchToLoggingPadding + 2 * (EditorGUI.kSingleLineHeight + loggingVerticalPadding);
            }
            v.y += GetTotalTreeViewHeight();
            v.y += EditorGUI.kSingleLineHeight;
            v.y += ConnectionDropDownStyles.troubleShootBtnPadding;
            return v;
        }

        private float GetTotalTreeViewHeight()
        {
            return m_connectionTreeView.GetHeight();
        }

        float GetLargestWidth(int idx)
        {
            return idx switch
            {
                0 => GetRequiredDisplayNameColumnWidth(),
                1 => GetRequiredProjectNameColumnWidth(),
                2 => GetRequiredIPColumnWidth(),
                3 => GetRequiredPortColumnWidth(),
                _ => 0
            };
        }

        private float GetRequiredProjectNameColumnWidth()
        {
            return Mathf.Max(Content.ProjectNameMinWidth,
                connectionItems.Max(x => x.ProjectNameSize));
        }

        private float GetRequiredDisplayNameColumnWidth()
        {
            return Mathf.Max(Content.PlayerNameMinWidth,
                connectionItems.Max(x => x.DisplayNameSize) + m_connectionTreeView.DisplayNameIndent );
        }

        private float GetRequiredIPColumnWidth()
        {
            return Mathf.Max(Content.IPMinWidth, connectionItems.Max(x => x.IPSize));
        }

        private float GetRequiredPortColumnWidth()
        {
            return Mathf.Max(Content.PortMinWidth, connectionItems.Max(x => x.PortSize));
        }

        float GetTotalVisibleWidth()
        {
            return m_connectionTreeView.multiColumnHeader.state.widthOfAllVisibleColumns;
        }

        //needed for tests
        public int GetItemCount()
        {
            return m_connectionTreeView?.dropDownItems.Count ?? 0;
        }

        public void Clear()
        {
            m_connectionTreeView?.dropDownItems.Clear();
        }

        public void SetLoggingOptions(ConsoleWindow.ConsoleAttachToPlayerState state)
        {
            consoleAttachToPlayerState = state;
        }
    }
}
