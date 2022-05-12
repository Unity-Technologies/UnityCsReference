// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.TerrainTools
{
    public static class PaintDetailsToolUtility
    {
        private static int s_LastTerrainDataDirtyCount = -1;
        private static float s_LastTerrainDetailDensity = 0.0f;
        private static Vector2Int[] s_CachedClampedPatches;
        private static bool s_ShowTooManyDetailText = false;
        private static Dictionary<Vector2Int, Vector2> s_CachedPatchHeightMinMax = new Dictionary<Vector2Int, Vector2>();
        private static readonly GUIContent k_TooManyDetails = EditorGUIUtility.TrTextContent(
            "This area contains too many detail objects.\nDecrease the detail object density or remove some by pressing Ctrl while you paint.",
            EditorGUIUtility.GetHelpIcon(MessageType.Warning));

        private const float k_IconSize = 24;

        public static int FindDetailPrototype(Terrain terrain, Terrain sourceTerrain, int sourceDetail)
        {
            if (sourceDetail == PaintDetailsTool.kInvalidDetail ||
                sourceDetail >= sourceTerrain.terrainData.detailPrototypes.Length)
            {
                return PaintDetailsTool.kInvalidDetail;
            }

            if (terrain == sourceTerrain)
            {
                return sourceDetail;
            }

            DetailPrototype sourceDetailPrototype = sourceTerrain.terrainData.detailPrototypes[sourceDetail];
            for (int i = 0; i < terrain.terrainData.detailPrototypes.Length; ++i)
            {
                if (sourceDetailPrototype.Equals(terrain.terrainData.detailPrototypes[i]))
                    return i;
            }

            return PaintDetailsTool.kInvalidDetail;
        }

        public static int CopyDetailPrototype(Terrain terrain, Terrain sourceTerrain, int sourceDetail)
        {
            DetailPrototype sourceDetailPrototype = sourceTerrain.terrainData.detailPrototypes[sourceDetail];
            DetailPrototype[] newDetailPrototypesArray = new DetailPrototype[terrain.terrainData.detailPrototypes.Length + 1];
            System.Array.Copy(terrain.terrainData.detailPrototypes, newDetailPrototypesArray, terrain.terrainData.detailPrototypes.Length);
            newDetailPrototypesArray[newDetailPrototypesArray.Length - 1] = new DetailPrototype(sourceDetailPrototype);
            terrain.terrainData.detailPrototypes = newDetailPrototypesArray;
            return newDetailPrototypesArray.Length - 1;
        }

        public static int GetMaxDetailInstancesPerPatch(TerrainData terrainData)
        {
            //instance count mode
            if (terrainData.detailScatterMode == DetailScatterMode.InstanceCountMode)
                return terrainData.detailResolutionPerPatch * terrainData.detailResolutionPerPatch * terrainData.maxDetailScatterPerRes;

            // coverage mode
            float detailCoveragePerUnitSquared = 0;
            for(int i = 0; i < terrainData.detailPrototypes.Length; i++)
            {
                detailCoveragePerUnitSquared += terrainData.ComputeDetailCoverage(i);
            }
            float patchSizeX = terrainData.size.x / terrainData.detailPatchCount;
            float patchSizeZ = terrainData.size.z / terrainData.detailPatchCount;
            return Mathf.RoundToInt(detailCoveragePerUnitSquared * patchSizeX * patchSizeZ);
        }

        public static GUIContent[] LoadDetailIcons(DetailPrototype[] prototypes)
        {
            // Locate the proto types asset preview textures
            var guiContents = new GUIContent[prototypes.Length];
            for (int i = 0; i < guiContents.Length; i++)
            {
                guiContents[i] = new GUIContent();

                if (prototypes[i].usePrototypeMesh)
                {
                    Texture tex = AssetPreview.GetAssetPreview(prototypes[i].prototype);
                    guiContents[i].image = tex != null ? tex : null;
                    guiContents[i].text = guiContents[i].tooltip = prototypes[i].prototype != null ? prototypes[i].prototype.name : "Missing";
                }
                else
                {
                    Texture tex = prototypes[i].prototypeTexture;
                    guiContents[i].image = tex != null ? tex : null;
                    guiContents[i].text = guiContents[i].tooltip = tex != null ? tex.name : "Missing";
                }
            }
            return guiContents;
        }

        public static int ClampedDetailPatchesGUI(Terrain terrain, out Vector2 detailMinMaxHeight, out List<Vector4> clampedDetailPatchIconScreenPositions)
        {
            detailMinMaxHeight = Vector2.zero;
            clampedDetailPatchIconScreenPositions = null;

            if (!AnnotationWindow.ShowTerrainDebugWarnings || !SceneView.currentDrawingSceneView.drawGizmos || terrain == null || terrain.terrainData == null)
                return -1;

            var terrainData = terrain.terrainData;
            var detailPrototypes = terrainData.detailPrototypes;
            if (detailPrototypes.Length == 0)
                return -1;

            if (s_LastTerrainDataDirtyCount != EditorUtility.GetDirtyCount(terrainData)
                || s_LastTerrainDetailDensity != terrain.detailObjectDensity)
            {
                s_LastTerrainDataDirtyCount = EditorUtility.GetDirtyCount(terrainData);
                s_LastTerrainDetailDensity = terrain.detailObjectDensity;
                s_CachedClampedPatches = null;
                s_ShowTooManyDetailText = false;
            }

            if (s_CachedClampedPatches == null)
                s_CachedClampedPatches = terrainData.GetClampedDetailPatches(terrain.detailObjectDensity);

            float maxHeight = 0.0f;
            float minHeight = 0.0f;
            foreach (var prototype in detailPrototypes)
            {
                var meshTop = 1.0f;
                var meshBottom = 0.0f;
                if (prototype.renderMode != DetailRenderMode.GrassBillboard
                    && prototype.prototype != null
                    && prototype.prototype.TryGetComponent<MeshFilter>(out var meshFilter)
                    && meshFilter.sharedMesh != null)
                {
                    meshTop = meshFilter.sharedMesh.bounds.max.y;
                    meshBottom = Mathf.Min(meshFilter.sharedMesh.bounds.min.y, 0);
                }
                maxHeight = Mathf.Max(maxHeight, meshTop * prototype.maxHeight);
                minHeight = Mathf.Min(minHeight, meshBottom * prototype.maxHeight);
            }
            detailMinMaxHeight = new Vector2(minHeight, maxHeight);

            var sceneViewRect = SceneView.currentDrawingSceneView.cameraViewport;
            sceneViewRect.y = 0;

            clampedDetailPatchIconScreenPositions = CalculateClampedDetailPatchIconScreenPositions(detailMinMaxHeight, terrain.GetPosition(), terrainData);

            if (GUIUtility.hotControl == 0)
            {
                // Mouse test from front to back
                for (int i = clampedDetailPatchIconScreenPositions.Count - 1; i >= 0; --i)
                {
                    var p = clampedDetailPatchIconScreenPositions[i];
                    var rect = new Rect(p.x - k_IconSize / 2, p.y - k_IconSize / 2, k_IconSize, k_IconSize);
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        int patchIndex = (int)p.w;

                        if (Event.current.type == EventType.MouseUp)
                            s_ShowTooManyDetailText = !s_ShowTooManyDetailText;

                        // Use up the mouse events to prevent painting on clicking.
                        if (Event.current.type == EventType.MouseDown
                            || Event.current.type == EventType.MouseUp)
                            Event.current.Use();
                        HandleUtility.Repaint();

                        return patchIndex;
                    }
                }
            }

            return -1;
        }

        public static void DrawClampedDetailPatchGUI(int mouseOnPatchIndex, List<Vector4> clampedDetailPatchIconScreenPositions, Vector2 detailMinMaxHeight, Terrain terrain, IOnSceneGUI editContext)
        {
            if (mouseOnPatchIndex == -1)
                s_ShowTooManyDetailText = false;

            if (clampedDetailPatchIconScreenPositions == null
                || Event.current.type != EventType.Repaint)
                return;

            var terrainData = terrain.terrainData;
            float patchUVSize = 1.0f / terrainData.detailPatchCount;
            int heightmapRes = terrainData.heightmapResolution;

            HandleUtility.ApplyWireMaterial(CompareFunction.LessEqual);
            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.TRS(terrain.GetPosition(), Quaternion.identity, terrainData.size));
            GL.Begin(GL.LINES);

            float centerOffset = (detailMinMaxHeight.y + detailMinMaxHeight.x) / terrainData.size.y * 0.5f;
            float halfSizeOffset = (detailMinMaxHeight.y - detailMinMaxHeight.x) / terrainData.size.y * 0.5f + 0.001f;

            if (GUIUtility.hotControl != 0 && editContext.hitValidTerrain)
            {
                // hotControl != 0 && hitValidTerrain: during painting
                for (int i = 0; i < s_CachedClampedPatches.Length; ++i)
                {
                    if (s_CachedClampedPatches[i].x * patchUVSize <= editContext.raycastHit.textureCoord.x
                        && s_CachedClampedPatches[i].y * patchUVSize <= editContext.raycastHit.textureCoord.y
                        && (s_CachedClampedPatches[i].x + 1) * patchUVSize > editContext.raycastHit.textureCoord.x
                        && (s_CachedClampedPatches[i].y + 1) * patchUVSize > editContext.raycastHit.textureCoord.y)
                    {
                        mouseOnPatchIndex = i;
                        break;
                    }
                }
            }

            for (int i = 0; i < s_CachedClampedPatches.Length; ++i)
            {
                var patch = s_CachedClampedPatches[i];
                var heightMinMax = CalculatePatchHeightMinMaxCached(patch, patchUVSize, heightmapRes, terrainData);
                var center = new Vector3((patch.x + 0.5f) * patchUVSize, (heightMinMax.y + heightMinMax.x) * 0.5f, (patch.y + 0.5f) * patchUVSize);
                var halfSize = new Vector3(patchUVSize, heightMinMax.y - heightMinMax.x, patchUVSize) * 0.5f;
                center.y += centerOffset;
                halfSize.y += halfSizeOffset;

                GL.Color(i == mouseOnPatchIndex ? new Color(1, 0.2f, 0.2f, 1) : new Color(0.75f, 0, 0, 0.33f));
                GL.Vertex(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, -halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, -halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z));

                GL.Vertex(center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(-halfSize.x, halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(-halfSize.x, halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z));

                GL.Vertex(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, halfSize.y, -halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, -halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(halfSize.x, halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z));
                GL.Vertex(center + new Vector3(-halfSize.x, halfSize.y, halfSize.z));
            }

            GL.End();
            GL.PopMatrix();

            Handles.BeginGUI();

            // Draw icons from back to front
            var showTooManyDetailTextPos = Vector4.zero;
            for (int i = 0; i < clampedDetailPatchIconScreenPositions.Count; ++i)
            {
                var p = clampedDetailPatchIconScreenPositions[i];
                var rect = new Rect(p.x - k_IconSize / 2, p.y - k_IconSize / 2, k_IconSize, k_IconSize);
                GUI.DrawTexture(rect, k_TooManyDetails.image, ScaleMode.StretchToFill, true, 1.0f, Color.white * ((int)p.w == mouseOnPatchIndex ? 1 : 0.75f), 0, 0);
                if ((int)p.w == mouseOnPatchIndex && s_ShowTooManyDetailText)
                    showTooManyDetailTextPos = p;
            }

            if (mouseOnPatchIndex >= 0 && s_ShowTooManyDetailText)
            {
                var sceneViewRect = SceneView.lastActiveSceneView.cameraViewport;
                sceneViewRect.y = 0;

                var textSize = EditorStyles.tooltip.CalcSize(k_TooManyDetails);
                var textRect = new Rect(showTooManyDetailTextPos.x + k_IconSize / 2 + 4, showTooManyDetailTextPos.y - k_IconSize / 2, textSize.x, textSize.y);
                if (textRect.xMax > sceneViewRect.xMax)
                {
                    // move left
                    var newX = showTooManyDetailTextPos.x - k_IconSize / 2 - 4 - textSize.x;
                    if (newX >= 0)
                        textRect.x = newX;
                }
                if (textRect.yMax > sceneViewRect.yMax)
                {
                    // move up
                    var newY = showTooManyDetailTextPos.y + k_IconSize / 2 - textSize.y;
                    if (newY >= 0)
                        textRect.y = newY;
                }

                EditorStyles.tooltip.Draw(textRect, k_TooManyDetails, false, false, false, false);
            }

            Handles.EndGUI();
        }

        private static List<Vector4> CalculateClampedDetailPatchIconScreenPositions(Vector2 detailMinMaxHeight, Vector3 terrainPosition, TerrainData terrainData)
        {
            float patchUVSize = 1.0f / terrainData.detailPatchCount;
            int heightmapRes = terrainData.heightmapResolution;
            var sceneViewRect = SceneView.lastActiveSceneView.cameraViewport;
            sceneViewRect.y = 0;

            var projectedPointsWithDepth = new List<Vector4>(s_CachedClampedPatches.Length);
            for (int i = 0; i < s_CachedClampedPatches.Length; ++i)
            {
                var patch = s_CachedClampedPatches[i];
                var heightMinMax = CalculatePatchHeightMinMaxCached(patch, patchUVSize, heightmapRes, terrainData);

                var p = new Vector3((patch.x + 0.5f) * patchUVSize * terrainData.size.x, heightMinMax.y * terrainData.size.y + detailMinMaxHeight.y, (patch.y + 0.5f) * patchUVSize * terrainData.size.z);
                var sp = HandleUtility.WorldToGUIPointWithDepth(p + terrainPosition);
                if (sp.z > 0.0f && sp.x >= sceneViewRect.xMin - k_IconSize / 2 && sp.y >= sceneViewRect.yMin - k_IconSize / 2 && sp.x <= sceneViewRect.xMax + k_IconSize / 2 && sp.y <= sceneViewRect.yMax + k_IconSize / 2)
                    projectedPointsWithDepth.Add(new Vector4(sp.x, sp.y, sp.z, i));
            }

            projectedPointsWithDepth.Sort((lhs, rhs) => rhs.z.CompareTo(lhs.z));
            return projectedPointsWithDepth;
        }

        private static Vector2 CalculatePatchHeightMinMaxCached(Vector2Int patch, float patchUVSize, int heightmapRes, TerrainData terrainData)
        {
            if (s_CachedPatchHeightMinMax.TryGetValue(patch, out var heightMinMax))
                return heightMinMax;

            var patchHmXMin = Mathf.FloorToInt(patch.x * patchUVSize * heightmapRes);
            var patchHmYMin = Mathf.FloorToInt(patch.y * patchUVSize * heightmapRes);
            var patchHmXMax = Mathf.CeilToInt((patch.x + 1) * patchUVSize * heightmapRes);
            var patchHmYMax = Mathf.CeilToInt((patch.y + 1) * patchUVSize * heightmapRes);
            var heights = terrainData.GetHeights(patchHmXMin, patchHmYMin, Mathf.Min(patchHmXMax + 1, heightmapRes) - patchHmXMin, Mathf.Min(patchHmYMax + 1, heightmapRes) - patchHmYMin);

            float heightMin = heights[0, 0];
            float heightMax = heights[0, 0];
            foreach (var height in heights)
            {
                heightMin = Mathf.Min(heightMin, height);
                heightMax = Mathf.Max(heightMax, height);
            }

            heightMinMax = new Vector2(heightMin, heightMax);
            s_CachedPatchHeightMinMax.Add(patch, heightMinMax);
            return heightMinMax;
        }

        public static void ResetDetailsUtilityData()
        {
            s_CachedClampedPatches = null;
            s_ShowTooManyDetailText = false;
            s_LastTerrainDataDirtyCount = -1;
            s_LastTerrainDetailDensity = 0.0f;
            s_CachedPatchHeightMinMax.Clear();
        }
    }

    public class DetailBrushRepresentation
    {
        private const int kMinBrushSize = 3;

        private int m_Size;
        private float[] m_Strength;
        private Texture2D m_OldBrushTex;

        public float GetStrength(int ix, int iy)
        {
            ix = Mathf.Clamp(ix, 0, m_Size - 1);
            iy = Mathf.Clamp(iy, 0, m_Size - 1);

            float s = m_Strength[iy * m_Size + ix];

            return s;
        }

        public float GetStrength(Vector2Int position)
        {
            return GetStrength(position.x, position.y);
        }

        public void Update(Texture2D brushTex, int size, bool forceUpdate = false)
        {
            if (!forceUpdate && size == m_Size && m_OldBrushTex == brushTex && m_Strength != null)
                return;

            Texture2D mask = brushTex;
            if (mask != null)
            {
                Texture2D readableTexture = null;
                if (!mask.isReadable)
                {
                    readableTexture = new Texture2D(mask.width, mask.height, mask.format, mask.mipmapCount > 1);
                    Graphics.CopyTexture(mask, readableTexture);
                    readableTexture.Apply();
                }
                else
                {
                    readableTexture = mask;
                }

                float fSize = size;
                m_Size = size;
                m_Strength = new float[m_Size * m_Size];
                if (m_Size > kMinBrushSize)
                {
                    for (int y = 0; y < m_Size; y++)
                    {
                        float v = y / fSize;
                        for (int x = 0; x < m_Size; x++)
                        {
                            float u = x / fSize;
                            m_Strength[y * m_Size + x] = readableTexture.GetPixelBilinear(u, v).r;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < m_Strength.Length; i++)
                        m_Strength[i] = 1.0F;
                }

                if (readableTexture != mask)
                    Object.DestroyImmediate(readableTexture);
            }
            else
            {
                m_Strength = new float[1];
                m_Strength[0] = 1.0F;
                m_Size = 1;
            }

            m_OldBrushTex = brushTex;
        }
    }

    public readonly struct DetailBrushBounds
    {
        readonly int m_Radius;
        readonly int m_Fraction;
        readonly Vector2Int m_Min;
        readonly Vector2Int m_Max;
        readonly RectInt m_Bounds;

        public Vector2Int min { get => m_Min;}
        public Vector2Int max { get => m_Max;}
        public RectInt bounds { get => m_Bounds;}

        public DetailBrushBounds(TerrainData terrainData, PaintTreesDetailsContext ctx, int size, int uvIndex = 0)
        {
            Vector2 ctxUV = ctx.neighborUvs[uvIndex];
            m_Bounds = new RectInt();
            m_Bounds.x = Mathf.FloorToInt(ctxUV.x * terrainData.detailWidth);
            m_Bounds.y = Mathf.FloorToInt(ctxUV.y * terrainData.detailHeight);

            m_Radius = Mathf.RoundToInt(size) / 2;
            m_Fraction = Mathf.RoundToInt(size) % 2;

            m_Min = new Vector2Int(m_Bounds.x - m_Radius, m_Bounds.y - m_Radius);
            m_Max = new Vector2Int(m_Bounds.x + m_Radius + m_Fraction, m_Bounds.y + m_Radius + m_Fraction);

            if (m_Min.x >= terrainData.detailWidth || m_Min.y >= terrainData.detailHeight || m_Max.x <= 0 || m_Max.y <= 0)
            {
                return;
            }

            m_Min.x = Mathf.Clamp(m_Min.x, 0, terrainData.detailWidth - 1);
            m_Min.y = Mathf.Clamp(m_Min.y, 0, terrainData.detailHeight - 1);

            m_Max.x = Mathf.Clamp(m_Max.x, 0, terrainData.detailWidth);
            m_Max.y = Mathf.Clamp(m_Max.y, 0, terrainData.detailHeight);

            m_Bounds.size = new Vector2Int(m_Max.x - m_Min.x, m_Max.y - m_Min.y);
        }

        public Vector2Int GetBrushOffset(int xPos, int yPos)
        {
            return new Vector2Int(
                (m_Min.x + xPos) - (m_Bounds.x - m_Radius + m_Fraction),
                (m_Min.y + yPos) - (m_Bounds.y - m_Radius + m_Fraction)
                );
        }
    }
}
