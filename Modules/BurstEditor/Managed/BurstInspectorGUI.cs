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
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

[assembly: InternalsVisibleTo("Unity.Burst.Editor.Tests")]
[assembly: InternalsVisibleTo("Unity.Burst.Tester.Editor.Tests")]
[assembly: InternalsVisibleTo("UnityEditor.Modules.Burst.EditModeTests")]

namespace Unity.Burst.Editor
{
    internal class BurstInspectorGUI : EditorWindow
    {
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

        private static readonly string[] DisassemblyKindNames =
        {
            "Assembly",
            ".NET IL",
            "LLVM IR (Unoptimized)",
            "LLVM IR (Optimized)",
            "LLVM IR Optimisation Diagnostics"
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

        private static readonly SplitterState TreeViewSplitterState = new SplitterState(new float[] { 30, 70 }, new int[] { 155, 128 }, null);

        private static readonly string[] TargetCpuNames = Enum.GetNames(typeof(BurstTargetCpu));
        private static readonly string[] SIMDSmellTest = { "False", "True" };

        private static readonly int[] FontSizes =
        {
            8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 20
        };

        private static string[] _fontSizesText;
        internal const int _scrollbarThickness = 14;

        internal float _buttonOverlapInspectorView = 0;

        /// <remarks>Used because it's not legal to change layout of GUI in a frame without the users input.</remarks>
        private float _buttonBarWidth = -1;
        private float _compileTargetsPanelWidth = -1;

        [NonSerialized]
        internal readonly BurstDisassembler _burstDisassembler;

        private const string BurstSettingText = "Inspector Settings/";

        [SerializeField] private BurstTargetCpu _targetCpu = BurstTargetCpu.Auto;

        [SerializeField] private DisassemblyKind _disasmKind = DisassemblyKind.Asm;
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
        [SerializeField] private bool _flowCompileTogglesToNewLine = false;
        [SerializeField] private bool _focusTargetJob = true;
        [SerializeField] private string _searchFilterAssembly = String.Empty;

        [SerializeField] private bool _sameTargetButDifferentAssemblyKind = false;
        [SerializeField] internal Vector2 _scrollPos;
        internal SearchField _searchFieldJobs;
        internal SearchField _searchFieldAssembly;
        private bool saveSearchFieldFromEvent = false;

        [SerializeField] private bool _searchBarVisible = true;

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
        private bool _requiresRepaint;

        private int FontSize => FontSizes[fontSizeIndex];

        private static readonly Regex _rx = new Regex(@"^.*\(\d+,\d+\):\sBurst\serror");

        private bool _leftClicked = false;

        [SerializeField] private bool _isCompileError = false;
        [SerializeField] private bool _prevWasCompileError;

        [SerializeField] private bool _smellTest = false;

        // Caching GUIContent and style options for button bar
        private readonly GUIContent _contentShowUnityNamespaceJobs = new GUIContent("Show Unity Namespace");
        private readonly GUIContent _contentShowDOTSGeneratedJobs = new GUIContent("Show \".Generated\"");
        private readonly GUIContent _contentDisasm = new GUIContent("Enhanced With Minimal Debug Information");
        private readonly GUIContent _contentCollapseToCode = new GUIContent("Focus on Code");
        private readonly GUIContent _contentExpandAll = new GUIContent("Expand All");
        private readonly GUIContent _contentBranchLines = new GUIContent("Show Branch Flow");
        private readonly GUIContent[] _contentsTarget;
        private readonly GUIContent[] _contentsFontSize;
        private readonly GUIContent[] _contentsSmellTest =
        {
            new GUIContent("Highlight SIMD Scalar vs Packed (False)"),
            new GUIContent("Highlight SIMD Scalar vs Packed (True)")
        };

        // content for button search bar
        private readonly GUIContent _ignoreCase = new GUIContent("Match Case");
        private readonly GUIContent _matchWord = new GUIContent("Whole words");
        private readonly GUIContent _regexSearch = new GUIContent("Regex");

        private readonly GUILayoutOption[] _toolbarStyleOptions = { GUILayout.ExpandWidth(true), GUILayout.MinWidth(5 * 10) };

        private readonly string[] _branchMarkerOptions = { "Hide Branch Flow", "Show Branch Flow" };
        private readonly string[] _safetyCheckOptions = { "Safety Check On", "Safety Check Off" };


        private enum KeyboardOperation
        {
            SelectAll,
            Copy,
            MoveLeft,
            MoveRight,
            MoveUp,
            MoveDown,
            Search,
            Escape,
            Enter,
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
            _keyboardEvents.Add(Event.KeyboardEvent("escape"), KeyboardOperation.Escape);
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
            }
            else
            {
                // windows or linux bindings.
                _keyboardEvents.Add(Event.KeyboardEvent("^a"), KeyboardOperation.SelectAll);
                _keyboardEvents.Add(Event.KeyboardEvent("^c"), KeyboardOperation.Copy);
                _keyboardEvents.Add(Event.KeyboardEvent("^f"), KeyboardOperation.Search);
            }
        }

