// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.EditorTools;
using UnityEditor.TerrainTools;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using UnityEngine.PlayerLoop;


namespace UnityEditor.TerrainTools
{

    class TerrainToolbarOverlayPostProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (didDomainReload)
            {
                if (TerrainTransientToolbarOverlay.s_TerrainTransientToolbarOverlay != null)
                {
                    TerrainTransientToolbarOverlay.s_TerrainToolbarOverlay = new TerrainTransientToolbar();
                    TerrainTransientToolbarOverlay.s_TerrainTransientToolbarOverlay.UpdateCollapsedIcon(); // subscribe to action once more
                    TerrainTransientToolbarOverlay.s_TerrainTransientToolbarOverlay.RebuildContent();
                }
            }
        }
    }

    [Overlay(typeof(SceneView), "Terrain Tools")]
    internal class TerrainTransientToolbarOverlay : ToolbarOverlay, ITransientOverlay, ICreateHorizontalToolbar, ICreateVerticalToolbar
    {
        bool m_OverlaysPackageInstalled;
        
        // See also
        // - SceneViewToolbars
        public TerrainTransientToolbarOverlay() : base("TerrainTransientToolbar")
        {
            // default collapsed icon
            collapsedIcon = EditorGUIUtility.LoadIcon("TerrainOverlays/ToolModeIcons/SculptMode_On.png");
            CreateToolbarIfNull();
            s_TerrainTransientToolbarOverlay = this;
            m_OverlaysPackageInstalled = IsOverlaysPackageVersionInstalled();
        }

        private void CreateToolbarIfNull()
        {
            if (s_TerrainToolbarOverlay == null)
            {
                s_TerrainToolbarOverlay = new TerrainTransientToolbar();
                UpdateCollapsedIcon();
            }
        }

        internal static TerrainTransientToolbarOverlay s_TerrainTransientToolbarOverlay;

        // determines whether the toolbar should be visible or not
        public bool visible => m_OverlaysPackageInstalled && TerrainInspector.s_activeTerrainInspectorInstance != null && BrushesOverlay.IsSelectedObjectTerrain();

        internal static TerrainTransientToolbar s_TerrainToolbarOverlay;
        private string m_PackageVersion = string.Empty;
        private bool m_PackageInstalled;

        internal void UpdateCollapsedIcon()
        {
            s_TerrainToolbarOverlay.CategoryChangeAction += () =>
            {
                // change the icon
                string iconPath = s_TerrainToolbarOverlay.CurrentTerrainCategory;
                collapsedIcon = EditorGUIUtility.LoadIcon(iconPath);
            };
        }

        // is the CORRECT overlays package version installed
        private bool IsOverlaysPackageVersionInstalled()
        {
            var upm = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            var terrainPackageInfo = upm.Where(pi => pi.name == "com.unity.terrain-tools").ToArray();

            Debug.Assert(terrainPackageInfo.Length <= 1, "Only one version of terrain-tools package allowed to be installed");

            if (terrainPackageInfo.Length == 0)
            {
                m_PackageInstalled = false;
                m_PackageVersion = "";
                return true; // no package installed, true bc should display core tools with overlays
            }

            var version = terrainPackageInfo[0].version;

            Debug.Assert(!string.IsNullOrEmpty(version), "Package version cannot be empty");

            // this wraps parsing logic for terrain-tools versioning scheme
            bool ParseTerrainToolsVersion(string versionString, int d0, int d1, int d2)
            {
                // scheme is prefix-major.minor.patch-postfix.version
                // where prefix is letters, major, minor, patch are numbers,
                // postfix is letters, and postfix.version is a number

                // use regex to get the major, minor, patch components of the string
                var versionDigits = Regex.Match(versionString, @"\d+\.\d+\.\d+");

                if (!versionDigits.Success) return false;
                var parts = versionDigits.Value.Split(".");
                if (parts.Length != 3) return false;

                if (!Int32.TryParse(parts[0], out var major)) return false;
                if (!Int32.TryParse(parts[1], out var minor)) return false;
                if (!Int32.TryParse(parts[2], out var patch)) return false;

                // d0, d1, d2 are the specified _minimum version_. Logic here is to produce
                // a >=0 result for any version greater than d0.d1.d2
                return Math.Sign(major - d0) * 100 + Math.Sign(minor - d1) * 10 + Math.Sign(patch - d2) >= 0;
            }

            if (string.IsNullOrEmpty(version))
            {
                m_PackageInstalled = false;
                m_PackageVersion = "";
                return m_PackageInstalled;
            }

            // only parse if version is different than what we have cached -- otherwise
            // just return the result from the last parse
            if (version.Equals(m_PackageVersion)) return m_PackageInstalled;

            m_PackageInstalled = ParseTerrainToolsVersion(version, 5, 1, 0);
            m_PackageVersion = version;

            return m_PackageInstalled;
        }

        public override VisualElement CreatePanelContent()
        {
            return s_TerrainToolbarOverlay;
        }

        OverlayToolbar ICreateHorizontalToolbar.CreateHorizontalToolbarContent()
        {
            return s_TerrainToolbarOverlay;
        }

        OverlayToolbar ICreateVerticalToolbar.CreateVerticalToolbarContent()
        {
            return s_TerrainToolbarOverlay;
        }

        public static void SetEditorToolActive(ITerrainPaintToolWithOverlays editorPaintTool)
        {
            if (s_TerrainToolbarOverlay == null) return; // avoid setting null tool when adding package
            if (!BrushesOverlay.IsSelectedObjectTerrain()) return; //avoid setting tools when selected object IS NOT terrain
            s_TerrainToolbarOverlay.SetToolActive(editorPaintTool);
        }
    }

    [EditorToolbarElement("TerrainTransientToolbar", typeof(SceneView))]
    class TerrainTransientToolbar : OverlayToolbar
    {
        VisualElement m_DefaultToolsVE;
        VisualElement m_MenuButtonsVE;
        VisualElement m_SeparatorVE;

        private Dictionary<TerrainCategory, List<ITerrainPaintToolWithOverlays>> m_CategoryToTools =
            new ();

        // the last active tool is given by m_CategoryToLastUsedTool[category]
        Dictionary<TerrainCategory, EditorTool> m_CategoryToLastUsedTool = new ();

        Dictionary<ITerrainPaintToolWithOverlays, Type> m_ToolToTypeDict;
        List<string> m_ToolNames = new ();
        Dictionary<EditorToolbarToggle, ITerrainPaintToolWithOverlays> m_ButtonToTool =
            new ();
        Dictionary<ITerrainPaintToolWithOverlays, EditorToolbarToggle> m_ToolToButton = new ();

        void StoreToolsInDictionary()
        {
            foreach (var category in Enum.GetValues(typeof(TerrainCategory)).Cast<TerrainCategory>())
            {
                m_CategoryToTools[category] =
                    new List<ITerrainPaintToolWithOverlays>();
            }
        }

        internal void SetToolActive(ITerrainPaintToolWithOverlays editorPaintTool)
        {
            // keep track of the last selected tool in each category
            m_CategoryToLastUsedTool[editorPaintTool.Category] = (EditorTool) editorPaintTool;

            if ((int) editorPaintTool.Category != m_CurrIndex)
            {
                // change category and load tools if necessary
                m_CurrIndex = (int) editorPaintTool.Category;
                LoadTool(editorPaintTool.Category);
                OnCategoryChangeAction(); // invoke the action for changing the category
            }

            // activating the tool after LoadTool() call so its in the dictionary
            EditorToolbarToggle editorPaintToolButton = m_ToolToButton[editorPaintTool];
            editorPaintToolButton.SetValueWithoutNotify(true); // set the paint tool button active

            UpdateState();
            UpdateMenu();
        }

        private void GetPaintTools()
        {
            m_ToolToTypeDict = new Dictionary<ITerrainPaintToolWithOverlays, Type>();

            foreach (var klass in TypeCache.GetTypesDerivedFrom(typeof(ITerrainPaintToolWithOverlays)))
            {
                if (klass.IsAbstract) continue;

                // load all tools. eventually we should only be loading when Terrain is selected and not loading tools that are overriden
                var tool = (ITerrainPaintToolWithOverlays)EditorToolManager.GetSingleton(klass);

                // add tool to proper list
                m_CategoryToTools[tool.Category].Add(tool);

                // add the corresponding type
                m_ToolToTypeDict[tool] = klass;
                m_ToolNames.Add(tool.GetName());
            }
        }

        private void KeepOverrides()
        {
            // if duplicate names, keep only the overrides and NOT the built-in tool
            foreach (var category in Enum.GetValues(typeof(TerrainCategory)).Cast<TerrainCategory>())
            {
                m_CategoryToTools[category] =
                    KeepOverrides(m_CategoryToTools[category]);
            }
        }

        private void SetLastActiveToolToDefaultTool()
        {
            // by default, set the last active sculptTool to the first in the list
            foreach (var category in Enum.GetValues(typeof(TerrainCategory)).Cast<TerrainCategory>())
            {
                m_CategoryToLastUsedTool[category] = (EditorTool)m_CategoryToTools[category].FirstOrDefault() ??
                                                     EditorToolManager.GetSingleton<NoneTool>();
            }
        }

        private List<ITerrainPaintToolWithOverlays> KeepOverrides(List<ITerrainPaintToolWithOverlays> list)
        {
            List<ITerrainPaintToolWithOverlays> noRepeats = new List<ITerrainPaintToolWithOverlays>();
            foreach (var tool in list)
            {
                // if the tool occurs more than once in the tools name list
                if (m_ToolNames.Count(t => t == tool.GetName()) > 1)
                {
                    var klass = m_ToolToTypeDict[tool];
                    //if this tool is a builtin tool
                    if (klass.Assembly.GetCustomAttributes(typeof(AssemblyIsEditorAssembly), false).Any())
                        continue;
                    noRepeats.Add(tool); // if not a builtin tool, then add
                }
                else
                {
                    // else the tool only occurs once
                    noRepeats.Add(tool);
                }
            }

            return noRepeats;
        }

        private void LoadTool(TerrainCategory category)
        {

            LoadTool(m_CategoryToTools[category], category);
            if (m_CategoryToLastUsedTool[category] is NoneTool) return; // check for an empty category, most likely custom tools
            // set the tool active in inspector if it is a paint tool (this is because the paint tools are all in the same drop down)
            // materials tools, custom tools, and sculpt tools = paint tools
            // however, do not check for specific categories because people may put custom tools in whichever category they wish
            SetToolActiveInInspector((ITerrainPaintToolWithOverlays)m_CategoryToLastUsedTool[category]);
            if (!BrushesOverlay.IsSelectedObjectTerrain()) return;
            ToolManager.SetActiveTool(m_CategoryToLastUsedTool[category]);
        }

        private void SetToolActiveInInspector(ITerrainPaintToolWithOverlays tool)
        {
            // for updating the terrain inspector in OnInspectorGUI, need to update the active paint tool's index
            if (TerrainInspector.s_activeTerrainInspectorInstance)
            {
                if (TerrainInspector.IsPaintTool(tool.GetName()))
                {
                    TerrainInspector.s_activeTerrainInspectorInstance.m_ActivePaintToolIndex =
                        TerrainInspector.s_activeTerrainInspectorInstance.GetPaintToolIndex(tool.GetName());
                }
                TerrainInspector.s_ActiveTerrainToolIsEditorTool = true;
            }
        }

        private void LoadTool(List<ITerrainPaintToolWithOverlays> list, TerrainCategory category)
        {
            m_DefaultToolsVE.Clear();
            foreach (var tool in list)
            {
                // for editor toolbartoggle
                EditorToolbarToggle button = new EditorToolbarToggle
                {
                    tooltip = tool.GetName(),
                    onIcon = EditorGUIUtility.LoadIcon(tool.OnIcon),
                    offIcon = EditorGUIUtility.LoadIcon(tool.OffIcon)
                };

                button.RegisterValueChangedCallback((evt) =>
                {
                    if (evt.newValue)
                    {
                        ToolManager.SetActiveTool(m_ToolToTypeDict[tool]);
                        SetToolActiveInInspector(tool);

                        // keep track of the last selected tool in each category
                        m_CategoryToLastUsedTool[category] = (EditorTool) tool;
                    }

                    // Keep the toggle checked if target is still the current tool
                    if (EditorToolManager.GetActiveTool() == (EditorTool)tool)
                        button.SetValueWithoutNotify(true);

                    UpdateState();
                });

                button.RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
                button.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

                // make the buttons have a darker background color using USS
                button.name = "terrain_tool_button";
                EditorToolbarUtility.LoadStyleSheets("TerrainToolbar", button);

                m_ButtonToTool[button] = tool;
                m_ToolToButton[tool] = button;
                m_DefaultToolsVE.Add(button);
            }

            EditorToolbarUtility.SetupChildrenAsButtonStrip(m_DefaultToolsVE);
        }

        // ----- editorToolbarToggle button functions -----
        void UpdateState()
        {
            //item.Key is the button, item.Value is the tool
            foreach (var item in m_ButtonToTool)
            {
                // if that tool is not the active tool, then change the toggle
                item.Key.SetValueWithoutNotify(EditorToolManager.GetActiveTool() == (EditorTool)item.Value);
            }
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            ToolManager.activeToolChanged += UpdateState;
            ToolManager.activeContextChanged += UpdateState;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ToolManager.activeContextChanged -= UpdateState;
            ToolManager.activeToolChanged -= UpdateState;
        }

        struct MenuItem
        {
            public readonly string OnIcon;
            public readonly string OffIcon;
            public readonly string ToolTip;

            public MenuItem(string _OnIcon, string _OffIcon, string _ToolTip)
            {
                OnIcon = _OnIcon;
                OffIcon = _OffIcon;
                ToolTip = _ToolTip;
            }
        }

        static MenuItem s_SculptMode = new MenuItem("TerrainOverlays/ToolModeIcons/SculptMode_On.png", "TerrainOverlays/ToolModeIcons/SculptMode.png", "Sculpt Mode");
        static MenuItem s_MaterialMode = new MenuItem("TerrainOverlays/ToolModeIcons/MaterialsMode_On.png", "TerrainOverlays/ToolModeIcons/MaterialsMode.png", "Materials Mode");
        static MenuItem s_FoliageMode = new MenuItem("TerrainOverlays/ToolModeIcons/FoliageMode_On.png", "TerrainOverlays/ToolModeIcons/FoliageMode.png", "Foliage Mode");
        static MenuItem s_NeighborMode = new MenuItem("TerrainOverlays/ToolModeIcons/NeighborTerrainsMode_On.png", "TerrainOverlays/ToolModeIcons/NeighborTerrainsMode.png", "Neighbor Terrains Mode");
        static MenuItem s_CustomBrushesMode = new MenuItem("TerrainOverlays/ToolModeIcons/CustomBrushesMode_On.png", "TerrainOverlays/ToolModeIcons/CustomBrushesMode.png", "Custom Brushes Mode");

        private static Texture2D s_SeparatorIcon =
            EditorGUIUtility.LoadIcon("TerrainOverlays/SeparatorDot.png");

        static MenuItem[] s_ModeMenu = { s_SculptMode, s_MaterialMode, s_FoliageMode, s_NeighborMode, s_CustomBrushesMode };

        int m_CurrIndex = 0;
        List<EditorToolbarToggle> m_MenuButtons = new List<EditorToolbarToggle>();

        void UpdateMenu()
        {
            int i = 0;
            foreach (var menuButton in m_MenuButtons)
            {
                // the only menu button that should be active is the one at the currentIndex
                menuButton.SetValueWithoutNotify(i == m_CurrIndex);
                i++;
            }
        }

        void CreateMainMenu()
        {
            m_MenuButtonsVE = new VisualElement();
            m_MenuButtonsVE.AddToClassList("toolbar-contents");

            // going through all the icons in the menu
            for (int i = 0; i < s_ModeMenu.Length; i++)
            {
                // check if the menu mode tools are empty or not
                if (m_CategoryToTools[(TerrainCategory)i].Count == 0)
                {
                    continue;
                }

                EditorToolbarToggle button = new EditorToolbarToggle
                {
                    tooltip = s_ModeMenu[i].ToolTip,
                    onIcon = EditorGUIUtility.LoadIcon(s_ModeMenu[i].OnIcon),
                    offIcon = EditorGUIUtility.LoadIcon(s_ModeMenu[i].OffIcon)
                };

                int indexCopy = i;
                button.RegisterValueChangedCallback((evt) =>
                {
                    m_CurrIndex = indexCopy;
                    if (evt.newValue)
                    {
                        m_DefaultToolsVE.Clear();
                        ToolManager.SetActiveTool<NoneTool>(); // the tool deselects
                        LoadTool((TerrainCategory) indexCopy);
                    }

                    UpdateMenu();

                    // action called when terrain category is changed
                    OnCategoryChangeAction();

                });
                m_MenuButtons.Add(button);
                m_MenuButtonsVE.Add(button);
            }
            EditorToolbarUtility.SetupChildrenAsButtonStrip(m_MenuButtonsVE);
        }

        void CreateSeparator()
        {
            m_SeparatorVE = new VisualElement();
            EditorToolbarUtility.LoadStyleSheets("TerrainToolbar", m_SeparatorVE);
            m_SeparatorVE.AddToClassList("toolbar-contents");
            m_SeparatorVE.AddToClassList("unity-editor-toolbar-element__separator");
            m_SeparatorVE.style.backgroundImage = s_SeparatorIcon;
        }

        // invoke an action when changing the menu category on the terrain toolbar
        public string CurrentTerrainCategory = s_ModeMenu[0].OnIcon;
        public Action CategoryChangeAction;
        void OnCategoryChangeAction()
        {
            CurrentTerrainCategory = s_ModeMenu[m_CurrIndex].OnIcon;
            CategoryChangeAction?.Invoke();
        }

        public TerrainTransientToolbar()
        {
            m_DefaultToolsVE = new VisualElement();
            m_DefaultToolsVE.AddToClassList("toolbar-contents");

            // store the tool lists in the dictionary
            StoreToolsInDictionary();

            // loop through all the terrain tools and store them in TerrainTool lists
            GetPaintTools();

            // sort each list by terrainTool.GetIconIndex()
            foreach (var category in Enum.GetValues(typeof(TerrainCategory)).Cast<TerrainCategory>())
            {
                m_CategoryToTools[category] =
                    m_CategoryToTools[category].OrderBy(o => o.IconIndex).ToList();
            }

            // cull the repeats if exists (keep the appropriate overrides)
            KeepOverrides();

            // by default, set the last active tool to be the first in the list
            SetLastActiveToolToDefaultTool();

            CreateMainMenu();

            CreateSeparator();

            UpdateMenu(); // make the first button activate

            LoadTool(TerrainCategory.Sculpt);

            UpdateState(); // this should set the default first buttons to be active (in this case, the first tool in sculptTools)

            Add(m_MenuButtonsVE);
            Add(m_SeparatorVE);
            Add(m_DefaultToolsVE);

        }
    }
}
