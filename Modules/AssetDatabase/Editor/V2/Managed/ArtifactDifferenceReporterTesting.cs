// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    internal partial class ArtifactDifferenceReporter
    {
        internal static void Test_GraphicsAPIMaskModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            //Only way to test internals
            GraphicsAPIMaskModified(ref diff, msgsList);
        }
    }
}