        public BurstInspectorGUI()
        {
            _burstDisassembler = new BurstDisassembler();

            string[] names = Enum.GetNames(typeof(BurstTargetCpu));
            int size = names.Length;
            _contentsTarget = new GUIContent[size];
            for (int i = 0; i < size; i++)
            {
                _contentsTarget[i] = new GUIContent($"Target ({names[i]})");
            }

            size = FontSizes.Length;
            _contentsFontSize = new GUIContent[size];
            for (int i = 0; i < size; i++)
            {
                _contentsFontSize[i] = new GUIContent($"Font Size ({FontSizes[i].ToString()})");
            }
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

            Task.Run(
                    () =>
                    {
                        // Do this stuff asynchronously.
                        var result = BurstReflection.FindExecuteMethods();
                        _targets = result.CompileTargets;
                        return result;
                    })
                .ContinueWith(t =>
                {
                    // Do this stuff on the main (UI) thread.
                    if (t.Status == TaskStatus.RanToCompletion)
                    {
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

                        bool identical = !newTreeState && newNames.Count == targetNames.Count;
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
                            _previousTargetIndex = -1;
                            _scrollPos = Vector2.zero;
                        }

                        _requiresRepaint = true;
                        _initialized = true;
                    }
                    else if (t.Exception != null)
                    {
                        Debug.LogError($"Could not load Inspector: {t.Exception}");
                    }
                });
        }

