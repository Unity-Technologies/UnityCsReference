// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal;

internal class CustomDisplayDialogArgs
{
    public readonly string windowTitle;
    public readonly string idForAnalytics;
    public Icon headerIcon;
    public string headerMainText;
    public string headerSubText;
    public Icon headerInfoBoxIcon;
    public string headerInfoBoxText;
    public string bodyText;
    public string readMoreUrl;
    public string readMoreClickedAnalyticsId;

    public CustomDisplayDialogArgs(string windowTitle, string idForAnalytics)
    {
        this.windowTitle = windowTitle;
        this.idForAnalytics = idForAnalytics;
    }
}
