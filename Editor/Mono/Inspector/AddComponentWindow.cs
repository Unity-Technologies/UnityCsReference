// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [InitializeOnLoad]
    internal class AddComponentWindow : EditorWindow
    {
        // Element classes

        internal enum Language
        {
            CSharp = 0
        }

        const Language kDefaultLanguage = Language.CSharp;

        class Element : System.IComparable
        {
            public int level;
            public GUIContent content;
            public string name { get { return content.text; } }

            public virtual int CompareTo(object o)
            {
                return name.CompareTo((o as Element).name);
            }
        }

        class ComponentElement : Element
        {
            public string menuPath;
            public bool isLegacy;
            private GUIContent m_LegacyContentCache;
            public GUIContent legacyContent
            {
                get
                {
                    if (m_LegacyContentCache == null)
                    {
                        m_LegacyContentCache = new GUIContent(content);
                        m_LegacyContentCache.text += " (Legacy)";
                    }
                    return m_LegacyContentCache;
                }
            }

            public ComponentElement(int level, string name, string menuPath, string commandString)
            {
                this.level = level;
                this.menuPath = menuPath;
                isLegacy = menuPath.Contains("Legacy");

                if (commandString.StartsWith("SCRIPT"))
                {
                    int scriptID = int.Parse(commandString.Substring(6));
                    Object obj = EditorUtility.InstanceIDToObject(scriptID);
                    Texture icon = AssetPreview.GetMiniThumbnail(obj);
                    content = new GUIContent(name, icon);
                }
                else
                {
                    int classID = int.Parse(commandString);
                    content = new GUIContent(name, AssetPreview.GetMiniTypeThumbnailFromClassID(classID));
                }
            }

            public override int CompareTo(object o)
            {
                if (o is ComponentElement)
                {
                    // legacy elements should always come after non legacy elements
                    var componentElement = (ComponentElement)o;
                    if (this.isLegacy && !componentElement.isLegacy)
                        return 1;
                    if (!this.isLegacy && componentElement.isLegacy)
                        return -1;
                }
                return base.CompareTo(o);
            }
        }

        [System.Serializable]
        class GroupElement : Element
        {
            public Vector2 scroll;
            public int selectedIndex = 0;

            public GroupElement(int level, string name)
            {
                this.level = level;
                content = new GUIContent(name);
            }
        }

        class NewScriptElement : GroupElement
        {
            // char array can't be const for compiler reasons but this should still be treated as such.
            private char[] kInvalidPathChars = new char[] {'<', '>', ':', '"', '|', '?', '*', (char)0};
            private char[] kPathSepChars = new char[] {'/', '\\'};
            private const string kResourcesTemplatePath = "Resources/ScriptTemplates";

            private string m_Directory = string.Empty;

            public NewScriptElement() : base(1, "New Script") {}

            public void OnGUI()
            {
                GUILayout.Label("Name", EditorStyles.label);

                EditorGUI.FocusTextInControl("NewScriptName");
                GUI.SetNextControlName("NewScriptName");
                className = EditorGUILayout.TextField(className);

                EditorGUILayout.Space();

                Language langNew = (Language)EditorGUILayout.EnumPopup("Language", s_Lang);
                if (langNew != s_Lang)
                {
                    s_Lang = langNew;
                    EditorPrefs.SetInt(kLanguageEditorPrefName, (int)langNew);
                }

                EditorGUILayout.Space();

                bool canCreate = CanCreate();
                if (!canCreate && className != "")
                    GUILayout.Label(GetError(), EditorStyles.helpBox);

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(!canCreate))
                {
                    if (GUILayout.Button("Create and Add"))
                    {
                        Create();
                    }
                }

                EditorGUILayout.Space();
            }

            public bool CanCreate()
            {
                return className.Length > 0 &&
                    !File.Exists(TargetPath()) &&
                    !ClassAlreadyExists() &&
                    !ClassNameIsInvalid() &&
                    !InvalidTargetPath();
            }

            private string GetError()
            {
                // Create string to tell the user what the problem is
                string blockReason = string.Empty;
                if (className != string.Empty)
                {
                    if (File.Exists(TargetPath()))
                        blockReason = "A script called \"" + className + "\" already exists at that path.";
                    else if (ClassAlreadyExists())
                        blockReason = "A class called \"" + className + "\" already exists.";
                    else if (ClassNameIsInvalid())
                        blockReason = "The script name may only consist of a-z, A-Z, 0-9, _.";
                    else if (InvalidTargetPath())
                        blockReason = "The folder path contains invalid characters.";
                }
                return blockReason;
            }

            public void Create()
            {
                if (!CanCreate())
                    return;

                CreateScript();

                foreach (GameObject go in gameObjects)
                {
                    MonoScript script = AssetDatabase.LoadAssetAtPath(TargetPath(), typeof(MonoScript)) as MonoScript;
                    script.SetScriptTypeWasJustCreatedFromComponentMenu();
                    InternalEditorUtility.AddScriptComponentUncheckedUndoable(go, script);
                }

                s_AddComponentWindow.SendUsabilityAnalyticsEvent(new AnalyticsEventData
                {
                    name = className,
                    filter = s_AddComponentWindow.m_DelayedSearch ?? s_AddComponentWindow.m_Search,
                    isNewScript = true
                });

                s_AddComponentWindow.Close();
            }

            private string extension
            {
                get
                {
                    switch (s_Lang)
                    {
                        case Language.CSharp:
                            return "cs";
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            private string templatePath
            {
                get
                {
                    string basePath = Path.Combine(EditorApplication.applicationContentsPath, kResourcesTemplatePath);
                    switch (s_Lang)
                    {
                        case Language.CSharp:
                            return Path.Combine(basePath, "81-C# Script-NewBehaviourScript.cs.txt");
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            private bool InvalidTargetPath()
            {
                if (m_Directory.IndexOfAny(kInvalidPathChars) >= 0)
                    return true;
                if (TargetDir().Split(kPathSepChars, StringSplitOptions.None).Contains(string.Empty))
                    return true;
                return false;
            }

            public string TargetPath()
            {
                return Path.Combine(TargetDir(), className + "." + extension);
            }

            private string TargetDir()
            {
                return Path.Combine("Assets", m_Directory.Trim(kPathSepChars));
            }

            private bool ClassNameIsInvalid()
            {
                return !System.CodeDom.Compiler.CodeGenerator.IsValidLanguageIndependentIdentifier(
                    className);
            }

            private bool ClassExists(string className)
            {
                return AppDomain.CurrentDomain.GetAssemblies()
                    .Any(a => a.GetType(className, false) != null);
            }

            private bool ClassAlreadyExists()
            {
                if (className == string.Empty)
                    return false;
                return ClassExists(className);
            }

            private void CreateScript()
            {
                ProjectWindowUtil.CreateScriptAssetFromTemplate(TargetPath(), templatePath);
                AssetDatabase.Refresh();
            }
        }

        [System.Serializable]
        class AnalyticsEventData
        {
            public string name;
            public string filter;
            public bool isNewScript;
        }

        // Styles

        class Styles
        {
            public GUIStyle header = new GUIStyle(EditorStyles.inspectorBig);
            public GUIStyle componentButton = new GUIStyle("PR Label");
            public GUIStyle groupButton;
            public GUIStyle background = "grey_border";
            public GUIStyle previewHeader = new GUIStyle(EditorStyles.label);
            public GUIStyle previewText = new GUIStyle(EditorStyles.wordWrappedLabel);
            public GUIStyle rightArrow = "AC RightArrow";
            public GUIStyle leftArrow = "AC LeftArrow";

            public Styles()
            {
                header.font = EditorStyles.boldLabel.font;

                componentButton.alignment = TextAnchor.MiddleLeft;
                componentButton.padding.left -= 15;
                componentButton.fixedHeight = 20;

                groupButton = new GUIStyle(componentButton);
                groupButton.padding.left += 17;

                previewText.padding.left += 3;
                previewText.padding.right += 3;
                previewHeader.padding.left += 3 - 2;
                previewHeader.padding.right += 3;
                previewHeader.padding.top += 3;
                previewHeader.padding.bottom += 2;
            }
        }

        // Constants

        private const int kHeaderHeight = 30;
        private const int kWindowHeight = 400 - 80;
        private const int kHelpHeight = 80 * 0;
        private const string kLanguageEditorPrefName = "NewScriptLanguage";
        private const string kComponentSearch = "ComponentSearchString";

        // Static variables

        private static Styles s_Styles;
        private static AddComponentWindow s_AddComponentWindow = null;
        private static long s_LastClosedTime;
        internal static Language s_Lang;
        private static bool s_DirtyList = false;

        // Member variables with static accessors

        private string m_ClassName = "";
        internal static string className { get { return s_AddComponentWindow.m_ClassName; } set { s_AddComponentWindow.m_ClassName = value; } }
        private GameObject[] m_GameObjects;
        internal static GameObject[] gameObjects { get { return s_AddComponentWindow.m_GameObjects; } }

        // Member variables

        private Element[] m_Tree;
        private Element[] m_SearchResultTree;
        private List<GroupElement> m_Stack = new List<GroupElement>();

        private float m_Anim = 1;
        private int m_AnimTarget = 1;
        private long m_LastTime = 0;
        private bool m_ScrollToSelected = false;
        private string m_DelayedSearch = null;
        private string m_Search = "";

        private DateTime m_OpenTime;

        // Properties

        private bool hasSearch { get { return !string.IsNullOrEmpty(m_Search); } }
        private GroupElement activeParent { get { return m_Stack[m_Stack.Count - 2 + m_AnimTarget]; } }
        private Element[] activeTree { get { return hasSearch ? m_SearchResultTree : m_Tree; } }
        private Element activeElement
        {
            get
            {
                if (activeTree == null)
                    return null;

                List<Element> children = GetChildren(activeTree, activeParent);
                if (children.Count == 0)
                    return null;

                return children[activeParent.selectedIndex];
            }
        }
        private bool isAnimating { get { return m_Anim != m_AnimTarget; } }
        // Methods

        static AddComponentWindow()
        {
            s_DirtyList = true;
        }

        void OnEnable()
        {
            s_AddComponentWindow = this;

            s_Lang = (Language)EditorPrefs.GetInt(kLanguageEditorPrefName, (int)kDefaultLanguage);
            if (!Enum.IsDefined(typeof(Language), s_Lang))
            {
                // We removed boo from the Language enum so ensure persistent value is valid
                EditorPrefs.SetInt(kLanguageEditorPrefName, (int)kDefaultLanguage);
                s_Lang = kDefaultLanguage;
            }

            m_Search = EditorPrefs.GetString(kComponentSearch, "");
        }

        void OnDisable()
        {
            s_LastClosedTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            s_AddComponentWindow = null;
        }

        private static InspectorWindow FirstInspectorWithGameObject()
        {
            foreach (InspectorWindow insp in InspectorWindow.GetInspectors())
                if (insp.GetInspectedObject() is GameObject)
                    return insp;
            return null;
        }

        internal static bool ValidateAddComponentMenuItem()
        {
            if (FirstInspectorWithGameObject() != null)
                return true;
            return false;
        }

        internal static void ExecuteAddComponentMenuItem()
        {
            InspectorWindow insp = FirstInspectorWithGameObject();
            if (insp != null)
                insp.SendEvent(EditorGUIUtility.CommandEvent("OpenAddComponentDropdown"));
        }

        internal static bool Show(Rect rect, GameObject[] gos)
        {
            // If the window is already open, close it instead.
            Object[] wins = Resources.FindObjectsOfTypeAll(typeof(AddComponentWindow));
            if (wins.Length > 0)
            {
                ((EditorWindow)wins[0]).Close();
                return false;
            }

            // We could not use realtimeSinceStartUp since it is set to 0 when entering/exitting playmode, we assume an increasing time when comparing time.
            long nowMilliSeconds = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            bool justClosed = nowMilliSeconds < s_LastClosedTime + 50;
            if (!justClosed)
            {
                Event.current.Use();
                if (s_AddComponentWindow == null)
                    s_AddComponentWindow = ScriptableObject.CreateInstance<AddComponentWindow>();
                s_AddComponentWindow.Init(rect);
                s_AddComponentWindow.m_GameObjects = gos;
                return true;
            }
            return false;
        }

        void Init(Rect buttonRect)
        {
            m_OpenTime = System.DateTime.UtcNow;

            // Has to be done before calling Show / ShowWithMode
            buttonRect = GUIUtility.GUIToScreenRect(buttonRect);

            CreateComponentTree();

            ShowAsDropDown(buttonRect, new Vector2(buttonRect.width, kWindowHeight), null, ShowMode.PopupMenuWithKeyboardFocus);

            Focus();

            // Add after unfreezing display because AuxWindowManager.cpp assumes that aux windows are added after we got/lost- focus calls.
            m_Parent.AddToAuxWindowList();

            wantsMouseMove = true;
        }

        private void CreateComponentTree()
        {
            string[] menus = Unsupported.GetSubmenus("Component");
            string[] commands = Unsupported.GetSubmenusCommands("Component");
            List<string> stack = new List<string>();
            List<Element> tree = new List<Element>();
            for (int i = 0; i < menus.Length; i++)
            {
                //We don't want to show the "Add..." that is shown on the Main Menu under Component/...
                //that would opens this menu.
                //Although it seem like doing this on MenuController::ExtractSubmenus could make more sense.
                if (commands[i] == "ADD")
                {
                    continue;
                }

                string str = menus[i];
                string[] path = str.Split('/');
                while (path.Length - 1 < stack.Count)
                    stack.RemoveAt(stack.Count - 1);
                while (stack.Count > 0 && path[stack.Count - 1] != stack[stack.Count - 1])
                    stack.RemoveAt(stack.Count - 1);
                while (path.Length - 1 > stack.Count)
                {
                    tree.Add(new GroupElement(stack.Count, LocalizationDatabase.GetLocalizedString(path[stack.Count])));
                    stack.Add(path[stack.Count]);
                }
                tree.Add(new ComponentElement(stack.Count, LocalizationDatabase.GetLocalizedString(path[path.Length - 1]), str, commands[i]));
            }
            tree.Add(new NewScriptElement());

            m_Tree = tree.ToArray();

            // Rebuild stack
            if (m_Stack.Count == 0)
                m_Stack.Add(m_Tree[0] as GroupElement);
            else
            {
                // The root is always the match for level 0
                GroupElement match = m_Tree[0] as GroupElement;
                int level = 0;
                while (true)
                {
                    // Assign the match for the current level
                    GroupElement oldElement = m_Stack[level];
                    m_Stack[level] = match;
                    m_Stack[level].selectedIndex = oldElement.selectedIndex;
                    m_Stack[level].scroll = oldElement.scroll;

                    // See if we reached last element of stack
                    level++;
                    if (level == m_Stack.Count)
                        break;

                    // Try to find a child of the same name as we had before
                    List<Element> children = GetChildren(activeTree, match);
                    Element childMatch = children.FirstOrDefault(c => c.name == m_Stack[level].name);
                    if (childMatch != null && childMatch is GroupElement)
                    {
                        match = childMatch as GroupElement;
                    }
                    else
                    {
                        // If we couldn't find the child, remove all further elements from the stack
                        while (m_Stack.Count > level)
                            m_Stack.RemoveAt(level);
                    }
                }
            }

            //Debug.Log ("Rebuilt tree - "+m_Tree.Length+" elements");
            s_DirtyList = false;
            RebuildSearch();
        }

        internal void OnGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            GUI.Label(new Rect(0, 0, position.width, position.height), GUIContent.none, s_Styles.background);


            if (s_DirtyList)
                CreateComponentTree();

            // Keyboard
            HandleKeyboard();

            GUILayout.Space(7);

            // Search
            if (!(activeParent is NewScriptElement))
                EditorGUI.FocusTextInControl("ComponentSearch");
            Rect searchRect = GUILayoutUtility.GetRect(10, 20);
            searchRect.x += 8;
            searchRect.width -= 16;

            GUI.SetNextControlName("ComponentSearch");

            using (new EditorGUI.DisabledScope(activeParent is NewScriptElement))
            {
                string newSearch = EditorGUI.SearchField(searchRect, m_DelayedSearch ?? m_Search);

                if (newSearch != m_Search || m_DelayedSearch != null)
                {
                    if (!isAnimating)
                    {
                        m_Search = m_DelayedSearch ?? newSearch;
                        EditorPrefs.SetString(kComponentSearch, m_Search);
                        RebuildSearch();
                        m_DelayedSearch = null;
                    }
                    else
                    {
                        m_DelayedSearch = newSearch;
                    }
                }
            }

            // Show lists
            ListGUI(activeTree, m_Anim, GetElementRelative(0), GetElementRelative(-1));
            if (m_Anim < 1)
                ListGUI(activeTree, m_Anim + 1, GetElementRelative(-1), GetElementRelative(-2));

            // Animate
            if (isAnimating && Event.current.type == EventType.Repaint)
            {
                long now = System.DateTime.Now.Ticks;
                float deltaTime = (now - m_LastTime) / (float)System.TimeSpan.TicksPerSecond;
                m_LastTime = now;
                m_Anim = Mathf.MoveTowards(m_Anim, m_AnimTarget, deltaTime * 4);
                if (m_AnimTarget == 0 && m_Anim == 0)
                {
                    m_Anim = 1;
                    m_AnimTarget = 1;
                    m_Stack.RemoveAt(m_Stack.Count - 1);
                }
                Repaint();
            }
        }

        private void HandleKeyboard()
        {
            Event evt = Event.current;
            if (evt.type == EventType.KeyDown)
            {
                // Special handling when in new script panel
                if (activeParent is NewScriptElement)
                {
                    // When creating new script name we want to dedicate both left/right arrow and backspace
                    // to editing the script name so they can't be used for navigating the menus.
                    // The only way to get back using the keyboard is pressing Esc.
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        (activeParent as NewScriptElement).Create();
                        evt.Use();
                        GUIUtility.ExitGUI();
                    }
                    if (evt.keyCode == KeyCode.Escape)
                    {
                        GoToParent();
                        evt.Use();
                    }
                }
                else
                {
                    // Always do these
                    if (evt.keyCode == KeyCode.DownArrow)
                    {
                        activeParent.selectedIndex++;
                        activeParent.selectedIndex = Mathf.Min(activeParent.selectedIndex, GetChildren(activeTree, activeParent).Count - 1);
                        m_ScrollToSelected = true;
                        evt.Use();
                    }
                    if (evt.keyCode == KeyCode.UpArrow)
                    {
                        activeParent.selectedIndex--;
                        activeParent.selectedIndex = Mathf.Max(activeParent.selectedIndex, 0);
                        m_ScrollToSelected = true;
                        evt.Use();
                    }
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        GoToChild(activeElement, true);
                        evt.Use();
                    }

                    // Do these if we're not in search mode
                    if (!hasSearch)
                    {
                        if (evt.keyCode == KeyCode.LeftArrow || evt.keyCode == KeyCode.Backspace)
                        {
                            GoToParent();
                            evt.Use();
                        }
                        if (evt.keyCode == KeyCode.RightArrow)
                        {
                            GoToChild(activeElement, false);
                            evt.Use();
                        }
                        if (evt.keyCode == KeyCode.Escape)
                        {
                            Close();
                            evt.Use();
                        }
                    }
                }
            }
        }

        const string kSearchHeader = "Search";

        private void RebuildSearch()
        {
            if (!hasSearch)
            {
                m_SearchResultTree = null;
                if (m_Stack[m_Stack.Count - 1].name == kSearchHeader)
                {
                    m_Stack.Clear();
                    m_Stack.Add(m_Tree[0] as GroupElement);
                }
                m_AnimTarget = 1;
                m_LastTime = System.DateTime.Now.Ticks;
                m_ClassName = "NewBehaviourScript";
                return;
            }

            m_ClassName = m_Search;

            // Support multiple search words separated by spaces.
            string[] searchWords = m_Search.ToLower().Split(' ');

            // We keep two lists. Matches that matches the start of an item always get first priority.
            List<Element> matchesStart = new List<Element>();
            List<Element> matchesWithin = new List<Element>();

            foreach (Element e in m_Tree)
            {
                if (!(e is ComponentElement))
                    continue;

                string name = e.name.ToLower().Replace(" ", "");
                bool didMatchAll = true;
                bool didMatchStart = false;

                // See if we match ALL the seaarch words.
                for (int w = 0; w < searchWords.Length; w++)
                {
                    string search = searchWords[w];
                    if (name.Contains(search))
                    {
                        // If the start of the item matches the first search word, make a note of that.
                        if (w == 0 && name.StartsWith(search))
                            didMatchStart = true;
                    }
                    else
                    {
                        // As soon as any word is not matched, we disregard this item.
                        didMatchAll = false;
                        break;
                    }
                }
                // We always need to match all search words.
                // If we ALSO matched the start, this item gets priority.
                if (didMatchAll)
                {
                    if (didMatchStart)
                        matchesStart.Add(e);
                    else
                        matchesWithin.Add(e);
                }
            }

            matchesStart.Sort();
            matchesWithin.Sort();

            // Create search tree
            List<Element> tree = new List<Element>();
            // Add parent
            tree.Add(new GroupElement(0, kSearchHeader));
            // Add search results
            tree.AddRange(matchesStart);
            tree.AddRange(matchesWithin);
            // Add the new script element
            tree.Add(m_Tree[m_Tree.Length - 1]);
            // Create search result tree
            m_SearchResultTree = tree.ToArray();
            m_Stack.Clear();
            m_Stack.Add(m_SearchResultTree[0] as GroupElement);

            // Always select the first search result when search is changed (e.g. a character was typed in or deleted),
            // because it's usually the best match.
            if (GetChildren(activeTree, activeParent).Count >= 1)
                activeParent.selectedIndex = 0;
            else
                activeParent.selectedIndex = -1;
        }

        private GroupElement GetElementRelative(int rel)
        {
            int i = m_Stack.Count + rel - 1;
            if (i < 0)
                return null;
            return m_Stack[i] as GroupElement;
        }

        private void GoToParent()
        {
            if (m_Stack.Count > 1)
            {
                m_AnimTarget = 0;
                m_LastTime = System.DateTime.Now.Ticks;
            }
        }

        private void GoToChild(Element e, bool addIfComponent)
        {
            if (e is NewScriptElement)
            {
                if (!hasSearch)
                {
                    // Get unique file name
                    m_ClassName = AssetDatabase.GenerateUniqueAssetPath((e as NewScriptElement).TargetPath());
                    m_ClassName = Path.GetFileNameWithoutExtension(m_ClassName);
                }
            }
            if (e is ComponentElement)
            {
                if (addIfComponent)
                {
                    SendUsabilityAnalyticsEvent(new AnalyticsEventData
                    {
                        name = ((ComponentElement)e).name,
                        filter = m_DelayedSearch ?? m_Search,
                        isNewScript = false
                    });

                    EditorApplication.ExecuteMenuItemOnGameObjects(((ComponentElement)e).menuPath, m_GameObjects);
                    Close();
                }
            }
            else if ((!hasSearch) || e is NewScriptElement)
            {
                m_LastTime = System.DateTime.Now.Ticks;
                if (m_AnimTarget == 0)
                    m_AnimTarget = 1;
                else if (m_Anim == 1)
                {
                    m_Anim = 0;
                    m_Stack.Add(e as GroupElement);
                }
            }
        }

        void SendUsabilityAnalyticsEvent(AnalyticsEventData eventData)
        {
            UsabilityAnalytics.SendEvent("executeAddComponentWindow", m_OpenTime, DateTime.UtcNow - m_OpenTime, false, eventData);
        }

        private void ListGUI(Element[] tree, float anim, GroupElement parent, GroupElement grandParent)
        {
            // Smooth the fractional part of the anim value
            anim = Mathf.Floor(anim) + Mathf.SmoothStep(0, 1, Mathf.Repeat(anim, 1));

            // Calculate rect for animated area
            Rect animRect = position;
            animRect.x = position.width * (1 - anim) + 1;
            animRect.y = kHeaderHeight;
            animRect.height -= kHeaderHeight + kHelpHeight;
            animRect.width -= 2;

            // Start of animated area (the part that moves left and right)
            GUILayout.BeginArea(animRect);

            // Header
            Rect headerRect = GUILayoutUtility.GetRect(10, 25);
            string name = parent.name;
            GUI.Label(headerRect, name, s_Styles.header);

            // Back button
            if (grandParent != null)
            {
                Rect arrowRect = new Rect(headerRect.x + 4, headerRect.y + 7, 13, 13);
                if (Event.current.type == EventType.Repaint)
                    s_Styles.leftArrow.Draw(arrowRect, false, false, false, false);
                if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
                {
                    GoToParent();
                    Event.current.Use();
                }
            }

            if (parent is NewScriptElement)
                (parent as NewScriptElement).OnGUI();
            else
                ListGUI(tree, parent);

            GUILayout.EndArea();
        }

        private void ListGUI(Element[] tree, GroupElement parent)
        {
            // Start of scroll view list
            parent.scroll = GUILayout.BeginScrollView(parent.scroll);

            EditorGUIUtility.SetIconSize(new Vector2(16, 16));

            List<Element> children = GetChildren(tree, parent);

            Rect selectedRect = new Rect();


            // Iterate through the children
            for (int i = 0; i < children.Count; i++)
            {
                Element e = children[i];
                Rect r = GUILayoutUtility.GetRect(16, 20, GUILayout.ExpandWidth(true));

                // Select the element the mouse cursor is over.
                // Only do it on mouse move - keyboard controls are allowed to overwrite this until the next time the mouse moves.
                if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDown)
                {
                    if (parent.selectedIndex != i && r.Contains(Event.current.mousePosition))
                    {
                        parent.selectedIndex = i;
                        Repaint();
                    }
                }

                bool selected = false;
                // Handle selected item
                if (i == parent.selectedIndex)
                {
                    selected = true;
                    selectedRect = r;
                }

                // Draw element
                if (Event.current.type == EventType.Repaint)
                {
                    GUIStyle labelStyle = s_Styles.groupButton;
                    GUIContent labelContent = e.content;

                    bool isComponent = e is ComponentElement;
                    if (isComponent)
                    {
                        var componentElement = (ComponentElement)e;

                        labelStyle = s_Styles.componentButton;
                        if (componentElement.isLegacy && hasSearch)
                            labelContent = componentElement.legacyContent;
                    }

                    labelStyle.Draw(r, labelContent, false, false, selected, selected);
                    if (!isComponent)
                    {
                        Rect arrowRect = new Rect(r.x + r.width - 13, r.y + 4, 13, 13);
                        s_Styles.rightArrow.Draw(arrowRect, false, false, false, false);
                    }
                }
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    parent.selectedIndex = i;
                    GoToChild(e, true);
                }
            }

            EditorGUIUtility.SetIconSize(Vector2.zero);

            GUILayout.EndScrollView();

            // Scroll to show selected
            if (m_ScrollToSelected && Event.current.type == EventType.Repaint)
            {
                m_ScrollToSelected = false;
                Rect scrollRect = GUILayoutUtility.GetLastRect();
                if (selectedRect.yMax - scrollRect.height > parent.scroll.y)
                {
                    parent.scroll.y = selectedRect.yMax - scrollRect.height;
                    Repaint();
                }
                if (selectedRect.y < parent.scroll.y)
                {
                    parent.scroll.y = selectedRect.y;
                    Repaint();
                }
            }
        }

        private List<Element> GetChildren(Element[] tree, Element parent)
        {
            List<Element> children = new List<Element>();
            int level = -1;
            int i = 0;
            for (i = 0; i < tree.Length; i++)
            {
                if (tree[i] == parent)
                {
                    level = parent.level + 1;
                    i++;
                    break;
                }
            }
            if (level == -1)
                return children;

            for (; i < tree.Length; i++)
            {
                Element e = tree[i];

                if (e.level < level)
                    break;
                if (e.level > level && !hasSearch)
                    continue;

                children.Add(e);
            }

            return children;
        }
    }
}
