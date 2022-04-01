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

        internal static void Test_GlobalArtifactFormatVersionModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            //Only way to test internals
            GlobalArtifactFormatVersionModified(ref diff, msgsList);
        }

        internal static void Test_GlobalAllImporterVersionModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            //Only way to test internals
            GlobalAllImporterVersionModified(ref diff, msgsList);
        }

        internal static void Test_ArtifactFileIdOfMainObjectModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            //Only way to test internals
            ArtifactFileIdOfMainObjectModified(ref diff, msgsList);
        }

        internal static void Test_ScriptingRuntimeVersionModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            //Only way to test internals
            ScriptingRuntimeVersionModified(ref diff, msgsList);
        }
    }
}