        public void Update()
        {
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

        /// <summary>
        /// Checks if there is space for given content withs style, and starts new horizontalgroup
        /// if there is no space on this line.
        /// </summary>
        private void FlowToNewLine(ref float remainingWidth, float width, Vector2 size)
        {
            float sizeX = size.x + _scrollbarThickness / 2;
            if (sizeX >= remainingWidth)
            {
                _buttonOverlapInspectorView += size.y + 2;
                remainingWidth = width - sizeX;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }
            else
            {
                remainingWidth -= sizeX;
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
            // We can only make an educated guess for the correct width.
            if (_buttonBarWidth == -1)
            {
                _buttonBarWidth = (position.width * 2) / 3 - _scrollbarThickness;
            }

            RenderButtonBars(_buttonBarWidth, target, out fontIndex, out collapse, out focusCode);

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

        private void RenderButtonBars(float width, BurstCompileTarget target, out int fontIndex, out bool collapse, out bool focus)
        {
            var remainingWidth = width;
            GUILayout.BeginHorizontal();

            // First button should never call beginHorizontal().
            remainingWidth -= (EditorStyles.popup.CalcSize(_contentDisasm).x + _scrollbarThickness / 2f);

            EditorGUI.BeginDisabledGroup(target.DisassemblyKind == DisassemblyKind.IRPassAnalysis);

            _assemblyKind = (AssemblyOptions)EditorGUILayout.EnumPopup(GUIContent.none, _assemblyKind, DisplayAssemblyKind, true);

            EditorGUI.EndDisabledGroup();

            FlowToNewLine(ref remainingWidth, width, EditorStyles.popup.CalcSize(_contentBranchLines));
            // Reversed "logic" to match the array of options, which has "positive" case on idx 0.
            _safetyChecks = EditorGUILayout.Popup(_safetyChecks ? 0 : 1, _safetyCheckOptions) == 0;

            EditorGUI.BeginDisabledGroup(!target.IsValidTarget);

            GUIContent targetContent = _contentsTarget[(int)_targetCpu];
            FlowToNewLine(ref remainingWidth, width, EditorStyles.popup.CalcSize(targetContent));
            _targetCpu = (BurstTargetCpu)LabeledPopup.Popup((int)_targetCpu, targetContent, TargetCpuNames);

            GUIContent fontSizeContent = _contentsFontSize[fontSizeIndex];
            FlowToNewLine(ref remainingWidth, width, EditorStyles.popup.CalcSize(fontSizeContent));
            fontIndex = LabeledPopup.Popup(fontSizeIndex, fontSizeContent, _fontSizesText);

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!IsEnhanced((AssemblyOptions)_assemblyKind) || !SupportsEnhancedRendering || _isCompileError);

            FlowToNewLine(ref remainingWidth, width, EditorStyles.miniButton.CalcSize(_contentCollapseToCode));
            focus = GUILayout.Button(_contentCollapseToCode, EditorStyles.miniButton);

            FlowToNewLine(ref remainingWidth, width, EditorStyles.miniButton.CalcSize(_contentExpandAll));
            collapse = GUILayout.Button(_contentExpandAll, EditorStyles.miniButton);

            FlowToNewLine(ref remainingWidth, width, EditorStyles.popup.CalcSize(_contentBranchLines));
            _showBranchMarkers = EditorGUILayout.Popup(Convert.ToInt32(_showBranchMarkers), _branchMarkerOptions) == 1;

            int smellTestIdx = Convert.ToInt32(_smellTest);
            GUIContent smellTestContent = _contentsSmellTest[smellTestIdx];
            FlowToNewLine(ref remainingWidth, width, EditorStyles.popup.CalcSize(smellTestContent));
            _smellTest = LabeledPopup.Popup(smellTestIdx, smellTestContent, SIMDSmellTest) == 1;

            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();

            _oldDisasmKind = _disasmKind;
            _disasmKind = (DisassemblyKind)GUILayout.Toolbar((int)_disasmKind, DisassemblyKindNames, _toolbarStyleOptions);
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
                    _searchBarVisible = true;
                    _searchFieldAssembly?.SetFocus();
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
                    case KeyboardOperation.Escape:
                        if (_searchFieldAssembly != null && _searchFieldAssembly.HasFocus() && _searchFilterAssembly == "")
                        {
                            _searchBarVisible = false;
                            evt.Use();
                        }
                        break;
                    case KeyboardOperation.Enter:
                        if (_searchFieldAssembly != null && _searchFieldAssembly.HasFocus())
                        {
                            _textArea.NextSearchHit(evt.shift, workingArea);
                            saveSearchFieldFromEvent = true;
                            evt.Use();
                        }
                        break;
                }
            }
        }

