// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;
using Unity.Scripting.LifecycleManagement;

[assembly: InternalsVisibleTo("Unity.Burst.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.Burst.Tester.Editor.Tests")]
[assembly: InternalsVisibleTo("UnityEditor.Modules.Burst.EditModeTests")]

namespace Unity.Burst.Editor
{
    internal partial class BurstInspectorGUI : EditorWindow
    {
        [AutoStaticsCleanupOnCodeReload]
        private static bool Initialized;

        private static void EnsureInitialized()
        {
            if (Initialized)
            {
                return;
            }

            Initialized = true;

            BurstLoader.OnBurstShutdown += () =>
            {
                if (EditorWindow.HasOpenInstances<BurstInspectorGUI>())
                {
                    var window = EditorWindow.GetWindow<BurstInspectorGUI>("Burst Inspector");
                    window.Close();
                }
            };
        }

        private const string FontSizeIndexPref = "BurstInspectorFontSizeIndex";

        private static readonly GUIContent[] DisassemblyKindContents =
        {
            new GUIContent("Assembly", "The final optimized native code that Burst generated."),
            new GUIContent(".NET IL", "The original .NET IL extracted from the job method."),
            new GUIContent("LLVM IR (Unoptimized)", "The internal LLVM IR before optimizations."),
            new GUIContent("LLVM IR (Optimized)", "The internal LLVM IR after optimizations."),
            new GUIContent("LLVM IR Optimization Diagnostics", "LLVM diagnostics of the optimizations, such as whether they succeeded or failed.")
        };

        private static readonly string[] AssemblyOptionNames =
        {
            "Plain Without Debug Information",
            "Plain With Debug Information",
            "Enhanced With Minimal Debug Information",
            "Enhanced With Full Debug Information",
            "Coloured With Minimal Debug Information",
            "Coloured With Full Debug Information"
        };

        internal enum AssemblyOptions
        {
            PlainWithoutDebugInformation = 0,
            PlainWithDebugInformation = 1,
            EnhancedWithMinimalDebugInformation = 2,
            EnhancedWithFullDebugInformation = 3,
            ColouredWithMinimalDebugInformation = 4,
            ColouredWithFullDebugInformation = 5
        }
        internal AssemblyOptions? _assemblyKind = null;
        private AssemblyOptions? _assemblyKindPrior = null;
        private AssemblyOptions _oldAssemblyKind;

        private bool SupportsEnhancedRendering => _disasmKind == DisassemblyKind.Asm || _disasmKind == DisassemblyKind.OptimizedIR || _disasmKind == DisassemblyKind.UnoptimizedIR;

        [NoAutoStaticsCleanup] // lazily rebuilt on demand by GetDisasmOptions() when null; not dependent on reload-sensitive state
        private static string[] DisasmOptions;

