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

    StringBuilder m_MessageBuilder = new();

    internal BuildProfileBootstrapView()
    {
        var uxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
        var uss = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
        styleSheets.Add(uss);
        uxml.CloneTree(this);
        m_PackageAddProgressLabel = this.Q<Label>("package-add-progress");
    }

    internal void Set(IReadOnlyList<BuildProfilePackageAddInfo.ProgressEntry> info)
    {
        m_MessageBuilder.Clear();
        m_MessageBuilder.AppendLine(TrText.bootstrapHeader);
        foreach (var entry in info)
        {
            switch (entry.state)
            {
                case BuildProfilePackageAddInfo.ProgressState.PackageStateUnknown:
                    m_MessageBuilder.AppendLine(string.Format(TrText.packageAddStateUnknown, entry.name));
                    break;
                case BuildProfilePackageAddInfo.ProgressState.PackagePending:
                    m_MessageBuilder.AppendLine(string.Format(TrText.packageAddPending, entry.name));
                    break;
                case BuildProfilePackageAddInfo.ProgressState.PackageDownloading:
                    m_MessageBuilder.AppendLine(string.Format(TrText.packageAddDownloading, entry.name));
                    break;
                case BuildProfilePackageAddInfo.ProgressState.PackageInstalling:
                    m_MessageBuilder.AppendLine(string.Format(TrText.packageAddInstalling, entry.name));
                    break;
                case BuildProfilePackageAddInfo.ProgressState.PackageReady:
                    m_MessageBuilder.AppendLine(string.Format(TrText.packageAddReady, entry.name));
                    break;
                case BuildProfilePackageAddInfo.ProgressState.PackageError:
                    m_MessageBuilder.AppendLine(string.Format(TrText.packageAddError, entry.name));
                    break;
                case BuildProfilePackageAddInfo.ProgressState.ConfigurationPending:
                    m_MessageBuilder.AppendLine(TrText.configurationPending);
                    break;
                case BuildProfilePackageAddInfo.ProgressState.ConfigurationRunning:
                    m_MessageBuilder.AppendLine(TrText.configurationRunning);
                    break;
            }
        }
        m_PackageAddProgressLabel.text = m_MessageBuilder.ToString();
    }
}
