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
        const string k_UxmlUnityTextInput = "unity-text-input";
        const string k_UxmlRenameFieldWarning = "profiler-capture-file__warning";
        const string k_UxmlRenameFieldWarningMsg = "captures-list__warning-msg";
        const string k_UxmlChangeFPSField = "profiler-capture-file__meta-data__change_fps";

        const string k_DeleteCaptureDialogTitle = "Delete Capture";
        const string k_DeleteCaptureDialogMessage = "Are you sure you want to permanently delete this profiler capture file?";
        const string k_DeleteCaptureDialogAccept = "OK";
        const string k_DeleteCaptureDialogCancel = "Cancel";

        const string k_TargetFPSMenu = "Target Frame Time/";

        readonly int m_StrLenMaxFPS;

        static readonly GUIContent k_CaptureOptionMenuItemDelete = new("Delete", "Deletes the capture file from disk.");
        static readonly GUIContent k_CaptureOptionMenuItemRename = new("Rename", "Renames the capture file on disk.");
        static readonly GUIContent k_CaptureOptionMenuItemBrowse = new("Open Folder", "Opens the folder where the capture file is located on disk.");

        // State
        readonly CaptureDataService m_CaptureDataService;
        bool m_IsLoaded;

        // View
        VisualElement m_Container;
        Label m_OpenCaptureTag;
        TextField m_RenameField;
        TextElement m_RenameFieldInputArea;
        TextField m_ChangeFPSField;
        TextElement m_ChangeFPSFieldInputArea;
        Label m_WarningMessage;

        public CaptureFileTreeItemViewController(CaptureFileModel model, CaptureDataService CaptureDataService, ScreenshotsManager screenshotsManager) :
            base(model, screenshotsManager)
        {
            m_CaptureDataService = CaptureDataService;
            m_StrLenMaxFPS = ProfilerUserSettings.k_MaximumTargetFramesPerSecond.ToString().Length;
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
                m_WarningMessage?.RemoveFromHierarchy();

            base.Dispose(disposing);
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml(k_UxmlAsset);
            if (view == null)
                throw new InvalidOperationException("Unable to create view from Uxml. Uxml must contain at least one child element.");

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
            m_RenameFieldInputArea = m_RenameField.Q<TextElement>();
            m_ChangeFPSField = view.Q<TextField>(k_UxmlChangeFPSField);
            m_ChangeFPSFieldInputArea = m_ChangeFPSField.Q<TextElement>();

            m_WarningMessage = new Label();
            m_WarningMessage.AddToClassList(k_UxmlRenameFieldWarningMsg);
        }

        bool KeyEventEnterPressedOrSimilar(KeyDownEvent evt)
        {
            return evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter ||
                evt.character == '\n' || evt.character == '\r' || evt.character == 0x10;
        }

        protected override void RefreshView()
        {
            base.RefreshView();

            Debug.Assert(Model != null);

            m_Container.AddManipulator(new ContextualMenuManipulator(binder => PopulateOpenCaptureOptionMenu(binder)));
            m_Container.RegisterCallback<MouseUpEvent>(evt =>
            {
                if ((MouseButton)evt.button == MouseButton.LeftMouse)
                {
                    OpenCapture();
                    evt.StopPropagation();
                }
            });

            m_Name.AddManipulator(new Clickable(() => RenameCapture()));
            m_FPSTarget.AddManipulator(new Clickable(() => EditCaptureFPS()));

            m_RenameField.isDelayed = true;
            m_RenameField.SetValueWithoutNotify(Model.Name);
            m_RenameField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (KeyEventEnterPressedOrSimilar(evt))
                {
                    if (!ValidateInput(m_RenameField.text))
                    {
                        // Don't allow input field to finish editing
                        // if input value is invalid
                        evt.StopImmediatePropagation();
                        m_RenameField.focusController.IgnoreEvent(evt);
                    }
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    // Undo any edits
                    m_RenameField.SetValueWithoutNotify(Model.Name);
                    ResetRenameState();
                    evt.StopImmediatePropagation();
                }
            }, TrickleDown.TrickleDown);
            m_RenameField.RegisterCallback<KeyUpEvent>(_ =>
            {
                // We validate it separately, overwise m_RenameField.text
                // will have value before key input is applied
                ValidateInput(m_RenameField.text);
            });
            m_RenameField.RegisterCallback<MouseUpEvent>(evt =>
            {
                // Block mouse events, so that it doesn't cause open when
                // we edit input field and click on it
                evt.StopImmediatePropagation();
            });
            m_RenameField.RegisterCallback<FocusOutEvent>(_ =>
            {
                // We don't validate here, as otherwise we can end up
                // in situation of invalid input and lost focus, which
                // is hard to exit (you'll need to focus and press `esc`)
                TryRename(m_RenameField.text);
                ResetRenameState();
            });

            m_ChangeFPSField.isDelayed = true;
            m_ChangeFPSField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Escape)
                {
                    // Undo any edits
                    m_ChangeFPSField.SetValueWithoutNotify(GetFramerateTarget().ToString());
                    ResetFPSChangeState();
                    evt.StopImmediatePropagation();
                }
            }, TrickleDown.TrickleDown);
            m_ChangeFPSField.RegisterCallback<MouseUpEvent>(evt =>
            {
                // Block mouse events, so that it doesn't cause open when
                // we edit input field and click on it
                evt.StopImmediatePropagation();
            });
            m_ChangeFPSField.RegisterCallback<FocusOutEvent>(evt =>
            {
                m_ChangeFPSField.text = ValidateFPSInput(m_ChangeFPSField.text);
                TryChangeFPS(m_ChangeFPSField.text);
                ResetFPSChangeState();
            });

            m_CaptureDataService.LoadedCapturesChanged += RefreshLoadedState;
            RefreshLoadedState();
        }

        void RefreshLoadedState()
        {
            View.RemoveFromClassList("profiler-capture-file__state__in-view");
            UIUtility.SetElementDisplay(m_OpenCaptureTag, false);

            if (IsLoaded)
                View.AddToClassList("profiler-capture-file__state__in-view");
        }

        void PopulateOpenCaptureOptionMenu(ContextualMenuPopulateEvent binder)
        {
            binder.menu.AppendAction(k_CaptureOptionMenuItemDelete.text, a =>
            {
                DelayedAction(DeleteCapture);
            });
            binder.menu.AppendAction(k_CaptureOptionMenuItemRename.text, a =>
            {
                DelayedAction(RenameCapture);
            });
            binder.menu.AppendAction(k_CaptureOptionMenuItemBrowse.text, a =>
            {
                BrowseCaptureFolder();
            });

            // Don't add bottleneck menus if one isn't yet loaded
            if (m_BottleneckModel == null)
                return;

            foreach (var fpsValue in BottlenecksChartViewController.k_FPSValues)
            {
                binder.menu.AppendAction(k_TargetFPSMenu + $"{fpsValue} FPS", a =>
                {
                    DoFPSChange(fpsValue);
                });
            }

            binder.menu.AppendAction(k_TargetFPSMenu + "Custom", a =>
            {
                DelayedAction(EditCaptureFPS);
            });
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
            UIUtility.SwitchVisibility(m_RenameField, m_Name);
            m_RenameField.SetValueWithoutNotify(m_Name.text);
            FocusRenameField();
        }

        void EditCaptureFPS()
        {
            UIUtility.SwitchVisibility(m_ChangeFPSField, m_FPSTarget);
            m_ChangeFPSField.SetValueWithoutNotify(GetFramerateTarget().ToString());
            FocusFPSField();
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

            if (!m_CaptureDataService.ValidateName(newCaptureName))
            {
                ShowRenameWarning("Name contains invalid characters");
                return false;
            }

            if (!m_CaptureDataService.CanRename(Model.FullPath, newCaptureName) && (Model.Name != newCaptureName))
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

            if (!m_CaptureDataService.ValidateName(newCaptureName))
                return;

            if (!m_CaptureDataService.CanRename(Model.FullPath, newCaptureName))
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

        void FocusRenameField()
        {
            // We need this because dialogs don't restore
            // EditorWindow focus, if it's a detached window
            EditorWindow.FocusWindowIfItsOpen<ProfilerWindow>();

            // Delay field re-focus so that EditorWindow has time to get focus
            DelayedAction(() => m_RenameFieldInputArea.Focus());
        }

        void FocusFPSField()
        {
            // We need this because dialogs don't restore
            // EditorWindow focus, if it's a detached window
            EditorWindow.FocusWindowIfItsOpen<ProfilerWindow>();

            // Delay field re-focus so that EditorWindow has time to get focus
            DelayedAction(() => m_ChangeFPSFieldInputArea.Focus());
        }

        void ResetRenameState()
        {
            UIUtility.SwitchVisibility(m_RenameField, m_Name, false);
            HideRenameWarning();
        }

        void ResetFPSChangeState()
        {
            UIUtility.SwitchVisibility(m_ChangeFPSField, m_FPSTarget, false);
        }

        void DelayedAction(Action action, int framesDelay = 2)
        {
            EditorCoroutineUtility.StartCoroutine(DelayedActionExecutor(action, framesDelay), this);
        }

        IEnumerator DelayedActionExecutor(Action action, int framesDelay)
        {
            for (int i = 0; i < framesDelay; i++)
                yield return null;

            action.Invoke();
        }

        void ShowRenameWarning(string message)
        {

            if (!m_RenameField.visible)
                return;

            m_RenameField.Q(k_UxmlUnityTextInput).AddToClassList(k_UxmlRenameFieldWarning);

            var viewRoot = m_RenameField.panel.visualTree.Q("captures-list-view");
            var bounds = m_RenameField.ChangeCoordinatesTo(viewRoot, m_RenameField.contentRect);
            m_WarningMessage.RemoveFromHierarchy();
            viewRoot.Add(m_WarningMessage);
            m_WarningMessage.style.left = bounds.xMin;
            m_WarningMessage.style.top = bounds.yMax + 4;
            m_WarningMessage.text = message;
        }

        void HideRenameWarning()
        {
            m_RenameField.Q(k_UxmlUnityTextInput).RemoveFromClassList(k_UxmlRenameFieldWarning);
            m_WarningMessage.RemoveFromHierarchy();
        }
    }
}