        internal static string[] GetDisasmOptions()
        {
            if (DisasmOptions == null)
            {
                // We can't initialize this in BurstInspectorGUI.cctor because BurstCompilerOptions may not yet
                // have been initialized by BurstLoader. So we initialize on-demand here. This method doesn't need to
                // be thread-safe because it's only called from the UI thread.
                DisasmOptions = new[]
                {
                    "\n" + BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionDump, NativeDumpFlags.Asm),
                    "\n" + BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionDump, NativeDumpFlags.IL),
                    "\n" + BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionDump, NativeDumpFlags.IR),
                    "\n" + BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionDump, NativeDumpFlags.IROptimized),
                    "\n" + BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionDump, NativeDumpFlags.IRPassAnalysis)
                };
            }
            return DisasmOptions;
        }

        private static readonly string[] TargetCpuNames = Enum.GetNames(typeof(BurstTargetCpu));

        private static readonly int[] FontSizes =
        {
            8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 20
        };

        internal const int _scrollbarThickness = 14;

        internal float _buttonOverlapInspectorView = 0;

        [NonSerialized]
        internal readonly BurstDisassembler _burstDisassembler;

        private const string BurstSettingText = "Inspector Settings/";

        [SerializeField] private BurstTargetCpu _targetCpu = BurstTargetCpu.Auto;

        [SerializeField] internal DisassemblyKind _disasmKind = DisassemblyKind.Asm;
        [SerializeField] private DisassemblyKind _oldDisasmKind = DisassemblyKind.Asm;

        [NonSerialized]
        internal GUIStyle fixedFontStyle;

        [NonSerialized]
        internal int fontSizeIndex = -1;

        [SerializeField] private int _previousTargetIndex = -1;

        [SerializeField] private bool _safetyChecks = false;
        [SerializeField] private bool _showBranchMarkers = true;
        [SerializeField] private bool _enhancedDisassembly = true;
        [SerializeField] private string _searchFilterJobs;
        [SerializeField] private bool _showUnityNamespaceJobs = false;
        [SerializeField] private bool _showDOTSGeneratedJobs = false;
        [SerializeField] private bool _focusTargetJob = true;
        [SerializeField] private string _searchFilterAssembly = String.Empty;

        [SerializeField] private bool _sameTargetButDifferentAssemblyKind = false;
        [SerializeField] internal Vector2 _scrollPos;
        internal SearchField _searchFieldJobs;
        internal SearchField _searchFieldAssembly;
        private bool saveSearchFieldFromEvent = false;

        [SerializeField] private string _selectedItem;

        [NonSerialized]
        private BurstCompileTarget _target;
        [NonSerialized]
        private BurstCompileTarget[] _targets;
        // Used as a serialized representation of _targets:
        [SerializeField] private List<string> targetNames;

        [NonSerialized]
        internal LongTextArea _textArea;

        internal Rect _inspectorView;

        [NonSerialized]
        internal Font _font;

        [NonSerialized]
        internal BurstMethodTreeView _treeView;
        // Serialized representation of _treeView:
        [SerializeField] private TreeViewState treeViewState;

        [NonSerialized]
        internal bool _initialized;

        [NonSerialized]
        private VisualElement _loadingView;
        [NonSerialized]
        private TwoPaneSplitView _splitView;
        [NonSerialized]
        internal TabView _disasmKindTabs;
        [NonSerialized]
        private IMGUIContainer _jobsPane;
        [NonSerialized]
        private IMGUIContainer _assemblyPane;

        private const string StyleSheetPath = "Burst/StyleSheets/BurstInspector.uss";
        private const string OutputTabsUssClassName = "burst-inspector__output-tabs";
        private const float JobsPaneMinWidth = 155f;
        private const float OutputPaneMinWidth = 128f;

        [NonSerialized]
        private bool _requiresRepaint;

        [NonSerialized]
        private Task<BurstReflection.FindExecuteMethodsResult> _loadTargetsTask;

        [NonSerialized]
        private bool _newTreeState;

        private int FontSize => FontSizes[fontSizeIndex];

        private static readonly Regex _rx = new Regex(@"^.*\(\d+,\d+\):\sBurst\serror");

        private bool _leftClicked = false;

        [SerializeField] private bool _isCompileError = false;
        [SerializeField] private bool _prevWasCompileError;

        [SerializeField] private bool _smellTest = false;

        // Caching GUIContent for the toolbars
        private readonly GUIContent _contentShowUnityNamespaceJobs = new GUIContent("Show Unity Namespace");
        private readonly GUIContent _contentShowDOTSGeneratedJobs = new GUIContent("Show \".Generated\"");
        private readonly GUIContent _contentJobsFilter = new GUIContent("Filter", "Choose which compile targets to list.");
        private readonly GUIContent _contentSafetyChecks = new GUIContent("Safety Checks", "Generate code that includes container access safety checks.");
        private readonly GUIContent _contentView = new GUIContent("View", "Font size and display options for the output.");
        private readonly GUIContent _contentCollapseToCode = new GUIContent("Focus on Code", "Collapse directives and data blocks.");
        private readonly GUIContent _contentExpandAll = new GUIContent("Expand All", "Expand all collapsed blocks.");
        private readonly GUIContent _contentShowBranchFlow = new GUIContent("Show Branch Flow");
        private readonly GUIContent _contentHighlightSIMD = new GUIContent("Highlight SIMD Scalar vs Packed");

        // content for search bar
        private readonly GUIContent _ignoreCase = new GUIContent("Match Case");
        private readonly GUIContent _matchWord = new GUIContent("Whole Words");
        private readonly GUIContent _regexSearch = new GUIContent("Regex");
        private GUIContent _previousSearchHit;
        private GUIContent _nextSearchHit;
        private GUIStyle _searchHitCountStyle;

        private readonly GUIContent _contentNoFunction = new GUIContent("Go to Function", "Jump to a function in the disassembly.");
        private readonly GUIContent _contentFunctionsUnavailable = new GUIContent("Go to Function", "Function navigation is only available for Enhanced or Coloured Assembly output.");
        private const string FunctionLabelPrefix = "Function: ";
        private const float MinFunctionListWidth = 400f;

        [NonSerialized]
        private int _requestedFontSizeIndex = -1;
        [NonSerialized]
        private bool? _requestedSmellTest;
        [NonSerialized]
        private DisassemblyKind? _requestedDisasmKind;

        // Number of lines kept above a function when jumping to it.
        private const int FunctionJumpMarginLines = 2;

        [NonSerialized]
        private AdvancedDropdownState _functionDropdownState;
        [NonSerialized]
        internal int _pendingFunctionJump = -1;
        [NonSerialized]
        internal int _pendingSearchHitStep;


        private enum KeyboardOperation
        {
            SelectAll,
            Copy,
            MoveLeft,
            MoveRight,
            MoveUp,
            MoveDown,
            Search,
            Enter,
            FindNext,
            FindPrevious,
        }

        private Dictionary<Event, KeyboardOperation> _keyboardEvents;

        private void FillKeyboardEvent()
        {
            if (_keyboardEvents != null)
            {
                return;
            }

            _keyboardEvents = new Dictionary<Event, KeyboardOperation>();

            _keyboardEvents.Add(Event.KeyboardEvent("#left"), KeyboardOperation.MoveLeft);
            _keyboardEvents.Add(Event.KeyboardEvent("#right"), KeyboardOperation.MoveRight);
            _keyboardEvents.Add(Event.KeyboardEvent("#down"), KeyboardOperation.MoveDown);
            _keyboardEvents.Add(Event.KeyboardEvent("#up"), KeyboardOperation.MoveUp);
            _keyboardEvents.Add(Event.KeyboardEvent("return"), KeyboardOperation.Enter);
            _keyboardEvents.Add(Event.KeyboardEvent("#return"), KeyboardOperation.Enter);

            _keyboardEvents.Add(Event.KeyboardEvent("left"), KeyboardOperation.MoveLeft);
            _keyboardEvents.Add(Event.KeyboardEvent("right"), KeyboardOperation.MoveRight);
            _keyboardEvents.Add(Event.KeyboardEvent("up"), KeyboardOperation.MoveUp);
            _keyboardEvents.Add(Event.KeyboardEvent("down"), KeyboardOperation.MoveDown);

            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            {
                _keyboardEvents.Add(Event.KeyboardEvent("%a"), KeyboardOperation.SelectAll);
                _keyboardEvents.Add(Event.KeyboardEvent("%c"), KeyboardOperation.Copy);
                _keyboardEvents.Add(Event.KeyboardEvent("%f"), KeyboardOperation.Search);
                _keyboardEvents.Add(Event.KeyboardEvent("%g"), KeyboardOperation.FindNext);
                _keyboardEvents.Add(Event.KeyboardEvent("%#g"), KeyboardOperation.FindPrevious);
            }
            else
            {
                // windows or linux bindings.
                _keyboardEvents.Add(Event.KeyboardEvent("^a"), KeyboardOperation.SelectAll);
                _keyboardEvents.Add(Event.KeyboardEvent("^c"), KeyboardOperation.Copy);
                _keyboardEvents.Add(Event.KeyboardEvent("^f"), KeyboardOperation.Search);
            }

            _keyboardEvents.Add(Event.KeyboardEvent("f3"), KeyboardOperation.FindNext);
            _keyboardEvents.Add(Event.KeyboardEvent("#f3"), KeyboardOperation.FindPrevious);
        }

        public BurstInspectorGUI()
        {
            _burstDisassembler = new BurstDisassembler();
        }

        private bool DisplayAssemblyKind(Enum assemblyKind)
        {
            var assemblyOption = (AssemblyOptions)assemblyKind;
            if (_disasmKind != DisassemblyKind.Asm || _isCompileError)
            {
                return assemblyOption == AssemblyOptions.PlainWithoutDebugInformation;
            }
            return true;
        }

        public void OnEnable()
        {
            EnsureInitialized();

            var newTreeState = false;
            if (treeViewState is null)
            {
                treeViewState = new TreeViewState();
                newTreeState = true;
            }
            _treeView ??= _treeView = new BurstMethodTreeView
            (
                treeViewState,
                () => _searchFilterJobs,
                () => (_showUnityNamespaceJobs, _showDOTSGeneratedJobs)
            );

            if (_keyboardEvents == null) FillKeyboardEvent();

            // Find the targets off the main thread, then finish loading in Update, as the tree view must be used from the main thread.
            _newTreeState = newTreeState;
#pragma warning disable UAL0018 // non-serialized field of an EditorWindow, which is recreated on code reload; the task is also dropped in Update as soon as it completes
            _loadTargetsTask = Task.Run(() => BurstReflection.FindExecuteMethods());
#pragma warning restore UAL0018
        }

        private void FinishLoadingTargets(Task<BurstReflection.FindExecuteMethodsResult> t)
        {
            if (t.Status == TaskStatus.RanToCompletion)
            {
                _targets = t.Result.CompileTargets;

                foreach (var logMessage in t.Result.LogMessages)
                {
                    switch (logMessage.LogType)
                    {
                        case BurstReflection.LogType.Warning:
                            Debug.LogWarning(logMessage.Message);
                            break;
                        case BurstReflection.LogType.Exception:
                            Debug.LogException(logMessage.Exception);
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }

                var newNames = new List<string>(_targets.Length);
                foreach (var target in _targets)
                {
                    newNames.Add(target.FullName);
                }

                bool identical = !_newTreeState && newNames.Count == targetNames.Count;
                int len = newNames.Count;
                int i = 0;
                while (identical && i < len)
                {
                    identical = newNames[i] == targetNames[i];
                    i++;
                }
                targetNames = newNames;
                _treeView.Initialize(_targets, identical);

                if (_selectedItem == null || !_treeView.TrySelectByDisplayName(_selectedItem))
                {
                    _treeView.SetSelection(new List<int>());
                    _previousTargetIndex = -1;
                    _scrollPos = Vector2.zero;
                }

                _requiresRepaint = true;
                _initialized = true;
                UpdateInitializedView();
            }
            else if (t.Exception != null)
            {
                Debug.LogError($"Could not load Inspector: {t.Exception}");
            }
        }

        public void Update()
        {
            if (_loadTargetsTask != null && _loadTargetsTask.IsCompleted)
            {
                var task = _loadTargetsTask;
                _loadTargetsTask = null;
                FinishLoadingTargets(task);
            }

            UpdateOutputTabsVisibility();

            // Need to do this because if we call Repaint from anywhere else,
            // it doesn't do anything if this window is not currently focused.
            if (_requiresRepaint)
            {
                Repaint();
                _requiresRepaint = false;
            }

            // Need this because pressing new target, and then not invoking new events,
            // will leave the assembly unrendered.
            // This is not included in above, to minimize needed calls.
            if (_target != null && _target.JustLoaded)
            {
                Repaint();
            }
        }

        private bool IsRaw(AssemblyOptions kind)
        {
            return kind == AssemblyOptions.PlainWithoutDebugInformation || kind == AssemblyOptions.PlainWithDebugInformation;
        }

        private bool IsEnhanced(AssemblyOptions kind)
        {
            return !IsRaw(kind);
        }

        private bool IsColoured(AssemblyOptions kind)
        {
            return kind == AssemblyOptions.ColouredWithMinimalDebugInformation || kind == AssemblyOptions.ColouredWithFullDebugInformation;
        }

        /// <summary>
        /// Renders buttons bar, and handles saving/loading of _assemblyKind options when changing in inspector settings
        /// that disable/enables some options for _assemblyKind.
        /// </summary>
        private void HandleButtonBars(BurstCompileTarget target, bool targetChanged, out int fontIndex, out bool collapse, out bool focusCode)
        {
            RenderButtonBars(target, out fontIndex, out collapse, out focusCode);

            var disasmKindChanged = _oldDisasmKind != _disasmKind;

            // Handles saving and loading _assemblyKind option when going between settings, that disable/enable some options for it
            if ((disasmKindChanged && _oldDisasmKind == DisassemblyKind.Asm && !_isCompileError)
                || (targetChanged && !_prevWasCompileError && _isCompileError && _disasmKind == DisassemblyKind.Asm))
            {
                // save when _disasmKind changed from Asm WHEN we are not looking at a burst compile error,
                // or when target changed from non compile error to compile error and current _disasmKind is Asm.
                _oldAssemblyKind = (AssemblyOptions)_assemblyKind;
            }
            else if ((disasmKindChanged && _disasmKind == DisassemblyKind.Asm && !_isCompileError) ||
                     (targetChanged && _prevWasCompileError && _disasmKind == DisassemblyKind.Asm))
            {
                // load when _diasmKind changed to Asm and we are not at burst compile error,
                // or when target changed from a burst compile error while _disasmKind is Asm.
                _assemblyKind = _oldAssemblyKind;
            }

            // if _assemblyKind is something that is not available, force it up to PlainWithoutDebugInformation.
            if ((_disasmKind != DisassemblyKind.Asm && _assemblyKind != AssemblyOptions.PlainWithoutDebugInformation)
                || _isCompileError)
            {
                _assemblyKind = AssemblyOptions.PlainWithoutDebugInformation;
            }
        }

        private void RenderButtonBars(BurstCompileTarget target, out int fontIndex, out bool collapse, out bool focus)
        {
            _oldDisasmKind = _disasmKind;
            // Switch on Layout, so the Layout and Repaint of a frame draw the same toolbars.
            if (_requestedDisasmKind.HasValue && Event.current.type == EventType.Layout)
            {
                _disasmKind = _requestedDisasmKind.Value;
                _requestedDisasmKind = null;
            }

            // Compilation and display options.
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginDisabledGroup(target.DisassemblyKind == DisassemblyKind.IRPassAnalysis);
            ToolbarDropdown(new GUIContent(AssemblyOptionNames[(int)_assemblyKind], "Output format of the disassembly."), BuildAssemblyOptionsMenu);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!target.IsValidTarget);
            ToolbarDropdown(new GUIContent($"Target: {TargetCpuNames[(int)_targetCpu]}", "CPU architecture to compile for."), BuildTargetMenu);
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(ToolbarGroupSpacing);
            _safetyChecks = GUILayout.Toggle(_safetyChecks, _contentSafetyChecks, EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();
            ToolbarDropdown(_contentView, BuildViewMenu);
            GUILayout.EndHorizontal();

            // Navigation.
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            RenderFunctionDropdown();

            GUILayout.Space(ToolbarGroupSpacing);
            EditorGUI.BeginDisabledGroup(!IsEnhancedViewAvailable);
            focus = GUILayout.Button(_contentCollapseToCode, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            collapse = GUILayout.Button(_contentExpandAll, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            _buttonOverlapInspectorView = HeaderToolbarCount * EditorStyles.toolbar.fixedHeight;

            fontIndex = _requestedFontSizeIndex >= 0 ? _requestedFontSizeIndex : fontSizeIndex;
            _requestedFontSizeIndex = -1;
            if (_requestedSmellTest.HasValue)
            {
                _smellTest = _requestedSmellTest.Value;
                _requestedSmellTest = null;
            }
        }

        private const int HeaderToolbarCount = 2;
        private const float ToolbarGroupSpacing = 8f;

        private bool IsEnhancedViewAvailable => IsEnhanced((AssemblyOptions)_assemblyKind) && SupportsEnhancedRendering && !_isCompileError;

        private static void ToolbarDropdown(GUIContent content, Func<GenericMenu> buildMenu)
        {
            var rect = GUILayoutUtility.GetRect(content, EditorStyles.toolbarDropDown);
            if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                buildMenu().DropDown(rect);
            }
        }

        private GenericMenu BuildAssemblyOptionsMenu()
        {
            var menu = new GenericMenu();
            for (var i = 0; i < AssemblyOptionNames.Length; i++)
            {
                var option = (AssemblyOptions)i;
                var content = new GUIContent(AssemblyOptionNames[i]);
                if (i > 0 && i % 2 == 0)
                {
                    menu.AddSeparator(string.Empty);
                }

                if (DisplayAssemblyKind(option))
                {
                    menu.AddItem(content, _assemblyKind == option, () =>
                    {
                        _assemblyKind = option;
                        Repaint();
                    });
                }
                else
                {
                    menu.AddDisabledItem(content, _assemblyKind == option);
                }
            }
            return menu;
        }

        private GenericMenu BuildTargetMenu()
        {
            var menu = new GenericMenu();
            for (var i = 0; i < TargetCpuNames.Length; i++)
            {
                var targetCpu = (BurstTargetCpu)i;
                menu.AddItem(new GUIContent(TargetCpuNames[i]), _targetCpu == targetCpu, () =>
                {
                    _targetCpu = targetCpu;
                    Repaint();
                });
            }
            return menu;
        }

        private GenericMenu BuildViewMenu()
        {
            var menu = new GenericMenu();
            for (var i = 0; i < FontSizes.Length; i++)
            {
                var index = i;
                menu.AddItem(new GUIContent($"Font Size/{FontSizes[i]}"), fontSizeIndex == i, () =>
                {
                    _requestedFontSizeIndex = index;
                    Repaint();
                });
            }

            menu.AddSeparator(string.Empty);
            if (IsEnhancedViewAvailable)
            {
                menu.AddItem(_contentShowBranchFlow, _showBranchMarkers, () =>
                {
                    _showBranchMarkers = !_showBranchMarkers;
                    Repaint();
                });
                menu.AddItem(_contentHighlightSIMD, _smellTest, () =>
                {
                    _requestedSmellTest = !_smellTest;
                    Repaint();
                });
            }
            else
            {
                menu.AddDisabledItem(_contentShowBranchFlow, _showBranchMarkers);
                menu.AddDisabledItem(_contentHighlightSIMD, _smellTest);
            }
            return menu;
        }

        private void RenderFunctionDropdown()
        {
            var functions = _textArea?.Functions;
            var hasFunctions = functions != null && functions.Count > 0 && !_isCompileError;

            GUIContent content;
            if (!hasFunctions)
            {
                content = _contentFunctionsUnavailable;
            }
            else
            {
                var functionIdx = _textArea.GetFunctionIdxAtPositionY(_scrollPos.y + FunctionJumpMarginLines * _textArea.fontHeight);
                content = functionIdx < 0
                    ? _contentNoFunction
                    : new GUIContent(FunctionLabelPrefix + functions[functionIdx].ShortName, functions[functionIdx].DisplayName);
            }

            var style = EditorStyles.toolbarDropDown;
            var rect = GUILayoutUtility.GetRect(GUIContent.none, style, GUILayout.ExpandWidth(true));
            content = new GUIContent(TruncateToWidth(content.text, style, rect.width), content.tooltip);

            EditorGUI.BeginDisabledGroup(!hasFunctions);
            if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, style))
            {
                _functionDropdownState ??= new AdvancedDropdownState();
                // The list stretches to the width of the rect it opens from, so open it from a narrow one and let it fit its content.
                var listRect = new Rect(rect.x, rect.y, Math.Min(rect.width, MinFunctionListWidth), rect.height);
                new BurstFunctionDropdown(_functionDropdownState, functions, idx =>
                {
                    _pendingFunctionJump = idx;
                    Repaint();
                }).Show(listRect);
            }
            EditorGUI.EndDisabledGroup();
        }

        private static string TruncateToWidth(string text, GUIStyle style, float width)
        {
            if (width <= 1f || style.CalcSize(new GUIContent(text)).x <= width)
            {
                return text;
            }

            const string ellipsis = "\u2026";
            int low = 0, high = text.Length;
            while (low < high)
            {
                var mid = (low + high + 1) / 2;
                if (style.CalcSize(new GUIContent(text.Substring(0, mid) + ellipsis)).x <= width)
                {
                    low = mid;
                }
                else
                {
                    high = mid - 1;
                }
            }
            return text.Substring(0, low) + ellipsis;
        }

        private void JumpToPendingFunction()
        {
            var functions = _textArea.Functions;
            if (functions != null && _pendingFunctionJump < functions.Count)
            {
                var function = functions[_pendingFunctionJump];
                var positionY = _textArea.GetLinePositionY(function.BlockIndex, function.LineInBlock);
                _scrollPos.y = Math.Max(0f, positionY - FunctionJumpMarginLines * _textArea.fontHeight);
            }
            _pendingFunctionJump = -1;
        }

        /// <summary>
        /// Handles mouse events for selecting text.
        /// </summary>
        /// <remarks>
        /// Must be called after Render(...), as it uses the mouse events, and Render(...)
        /// need mouse events for buttons etc.
        /// </remarks>
        private void HandleMouseEventForSelection(Rect workingArea, int controlID, bool showBranchMarkers)
        {
            var evt = Event.current;
            var mousePos = evt.mousePosition;

            if (_textArea.MouseOutsideView(workingArea, mousePos, controlID))
            {
                return;
            }

            switch (evt.type)
            {
                case EventType.MouseDown:
                    // button 0 is left and 1 is right
                    if (evt.button == 0)
                    {
                        _textArea.MouseClicked(showBranchMarkers, evt.shift, mousePos, controlID);
                    }
                    else
                    {
                        _leftClicked = true;
                    }
                    evt.Use();
                    break;
                case EventType.MouseDrag:
                    _textArea.DragMouse(mousePos, showBranchMarkers);
                    evt.Use();
                    break;
                case EventType.MouseUp:
                    _textArea.MouseReleased();
                    evt.Use();
                    break;
                case EventType.ScrollWheel:
                    _textArea.DoScroll(workingArea, evt.delta.y);
                    // we cannot Use() (consume) scrollWheel events, as they are still needed in EndScrollView.
                    break;
            }
        }

        private bool AssemblyFocused() => !((_treeView != null && _treeView.HasFocus()) || (_searchFieldAssembly != null && _searchFieldAssembly.HasFocus()));

        private void EnsureSearchFieldAssembly()
        {
            if (_searchFieldAssembly == null)
            {
                _searchFieldAssembly = new SearchField();
                _searchFieldAssembly.autoSetFocusOnFindCommand = false;
            }
        }

        private void HandleKeyboardEventAssemblyView(Rect workingArea, KeyboardOperation op, Event evt, bool showBranchMarkers)
        {
            switch (op)
            {
                case KeyboardOperation.SelectAll:
                    _textArea.SelectAll();
                    evt.Use();
                    break;

                case KeyboardOperation.Copy:
                    _textArea.DoSelectionCopy();
                    evt.Use();
                    break;

                case KeyboardOperation.MoveLeft:
                    if (evt.shift)
                    {
                        if (_textArea.HasSelection) _textArea.MoveSelectionLeft(workingArea, showBranchMarkers);
                    }
                    else
                    {
                        _textArea.MoveView(LongTextArea.Direction.Left, workingArea);
                    }

                    evt.Use();
                    break;

                case KeyboardOperation.MoveRight:
                    if (evt.shift)
                    {
                        if (_textArea.HasSelection) _textArea.MoveSelectionRight(workingArea, showBranchMarkers);
                    }
                    else
                    {
                        _textArea.MoveView(LongTextArea.Direction.Right, workingArea);
                    }

                    evt.Use();
                    break;

                case KeyboardOperation.MoveUp:
                    if (evt.shift)
                    {
                        if (_textArea.HasSelection) _textArea.MoveSelectionUp(workingArea, showBranchMarkers);
                    }
                    else
                    {
                        _textArea.MoveView(LongTextArea.Direction.Up, workingArea);
                    }

                    evt.Use();
                    break;

                case KeyboardOperation.MoveDown:
                    if (evt.shift)
                    {
                        if (_textArea.HasSelection) _textArea.MoveSelectionDown(workingArea, showBranchMarkers);
                    }
                    else
                    {
                        _textArea.MoveView(LongTextArea.Direction.Down, workingArea);
                    }

                    evt.Use();
                    break;
                case KeyboardOperation.Search:
                    FocusSearchField();
                    evt.Use();
                    break;
            }
        }

        /// <remarks>
        /// Must be called after Render(...) because of depenency on LongTextArea.finalAreaSize.
        /// </remarks>
        private void HandleKeyboardEventForSelection(Rect workingArea, bool showBranchMarkers)
        {
            var evt = Event.current;

            if (!_keyboardEvents.TryGetValue(evt, out var op))
            {
                return;
            }

            if (op == KeyboardOperation.FindNext || op == KeyboardOperation.FindPrevious)
            {
                if (_textArea.NrSearchHits > 0)
                {
                    _textArea.NextSearchHit(op == KeyboardOperation.FindPrevious, workingArea);
                    saveSearchFieldFromEvent = _searchFieldAssembly != null && _searchFieldAssembly.HasFocus();
                    evt.Use();
                }
                return;
            }

            if (AssemblyFocused())
            {
                // Do input handling for assembly view.
                HandleKeyboardEventAssemblyView(workingArea, op, evt, showBranchMarkers);
            }
            else
            {
                // This amounts to logic for all else.
                switch (op)
                {
                    case KeyboardOperation.Enter:
                        if (_searchFieldAssembly != null && _searchFieldAssembly.HasFocus())
                        {
                            _textArea.NextSearchHit(evt.shift, workingArea);
                            saveSearchFieldFromEvent = true;
                            evt.Use();
                        }
                        break;
                    case KeyboardOperation.Search:
                        FocusSearchField();
                        evt.Use();
                        break;
                }
            }
        }

        private void RenderCompileTargetsToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            var newFilter = _searchFieldJobs.OnToolbarGUI(_searchFilterJobs, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(float.MaxValue));
            if (newFilter != _searchFilterJobs)
            {
                _searchFilterJobs = newFilter;
                _treeView.Reload();
            }

            ToolbarDropdown(_contentJobsFilter, BuildJobsFilterMenu);
            GUILayout.EndHorizontal();
        }

        private GenericMenu BuildJobsFilterMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(_contentShowUnityNamespaceJobs, _showUnityNamespaceJobs, () =>
            {
                _showUnityNamespaceJobs = !_showUnityNamespaceJobs;
                _treeView.Reload();
                Repaint();
            });
            menu.AddItem(_contentShowDOTSGeneratedJobs, _showDOTSGeneratedJobs, () =>
            {
                _showDOTSGeneratedJobs = !_showDOTSGeneratedJobs;
                _treeView.Reload();
                Repaint();
            });
            return menu;
        }

        private void CompileNewTarget(BurstCompileTarget target)
        {
            if (target.IsLoading) { return; }

            target.IsLoading = true;
            target.JustLoaded = false;

            // Setup target and it's compilation options.
            // This is done here as EditorGUIUtility.isProSkin must be on main thread.
            target.TargetCpu = _targetCpu;
            target.DisassemblyKind = _disasmKind;
            target.EnableBurstSafetyChecks = _safetyChecks;
            target.IsDarkMode = EditorGUIUtility.isProSkin;

            Task.Run(() =>
            {
                var options = new StringWriter { NewLine = "\n" };

                if (target.IsValidTarget)
                {
                    options.WriteLine(target.DefaultOptions);

                    if (!target.EnableBurstSafetyChecks)
                    {
                        options.WriteLine($"{BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionDisableSafetyChecks)}");
                    }

                    // Disables the 2 current warnings generated from code (since they clutter up the inspector display)
                    // BC1370 - throw inside code not guarded with ConditionalSafetyCheck attribute
                    // BC1322 - loop intrinsic on loop that has been optimized away
                    options.WriteLine($"{BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionDisableWarnings, "BC1370;BC1322")}");

                    options.WriteLine($"{BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionTarget, TargetCpuNames[(int)_targetCpu])}");

                    // For IRPassAnalysis, we always want full debug information.
                    if (_disasmKind != DisassemblyKind.IRPassAnalysis)
                    {
                        switch (_assemblyKind)
                        {
                            case AssemblyOptions.EnhancedWithMinimalDebugInformation:
                            case AssemblyOptions.ColouredWithMinimalDebugInformation:
                                options.WriteLine($"{BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionDebug, "2")}");
                                break;
                            case AssemblyOptions.ColouredWithFullDebugInformation:
                            case AssemblyOptions.EnhancedWithFullDebugInformation:
                            case AssemblyOptions.PlainWithDebugInformation:
                                options.WriteLine($"{BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionDebug, "1")}");
                                break;
                            default:
                            case AssemblyOptions.PlainWithoutDebugInformation:
                                options.WriteLine($"{BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionDebug, "0")}");
                                break;
                        }
                    }
                    else
                    {
                        options.WriteLine($"{BurstCompilerOptions.GetOption(BurstCompilerOptions.OptionDebug, "1")}");
                    }

                    var baseOptions = options.ToString();

                    target.RawDisassembly = GetDisassembly(target.MethodName, baseOptions + GetDisasmOptions()[(int)_disasmKind]);

                    target.FormattedDisassembly = null;

                    target.IsBurstError = IsBurstError(target.RawDisassembly);
                }

                target.IsLoading = false;
                target.JustLoaded = true;
            });
        }

        private void HandleHorizontalFocus(float workingWidth, bool shouldSetupText, bool isTextFormatted)
        {
            if (!shouldSetupText || !isTextFormatted || !_burstDisassembler.IsInitialized) { return; }

            var branchFiller = _textArea.MaxLineDepth * 10;

            if (branchFiller < workingWidth / 2f) { return; }

            // Do horizontal padding:
            _scrollPos.x = _textArea.MaxLineDepth * 10;
        }


        private static void RenderLoading()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Loading...");
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public void CreateGUI()
        {
            _loadingView = new IMGUIContainer(RenderLoading) { style = { flexGrow = 1 } };

            _splitView = new TwoPaneSplitView(0, Mathf.Max(JobsPaneMinWidth, position.width * 0.3f), TwoPaneSplitViewOrientation.Horizontal)
            {
                viewDataKey = "BurstInspectorSplitView",
            };

            _jobsPane = new IMGUIContainer(OnJobsPaneGUI) { style = { minWidth = JobsPaneMinWidth } };

            var outputPane = new VisualElement { style = { minWidth = OutputPaneMinWidth } };
            _disasmKindTabs = new TabView();
            _disasmKindTabs.AddToClassList(OutputTabsUssClassName);
            foreach (var content in DisassemblyKindContents)
            {
                _disasmKindTabs.Add(new Tab(content.text) { tooltip = content.tooltip });
            }
            _disasmKindTabs.selectedTabIndex = (int)_disasmKind;
            _disasmKindTabs.activeTabChanged += OnDisasmKindTabChanged;
            _assemblyPane = new IMGUIContainer(OnAssemblyPaneGUI) { style = { flexGrow = 1 } };
            outputPane.Add(_disasmKindTabs);
            outputPane.Add(_assemblyPane);

            _splitView.Add(_jobsPane);
            _splitView.Add(outputPane);

            rootVisualElement.styleSheets.Add(EditorGUIUtility.LoadRequired(StyleSheetPath) as StyleSheet);
            rootVisualElement.Add(_loadingView);
            rootVisualElement.Add(_splitView);

            // Listen above the dock area too, which takes the focus when the window's tab is clicked.
            rootVisualElement.panel?.visualTree.RegisterCallback<KeyDownEvent>(OnWindowKeyDown);
            rootVisualElement.RegisterCallback<AttachToPanelEvent>(evt => evt.destinationPanel.visualTree.RegisterCallback<KeyDownEvent>(OnWindowKeyDown));
            rootVisualElement.RegisterCallback<DetachFromPanelEvent>(evt => evt.originPanel.visualTree.UnregisterCallback<KeyDownEvent>(OnWindowKeyDown));

            UpdateInitializedView();
            UpdateOutputTabsVisibility();
        }

        // The output tabs only apply to a selected compile target.
        private void UpdateOutputTabsVisibility()
        {
            if (_disasmKindTabs == null) return;

            var selection = _initialized ? _treeView.GetSelection() : null;
            var hasTarget = selection != null && selection.Count == 1 && selection[0] >= 1 && selection[0] <= _targets.Length;
            var display = hasTarget ? DisplayStyle.Flex : DisplayStyle.None;
            if (_disasmKindTabs.style.display != display)
            {
                _disasmKindTabs.style.display = display;
            }
        }

        private void UpdateInitializedView()
        {
            if (_splitView == null) return;

            _loadingView.style.display = _initialized ? DisplayStyle.None : DisplayStyle.Flex;
            _splitView.style.display = _initialized ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnDisasmKindTabChanged(Tab previous, Tab current)
        {
            _requestedDisasmKind = (DisassemblyKind)_disasmKindTabs.selectedTabIndex;
            Repaint();
        }

        // Shortcuts that work wherever the focus is; the panes' IMGUI handles them first when it can.
        private void OnWindowKeyDown(KeyDownEvent evt)
        {
            // Without a focused element the event goes on to the panes' IMGUI, which handles it.
            if (evt.target == rootVisualElement.panel?.visualTree)
            {
                return;
            }

            if (evt.imguiEvent == null || _keyboardEvents == null || !_keyboardEvents.TryGetValue(evt.imguiEvent, out var op))
            {
                return;
            }

            switch (op)
            {
                case KeyboardOperation.Search:
                    _assemblyPane.Focus();
                    FocusSearchField();
                    break;
                case KeyboardOperation.FindNext:
                case KeyboardOperation.FindPrevious:
                    if (_textArea == null || _textArea.NrSearchHits == 0) return;
                    _pendingSearchHitStep = op == KeyboardOperation.FindPrevious ? -1 : 1;
                    break;
                default:
                    return;
            }

            Repaint();
            evt.StopPropagation();
        }

        private void EnsureGUIResources()
        {
            // Make sure that editor options are synchronized
            BurstEditorOptions.EnsureSynchronized();

            if (fontSizeIndex == -1)
            {
                fontSizeIndex = EditorPrefs.GetInt(FontSizeIndexPref, 5);
                fontSizeIndex = Math.Max(0, fontSizeIndex);
                fontSizeIndex = Math.Min(fontSizeIndex, FontSizes.Length - 1);
            }

            if (fixedFontStyle == null || fixedFontStyle.font == null) // also check .font as it's reset somewhere when going out of play mode.
            {
                fixedFontStyle = new GUIStyle(GUI.skin.label);

                _font = EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font;

                fixedFontStyle.font = _font;
                fixedFontStyle.fontSize = FontSize;
            }

            if (_searchFieldJobs == null) _searchFieldJobs = new SearchField();

            if (_textArea == null) _textArea = new LongTextArea();
        }

        private void OnJobsPaneGUI()
        {
            EnsureGUIResources();

            RenderCompileTargetsToolbar();

            var treeRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            _treeView.OnGUI(treeRect);
        }

        private void OnAssemblyPaneGUI()
        {
            EnsureGUIResources();

            // used to give hot control to inspector when a mouseDown event has happened.
            // This way we can register a mouseUp happening outside inspector.
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            var selection = _treeView.GetSelection();
            if (selection.Count == 1 && (selection[0] < 1 || selection[0] > _targets.Length))
            {
                // The selection does not refer to one of the current compile targets.
                _treeView.SetSelection(new List<int>());
                selection = _treeView.GetSelection();
            }

            if (selection.Count == 1)
            {
                var targetIndex = selection[0];
                _target = _targets[targetIndex - 1];

                var targetChanged = _previousTargetIndex != targetIndex;

                _previousTargetIndex = targetIndex;

                // Stash selected item name to handle domain reloads more gracefully
                _selectedItem = _target.FullName;

                if (_assemblyKind == null)
                {
                    if (_enhancedDisassembly)
                    {
                        _assemblyKind = AssemblyOptions.ColouredWithMinimalDebugInformation;
                    }
                    else
                    {
                        _assemblyKind = AssemblyOptions.PlainWithoutDebugInformation;
                    }
                    _oldAssemblyKind = (AssemblyOptions)_assemblyKind;
                }

                // We are currently formatting only Asm output
                var isTextFormatted = IsEnhanced((AssemblyOptions)_assemblyKind) && SupportsEnhancedRendering;

                // Depending if we are formatted or not, we don't render the same text
                var textToRender = _target.RawDisassembly?.TrimStart('\n');

                // Only refresh if we are switching to a new selection that hasn't been disassembled yet
                // Or we are changing disassembly settings (safety checks / enhanced disassembly)
                var targetRefresh = textToRender == null
                                    || _target.DisassemblyKind != _disasmKind
                                    || _target.EnableBurstSafetyChecks != _safetyChecks
                                    || _target.TargetCpu != _targetCpu
                                    || _target.IsDarkMode != EditorGUIUtility.isProSkin;

                if (_assemblyKindPrior != _assemblyKind)
                {
                    targetRefresh = true;
                    _assemblyKindPrior = _assemblyKind;  // Needs to be refreshed, as we need to change disassembly options

                    // If the target did not changed but our assembly kind did, we need to remember this.
                    if (!targetChanged)
                    {
                        _sameTargetButDifferentAssemblyKind = true;
                    }
                }

                // If the previous target changed the assembly kind and we have a target change, we need to
                // refresh the assembly because we'll have cached the previous assembly kinds output rather
                // than the one requested.
                if (_sameTargetButDifferentAssemblyKind && targetChanged)
                {
                    targetRefresh = true;
                    _sameTargetButDifferentAssemblyKind = false;
                }

                if (targetRefresh)
                {
                    CompileNewTarget(_target);
                }

                _prevWasCompileError = _isCompileError;
                _isCompileError = _target.IsBurstError;

                _buttonOverlapInspectorView = 0;
                var oldSimdSmellTest = _smellTest;
                HandleButtonBars(_target, targetChanged, out var fontSize, out var expandAllBlocks, out var focusCode);
                var simdSmellTestChanged = oldSimdSmellTest != _smellTest;

                // An output tab switch applied during this Layout: compile now, so this Layout and its Repaint both draw the loading view.
                if (_target.DisassemblyKind != _disasmKind)
                {
                    _assemblyKindPrior = _assemblyKind;
                    CompileNewTarget(_target);
                }

                // Guard against _textArea being used, as the assembly isn't ready yet.
                // Have to test against event so it cannot finish between a Layout event and Repaint event;
                // this is necessary as we cannot alter GUI between these events.
                if (_target.IsValidTarget && (_target.IsLoading || (_target.JustLoaded && Event.current.type != EventType.Layout)))
                {
                    RenderLoading();
                    return;
                }

                var justLoaded = _target.JustLoaded;
                _target.JustLoaded = false;

                // If ´CompileNewTarget´ finishes before we enter loading screen above `textToRender` might not be set.
                textToRender ??= _target.RawDisassembly?.TrimStart('\n');

                if (!string.IsNullOrEmpty(textToRender))
                {
                    // we should only call SetDisassembler(...) the first time assemblyKind is changed with same target.
                    // Otherwise it will kep re-initializing fields such as _folded, meaning we can no longer fold/unfold.
                    var shouldSetupText = !_textArea.IsTextSet(_selectedItem)
                                          || justLoaded
                                          || simdSmellTestChanged;

                    if (shouldSetupText)
                    {
                        _textArea.SetText(
                            _selectedItem,
                            textToRender,
                            _target.IsDarkMode,
                            _burstDisassembler,
                            isTextFormatted && _burstDisassembler.Initialize(
                                textToRender,
                                FetchAsmKind(_targetCpu, _disasmKind),
                                _target.IsDarkMode,
                                IsColoured((AssemblyOptions)_assemblyKind),
                                _smellTest));
                    }
                    if (justLoaded)
                    {
                        _scrollPos = Vector2.zero;
                    }

                    if (shouldSetupText)
                    {
                        _pendingFunctionJump = -1;
                    }

                    var paneSize = _assemblyPane.contentRect.size;

                    HandleHorizontalFocus(paneSize.x, shouldSetupText, isTextFormatted);

                    // The visible text area: the pane minus the toolbars, the search bar and the scroll bars.
                    _inspectorView.position = _scrollPos;
                    _inspectorView.width = paneSize.x - _scrollbarThickness;
                    _inspectorView.height = paneSize.y - _buttonOverlapInspectorView - EditorStyles.toolbar.fixedHeight - _scrollbarThickness;

                    // Do search if we did not try and find assembly and we were actually going to do a search.
                    if (_focusTargetJob && TryFocusJobInAssemblyView(ref _inspectorView, shouldSetupText, _target))
                    {
                        _scrollPos.y = _inspectorView.y - _textArea.fontHeight*10;
                    }

                    // Jump on layout, as unfolding the function's block changes the layout.
                    if (_pendingFunctionJump >= 0 && Event.current.type == EventType.Layout)
                    {
                        JumpToPendingFunction();
                    }

                    _scrollPos = GUILayout.BeginScrollView(_scrollPos, true, true);

                    if (Event.current.type != EventType.Layout) // we always want mouse position feedback
                    {
                        _textArea.Interact(_inspectorView, Event.current.type);
                    }

                    // Set up search information if it is happening.
                    Regex regx = default;
                    SearchCriteria sc = default;
                    var doSearch = _searchFilterAssembly != "";
                    var wrongRegx = false;
                    if (doSearch)
                    {
                        sc = new SearchCriteria(_searchFilterAssembly, _doIgnoreCase, _doWholeWordMatch, _doRegex);
                        if (_doRegex)
                        {
                            try
                            {
                                var opt = RegexOptions.Compiled | RegexOptions.CultureInvariant;
                                if (!_doIgnoreCase) opt |= RegexOptions.IgnoreCase;

                                var filter = _searchFilterAssembly;
                                if (_doWholeWordMatch) filter = @"\b" + filter + @"\b";

                                regx = new Regex(filter, opt);
                            }
                            catch (Exception)
                            {
                                // Regex was invalid
                                wrongRegx = true;
                                doSearch = false;
                            }
                        }
                    }

                    var doRepaint = _textArea.Render(fixedFontStyle, _inspectorView, _showBranchMarkers, doSearch, sc, regx);

                    // A change in the underlying textArea has happened, that requires the GUI to be repainted during this frame.
                    if (doRepaint) Repaint();

                    if (Event.current.type != EventType.Layout)
                    {
                        HandleMouseEventForSelection(_inspectorView, controlID, _showBranchMarkers);
                        HandleKeyboardEventForSelection(_inspectorView, _showBranchMarkers);

                        // The search buttons are outside the scroll view, so they can only request the scroll.
                        if (_pendingSearchHitStep != 0 && Event.current.type == EventType.Repaint)
                        {
                            _textArea.NextSearchHit(_pendingSearchHitStep < 0, _inspectorView);
                            _pendingSearchHitStep = 0;
                            Repaint();
                        }
                    }

                    if (_leftClicked)
                    {
                        GenericMenu menu = new GenericMenu();

                        menu.AddItem(L10n.TextContent("Copy Selection", null, null, null), false, _textArea.DoSelectionCopy);
                        menu.AddItem(L10n.TextContent("Copy Color Tags", null, null, null), _textArea.CopyColorTags, _textArea.ChangeCopyMode);
                        menu.AddItem(L10n.TextContent("Select All", null, null, null), false, _textArea.SelectAll);
                        menu.AddItem(L10n.TextContent($"Find in {DisassemblyKindContents[(int)_disasmKind].text}", null, null, null), false, FocusSearchField);
                        menu.ShowAsContext();

                        _leftClicked = false;
                    }

                    GUILayout.EndScrollView();

                    EnsureSearchFieldAssembly();

                    int hitnumbers = _textArea.NrSearchHits > 0 ? _textArea.ActiveSearchNr + 1 : 0;
                    var hitNumberContent = new GUIContent(hitnumbers + " of " + _textArea.NrSearchHits);

                    GUILayout.BeginHorizontal(EditorStyles.toolbar);

                    // Makes sure that on "enter" keyboard event, the focus is not taken away from searchField.
                    if (saveSearchFieldFromEvent) GUI.FocusControl("BurstInspectorGUI");

                    var searchFieldOptions = new[] { GUILayout.ExpandWidth(true), GUILayout.MaxWidth(float.MaxValue) };
                    string newFilterAssembly;
                    if (wrongRegx)
                    {
                        var colb = GUI.contentColor;
                        GUI.contentColor = Color.red;
                        newFilterAssembly = _searchFieldAssembly.OnToolbarGUI(_searchFilterAssembly, searchFieldOptions);
                        GUI.contentColor = colb;
                    }
                    else
                    {
                        newFilterAssembly = _searchFieldAssembly.OnToolbarGUI(_searchFilterAssembly, searchFieldOptions);
                    }
                    // Give back focus to the searchField, if we took it away.
                    if (saveSearchFieldFromEvent)
                    {
                        _searchFieldAssembly.SetFocus();
                        saveSearchFieldFromEvent = false;
                    }

                    if (newFilterAssembly != _searchFilterAssembly)
                    {
                        _searchFilterAssembly = newFilterAssembly;
                        _textArea.StopSearching();
                    }

                    GUILayout.Space(ToolbarGroupSpacing);
                    _doIgnoreCase = GUILayout.Toggle(_doIgnoreCase, _ignoreCase, EditorStyles.toolbarButton);
                    _doWholeWordMatch = GUILayout.Toggle(_doWholeWordMatch, _matchWord, EditorStyles.toolbarButton);
                    _doRegex = GUILayout.Toggle(_doRegex, _regexSearch, EditorStyles.toolbarButton);

                    GUILayout.Space(ToolbarGroupSpacing);
                    // Laid out like the toolbar buttons beside it, so the text lines up with theirs.
                    _searchHitCountStyle ??= new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        margin = EditorStyles.toolbarButton.margin,
                        padding = EditorStyles.toolbarButton.padding,
                        fixedHeight = EditorStyles.toolbarButton.fixedHeight,
                    };
                    GUILayout.Label(hitNumberContent, _searchHitCountStyle, GUILayout.ExpandWidth(false));

                    if (_previousSearchHit == null)
                    {
                        var isMac = SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX;
                        _previousSearchHit = new GUIContent("Previous", $"Go to the previous match (Shift+Enter, {(isMac ? "Shift+Cmd+G" : "Shift+F3")})");
                        _nextSearchHit = new GUIContent("Next", $"Go to the next match (Enter, {(isMac ? "Cmd+G" : "F3")})");
                    }

                    EditorGUI.BeginDisabledGroup(_textArea.NrSearchHits == 0);
                    if (GUILayout.Button(_previousSearchHit, EditorStyles.toolbarButton))
                    {
                        _pendingSearchHitStep = -1;
                        Repaint();
                    }
                    if (GUILayout.Button(_nextSearchHit, EditorStyles.toolbarButton))
                    {
                        _pendingSearchHitStep = 1;
                        Repaint();
                    }
                    EditorGUI.EndDisabledGroup();

                    GUILayout.EndHorizontal();
                }

                if (fontSize != fontSizeIndex)
                {
                    _textArea.Invalidate();
                    fontSizeIndex = fontSize;
                    EditorPrefs.SetInt(FontSizeIndexPref, fontSize);
                    fixedFontStyle = null;
                }

                if (expandAllBlocks)
                {
                    _textArea.ExpandAllBlocks();
                }

                if (focusCode)
                {
                    _textArea.FocusCodeBlocks();
                }
            }

        }

        public static bool IsBurstError(string disassembly)
        {
            return _rx.IsMatch(disassembly ?? "");
        }

        /// <summary>
        /// Focuses the view on the active function if a jump is doable.
        /// </summary>
        /// <param name="workingArea">Current assembly view.</param>
        /// <param name="wasTextSetup">Whether text was set in <see cref="_textArea"/>.</param>
        /// <param name="target">Target job to find function in.</param>
        /// <returns>Whether a focus was attempted or not.</returns>
        private bool TryFocusJobInAssemblyView(ref Rect workingArea, bool wasTextSetup, BurstCompileTarget target)
        {
            bool TryFindByLabel(ref Rect workingArea)
            {
                var searchName = target.SearchText;

                const RegexOptions opt = RegexOptions.Compiled | RegexOptions.CultureInvariant;
                var regx = new Regex(@$"{Regex.Escape(searchName)}[^"":]+"":", opt);

                var sc = new SearchCriteria(searchName, false, false, true);

                return _textArea.SearchText(sc, regx, ref workingArea, true, true);
            }

            var foundTarget = false;
            // _isTextSetLastEvent used so we call this at the first scroll-able event after text was set.
            // We cannot scroll during used or layout events, and the order of events are:
            //   1. Used event:     text is set in textArea
            //   2. Layout event:   Cannot do the jump yet
            //   3. Repaint event:  Now jump is doable
            // Hence _isTextSetLastEvent is only set on layout events (during phase 2)
            if (wasTextSetup)
            {
                // Need to call Layout to setup fontsize before searching
                _textArea.Layout(fixedFontStyle, _textArea.horizontalPad);

                foundTarget = TryFindByLabel(ref workingArea);
                _textArea.StopSearching(); // Clear the internals of _textArea from this search; to avoid highlighting

                // Clear other possible search, so it won't interfere with this.
                _searchFilterAssembly = string.Empty;

                // We need to do a Repaint() in order for the view to actually update immediately.
                if (foundTarget) Repaint();
            }

            return foundTarget;
        }

        private void FocusSearchField()
        {
            EnsureSearchFieldAssembly();
            _searchFieldAssembly.SetFocus();
        }

        private bool _doIgnoreCase = false;
        private bool _doWholeWordMatch = false;
        private bool _doRegex = false;

        internal static string GetDisassembly(string methodName, string options)
        {
            try
            {
                var result = BurstCompiler.GetInspectorDisassembly(methodName, options);
                if (result.IndexOf('\t') >= 0)
                {
                    result = result.Replace("\t", "        ");
                }

                // Workaround to remove timings
                if (result.Contains("Burst timings"))
                {
                    var index = result.IndexOf("While compiling", StringComparison.Ordinal);
                    if (index > 0)
                    {
                        result = result.Substring(index);
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                return "Failed to compile:\n" + e.Message;
            }
        }

        internal static BurstDisassembler.AsmKind FetchAsmKind(BurstTargetCpu cpu, DisassemblyKind kind)
        {
            if (kind == DisassemblyKind.Asm)
            {
                switch (cpu)
                {
                    case BurstTargetCpu.Auto:
                        var processArchitecture = RuntimeInformation.ProcessArchitecture;
                        if (processArchitecture == Architecture.Arm || processArchitecture == Architecture.Arm64)
                        {
                            return BurstDisassembler.AsmKind.ARM;
                        }
                        return BurstDisassembler.AsmKind.Intel;
                    case BurstTargetCpu.ARMV7A_NEON32:
                    case BurstTargetCpu.ARMV8A_AARCH64:
                    case BurstTargetCpu.ARMV8A_AARCH64_HALFFP:
                    case BurstTargetCpu.THUMB2_NEON32:
                    case BurstTargetCpu.ARMV9A:
                        return BurstDisassembler.AsmKind.ARM;
                    case BurstTargetCpu.WASM32:
                    case BurstTargetCpu.WASM64:
                        return BurstDisassembler.AsmKind.Wasm;
                    default:
                        return BurstDisassembler.AsmKind.Intel;
                }
            }
            else
            {
                return BurstDisassembler.AsmKind.LLVMIR;
            }
        }
    }

    /// <summary>
    /// Important: id for namespaces are negative, and ids for jobs are positive.
    ///            This lets us use the id for a job as an index directy into <see cref="_targets"/>.
    ///            Hence before going from <see cref="TreeViewItem"/> to <see cref="_targets"/> index,
    ///            One should check whether current item has any children (Only jobs are leafs).
    /// </summary>
    internal class BurstMethodTreeView : TreeView<int>
    {
        private readonly Func<string> _getFilter;
        private readonly Func<(bool,bool)> _getJobListFilterToggles;

        private BurstCompileTarget[] _targets;

        public BurstMethodTreeView(TreeViewState state, Func<string> getFilter, Func<(bool,bool)> getJobListFilterToggles) : base(state)
        {
            _getFilter = getFilter;
            _getJobListFilterToggles = getJobListFilterToggles;
            showBorder = true;
        }

        public void Initialize(BurstCompileTarget[] targets, bool identicalTargets)
        {
            _targets = targets;
            Reload();
            if (!identicalTargets) { ExpandAll(); }
        }

        /// <remarks>
        /// Assumes that namespaces and types are separated by '.'.
        /// </remarks>
        /// <param name="str">Given type name string.</param>
        /// <returns>List of namespaces/types</returns>
        internal static List<StringSlice> ExtractNameSpaces(string str)
        {
            if (str is null) { throw new ArgumentNullException(nameof(str)); }

            var nameSpaces = new List<StringSlice>();
            int len = str.Length;
            int scope = 0;
            int previdx = 0;
            int i;
            for (i = 0; i < len; i++)
            {
                bool stop = false;
                char c = str[i];
                switch (c)
                {
                    case '(':
                        // Jump out as we just found argument list!!!
                        stop = true;
                        break;
                    // We keep looking, as classes might have these in name:
                    case '{':
                    case '<':
                    case '[':
                        scope++;
                        break;
                    case '}':
                    case '>':
                    case ']':
                        scope--;
                        break;
                    case '.' when scope == 0:
                        nameSpaces.Add(new StringSlice(str, previdx, i - previdx));
                        previdx = i + 1;
                        break;
                }

                if (stop) { break; }
            }
            if (i - previdx > 0)
            {
                // Add final namespace.
                nameSpaces.Add(new StringSlice(str, previdx, i - previdx));
            }
            return nameSpaces;
        }

        internal static (int idN, List<TreeViewItem> added, List<StringSlice> nameSpace)
            ProcessNewItem(int idN, int idJ, BurstCompileTarget newTarget, List<StringSlice> oldNameSpace)
        {
            // Find all namespaces used for new target:
            var newNameSpaces = ExtractNameSpaces(newTarget.Scope);

            int iNewNs = 0;
            int lNewNs = newNameSpaces.Count;
            int iOldNs = 0;
            int lOldNs = oldNameSpace.Count;

            var added = new List<TreeViewItem>(lNewNs);
            int depth = 0;

            // Skip all namespaces shared by previous but increase depth accordingly:
            for (; iNewNs < lNewNs && iOldNs < lOldNs && newNameSpaces[iNewNs] == oldNameSpace[iOldNs];
                 depth++, iNewNs++, iOldNs++) {}

            // Handle all new namespaces:
            for (; iNewNs < lNewNs;
                 depth++, iNewNs++)
            {
                added.Add(new TreeViewItem { id = --idN, depth = depth, displayName = newNameSpaces[iNewNs].ToString()});
            }

            // Add the function name:
            added.Add(new TreeViewItem { id = idJ, depth = depth, displayName = newTarget.DisplayName });

            return (idN, added, newNameSpaces);
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem {id = 0, depth = -1, displayName = "Root"};
            var allItems = new List<TreeViewItem>();

            if (_targets != null)
            {
                var filter = _getFilter();
                var (showUnityNamespaceJobs, showDOTSGeneratedJobs) = _getJobListFilterToggles();
                // Have two separate ids so "jobs ids == jobs index".
                int idJ = 0;
                int idN = 0;
                var oldNameSpace = new List<StringSlice>();
                foreach (BurstCompileTarget target in _targets)
                {
                    // idJ used as index into _targets, which means it should also take hidden targets into account!
                    idJ++;

                    string displayName = target.FullName;

                    bool filtered =
                        (!string.IsNullOrEmpty(filter) &&
                         displayName.IndexOf(filter, 0, displayName.Length,
                             StringComparison.InvariantCultureIgnoreCase) < 0)
                        || (!showUnityNamespaceJobs &&
                            displayName.StartsWith("Unity.", StringComparison.InvariantCultureIgnoreCase))
                        || (!showDOTSGeneratedJobs &&
                            displayName.Contains(".Generated"));

                    if (filtered) { continue; }

                    try
                    {
                        var (newIdN, added, nameSpace) =
                            ProcessNewItem(idN, idJ, target, oldNameSpace);

                        allItems.AddRange(added);
                        idN = newIdN;
                        oldNameSpace = nameSpace;
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"Internal error: Could not add {displayName}\n  Because: {ex.Message}");
                    }
                }
            }
            SetupParentsAndChildrenFromDepths(root, allItems);
            return root;
        }

        public new IList<int> GetSelection()
        {
            IList<int> selection = base.GetSelection();
            // selection == non-leaf node => no job selected
            if (selection.Count > 0 && selection[0] < 0) { return new List<int>(); }
            return selection;
        }

        internal bool TrySelectByDisplayName(string name)
        {
            var id = 1;
            foreach (var t in _targets)
            {
                if (t.FullName == name)
                {
                    try
                    {
                        SetSelection(new[] { id });
                        FrameItem(id);
                        return true;
                    }
                    catch (ArgumentException)
                    {
                        // When a search is made in the job list, such that the job we search for is filtered away
                        // FrameItem(id) will throw a dictionary error. So we catch this, and tell the caller that
                        // it cannot be selected.
                        return false;
                    }
                }
                else
                {
                    ++id;
                }
            }
            return false;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (!args.item.hasChildren)
            {
                var target = _targets[args.item.id - 1];
                var wasEnabled = GUI.enabled;
                GUI.enabled = target.IsValidTarget;
                base.RowGUI(args);
                GUI.enabled = wasEnabled;
            }
            else
            {
                // Label GUI:
                base.RowGUI(args);
            }
        }

        protected override void SingleClickedItem(int id)
        {
            // If labeled click try and fold/expand:
            if (id < 0)
            {
                SetExpanded(id, !IsExpanded(id));
                SetSelection(new List<int>());
            }
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }
    }

    internal class BurstFunctionDropdown : AdvancedDropdown
    {
        private const string BurstInternalFunctionsName = "Burst Internal Functions";

        private readonly List<BurstDisassembler.AsmFunction> _functions;
        private readonly Action<int> _functionSelected;

        public BurstFunctionDropdown(AdvancedDropdownState state, List<BurstDisassembler.AsmFunction> functions, Action<int> functionSelected)
            : base(state)
        {
            _functions = functions;
            _functionSelected = functionSelected;
            minimumSize = new Vector2(0f, 300f);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            return BuildFunctionTree(_functions);
        }

        /// <summary>
        /// Lists the functions in disassembly order, with Burst internal functions in a sub-list, using the function index as item id.
        /// </summary>
        internal static AdvancedDropdownItem BuildFunctionTree(List<BurstDisassembler.AsmFunction> functions)
        {
            var root = new AdvancedDropdownItem("Functions");
            AdvancedDropdownItem internalFunctions = null;
            for (var i = 0; i < functions.Count; i++)
            {
                var function = functions[i];
                var item = new AdvancedDropdownItem(function.ShortName);
                if (function.IsBurstInternal)
                {
                    internalFunctions ??= new AdvancedDropdownItem(BurstInternalFunctionsName);
                    internalFunctions.AddChild(item);
                }
                else
                {
                    root.AddChild(item);
                }

                // AddChild replaces the id with one derived from the name.
                item.id = i;
            }

            if (internalFunctions != null)
            {
                root.AddChild(internalFunctions);
            }
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            _functionSelected(item.id);
        }
    }
}
