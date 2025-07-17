// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Scripting;
using UnityEditorInternal;
using UnityEditor.Overlays;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using System;
using System.Reflection;
using System.IO;

namespace UnityEditor
{
    sealed partial class MainToolbarWindow : EditorWindow, ISupportsOverlaysCustomMode
    {
        const string k_MainToolbarUSSClassName = "unity-editor-main-toolbar";

        internal static MainToolbarWindow instance;

        OverlayCanvasMode ISupportsOverlaysCustomMode.overlayCanvasMode => OverlayCanvasMode.MainToolbar;

        string[] m_UniqueMenuCategories;

        MainToolbarWindow()
        {
            instance = this;
        }

        string[] GetAllUniquePaths()
        {
            HashSet<string> uniquePaths = new HashSet<string>();
            foreach (var def in MainToolbar.GetAllElementDefinitions())
            {
                var path = Path.GetDirectoryName(def.attr.path);
                while (!string.IsNullOrEmpty(path))
                {
                    uniquePaths.Add(path);
                    path = Path.GetDirectoryName(path);
                }
            }

            string[] results = new string[uniquePaths.Count];
            int count = 0;
            foreach (var path in uniquePaths)
            {
                results[count] = path;
                ++count;
            }

            return results;
        }

        void OnEnable()
        {
            m_Parent = Toolbar.instance;

            m_UniqueMenuCategories = GetAllUniquePaths();

            overlayCanvas.rootVisualElement.AddToClassList(k_MainToolbarUSSClassName);

            UIElementsEditorUtility.AddDefaultEditorStyleSheets(rootVisualElement);
            EditorToolbarUtility.LoadStyleSheets("MainToolbar", overlayCanvas.rootVisualElement);

            rootVisualElement.style.unityEditorTextRenderingMode = new StyleEnum<EditorTextRenderingMode>(EditorTextSettings.GetEditorTextRenderingMode());
            rootVisualElement.style.unityTextGenerator = new StyleEnum<TextGeneratorType>(EditorTextSettings.GetEditorTextGeneratorType());
        }

        void CreateGUI()
        {
            overlayCanvas.rootVisualElement.RegisterCallback<ContextClickEvent>((evt) => ShowMenu(evt.mousePosition, overlayCanvas));
        }

        void ShowMenu(Vector2 position, OverlayCanvas canvas)
        {
            var dropdown = DropdownUtility.CreateDropdown();

            var overlays = new List<(Overlay overlay, MainToolbarElementAttribute attrib)>();
            var windowType = typeof(MainToolbarWindow);

            foreach (var overlay in canvas.overlays)
            {
                var mto = overlay as MainToolbarOverlay;
                overlays.Add((overlay, mto.createElementMethod.GetCustomAttribute<MainToolbarElementAttribute>()));
            }

            overlays.Sort((a, b) => ((int)a.attrib.defaultDockPosition * 100 + a.attrib.defaultDockIndex)
                .CompareTo((int)b.attrib.defaultDockPosition * 100 + b.attrib.defaultDockIndex));

            foreach (var pair in overlays)
            {
                if (pair.attrib.path != Toolbar.deprecatedElementsId || Toolbar.instance.deprecatedElements.Count > 0)
                {
                    dropdown.AddItem(pair.attrib.path, pair.overlay.displayed, () =>
                    {
                        pair.overlay.displayed = !pair.overlay.displayed;
                    });
                }
            }

            // Add Show/Hide All to each unique category
            foreach (var path in m_UniqueMenuCategories)
            {
                dropdown.AddSeparator($"{path}/");
                dropdown.AddItem($"{path}/Show All", false, () => MainToolbar.ShowAll(path));
                dropdown.AddItem($"{path}/Hide All", false, () => MainToolbar.HideAll(path));
            }

            dropdown.AddSeparator("");

            OverlayPresetManager.GenerateMenu(dropdown, "Presets/", this, new DefaultOverlayPreset(), new UnityOnlyToolbarPreset());

            dropdown.DropDown(new Rect(position, Vector2.zero), rootVisualElement);
        }
    }

    partial class Toolbar : HostView
    {
        static Toolbar s_Instance;
        public const float ToolbarHeight = 36f;

