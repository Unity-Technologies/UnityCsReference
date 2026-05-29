// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.U2D.Profiling
{
    static class TilemapProfilerMarkers
    {
        public const string k_TilemapCounterName = "Tilemaps rendered";
        public const string k_TilemapChunkCounterName = "Tilemap Chunks rendered";
        public const string k_TilemapChunkMeshesName = "Tilemap Meshes rendered";

        public static readonly string[] s_TilemapPhysicsMarkerNames = new[] {
            "TilemapCollider2D.CompositeChunkJob",
            "TilemapCollider2D.CompositeFinal",
            "Physics2D.TilemapColliderPrepareShapes",
            "Physics2D.TilemapColliderPreparePaths",
            "Physics2D.TilemapColliderTileUpdate"
        };

        public static readonly string[] s_TilemapSystemMarkerNames  = new[]
        {
            "Tilemap.Update()",
            "Tilemap.UpdateTileAnimation()",
        };

        public static readonly string[] s_TilemapRendererMarkerNames = new[]
        {
            "TilemapRenderer.CameraBoundsJob",
            "TilemapRenderer.GeometryJob",
            "TilemapRenderer.Render",
        };

        public static readonly string[] s_TilemapRendererIndividualModeMarkerNames = new[]
        {
            "TilemapRenderer.ScheduleIndividual",
            "TilemapRenderer.CountIndividualJob",
            "TilemapRenderer.CombineIndividualJob",
            "TilemapRenderer.ExecuteIndividualJob",
            "TilemapRenderer.CleanupIndividualJob",
        };

        public static readonly string[] s_TilemapRendererSRPBatchModeMarkerNames = new[]
        {
            "TilemapRenderer.ScheduleSRPBatch",
             "TilemapRenderer.ExecuteSRPBatchJob",
             "TilemapRenderer.CleanupSRPBatchJob",
             "TilemapRenderer.PrepareSRPBatchMode",
        };

        public static readonly string[] s_TilemapRendererChunkModeMarkerNames = new[]
        {
            "TilemapRenderer.DispatchJob",
            "TilemapRenderer.DistributeJobs",
            "TilemapRenderer.CalculateChunkCullingBounds",
            "TilemapRenderer.CameraBoundsCheck",
            "TilemapRenderer.PrepareChunkMode",
            "TilemapRenderer.BuildChunkJob",
            "TilemapRenderer.ClearChunks",
            "TilemapRenderer.CheckDirtyChunks",
        };
    }
}
