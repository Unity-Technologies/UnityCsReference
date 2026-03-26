// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Unity.VectorGraphics.Editor
{
    static class SVGImporterMetaUpgrader
    {
        internal static void ApplyPackageImporterProperties(SVGImporter importer, string assetPath)
        {
            // If this asset was already imported with the package version of the importer,
            // the .meta file properties will not be deserialized in this importer's properties.
            // This is caused by a ScriptedImporter mismatch in the asset's .meta file.
            //
            // As a patch, we "manually" read the .meta file and apply the properties. This is
            // unfortunate, but seems to be the safest, less intrusive way to handle a smooth
            // transition from the package to the module. Most other options would involve
            // AssetPostprocessor usage, which is frowned upon given its performance impact.

            var metaPath = assetPath + ".meta";
            if (!File.Exists(metaPath))
                return;

            var metaContents = File.ReadAllText(metaPath);

            var pattern = @"script:\s*\{fileID:\s*\d+,\s*guid:\s*([a-fA-F0-9]+),\s*type:\s*\d+\}";
            var regex = new System.Text.RegularExpressions.Regex(pattern);

            var match = regex.Match(metaContents);
            if (!match.Success)
                return;

            const string kBuiltinImporterGUID = "0000000000000000e000000000000000";
            var metaGuid = match.Groups[1].Value.ToLower();

            if (metaGuid == kBuiltinImporterGUID)
                return; // This is this version of the importer, nothing to do!

            ExtractPropertiesFromMetaContent(importer, metaContents);

            // Preserve the internalIDToNameTable entries from the package version
            // to maintain file ID compatibility and prevent broken GameObject references
            PreserveInternalIDToNameTable(importer, metaContents);
        }

        static void ExtractPropertiesFromMetaContent(SVGImporter importer, string metaContents)
        {
            if (TryExtractInt(metaContents, "svgType", out int svgType))
                importer.SvgType = (SVGType)svgType;

            if (TryExtractInt(metaContents, "texturedSpriteMeshType", out int meshType))
                importer.TexturedSpriteMeshType = (SpriteMeshType)meshType;

            if (TryExtractFloat(metaContents, "svgPixelsPerUnit", out float pixelsPerUnit))
                importer.SvgPixelsPerUnit = pixelsPerUnit;

            if (TryExtractInt(metaContents, "gradientResolution", out int gradientRes))
                importer.GradientResolution = (UInt16)gradientRes;

            if (TryExtractInt(metaContents, "alignment", out int alignment))
                importer.Alignment = (VectorUtils.Alignment)alignment;

            if (TryExtractVector2(metaContents, "customPivot", out Vector2 customPivot))
                importer.CustomPivot = customPivot;

            if (TryExtractInt(metaContents, "generatePhysicsShape", out int generatePhysics))
                importer.GeneratePhysicsShape = generatePhysics != 0;

            if (TryExtractInt(metaContents, "viewportOptions", out int viewportOptions))
                importer.ViewportOptions = (ViewportOptions)viewportOptions;

            if (TryExtractInt(metaContents, "advancedMode", out int advancedMode))
                importer.AdvancedMode = advancedMode != 0;

            if (TryExtractInt(metaContents, "tessellationMode", out int tessellationMode))
                importer.TessellationMode = (TessellationMode)tessellationMode;

            if (TryExtractInt(metaContents, "predefinedResolutionIndex", out int predefinedResIndex))
                importer.PredefinedResolutionIndex = predefinedResIndex;

            if (TryExtractInt(metaContents, "targetResolution", out int targetResolution))
                importer.TargetResolution = targetResolution;

            if (TryExtractFloat(metaContents, "resolutionMultiplier", out float resolutionMultiplier))
                importer.ResolutionMultiplier = resolutionMultiplier;

            if (TryExtractFloat(metaContents, "stepDistance", out float stepDistance))
                importer.StepDistance = stepDistance;

            if (TryExtractFloat(metaContents, "samplingStepDistance", out float samplingStepDistance))
                importer.SamplingStepDistance = samplingStepDistance;

            if (TryExtractInt(metaContents, "maxCordDeviationEnabled", out int maxCordDeviationEnabled))
                importer.MaxCordDeviationEnabled = maxCordDeviationEnabled != 0;

            if (TryExtractFloat(metaContents, "maxCordDeviation", out float maxCordDeviation))
                importer.MaxCordDeviation = maxCordDeviation;

            if (TryExtractInt(metaContents, "maxTangentAngleEnabled", out int maxTangentAngleEnabled))
                importer.MaxTangentAngleEnabled = maxTangentAngleEnabled != 0;

            if (TryExtractFloat(metaContents, "maxTangentAngle", out float maxTangentAngle))
                importer.MaxTangentAngle = maxTangentAngle;

            if (TryExtractInt(metaContents, "keepTextureAspectRatio", out int keepTextureAspectRatio))
                importer.KeepTextureAspectRatio = keepTextureAspectRatio != 0;

            if (TryExtractInt(metaContents, "textureSize", out int textureSize))
                importer.TextureSize = textureSize;

            if (TryExtractInt(metaContents, "textureWidth", out int textureWidth))
                importer.TextureWidth = textureWidth;

            if (TryExtractInt(metaContents, "textureHeight", out int textureHeight))
                importer.TextureHeight = textureHeight;

            if (TryExtractInt(metaContents, "wrapMode", out int wrapMode))
                importer.WrapMode = (TextureWrapMode)wrapMode;

            if (TryExtractInt(metaContents, "filterMode", out int filterMode))
                importer.FilterMode = (FilterMode)filterMode;

            if (TryExtractInt(metaContents, "sampleCount", out int sampleCount))
                importer.SampleCount = sampleCount;

            if (TryExtractInt(metaContents, "preserveSVGImageAspect", out int preserveSVGImageAspect))
                importer.PreserveSVGImageAspect = preserveSVGImageAspect != 0;

            if (TryExtractInt(metaContents, "useSVGPixelsPerUnit", out int useSVGPixelsPerUnit))
                importer.UseSVGPixelsPerUnit = useSVGPixelsPerUnit != 0;

            // Extract nested spriteData properties
            int spriteRectIndex = metaContents.IndexOf("SpriteRect:");

            if (spriteRectIndex != -1)
            {
                var spriteRectContent = metaContents.Substring(spriteRectIndex);

                if (TryExtractString(spriteRectContent, "name", out string spriteName))
                    importer.GetSVGSpriteData().SpriteName = spriteName;

                if (TryExtractVector2(spriteRectContent, "pivot", out Vector2 spritePivot))
                    importer.GetSVGSpriteData().SpritePivot = spritePivot;

                if (TryExtractInt(spriteRectContent, "alignment", out int spriteAlignment))
                    importer.GetSVGSpriteData().SpriteAlignment = (SpriteAlignment)spriteAlignment;

                if (TryExtractVector4(spriteRectContent, "border", out Vector4 spriteBorder))
                    importer.GetSVGSpriteData().SpriteBorder = spriteBorder;

                if (TryExtractInt(spriteRectContent, "x", out int spriteRectX) &&
                    TryExtractInt(spriteRectContent, "y", out int spriteRectY) &&
                    TryExtractInt(spriteRectContent, "width", out int spriteRectWidth) &&
                    TryExtractInt(spriteRectContent, "height", out int spriteRectHeight))
                {
                    importer.GetSVGSpriteData().SpriteRect = new Rect(spriteRectX, spriteRectY, spriteRectWidth, spriteRectHeight);
                }

                if (TryExtractString(spriteRectContent, "spriteID", out string spriteID))
                    importer.GetSVGSpriteData().SpriteID = spriteID;
            }

            int physicsOutlinesIndex = metaContents.IndexOf("PhysicsOutlines:");
            if (physicsOutlinesIndex != -1)
            {
                // Extract physics outline
                var spriteOutlinesContent = metaContents.Substring(physicsOutlinesIndex);

                if (TryExtractListVector2(spriteOutlinesContent, "PhysicsOutlines", out List<OutlineData> physicsOutlines))
                    importer.GetSVGSpriteData().PhysicsOutlines = physicsOutlines;
            }
        }

        static bool TryExtractInt(string metaContents, string propertyName, out int value)
        {
            var pattern = $@"{propertyName}:\s*(-?\d+)";
            var match = Regex.Match(metaContents, pattern);
            if (match.Success && int.TryParse(match.Groups[1].Value, out value))
                return true;
            value = 0;
            return false;
        }

        static bool TryExtractFloat(string metaContents, string propertyName, out float value)
        {
            var pattern = $@"{propertyName}:\s*(-?[\d.]+)";
            var match = Regex.Match(metaContents, pattern);
            if (match.Success && float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return true;
            value = 0f;
            return false;
        }

        static bool TryExtractVector2(string metaContents, string propertyName, out Vector2 value)
        {
            var pattern = $@"{propertyName}:\s*\{{\s*x:\s*(-?[\d.]+)\s*,\s*y:\s*(-?[\d.]+)\s*\}}";
            var match = Regex.Match(metaContents, pattern);
            if (match.Success &&
                float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
            {
                value = new Vector2(x, y);
                return true;
            }
            value = Vector2.zero;
            return false;
        }

        static bool TryExtractVector4(string metaContents, string propertyName, out Vector4 value)
        {
            var pattern = $@"{propertyName}:\s*\{{\s*x:\s*(-?[\d.]+)\s*,\s*y:\s*(-?[\d.]+)\s*,\s*z:\s*(-?[\d.]+)\s*,\s*w:\s*(-?[\d.]+)\s*\}}";
            var match = Regex.Match(metaContents, pattern);
            if (match.Success &&
                float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
                float.TryParse(match.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float z) &&
                float.TryParse(match.Groups[4].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float w))
            {
                value = new Vector4(x, y, z, w);
                return true;
            }
            value = Vector4.zero;
            return false;
        }

        static bool TryExtractString(string metaContents, string propertyName, out string value)
        {
            var pattern = $@"{propertyName}:\s*([^\r\n]*)";
            var match = Regex.Match(metaContents, pattern);
            if (match.Success)
            {
                value = match.Groups[1].Value;
                return true;
            }
            value = null;
            return false;
        }

        static bool TryExtractListVector2(string metaContents, string propertyName, out List<OutlineData> physicsOutlines)
        {
            physicsOutlines = new List<OutlineData>();

            var lines = metaContents.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            int lineIdx = 0;
            while (lineIdx < lines.Length && !lines[lineIdx].Contains("Vertices:"))
                ++lineIdx;

            ++lineIdx;

            // Scan each lines while it maches "- {x: 0.0, y:0.0}"
            var vertexPattern = @"-\s*\{\s*x:\s*(-?[\d.]+)\s*,\s*y:\s*(-?[\d.]+)\s*\}";
            var currentOutline = new List<Vector2>();

            while (lineIdx < lines.Length)
            {
                var line = lines[lineIdx].Trim();

                var match = Regex.Match(line, vertexPattern);
                if (!match.Success)
                {
                    SaveOutlineAndClear(currentOutline, physicsOutlines);

                    if (!line.Contains("Vertices:"))
                        break; // End of the list

                    ++lineIdx;
                    continue;
                }

                if (match.Success &&
                    float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                {
                    currentOutline.Add(new Vector2(x, y));
                }

                ++lineIdx;
            }

            SaveOutlineAndClear(currentOutline, physicsOutlines);

            return physicsOutlines.Count > 0;
        }

        static void SaveOutlineAndClear(List<Vector2> outline, List<OutlineData> output)
        {
            if (outline.Count == 0)
                return;

            var outlineData = new OutlineData();
            outlineData.Vertices = new Vector2[outline.Count];
            for (int i = 0; i < outline.Count; ++i)
                outlineData.Vertices[i] = outline[i];
            output.Add(outlineData);

            outline.Clear();
        }

        static void PreserveInternalIDToNameTable(SVGImporter importer, string metaContents)
        {
            // Parse the internalIDToNameTable from the package version .meta file
            // Format:
            //   internalIDToNameTable:
            //   - first:
            //       <persistentTypeID>: <fileID>
            //     second: <identifierName>

            var so = new SerializedObject(importer);
            var internalIDMap = so.FindProperty("m_InternalIDToNameTable");
            if (internalIDMap == null)
                return;

            var lines = metaContents.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            int lineIdx = 0;

            // Find the internalIDToNameTable section
            while (lineIdx < lines.Length && !lines[lineIdx].Contains("internalIDToNameTable:"))
                ++lineIdx;

            if (lineIdx >= lines.Length)
                return; // No internalIDToNameTable found

            ++lineIdx; // Move past the internalIDToNameTable: line

            // Parse each entry in the table
            while (lineIdx < lines.Length)
            {
                var line = lines[lineIdx].Trim();

                // Stop if we reach another top-level property
                if (!line.StartsWith("- first:"))
                    break;

                ++lineIdx;
                if (lineIdx >= lines.Length)
                    break;

                // Next line should be "<typeID>: <fileID>"
                var firstLine = lines[lineIdx].Trim();
                var firstMatch = Regex.Match(firstLine, @"(\d+):\s*(\d+)");
                if (!firstMatch.Success)
                    break;

                int persistentTypeID = int.Parse(firstMatch.Groups[1].Value);
                long fileID = long.Parse(firstMatch.Groups[2].Value);

                // Find the "second:" line with the identifier name
                ++lineIdx;
                while (lineIdx < lines.Length && !lines[lineIdx].Trim().StartsWith("second:"))
                    ++lineIdx;

                if (lineIdx >= lines.Length)
                    break;

                var secondLine = lines[lineIdx].Trim();
                var secondMatch = Regex.Match(secondLine, @"second:\s*(.+)");
                if (!secondMatch.Success)
                    break;

                string identifierName = secondMatch.Groups[1].Value.Trim();

                // Directly add entry to m_InternalIDToNameTable using SerializedProperty
                AddInternalIDEntry(internalIDMap, persistentTypeID, fileID, identifierName);

                ++lineIdx;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddInternalIDEntry(SerializedProperty internalIDMap, int persistentTypeID, long fileID, string name)
        {
            // Structure of m_InternalIDToNameTable:
            // - first:                                    (pair<pair<int, long>, string>)
            //   - first:                                  (pair<int, long>)
            //     - first: <persistentTypeID> (int)
            //     - second: <fileID> (long)
            //   - second: <name> (string)

            int newIndex = internalIDMap.arraySize;
            internalIDMap.arraySize++;

            var newEntry = internalIDMap.GetArrayElementAtIndex(newIndex);
            var firstPair = newEntry.FindPropertyRelative("first");
            var classID = firstPair.FindPropertyRelative("first");
            var localID = firstPair.FindPropertyRelative("second");
            var secondString = newEntry.FindPropertyRelative("second");

            classID.intValue = persistentTypeID;
            localID.longValue = fileID;
            secondString.stringValue = name;
        }
    }
}
