// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    internal class CaptureFileTreeItemViewController : CaptureFileBaseViewController
    {
        const string k_UxmlAsset = "CaptureFileTreeItemView.uxml";
        const string k_UssClass_Dark = "profiler-captures__dark";
        const string k_UssClass_Light = "profiler-captures__light";

        const string k_UxmlOpenButton = "profiler-capture-file__button";
        const string k_UxmlOpenCaptureTag = "profiler-capture-file__tag";
        const string k_UxmlRenameField = "profiler-capture-file__meta-data__rename";
        const string k_UxmlRenameFieldWarning = "profiler-capture-file__warning";
        const string k_UxmlChangeFPSField = "profiler-capture-file__meta-data__change_fps";
        const string k_UxmlMenuButton = "profiler-capture-file__menu-button";
        const string k_UssMenuButtonActive = "profiler-capture-file__menu-button--active";

        const string k_DeleteCaptureDialogTitle = "Delete Capture";
        const string k_DeleteCaptureDialogMessage = "Are you sure you want to permanently delete this profiler capture file?";
        const string k_DeleteCaptureDialogAccept = "OK";
        const string k_DeleteCaptureDialogCancel = "Cancel";

        const string k_TargetFPSMenu = "Target Frame Time/";
        const int k_MinTimeBetweenClicksMs = 100;
        const int k_WarningMessageVertOffset = -25;

        readonly int m_StrLenMaxFPS;

        static readonly GUIContent k_CaptureOptionMenuItemDelete = new("Delete", "Deletes the capture file from disk.");
        static readonly GUIContent k_CaptureOptionMenuItemRename = new("Rename", "Renames the capture file on disk.");
        static readonly GUIContent k_CaptureOptionMenuItemBrowse = new("Open Folder", "Opens the folder where the capture file is located on disk.");

        // State
        readonly CaptureDataService m_CaptureDataService;
        readonly ProfilerWindow m_ProfilerWindow;
        readonly Action<CaptureFileTreeItemViewController> m_OnEditStarted;
        long m_LastClickTimestamp;
        bool m_IsLoaded;

        // View
        VisualElement m_Container;
        VisualElement m_MenuButton;
        Label m_OpenCaptureTag;
        TextField m_RenameField;
        VisualElement m_RenameFieldTextInput;
        TextField m_ChangeFPSField;
        readonly Label m_WarningMessage;

        // The two inline editors this row owns. Both behave identically; see InlineTextEditor.
        InlineTextEditor m_RenameEditor;
        InlineTextEditor m_FPSEditor;

        public CaptureFileTreeItemViewController(CaptureFileModel model, CaptureDataService captureDataService, ScreenshotsManager screenshotsManager, ProfilerWindow profilerWindow, Label warningLabel, Action<CaptureFileTreeItemViewController> onEditStarted = null) :
            base(model, screenshotsManager)
        {
            m_CaptureDataService = captureDataService;
            m_StrLenMaxFPS = ProfilerUserSettings.k_MaximumTargetFramesPerSecond.ToString().Length;
            m_ProfilerWindow = profilerWindow;
            m_WarningMessage = warningLabel;
            m_OnEditStarted = onEditStarted;
            m_CaptureDataService.LoadedCapturesChanged += RefreshLoadedState;
        }

        public bool IsLoaded
        {
            get => m_IsLoaded;
            set
            {
                if (m_IsLoaded == value)
                    return;

                m_IsLoaded = value;
                if (IsViewLoaded)
                    RefreshLoadedState();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_CaptureDataService.LoadedCapturesChanged -= RefreshLoadedState;
                m_RenameEditor?.Detach();
                m_FPSEditor?.Detach();
                if (m_RenameField is { visible: true })
                {
                    HideRenameWarning();
                }
            }

            base.Dispose(disposing);
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml(k_UxmlAsset);
            if (view == null)
                throw new InvalidOperationException($"Unable to create view from built-in Uxml '{k_UxmlAsset}'. See the preceding error for details.");

            var themeUssClass = EditorGUIUtility.isProSkin ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void GatherReferencesInView(VisualElement view)
        {
            base.GatherReferencesInView(view);
            m_Container = view.Q(k_UxmlOpenButton);
            m_OpenCaptureTag = view.Q<Label>(k_UxmlOpenCaptureTag);
            m_RenameField = view.Q<TextField>(k_UxmlRenameField);
            m_RenameFieldTextInput = m_RenameField?.Q(TextField.textInputUssName);
            m_ChangeFPSField = view.Q<TextField>(k_UxmlChangeFPSField);
            m_MenuButton = view.Q(k_UxmlMenuButton);
        }

        protected override void ViewLoaded()
        {
            // Event callbacks must be registered exactly once. RefreshView can run multiple times
            // over the view controller's life (e.g. DoFPSChange calls it), so registering here would
            // otherwise accumulate duplicate handlers on every refresh.
            RegisterViewCallbacks();

            // Base implementation performs the first RefreshView.
            base.ViewLoaded();
        }

        void RegisterViewCallbacks()
        {
            m_Container.RegisterCallback<MouseUpEvent>(evt =>
            {
                if ((MouseButton)evt.button == MouseButton.RightMouse)
                {
                    ShowKebabMenu(new Rect(evt.mousePosition, Vector2.zero));
                    evt.StopPropagation();
                    return;
                }

                if ((MouseButton)evt.button != MouseButton.LeftMouse)
                    return;

                var msSinceLastClick = Math.Abs(m_LastClickTimestamp - evt.timestamp);
                m_LastClickTimestamp = evt.timestamp;

                // Do nothing if we're mid-load, or if it's likely a double input happened, else we can buffer multiple
                // load commands that will then run sequentially (UUM-133429)
                if (m_ProfilerWindow.IsCurrentlyLoadingFile || msSinceLastClick < k_MinTimeBetweenClicksMs)
                    return;

                OpenCapture();
                evt.StopPropagation();
            });

            m_MenuButton.RegisterCallback<ClickEvent>(evt =>
            {
                ShowKebabMenu();
                evt.StopPropagation();
            });
            m_MenuButton.RegisterCallback<MouseUpEvent>(evt =>
            {
                evt.StopImmediatePropagation();
            });

            // Both fields are inline editors with identical behaviour; InlineTextEditor owns the
            // commit/discard/select/scroll-cancel rules so that the two cannot drift apart.
            m_RenameEditor = new InlineTextEditor(
                m_RenameField,
                m_Name,
                readValue: () => Model.Name,
                commit: TryRename,
                validate: ValidateInput,
                closed: HideRenameWarning);

            m_FPSEditor = new InlineTextEditor(
                m_ChangeFPSField,
                m_FPSTarget,
                readValue: () => GetFramerateTarget().ToString(),
                commit: TryChangeFPS,
                sanitize: ValidateFPSInput);
        }

        protected override void RefreshView()
        {
            base.RefreshView();

            Debug.Assert(Model != null);

            m_RenameField.SetValueWithoutNotify(Model.Name);

            RefreshLoadedState();
        }

        void RefreshLoadedState()
        {
            View.RemoveFromClassList("profiler-capture-file__state__in-view");
            UIUtility.SetElementDisplay(m_OpenCaptureTag, false);

            // don't go via setter else we'll recurse
            m_IsLoaded = m_ProfilerWindow.CaptureFileIsOpen(Model.FullPath);

            if (IsLoaded)
                View.AddToClassList("profiler-capture-file__state__in-view");
        }

        void ShowKebabMenu(Rect? dropdownPosition = null)
        {
            var menu = new GenericMenu();

            menu.AddItem(k_CaptureOptionMenuItemDelete, false, () =>
            {
                DelayedAction(DeleteCapture);
            });
            menu.AddItem(k_CaptureOptionMenuItemRename, false, () =>
            {
                RenameCapture();
            });
            menu.AddItem(k_CaptureOptionMenuItemBrowse, false, () =>
            {
                BrowseCaptureFolder();
            });

            // Don't add bottleneck menus if one isn't yet loaded
            if (m_BottleneckModel != null)
            {
                foreach (var fpsValue in BottlenecksChartViewController.k_FPSValues)
                {
                    menu.AddItem(new GUIContent(k_TargetFPSMenu + $"{fpsValue} FPS"), false, () =>
                    {
                        DoFPSChange(fpsValue);
                    });
                }

                menu.AddItem(new GUIContent(k_TargetFPSMenu + "Custom"), false, () =>
                {
                    EditCaptureFPS();
                });
            }

            m_MenuButton.AddToClassList(k_UssMenuButtonActive);
            menu.DropDown(dropdownPosition ?? m_MenuButton.worldBound);
            m_MenuButton.schedule.Execute(() => m_MenuButton.RemoveFromClassList(k_UssMenuButtonActive));
        }

        void DoFPSChange(int fpsValue)
        {
            if (m_BottleneckModel.ChangeFPSTarget(fpsValue))
                RefreshView();
        }

        void BrowseCaptureFolder()
        {
            ScreenshotRefresh();
            EditorUtility.RevealInFinder(Model.FullPath);
        }

        void OpenCapture()
        {
            var keepExisting = Event.current.shift;
            // Delay opening so any unfocus events have a chance to execute
            DelayedAction(() => m_CaptureDataService.Load(keepExisting, Model.FullPath));
        }

        void RenameCapture()
        {
            if (!TryRunWhenAttached(RenameCapture))
                return;

            BeginEdit(m_RenameEditor);
        }

        void BeginEdit(InlineTextEditor fieldEdit)
        {
            // Only one field may be edited at a time. Opening a kebab menu moves focus to a
            // native menu window, so a row that is already editing never receives a FocusOutEvent
            // to close itself; the list stops the previously editing row, and the calls below stop
            // this row's other field.
            //
            // Never cancel the field being opened. Closing it schedules a FocusOutEvent that is not
            // always dispatched in the same frame, and Open clears the guard that would have
            // suppressed it - so that stale blur would commit and close the field just reopened.
            // Selecting the same command twice (kebab -> Rename, then Rename again) is the path
            // that reaches this. Reopening an already-open field is safe on its own: nothing is
            // hidden, so no blur is generated.
            m_OnEditStarted?.Invoke(this);

            if (fieldEdit != m_RenameEditor)
                m_RenameEditor?.Cancel();

            if (fieldEdit != m_FPSEditor)
                m_FPSEditor?.Cancel();

            // Dialogs and menus do not restore focus to a detached EditorWindow on their own. This
            // only asks for it - the platform finishes handing focus over asynchronously, which the
            // edit field waits out by retrying.
            EditorWindow.FocusWindowIfItsOpen<ProfilerWindow>();

            fieldEdit?.Open();
        }

        // Abandon whichever edit this row has open, without applying it. Used by the list when
        // another row starts editing.
        internal void CancelActiveEdit()
        {
            m_RenameEditor?.Cancel();
            m_FPSEditor?.Cancel();
        }

        // Returns true if the view is attached and the caller may proceed now. If it is currently
        // detached, schedules the action to run once it re-attaches and returns false.
        bool TryRunWhenAttached(Action action)
        {
            if (View.panel != null)
                return true;

            View.RegisterCallbackOnce<AttachToPanelEvent>(_ => action());
            return false;
        }

        void EditCaptureFPS()
        {
            if (!TryRunWhenAttached(EditCaptureFPS))
                return;

            BeginEdit(m_FPSEditor);
        }

        void DeleteCapture()
        {
            if (!EditorUtility.DisplayDialog(k_DeleteCaptureDialogTitle, k_DeleteCaptureDialogMessage, k_DeleteCaptureDialogAccept, k_DeleteCaptureDialogCancel))
                return;

            m_CaptureDataService.Delete(Model.FullPath);
        }

        bool ValidateInput(string newCaptureName)
        {
            if (string.IsNullOrEmpty(newCaptureName))
            {
                ShowRenameWarning("Name shouldn't be empty");
                return false;
            }

            if (!CaptureDataService.ValidateName(newCaptureName))
            {
                ShowRenameWarning("Name contains invalid characters");
                return false;
            }

            if (!CaptureDataService.PathLengthIsValid(Model.FullPath, newCaptureName))
            {
                ShowRenameWarning("File path is too long");
                return false;
            }

            if (!CaptureDataService.CanRename(Model.FullPath, newCaptureName) && Model.Name != newCaptureName)
            {
                ShowRenameWarning("Capture with the same name already exists");
                return false;
            }

            HideRenameWarning();
            return true;
        }

        string ValidateFPSInput(string newFPSValue)
        {
            // Get rid of non-numeric chars
            var fpsString = Regex.Replace(newFPSValue, @"[^0-9]", "");

            // Easiest way to avoid potential int parsing awkwardness
            if (fpsString.Length > m_StrLenMaxFPS)
                return ProfilerUserSettings.k_MaximumTargetFramesPerSecond.ToString();

            return fpsString;
        }

        void TryRename(string newCaptureName)
        {
            if (string.IsNullOrEmpty(newCaptureName) || newCaptureName == Model.Name)
                return;

            if (!CaptureDataService.ValidateName(newCaptureName))
                return;

            if (!CaptureDataService.PathLengthIsValid(Model.FullPath, newCaptureName))
                return;

            if (!CaptureDataService.CanRename(Model.FullPath, newCaptureName))
                return;

            m_CaptureDataService.Rename(Model.FullPath, newCaptureName);
        }

        void TryChangeFPS(string newFPS)
        {
            if (string.IsNullOrEmpty(newFPS))
                return;

            int parsedFPS;
            try
            {
                parsedFPS = int.Parse(newFPS);
            }
            catch (Exception)
            {
                return;
            }

            DoFPSChange(parsedFPS);
        }

        void DelayedAction(Action action, int framesDelay = 2)
        {
            EditorCoroutineUtility.StartCoroutine(DelayedActionExecutor(action, framesDelay), this);
        }

        static IEnumerator DelayedActionExecutor(Action action, int framesDelay)
        {
            for (int i = 0; i < framesDelay; i++)
                yield return null;

            action.Invoke();
        }

        void ShowRenameWarning(string message)
        {
            if (m_RenameField == null || !m_RenameField.visible || m_WarningMessage == null)
                return;

            m_RenameFieldTextInput?.AddToClassList(k_UxmlRenameFieldWarning);

            // Position warning message relative to the panel root
            var viewRoot = m_RenameField.panel?.visualTree;
            if (viewRoot == null)
                return;

            var bounds = m_RenameField.ChangeCoordinatesTo(viewRoot, m_RenameField.contentRect);

            m_WarningMessage.style.display = DisplayStyle.Flex;
            m_WarningMessage.style.left = bounds.xMin;
            m_WarningMessage.style.top = bounds.yMax + k_WarningMessageVertOffset;
            m_WarningMessage.text = message;
            m_WarningMessage.BringToFront();
        }

        void HideRenameWarning()
        {
            m_RenameFieldTextInput?.RemoveFromClassList(k_UxmlRenameFieldWarning);
            if (m_WarningMessage != null)
                m_WarningMessage.style.display = DisplayStyle.None;
        }
    }
}
