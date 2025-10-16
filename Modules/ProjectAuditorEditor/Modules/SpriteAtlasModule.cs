// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Unity.ProjectAuditor.Editor.Modules
{
    internal enum SpriteAtlasProperty
    {
        EmptySpacePercent,
        MainTextureResolution,
        SpriteCount,
        IncludedInBuild,
        Readable,
        Mipmaps,
        Padding,
        Num
    }

    class SpriteModule : ModuleWithAnalyzers<SpriteAtlasModuleAnalyzer>
    {
        static readonly string k_Unavailable = "Unavailable";

        static readonly IssueLayout k_SpriteAtlasLayout = new IssueLayout
        {
            Category = IssueCategory.SpriteAtlas,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Name", LongName = "Sprite Atlas Name", MaxAutoWidth = 500 },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(SpriteAtlasProperty.EmptySpacePercent), Format = PropertyFormat.String, Name = "Empty Space",
                    LongName = "Percentage of atlas that is empty pixels." +
                               "\n\nIf scanning Sprite Atlases takes too long, this analysis can also be disabled by " +
                               "setting the Sprite Atlas threshold to 100 in Project Settings > Project Auditor." },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(SpriteAtlasProperty.MainTextureResolution), Format = PropertyFormat.String, Name = "Resolution", LongName = "Main Texture Resolution" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(SpriteAtlasProperty.SpriteCount), Format = PropertyFormat.String, Name = "Sprites", LongName = "Sprite Count" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(SpriteAtlasProperty.IncludedInBuild), Format = PropertyFormat.Bool, Name = "In Build", LongName = "Included in build" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(SpriteAtlasProperty.Readable), Format = PropertyFormat.Bool, Name = "Readable", LongName = "Read/Write Enabled" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(SpriteAtlasProperty.Mipmaps), Format = PropertyFormat.Bool, Name = "MipMaps", LongName = "Texture MipMaps Enabled" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(SpriteAtlasProperty.Padding), Format = PropertyFormat.Integer, Name = "Padding", LongName = "Pixels of Padding" },
                new PropertyDefinition { Type = PropertyType.Path, Name = "Path", MaxAutoWidth = 500 }
            }
        };

        public override string Name => "Sprite Atlases";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts  => new IssueLayout[]
        {
            k_SpriteAtlasLayout,
            AssetsModule.k_IssueLayout
        };

        public override void Initialize()
        {
            base.Initialize();

            ProjectIssueExtensions.AddCustomComparer(IssueCategory.SpriteAtlas, PropertyTypeUtil.FromCustom(SpriteAtlasProperty.EmptySpacePercent),
                (a, b) =>
                {
                    var strValsA = a.GetProperty(PropertyTypeUtil.FromCustom(SpriteAtlasProperty.EmptySpacePercent));
                    var strValsB = b.GetProperty(PropertyTypeUtil.FromCustom(SpriteAtlasProperty.EmptySpacePercent));

                    int floatCheck = 0;
                    float floatA = 0;
                    float floatB = 0;

                    // This assumes that we don't add non-percentage strings ending with percentage signs...
                    if (strValsA.EndsWith("%"))
                    {
                        floatA = float.Parse(strValsA.Substring(0, strValsA.Length - 1));
                        ++floatCheck;
                    }

                    if (strValsB.EndsWith("%"))
                    {
                        floatB = float.Parse(strValsB.Substring(0, strValsB.Length - 1));
                        floatCheck += 2;
                    }

                    if (floatCheck == 3) // both are floats
                        return floatA < floatB ? -1 : floatA > floatB ? 1 : 0;

                    if (floatCheck == 2) // B is a float, A isn't, return B
                        return -1;

                    if (floatCheck == 1) // A is a float, B isn't, return A
                        return 1;

                    return String.Compare(strValsA, strValsB, StringComparison.OrdinalIgnoreCase);
                });

            // TODO: deduplicate with TextureModule?
            ProjectIssueExtensions.AddCustomComparer(IssueCategory.SpriteAtlas, PropertyTypeUtil.FromCustom(SpriteAtlasProperty.MainTextureResolution),
                (a, b) =>
                {
                    var strA = a.GetProperty(PropertyTypeUtil.FromCustom(SpriteAtlasProperty.MainTextureResolution));
                    var strB = b.GetProperty(PropertyTypeUtil.FromCustom(SpriteAtlasProperty.MainTextureResolution));

                    // Quick returns if at least one value is "we don't have a value"
                    var quickRet = 0;
                    if (strA == k_Unavailable)
                        ++quickRet;
                    if (strB == k_Unavailable)
                        quickRet += 2;

                    if (quickRet > 0)
                        return quickRet == 1 ? -1 : quickRet == 2 ? 1 : 0;

                    var strValsA = strA.Split('x');
                    var strValsB = strB.Split('x');

                    var aX = int.Parse(strValsA[0]);
                    var aY = int.Parse(strValsA[1]);
                    var aMult = aX * aY;

                    var bX = int.Parse(strValsB[0]);
                    var bY = int.Parse(strValsB[1]);
                    var bMult = bX * bY;

                    // Sort by total pixels first
                    var retVal = aMult < bMult ? -1 : aMult > bMult ? 1 : 0;

                    // If equal, sort by X value
                    if (retVal == 0)
                    {
                        retVal = aX < bX ? -1 : aX > bX ? 1 : 0;
                    }
                    return retVal;
                });
        }

        public override AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress = null)
        {
            var analyzers = GetCompatibleAnalyzers(analysisParams);
            var editorSpriteModeIsV2 = EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2;

            var spaceLimit = analysisParams.DiagnosticParams.GetParameter("SpriteAtlasEmptySpaceLimit");
            var emptySpaceTested = (spaceLimit < 100) && (spaceLimit >= 0);

            var context = new SpriteAtlasAnalysisContext
            {
                // AssetPath set in loop
                Params = analysisParams
            };

            var assetPaths = GetAssetPathsByFilter("t:SpriteAtlas, a:assets", context);

            progress?.Start("Finding Sprite Atlases", "Search in Progress...", assetPaths.Length);

            bool loggedWarning = false;

            var issues = new List<ReportItem>();

            foreach (var assetPath in assetPaths)
            {
                // v2 atlases can only be read when the sprite mode is set to always deal with them.
                // v1 atlases can be read with any non-v2 setting (even disabled), and get automatically upgraded to
                // v2 assets if the setting is changed to either of the v2 options.
                var spritesEnabled = editorSpriteModeIsV2 && assetPath.EndsWith("spriteatlasv2") || assetPath.EndsWith("spriteatlas");

                if (!loggedWarning && !spritesEnabled)
                {
                    Debug.LogWarning("For Sprite Atlas v2 empty space analysis, set Project Settings > Editor > Sprite Atlas Mode to \"Sprite Atlas V2 - Enabled\".");
                    loggedWarning = true;
                }

                if (progress?.IsCancelled ?? false)
                    return AnalysisResult.Cancelled;

                context.AssetPath = assetPath;
                context.SpriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(context.AssetPath);
                context.Name = context.SpriteAtlas.name;
                context.EmptySpacePercentage = -1;
                var mainTextureResolution = k_Unavailable;

                // Don't run GetEmptySpacePercentage if disabled, as it's reportedly quite a lengthy analysis on some projects.
                string reportedFreeSpace;
                if (!spritesEnabled)
                    reportedFreeSpace = "Cannot analyse without Sprite Atlas Mode set to \"Sprite Atlas V2 - Enabled\"";
                else if (!emptySpaceTested)
                    reportedFreeSpace = "Analysis disabled, SpriteAtlasEmptySpaceLimit is set to 100";
                else
                {
                    if (context.SpriteAtlas.spriteCount == 0)
                        reportedFreeSpace = "No sprites found";
                    else
                    {
                        var previewTexture = TextureUtils.GetPreviewTexture(context.SpriteAtlas);

                        if (previewTexture == null)
                        {
                            Debug.LogError($"Error getting preview image for sprite atlas \"{context.SpriteAtlas.name}\"");
                            reportedFreeSpace = "Error";
                        }
                        else
                        {
                            mainTextureResolution = previewTexture.width + "x" + previewTexture.height;

                            context.EmptySpacePercentage = TextureUtils.GetEmptyPixelsPercent(previewTexture);

                            if (context.EmptySpacePercentage < 0)
                            {
                                Debug.LogError(
                                    $"Error analysing texture \"{previewTexture.name}\" in sprite atlas \"{context.SpriteAtlas.name}\"");
                                reportedFreeSpace = "Error";
                            }
                            else
                            {
                                reportedFreeSpace = $"{context.EmptySpacePercentage}%";
                            }
                        }
                    }
                }

                var textureSettings = context.SpriteAtlas.GetTextureSettings();

                issues.Add(context.CreateInsight(IssueCategory.SpriteAtlas, context.Name)
                    .WithCustomProperties(
                        new object[]
                        {
                            reportedFreeSpace,
                            mainTextureResolution,
                            spritesEnabled ? context.SpriteAtlas.spriteCount.ToString() : k_Unavailable,
                            context.SpriteAtlas.IsIncludeInBuild(),
                            textureSettings.readable,
                            textureSettings.generateMipMaps,
                            context.SpriteAtlas.GetPackingSettings().padding,
                        })
                    .WithLocation(new Location(assetPath)));

                foreach (var analyzer in analyzers)
                {
                    analysisParams.OnIncomingIssues(analyzer.Analyze(context));
                }

                progress?.Advance();
            }

            if (issues.Count > 0)
                context.Params.OnIncomingIssues(issues);

            progress?.Clear();

            return AnalysisResult.Success;
        }
    }
}
