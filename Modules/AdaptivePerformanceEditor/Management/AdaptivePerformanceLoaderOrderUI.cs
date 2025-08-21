// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;

using UnityEditorInternal;
using UnityEngine;

using UnityEditor.AdaptivePerformance.Editor.Metadata;

namespace UnityEditor.AdaptivePerformance.Editor
{
    internal interface IAdaptivePerformanceLoaderOrderManager
    {
        List<AdaptivePerformanceLoaderInfo> AssignedLoaders { get; }
        List<AdaptivePerformanceLoaderInfo> UnassignedLoaders { get; }

        void AssignLoader(AdaptivePerformanceLoaderInfo assignedInfo);
        void UnassignLoader(AdaptivePerformanceLoaderInfo unassignedInfo);
        void Update();
    }

    internal class AdaptivePerformanceLoaderOrderUI
    {
        struct LoaderInformation
        {
            public string packageName;
            public string packageId;
            public string loaderName;
            public string loaderType;
            public string licenseURL;
            public string isDefaultPlatformProvider;
            public bool toggled;
            public bool stateChanged;
            public bool isDeprecated;
        }

        internal struct Content
        {
            public static readonly string k_AtNoLoaderInstance = L10n.Tr("There are no Adaptive Performance providers available for this platform.");
            public static readonly string k_LicenseText = L10n.Tr("By clicking the checkbox to install a provider, you acknowledge that you have read and agreed to the terms and conditions found under \"View licenses.\"");
            public static readonly GUIContent k_LicenseViewText = new GUIContent(L10n.Tr("View licenses"));
            public static readonly GUIContent k_LoaderUITitle = EditorGUIUtility.TrTextContent(L10n.Tr("Providers"));
            public static readonly GUIContent k_HelpContent = new GUIContent("", EditorGUIUtility.IconContent("_Help@2x").image, L10n.Tr("Selecting a provider installs that providers package. Packages can be managed through the Package Manager."));
        }

        private List<LoaderInformation> m_LoaderMetadata = null;

        ReorderableList m_OrderedList = null;

        public BuildTargetGroup CurrentBuildTargetGroup
        {
            get;
            set;
        }

        internal AdaptivePerformanceLoaderOrderUI()
        {
        }

        void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            var li = m_LoaderMetadata[index];

            li.toggled = AdaptivePerformancePackageMetadataStore.IsLoaderAssigned(li.loaderType, CurrentBuildTargetGroup);
            bool preToggledState = li.toggled;
            rect.width *= 0.51f;
            EditorGUIUtility.labelWidth = 250;
            li.toggled = EditorGUI.Toggle(rect, li.loaderName+(li.isDeprecated?" (deprecated)" : string.Empty), preToggledState);
            if (li.toggled != preToggledState)
            {
                li.stateChanged = true;
                m_LoaderMetadata[index] = li;
            }

            //if (li.licenseURL != null)
            //    DisplayLink(Content.k_LicenseViewText, new Uri(li.licenseURL), 2, 80, rect);
        }

        private void DisplayLink(GUIContent text, Uri link, float leftMargin, float width, Rect rect)
        {
            var labelStyle = EditorStyles.linkLabel;
            var uriRect = rect;

            uriRect.x += uriRect.width * 2;
            uriRect.y -= 1;
            uriRect.x -= width + leftMargin;
            uriRect.width = width;

            if (GUI.Button(uriRect, text, labelStyle))
            {
                System.Diagnostics.Process.Start(link.AbsoluteUri);
            }
            EditorGUIUtility.AddCursorRect(uriRect, MouseCursor.Link);
            EditorGUI.DrawRect(new Rect(uriRect.x + 2, uriRect.y + uriRect.height - 3, uriRect.width - 3, 1), labelStyle.normal.textColor);
        }

        float GetElementHeight(int index)
        {
            return m_OrderedList.elementHeight;
        }