        private void RenderCompileTargetsFilters(float width)
        {
            if (Event.current.type == EventType.Layout)
            {
                Vector2 unityToggleSize = EditorStyles.toggle.CalcSize(_contentShowUnityNamespaceJobs);
                Vector2 dotsToggleSize = EditorStyles.toggle.CalcSize(_contentShowDOTSGeneratedJobs);

                _flowCompileTogglesToNewLine = (unityToggleSize.x + dotsToggleSize.x + _scrollbarThickness) > width;
            }

            var newShowUnityTests = _showUnityNamespaceJobs;
            var newShowDOTSGeneratedJobs = _showDOTSGeneratedJobs;
            if (_flowCompileTogglesToNewLine)
            {
                GUILayout.BeginHorizontal();
                newShowUnityTests = GUILayout.Toggle(_showUnityNamespaceJobs, _contentShowUnityNamespaceJobs);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                newShowDOTSGeneratedJobs = GUILayout.Toggle(_showDOTSGeneratedJobs, _contentShowDOTSGeneratedJobs);
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                newShowUnityTests = GUILayout.Toggle(_showUnityNamespaceJobs, _contentShowUnityNamespaceJobs);
                newShowDOTSGeneratedJobs = GUILayout.Toggle(_showDOTSGeneratedJobs, _contentShowDOTSGeneratedJobs);
                GUILayout.EndHorizontal();
            }

            if (newShowUnityTests != _showUnityNamespaceJobs || newShowDOTSGeneratedJobs != _showDOTSGeneratedJobs)
            {
                _showDOTSGeneratedJobs = newShowDOTSGeneratedJobs;
                _showUnityNamespaceJobs = newShowUnityTests;
                _treeView.Reload();
            }

            // Handle and render search filter:
            var newFilter = _searchFieldJobs.OnGUI(_searchFilterJobs);
            if (newFilter != _searchFilterJobs)
            {
                _searchFilterJobs = newFilter;
                _treeView.Reload();
            }
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

        private void RenderBurstJobMenu()
        {
            GUILayout.BeginVertical(GUILayout.Width(position.width / 3));

            // Render Treeview showing burst targets:
            GUILayout.Label("Compile Targets", EditorStyles.boldLabel);

            // Dummy element to retrieve the width of the compile targets panel.
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (Event.current.type == EventType.Repaint)
            {
                _compileTargetsPanelWidth = GUILayoutUtility.GetLastRect().width - _scrollbarThickness;
            }

            RenderCompileTargetsFilters(_compileTargetsPanelWidth);

            // Does not give proper rect during layout event.
            _inspectorView = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            _treeView.OnGUI(_inspectorView);

            GUILayout.EndVertical();
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

        public void OnGUI()
        {
            if (!_initialized)
            {
                RenderLoading();
                return;
            }
            // used to give hot control to inspector when a mouseDown event has happened.
            // This way we can register a mouseUp happening outside inspector.
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            // Make sure that editor options are synchronized
            BurstEditorOptions.EnsureSynchronized();

            if (_fontSizesText == null)
            {
                _fontSizesText = new string[FontSizes.Length];
                for (var i = 0; i < FontSizes.Length; ++i) _fontSizesText[i] = FontSizes[i].ToString();
            }

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

            GUILayout.BeginHorizontal();

            // SplitterGUILayout.BeginHorizontalSplit is internal in Unity but we don't have much choice
            SplitterGUILayout.BeginHorizontalSplit(TreeViewSplitterState);

            RenderBurstJobMenu();

            GUILayout.BeginVertical();

            var selection = _treeView.GetSelection();
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

                // Guard against _textArea being used, as the assembly isn't ready yet.
                // Have to test against event so it cannot finish between a Layout event and Repaint event;
                // this is necessary as we cannot alter GUI between these events.
                if (_target.IsValidTarget && (_target.IsLoading || (_target.JustLoaded && Event.current.type != EventType.Layout)))
                {
                    RenderLoading();

                    // Need to close the splits we opened.
                    GUILayout.EndVertical();
                    SplitterGUILayout.EndHorizontalSplit();
                    GUILayout.EndHorizontal();
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

                    HandleHorizontalFocus(
                        _inspectorView.width == 1f ? _buttonBarWidth : _inspectorView.width,
                        shouldSetupText,
                        isTextFormatted
                    );

                    // Fixing lastRectSize to actually be size of scroll view
                    _inspectorView.position = _scrollPos;
                    _inspectorView.width = position.width - (_inspectorView.width + _scrollbarThickness);
                    _inspectorView.height -= (_buttonOverlapInspectorView + 4); //+4 for alignment.
                    if (_searchBarVisible) _inspectorView.height -= EditorStyles.searchField.CalcHeight(GUIContent.none, 2); // 2 is just arbitrary, as the width does not alter height

                    // repaint indicate end of frame, so we can alter width for menu items to new correct.
                    if (Event.current.type == EventType.Repaint)
                    {
                        _buttonBarWidth = _inspectorView.width - _scrollbarThickness;
                    }

                    // Do search if we did not try and find assembly and we were actually going to do a search.
                    if (_focusTargetJob && TryFocusJobInAssemblyView(ref _inspectorView, shouldSetupText, _target))
                    {
                        _scrollPos.y = _inspectorView.y - _textArea.fontHeight*10;
                    }

                    _scrollPos = GUILayout.BeginScrollView(_scrollPos, true, true);

                    if (Event.current.type != EventType.Layout) // we always want mouse position feedback
                    {
                        _textArea.Interact(_inspectorView, Event.current.type);
                    }

                    // Set up search information if it is happening.
                    Regex regx = default;
                    SearchCriteria sc = default;
                    var doSearch = _searchBarVisible && _searchFilterAssembly != "";
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
                    }

                    if (_leftClicked)
                    {
                        GenericMenu menu = new GenericMenu();

                        menu.AddItem(EditorGUIUtility.TrTextContent("Copy Selection"), false, _textArea.DoSelectionCopy);
                        menu.AddItem(EditorGUIUtility.TrTextContent("Copy Color Tags"), _textArea.CopyColorTags, _textArea.ChangeCopyMode);
                        menu.AddItem(EditorGUIUtility.TrTextContent("Select All"), false, _textArea.SelectAll);
                        menu.AddItem(EditorGUIUtility.TrTextContent($"Find in {DisassemblyKindNames[(int)_disasmKind]}"), _searchBarVisible, EnableDisableSearchBar);
                        menu.ShowAsContext();

                        _leftClicked = false;
                    }

                    GUILayout.EndScrollView();

                    if (_searchBarVisible)
                    {
                        if (_searchFieldAssembly == null)
                        {
                            _searchFieldAssembly = new SearchField();
                            _searchFieldAssembly.autoSetFocusOnFindCommand = false;
                        }

                        int hitnumbers = _textArea.NrSearchHits > 0 ? _textArea.ActiveSearchNr + 1 : 0;
                        var hitNumberContent = new GUIContent("    " + hitnumbers + " of " + _textArea.NrSearchHits + " hits");

                        GUILayout.BeginHorizontal();

                        // Makes sure that on "enter" keyboard event, the focus is not taken away from searchField.
                        if (saveSearchFieldFromEvent) GUI.FocusControl("BurstInspectorGUI");

                        string newFilterAssembly;
                        if (wrongRegx)
                        {
                            var colb = GUI.contentColor;
                            GUI.contentColor = Color.red;
                            newFilterAssembly = _searchFieldAssembly.OnGUI(_searchFilterAssembly);
                            GUI.contentColor = colb;
                        }
                        else
                        {
                            newFilterAssembly = _searchFieldAssembly.OnGUI(_searchFilterAssembly);
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

                        _doIgnoreCase = GUILayout.Toggle(_doIgnoreCase, _ignoreCase);
                        _doWholeWordMatch = GUILayout.Toggle(_doWholeWordMatch, _matchWord);
                        _doRegex = GUILayout.Toggle(_doRegex, _regexSearch);
                        GUILayout.Label(hitNumberContent);
                        if (GUILayout.Button(GUIContent.none, EditorStyles.searchFieldCancelButton))
                        {
                            _searchBarVisible = false;
                            _textArea.StopSearching();
                        }

                        GUILayout.EndHorizontal();
                    }
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

            GUILayout.EndVertical();

            SplitterGUILayout.EndHorizontalSplit();

            GUILayout.EndHorizontal();
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

        private void EnableDisableSearchBar()
        {
            _searchBarVisible = !_searchBarVisible;

            if (_searchBarVisible && _searchFieldAssembly != null)
            {
                _searchFieldAssembly.SetFocus();
            }
            else if (!_searchBarVisible)
            {
                _textArea.StopSearching();
            }
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
}
