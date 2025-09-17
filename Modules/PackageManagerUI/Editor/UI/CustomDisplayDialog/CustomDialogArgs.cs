// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal;

internal abstract class CustomDialogArgsBase
{
    public readonly string windowTitle;
    public readonly string idForAnalytics;
    public Icon headerIcon;
    public HeaderColor headerColor;
    public string headerMainText;
    public string headerSubText;
    public Icon headerInfoBoxIcon;
    public string headerInfoBoxText;
    public string bodyText;
    public string readMoreUrl;
    public string readMoreClickedAnalyticsId;

    public abstract IEnumerable<(string text, DialogResult result)> buttons { get; }

    protected CustomDialogArgsBase(string windowTitle, string idForAnalytics)
    {
        this.windowTitle = windowTitle;
        this.idForAnalytics = idForAnalytics;
        headerColor = HeaderColor.Default;
    }
}

internal class CustomDisplayDialogArgs : CustomDialogArgsBase
{
    public readonly string buttonText;

    public CustomDisplayDialogArgs(string windowTitle, string idForAnalytics, string buttonText) : base(windowTitle, idForAnalytics)
    {
        this.buttonText = buttonText;
    }

    public override IEnumerable<(string text, DialogResult result)> buttons
    {
        get
        {
            yield return (buttonText, DialogResult.DefaultAction);
        }
    }
}

internal class CustomDecisionDialogArgs : CustomDialogArgsBase
{
    public readonly string defaultButtonText;
    public readonly string cancelButtonText;

    public CustomDecisionDialogArgs(string windowTitle, string idForAnalytics, string defaultButtonText, string cancelButtonText) : base(windowTitle, idForAnalytics)
    {
        this.defaultButtonText = defaultButtonText;
        this.cancelButtonText = cancelButtonText;
    }

    public override IEnumerable<(string text, DialogResult result)> buttons
    {
        get
        {
            yield return (defaultButtonText, DialogResult.DefaultAction);
            yield return (cancelButtonText, DialogResult.Cancel);
        }
    }
}
