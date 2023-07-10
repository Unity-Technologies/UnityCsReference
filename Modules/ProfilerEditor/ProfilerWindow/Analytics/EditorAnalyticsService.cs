// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEditor.Profiling;
using UnityEngine.Scripting;

namespace UnityEditor.Profiling
{
    internal class EditorAnalyticsService : IAnalyticsService
    {
        AnalyticsResult IAnalyticsService.SendAnalytic(IAnalytic analytic)
        {
            return EditorAnalytics.SendAnalytic(analytic);
        }

    }
}
