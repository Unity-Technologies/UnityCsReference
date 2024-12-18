// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Text;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements;

/// <summary>
/// View to show progress while installing packages for preconfigured build profiles.
/// </summary>
internal class BuildProfileBootstrapView : VisualElement
{
    protected virtual string k_Uxml => "BuildProfile/UXML/BuildProfileBootstrapView.uxml";

    protected readonly Label m_PackageAddProgressLabel;

    private VisualElement m_Spinner;
    private bool m_SpinnerHasStarted = false;
    internal BuildProfileBootstrapView()
    {
        var uxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
        var uss = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
        styleSheets.Add(uss);
        uxml.CloneTree(this);
        m_PackageAddProgressLabel = this.Q<Label>("package-add-progress");
        m_Spinner = this.Q<VisualElement>("spinner");
    }
    // This call is needed separately to have the animation running.
    // Putting this in the construction somehow will not work.
    internal void StartSpinner()
    {
        if (!m_SpinnerHasStarted)
        {
            m_PackageAddProgressLabel.text = TrText.buildProfilePreparation;
            m_Spinner.AddToClassList("bp-spinner");
            m_SpinnerHasStarted = true;
        }
    }

    internal void Set(BuildProfilePackageAddInfo.ProgressEntry info)
    {
        string message = "";
        bool usePlural = info.packageCount > 1;
        switch (info.state)
        {
            case BuildProfilePackageAddInfo.ProgressState.PackageDownloading:
                message = usePlural? TrText.packagesAddDownloading : TrText.packagesAddDownloading;
                break;
            case BuildProfilePackageAddInfo.ProgressState.PackageInstalling:
                message = usePlural? TrText.packagesAddInstalling : TrText.packageAddInstalling;
                break;
            case BuildProfilePackageAddInfo.ProgressState.PackageError:
                message = string.Format(usePlural? TrText.packagesAddError : TrText.packageAddError, info.name);
                break;
            case BuildProfilePackageAddInfo.ProgressState.ConfigurationRunning:
                message = TrText.buildProfileConfiguration;
                break;
        }
        
        if(message != string.Empty)
            m_PackageAddProgressLabel.text = message;
    }
}