        internal bool OnGUI(BuildTargetGroup buildTargetGroup)
        {
            var settings = AdaptivePerformanceGeneralSettingsPerBuildTarget.AdaptivePerformanceGeneralSettingsForBuildTarget(buildTargetGroup);

            if (buildTargetGroup != CurrentBuildTargetGroup || m_LoaderMetadata == null)
            {
                CurrentBuildTargetGroup = buildTargetGroup;

                if (m_LoaderMetadata == null)
                    m_LoaderMetadata = new List<LoaderInformation>();
                else
                    m_LoaderMetadata.Clear();

                foreach (var pmd in AdaptivePerformancePackageMetadataStore.GetLoadersForBuildTarget(buildTargetGroup))
                {
                    m_LoaderMetadata.Add(new LoaderInformation() {
                        packageName = pmd.packageName,
                        packageId = pmd.packageId,
                        loaderName = pmd.loaderName,
                        loaderType = pmd.loaderType,
                        licenseURL = pmd.licenseURL,
                        isDefaultPlatformProvider = pmd.isDefaultPlatformProvider,
                        toggled = AdaptivePerformancePackageMetadataStore.IsLoaderAssigned(pmd.loaderType, buildTargetGroup),
                        isDeprecated = pmd.isDeprecated
                    });
                }

                if (settings != null)
                {
                    LoaderInformation li;
                    for (int i = 0; i < m_LoaderMetadata.Count; i++)
                    {
                        li = m_LoaderMetadata[i];
                        if (AdaptivePerformancePackageMetadataStore.IsLoaderAssigned(settings.AssignedSettings, li.loaderType))
                        {
                            li.toggled = true;
                            m_LoaderMetadata[i] = li;
                            break;
                        }
                    }
                }

                m_OrderedList = new ReorderableList(m_LoaderMetadata, typeof(LoaderInformation), false, true, false, false);
                m_OrderedList.drawHeaderCallback = (rect) =>
                {
                    var labelSize = EditorStyles.label.CalcSize(Content.k_LoaderUITitle);
                    var labelRect = new Rect(rect);
                    labelRect.width = labelSize.x;

                    labelSize = EditorStyles.label.CalcSize(Content.k_HelpContent);
                    var imageRect = new Rect(rect);
                    imageRect.xMin = labelRect.xMax + 1;
                    imageRect.width = labelSize.x;

                    EditorGUI.LabelField(labelRect, Content.k_LoaderUITitle, EditorStyles.label);
                    EditorGUI.LabelField(imageRect, Content.k_HelpContent);
                };

                m_OrderedList.drawElementCallback = (rect, index, isActive, isFocused) => DrawElementCallback(rect, index, isActive, isFocused);
                m_OrderedList.elementHeightCallback = (index) => GetElementHeight(index);
                m_OrderedList.drawFooterCallback = (rect) =>
                {
                    var status = AdaptivePerformancePackageMetadataStore.GetCurrentStatusDisplayText();
                    GUI.Label(rect, status, EditorStyles.label);
                };
            }

            if (m_LoaderMetadata == null || m_LoaderMetadata.Count == 0)
            {
                EditorGUILayout.HelpBox(Content.k_AtNoLoaderInstance, MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(Content.k_LicenseText, MessageType.Info);
                EditorGUILayout.Space();

                m_OrderedList.DoLayoutList();
                if (settings != null)
                {
                    LoaderInformation li;
                    for (int i = 0; i < m_LoaderMetadata.Count; i++)
                    {
                        li = m_LoaderMetadata[i];
                        if (li.stateChanged)
                        {
                            if (li.toggled)
                            {
                                AdaptivePerformancePackageMetadataStore.InstallPackageAndAssignLoaderForBuildTarget(li.packageId, li.loaderType, buildTargetGroup);
                            }
                            else
                            {
                                AdaptivePerformancePackageMetadataStore.RemoveLoader(settings.AssignedSettings, li.loaderType, buildTargetGroup);
                            }
                            li.stateChanged = false;
                            m_LoaderMetadata[i] = li;
                        }
                    }
                }
            }

            return false;
        }

        internal void RemoveAllLoadersFromTargetSetting(BuildTargetGroup buildTargetGroup)
        {
            if (m_LoaderMetadata == null) return;
            var settings = AdaptivePerformanceGeneralSettingsPerBuildTarget.AdaptivePerformanceGeneralSettingsForBuildTarget(buildTargetGroup);
            for (int i = 0; i < m_LoaderMetadata.Count; i++)
            {
                var li = m_LoaderMetadata[i];
                AdaptivePerformancePackageMetadataStore.RemoveLoader(settings.AssignedSettings, li.loaderType, buildTargetGroup);
            }
            m_LoaderMetadata = null;
        }
    }
}
