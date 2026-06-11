// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    abstract class SummaryViewController : ViewController, TopMarkersViewController.IResponder
    {
        const string k_ProfileAnalyzerPackageName = "com.unity.performance.profile-analyzer";
        static readonly string k_ProfileAnalyzerMenuItemPath = "Window/Analysis/Profile Analyzer";

        // Model.
        protected Range m_SelectedRange;
        protected readonly IProfilerCaptureDataService m_DataService;
        protected readonly IProfilerPersistentSettingsService m_SettingsService;
        protected readonly ProfilerWindow m_ProfilerWindow;
        protected readonly IResponder m_Responder;
        protected readonly IDetailsElementBinder m_DetailsBinder;
        protected CancellationTokenSource m_BuildModelCancellation;

        // View.
        Button m_OpenProfileAnalyzerButton;
        protected VisualElement m_TopSection;
        protected VisualElement m_BottlenecksContainer;
        protected VisualElement m_SystemsImpactContainer;
        protected VisualElement m_FrameTimesContainer;
        protected VisualElement m_AllocationsContainer;
        protected Label m_NoDataLabel;

        // Children.
        protected SystemsImpactViewController m_SystemsImpactViewController;
        protected FrameTimesSectionViewController m_FrameTimesSectionViewController;
        protected AllocationsSectionViewController m_AllocationsSectionViewController;

        public SummaryViewController(
            IProfilerCaptureDataService dataService,
            IProfilerPersistentSettingsService settingsService,
            ProfilerWindow profilerWindow,
            IResponder responder,
            IDetailsElementBinder detailsBinder)
        {
            m_DataService = dataService;
            m_SettingsService = settingsService;
            m_ProfilerWindow = profilerWindow;
            m_Responder = responder;
            m_DetailsBinder = detailsBinder;
        }

        protected void DeferIfNotCancelled(Action action, CancellationToken cancellationToken)
        {
            View.schedule.Execute(() =>
            {
                if (cancellationToken.IsCancellationRequested) return;
                action();
            });
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("SummaryView.uxml") ?? throw new InvalidViewDefinedInUxmlException();
            const string k_UssClass_Dark = "summary-view__dark";
            const string k_UssClass_Light = "summary-view__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            m_OpenProfileAnalyzerButton.clicked += OpenProfileAnalyzer;
        }

        internal static void OpenProfileAnalyzer()
        {
            if (UnityEditor.PackageManager.PackageInfo.IsPackageRegistered(k_ProfileAnalyzerPackageName))
                EditorApplication.ExecuteMenuItem(k_ProfileAnalyzerMenuItemPath);
            else
                UnityEditor.PackageManager.UI.Window.Open(k_ProfileAnalyzerPackageName);
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_OpenProfileAnalyzerButton = view.Q<Button>("summary-view__open-profile-analyzer-button");
            m_TopSection = view.Q<VisualElement>("summary-view__top-section");
            m_BottlenecksContainer = view.Q<VisualElement>("summary-view__bottlenecks-container");
            m_SystemsImpactContainer = view.Q<VisualElement>("summary-view__systems-impact-container");
            m_FrameTimesContainer = view.Q<VisualElement>("summary-view__frame-times-container");
            m_AllocationsContainer = view.Q<VisualElement>("summary-view__allocations-container");
            m_NoDataLabel = view.Q<Label>("summary-view__no-data-label");
        }

        void TopMarkersViewController.IResponder.OnMarkerSelected(
            TopMarkersModel.Marker marker,
            TopMarkersViewController.Action action)
        {
            m_Responder?.OnTopMarkerSelected(marker, action);
        }

        protected async void ReloadDataAsync(Range range, Action<IDetailsProvider> onDetailsProviderReady = null)
        {
            // Cancel previous model build. Do not dispose here — the in-flight task may still
            // access the token after cancellation. Overwrite the reference and let the GC collect
            // the old CancellationTokenSource once the task releases it.
            m_BuildModelCancellation?.Cancel();
            m_BuildModelCancellation = new CancellationTokenSource();

            ShowContentViewsAndHideNoDataView();
            ShowContentActivityIndicators();

            var cancellationToken = m_BuildModelCancellation.Token;
            var success = false;
            try
            {
                await BuildModelAsync(range, cancellationToken);
                success = true;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling. Don't report error.
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                // Only update the view if this is the current builder and it wasn't cancelled.
                var isCurrentBuilder = m_BuildModelCancellation?.Token == cancellationToken;
                if (!success)
                {
                    if (isCurrentBuilder)
                    {
                        HideContentActivityIndicators();
                        HideContentViewsAndShowNoDataView();
                        if (onDetailsProviderReady != null)
                            DeferIfNotCancelled(() => onDetailsProviderReady(null), cancellationToken);
                    }
                }
                else if (isCurrentBuilder)
                {
                    HideContentActivityIndicators();

                    // Bind the details provider to support right-click context menu.
                    var detailsProvider = CreateDetailsProvider(range);
                    m_DetailsBinder.BindDetailsElement(View, detailsProvider);

                    // Invoke callback to notify caller that details provider is ready.
                    if (onDetailsProviderReady != null)
                        DeferIfNotCancelled(() => onDetailsProviderReady(detailsProvider), cancellationToken);
                }
            }
        }

        private void ShowContentViewsAndHideNoDataView()
        {
            SetContentViewsVisible(true);
        }

        protected void HideContentViewsAndShowNoDataView()
        {
            SetContentViewsVisible(false);
        }

        private void SetContentViewsVisible(bool visible)
        {
            UIUtility.SetElementDisplay(m_OpenProfileAnalyzerButton, visible);
            UIUtility.SetElementDisplay(m_TopSection, visible);
            UIUtility.SetElementDisplay(m_BottlenecksContainer, visible);
            UIUtility.SetElementDisplay(m_SystemsImpactContainer, visible);
            UIUtility.SetElementDisplay(m_FrameTimesContainer, visible);
            UIUtility.SetElementDisplay(m_AllocationsContainer, visible);
            UIUtility.SetElementDisplay(m_NoDataLabel, !visible);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_BuildModelCancellation?.Cancel();
                m_BuildModelCancellation?.Dispose();
                m_BuildModelCancellation = null;
                m_DetailsBinder.UnbindDetailsElement(View);
            }

            base.Dispose(disposing);
        }

        protected abstract Task BuildModelAsync(Range range, CancellationToken cancellationToken);
        protected abstract IDetailsProvider CreateDetailsProvider(Range range);
        protected abstract void ShowContentActivityIndicators();
        protected abstract void HideContentActivityIndicators();

        public interface IResponder
        {
            void OnTopMarkerSelected(TopMarkersModel.Marker marker, TopMarkersViewController.Action action);
        }
    }
}
