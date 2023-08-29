// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Analytics;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IEditorAnalyticsProxy : IService
    {
        AnalyticsResult SendAnalytic(IAnalytic analytic);
    }

    internal class EditorAnalyticsProxy : BaseService<IEditorAnalyticsProxy>, IEditorAnalyticsProxy
    {
        public AnalyticsResult SendAnalytic(IAnalytic analytic)
        {
            return EditorAnalytics.SendAnalytic(analytic);
        }
    }
}