        internal static Toolbar instance => s_Instance;

        Toolbar()
        {
            s_Instance = this;
#pragma warning disable CS0618 // Type or member is obsolete
            get = s_Instance;
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_EventInterests.wantsLessLayoutEvents = true;

            if (actualView is not MainToolbarWindow)
                SetActualViewInternal(CreateInstance<MainToolbarWindow>(), false);

            InitializeFakeHierarchyForDeprecatedToolbarHacks();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        public float CalcHeight()
        {
            return ToolbarHeight;
        }

        [RequiredByNativeCode]
        internal static void RepaintToolbar()
        {
            if (instance != null)
                instance.Repaint();
        }

        // Repaints all views, called from C++ when playmode entering is aborted
        // and when the user clicks on the playmode button.
        [RequiredByNativeCode]
        static void InternalWillTogglePlaymode()
        {
            InternalEditorUtility.RepaintAllViews();
        }

        // TODO remove the following code, SubToolbar.cs and SubToolbarZone.cs when collab has stopped using it
        static List<SubToolbar> s_SubToolbars = new List<SubToolbar>();
        internal static IEnumerable<SubToolbar> subToolbars => s_SubToolbars;

        [Obsolete("Use MainToolbarElementAttribute Instead")]
        internal static void AddSubToolbar(SubToolbar subToolbar)
        {
            s_SubToolbars.Add(subToolbar);
        }

        VisualElement m_Root;
        internal const string deprecatedElementsId = "Unsupported User Elements";
        [Obsolete($"Use {nameof(instance)} instead")]
        internal static Toolbar get;
        List<VisualElement> m_DeprecatedElements = new List<VisualElement>();
        internal IReadOnlyList<VisualElement> deprecatedElements => m_DeprecatedElements;
        internal static event Action<MainToolbarDockPosition, VisualElement> populateFakeToolbar;

        void InitializeFakeHierarchyForDeprecatedToolbarHacks()
        {
            const string k_MainToolbarUSSClassName = "unity-editor-main-toolbar";
            var uxml = EditorToolbarUtility.LoadUxml("MainToolbar");

            var name = VisualElement.k_RootVisualContainerName;
            m_Root = new VisualElement()
            {
                name = VisualElementUtils.GetUniqueName(name),
                pickingMode = PickingMode.Ignore, // do not eat events so IMGUI gets them
                viewDataKey = name,
                renderHints = RenderHints.ClipWithScissors
            };
            m_Root.pseudoStates |= PseudoStates.Root;
            m_Root.AddToClassList(k_MainToolbarUSSClassName);

            var ve = new VisualElement();
            uxml.CloneTree(ve);
            m_Root.Add(ve);

            var leftZone = m_Root.Q("ToolbarZoneLeftAlign");
            populateFakeToolbar?.Invoke(MainToolbarDockPosition.Left, leftZone);
            CheckIfElementAddedToFakeToolbar(leftZone);

            var middleZone = m_Root.Q("ToolbarZonePlayMode");
            populateFakeToolbar?.Invoke(MainToolbarDockPosition.Middle, middleZone);
            CheckIfElementAddedToFakeToolbar(middleZone);

            var rightZone = m_Root.Q("ToolbarZoneRightAlign");
            populateFakeToolbar?.Invoke(MainToolbarDockPosition.Right, rightZone);
            CheckIfElementAddedToFakeToolbar(rightZone);
        }

        void CheckIfElementAddedToFakeToolbar(VisualElement element)
        {
            element.elementAdded += (ve, index) =>
            {
                Debug.LogWarning($"We have detected that your project includes custom elements added to the Unity Editor's main toolbar using unsupported methods. This approach is not supported and will lead to issues in future versions. Refer to the official <a href=\"https://docs.unity3d.com/ScriptingReference/Toolbars.MainToolbar.html\">API documentation</a> for adding custom elements to the main toolbar.\n\nYour custom toolbar elements can be unhidden via the context menu (right-click the main toolbar -> <i>Unsupported User Elements</i>).");
                m_DeprecatedElements.Add(ve);
                MainToolbar.Refresh(deprecatedElementsId);
            };
        }
    }
}
