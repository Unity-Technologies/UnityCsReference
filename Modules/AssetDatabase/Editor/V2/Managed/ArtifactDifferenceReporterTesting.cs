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

        internal static void Test_ImporterVersionModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            //Only way to test internals
            ImporterVersionModified(ref diff, msgsList);
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

        internal static void Test_PlatformGroupModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            PlatformGroupModified(ref diff, msgsList);
        }

        internal static void Test_PlatformDependencyModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            PlatformDependencyModified(ref diff, msgsList);
        }

        internal static void Test_PostProcessorVersionHashModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            PostProcessorVersionHashModified(ref diff, msgsList);
        }

        internal static void Test_TextureCompressionModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            TextureCompressionModified(ref diff, msgsList);
        }

        internal static void Test_GuidOfPathLocationModifiedViaHashOfSourceAsset(ref ArtifactInfoDifference diff, List<string> msgsList, string pathOfDependency)
        {
            GuidOfPathLocationModifiedViaHashOfSourceAsset(ref diff, msgsList, pathOfDependency);
        }
    }
}
