// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <undoc/>
    [StructLayout(LayoutKind.Sequential)]
    static class PhysicsWorldRenderer
    {
        static readonly string s_RenderCommandBufferName = "PhysicsCore2D.PhysicsWorld.Renderer";
        static bool s_IsInitialized = false;
        static bool s_UsingBIRP = false;
        static CommandBuffer s_RendererCommandBuffer = null;
        static DrawerGroup[] s_DrawerGroups = null;
        static Mesh s_RenderMesh = null;

        static int s_ElementBufferShaderProperty = Shader.PropertyToID("element_buffer");
        static int s_TransformPlaneShaderProperty = Shader.PropertyToID("transform_plane");
        static int s_TransformPlaneMatrixShaderProperty = Shader.PropertyToID("transform_plane_matrix");
        static int s_ThicknessShaderProperty = Shader.PropertyToID("thickness");
        static int s_FillAlphaShaderProperty = Shader.PropertyToID("fillAlpha");

        /// <undoc/>
        [RequiredByNativeCode]
        static void InitializeRendering()
        {
            // Finish if already initialized.
            if (s_IsInitialized)
                return;

            // Flag if using the built-in render pipeline or not.
            s_UsingBIRP = GraphicsSettings.currentRenderPipeline == null;

            // Register render callback.
            if (s_UsingBIRP)
                Camera.onPostRender += RenderWorlds_BIRP;
            else
                RenderPipelineManager.endContextRendering += RenderWorlds_SRP;

            // Flag as initialized.
            s_IsInitialized = true;
        }

        /// <undoc/>
        [RequiredByNativeCode]
        static void ShutdownRendering()
        {
            // Finish if not initialized.
            if (!s_IsInitialized)
                return;

            // Unregister render callback.
            if (s_UsingBIRP)
                Camera.onPostRender -= RenderWorlds_BIRP;
            else
                RenderPipelineManager.endContextRendering -= RenderWorlds_SRP;

            // Dispose of the drawer groups.
            if (s_DrawerGroups != null)
            {
                // Dispose of all the drawers.
                foreach (var drawerGroup in s_DrawerGroups)
                {
                    drawerGroup?.Dispose();
                }

                s_DrawerGroups = null;
            }

            // Destroy the render mesh.
            if (s_RenderMesh != null)
            {
                UnityEngine.Object.DestroyImmediate(s_RenderMesh);
                s_RenderMesh = null;
            }

            // Dispose command buffer.
            if (s_RendererCommandBuffer != null)
            {
                s_RendererCommandBuffer.Dispose();
                s_RendererCommandBuffer = null;
            }

            // Flag as not initialized.
            s_IsInitialized = false;
        }

        /// <undoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Mesh GetMesh()
        {
            // Return any existing mesh.
            if (s_RenderMesh != null)
                return s_RenderMesh;

            // Create the mesh.
            return s_RenderMesh = new()
            {
                vertices = new Vector3[]
                {
                            new(-1.1f, -1.1f, 0f),
                            new(-1.1f, 1.1f, 0f),
                            new(1.1f, 1.1f, 0f),
                            new(1.1f, -1.1f, 0f)
                },
                normals = new[] { -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward },
                uv = new[] { Vector2.zero, new Vector2(0f, 1f), Vector2.one, new Vector2(1f, 0f) },
                triangles = new[] { 0, 1, 2, 2, 3, 0 }
            };
        }

        /// <undoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static PhysicsAABB GetCameraViewAABB(Camera camera)
        {
            // Use an empty AABB if not orthographic.
            if (!camera.orthographic)
                return default;

            // Calculate the orthographic view bounds.
            var cameraPosition = (Vector2)camera.transform.position;
            var orthographicSize = camera.orthographicSize;
            var extent = new Vector2(orthographicSize * camera.aspect, orthographicSize);

            return new PhysicsAABB
            {
                lowerBound = cameraPosition - extent,
                upperBound = cameraPosition + extent
            };
        }

        /// <undoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsCameraTypeValid(Camera camera) => (camera.cameraType & (CameraType.Game | CameraType.SceneView)) != 0;

        /// <undoc/>
        static void RenderWorlds_BIRP(Camera camera)
        {
            // Ensure the camera type is valid.
            if (!IsCameraTypeValid(camera))
                return;

            // Finish if rendering is not allowed and we're not always drawing worlds.
            var isRenderingAllowed = PhysicsWorld.isRenderingAllowed;
            var alwaysDrawWorlds = PhysicsWorld.alwaysDrawWorlds;
            if (!isRenderingAllowed && !alwaysDrawWorlds)
                return;

            Profiler.BeginSample("PhysicsCore2D.DrawWorlds");

            // Draw all the worlds.
            PhysicsWorld.DrawAllWorlds(drawAABB: GetCameraViewAABB(camera));

            // Render if allowed.
            if (isRenderingAllowed && s_RendererCommandBuffer != null)
            {
                Profiler.BeginSample("PhysicsCore2D.DrawWorlds.ExecuteRenderCommands (BIRP)");

                // We're not custom rendering so render the final command buffer.
                Graphics.ExecuteCommandBuffer(s_RendererCommandBuffer);

                // Clear the render command buffer.
                s_RendererCommandBuffer.Clear();

                Profiler.EndSample();
            }

            Profiler.EndSample();
        }

        /// <undoc/>
        static void RenderWorlds_SRP(ScriptableRenderContext context, List<Camera> cameras)
        {
            // Not sure if this can happen but protect against it regardless.
            if (cameras.Count == 0)
                return;

            // Fetch the base camera.
            var camera = cameras[0];

            // Ensure the camera type is valid.
            if (!IsCameraTypeValid(camera))
                return;

            // Finish if rendering is not allowed and we're not always drawing worlds.
            var isRenderingAllowed = PhysicsWorld.isRenderingAllowed;
            var alwaysDrawWorlds = PhysicsWorld.alwaysDrawWorlds;
            if (!isRenderingAllowed && !alwaysDrawWorlds)
                return;

            {
                Profiler.BeginSample("PhysicsCore2D.DrawWorlds");

                // Draw all the worlds.
                PhysicsWorld.DrawAllWorlds(drawAABB: GetCameraViewAABB(camera));

                // Render if allowed.
                if (isRenderingAllowed && s_RendererCommandBuffer != null)
                {
                    Profiler.BeginSample("PhysicsCore2D.DrawWorlds.ExecuteRenderCommands (SRP)");

                    // Render the final command buffer.
                    context.ExecuteCommandBuffer(s_RendererCommandBuffer);
                    context.Submit();

                    // Clear the render command buffer.
                    s_RendererCommandBuffer.Clear();

                    Profiler.EndSample();
                }

                Profiler.EndSample();
            }
        }

        /// <undoc/>
        [RequiredByNativeCode]
        static void SendDrawResults(bool isRenderingAllowed, bool alwaysDrawWorlds, PhysicsWorld physicsWorld, ref PhysicsWorld.DrawResults drawResults, PhysicsWorld.TransformPlane transformPlane, ref Matrix4x4 transformPlaneCustomMatrix, float thickness, float fillAlpha)
        {
            // Is rendering allowed?
            if (isRenderingAllowed)
            {
                Profiler.BeginSample("PhysicsCore2D.DrawWorlds.AddRenderCommands");

                // Yes, so create the drawer groups.
                s_DrawerGroups ??= new DrawerGroup[PhysicsWorld.maximumWorldsAllocated];

                // Create the renderer command buffer.
                s_RendererCommandBuffer ??= new CommandBuffer { name = s_RenderCommandBufferName };

                // Only draw the results if they're valid.
                if (drawResults.isValid)
                {
                    // Fetch/Initialize the drawer group.
                    var drawerGroup = s_DrawerGroups[physicsWorld.m_Index1 - 1];
                    drawerGroup ??= s_DrawerGroups[physicsWorld.m_Index1 - 1] = new DrawerGroup();

                    // Draw the drawer group.
                    drawerGroup.Draw(rendererCommandBuffer: s_RendererCommandBuffer, drawResults: ref drawResults, thickness: thickness, fillAlpha: fillAlpha, transformPlane: transformPlane, transformPlaneCustomMatrix: ref transformPlaneCustomMatrix);
                }

                Profiler.EndSample();
            }

            // Is rendering allowed or are we always drawing worlds?
            if (isRenderingAllowed || alwaysDrawWorlds)
            { 
                Profiler.BeginSample("PhysicsCore2D.DrawWorlds.WorldDrawEvent");

                // Yes, so call the world draw results event.
                PhysicsEvents.InvokeWorldDrawResultsEvent(physicsWorld, ref drawResults);

                Profiler.EndSample();
            }
        }

        /// <undoc/>
        sealed class DrawerGroup : IDisposable
        {
            BaseDrawer[] m_Drawers;

            /// <undoc/>
            public bool isValid => m_Drawers != null;

            /// <undoc/>
            public DrawerGroup()
            {
                // Create the drawers.
                m_Drawers ??= new BaseDrawer[]
                {
                    new PolygonGeometryDrawer(),
                    new CircleGeometryDrawer(),
                    new CapsuleGeometryDrawer(),
                    new LineDrawer(),
                    new PointDrawer()
                };
            }

            /// <undoc/>
            public void Draw(CommandBuffer rendererCommandBuffer, ref PhysicsWorld.DrawResults drawResults, float thickness, float fillAlpha, PhysicsWorld.TransformPlane transformPlane, ref Matrix4x4 transformPlaneCustomMatrix)
            {
                // Draw all the drawers.
                foreach (var drawer in m_Drawers)
                    drawer.Draw(rendererCommandBuffer: rendererCommandBuffer, drawResults: ref drawResults, thickness: thickness, fillAlpha: fillAlpha, transformPlane: transformPlane, transformPlaneCustomMatrix: ref transformPlaneCustomMatrix);
            }

            /// <undoc/>
            public void Dispose()
            {
                if (!isValid)
                    return;

                foreach(var drawer in m_Drawers)
                {
                    drawer.Dispose();
                }

                m_Drawers = null;
            }

            /// <summary>
            /// Base drawer when using an arbitrary drawing method.
            /// </summary>
            abstract class BaseDrawer : IDisposable
            {
                bool m_Disposed;

                protected GraphicsBuffer m_GraphicsBuffer = new(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
                protected GraphicsBuffer.IndirectDrawIndexedArgs[] m_CommandData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
                protected ComputeBuffer m_ElementBuffer;
                protected Material m_ShaderMaterial;
                protected MaterialPropertyBlock m_ShaderMaterialPropertyBlock;

                /// <undoc/>
                public void Dispose()
                {
                    // Finish if disposed.
                    if (m_Disposed)
                        return;

                    m_GraphicsBuffer?.Dispose();
                    m_GraphicsBuffer = null;
                    m_CommandData = null;

                    m_ElementBuffer?.Dispose();
                    m_ElementBuffer = null;

                    m_ShaderMaterialPropertyBlock = null;

                    if (m_ShaderMaterial != null)
                    {
                        Resources.UnloadAsset(m_ShaderMaterial);
                        m_ShaderMaterial = null;
                    }

                    // Flag as disposed.
                    m_Disposed = true;
                }

                /// <undoc/>
                public abstract void Draw(CommandBuffer rendererCommandBuffer, ref PhysicsWorld.DrawResults drawResults, float thickness, float fillAlpha, PhysicsWorld.TransformPlane transformPlane, ref Matrix4x4 transformPlaneCustomMatrix);
            }

            /// <summary>
            /// Polygon Geometry Drawer.
            /// </summary>
            sealed class PolygonGeometryDrawer : BaseDrawer
            {
                /// <undoc/>
                public PolygonGeometryDrawer()
                {
                    m_ShaderMaterial = PhysicsWorld_GetRenderMaterial("Physics2D/DrawElements/SDF_PolygonGeometry.mat", "Hidden/Physics2D/SDF_PolygonGeometry");
                    m_ShaderMaterialPropertyBlock = new MaterialPropertyBlock();
                }

                /// <undoc/>
                public override void Draw(CommandBuffer rendererCommandBuffer, ref PhysicsWorld.DrawResults drawResults, float thickness, float fillAlpha, PhysicsWorld.TransformPlane transformPlane, ref Matrix4x4 transformPlaneCustomMatrix)
                {
                    var polygonGeometryElements = drawResults.polygonGeometryArray;
                    var count = polygonGeometryElements.Length;
                    if (count == 0)
                        return;

                    Profiler.BeginSample("PhysicsCore2D.DrawWorlds.PolygonCommand");

                    // Set-up command buffer.
                    m_CommandData[0].indexCountPerInstance = GetMesh().GetIndexCount(0);
                    m_CommandData[0].instanceCount = (uint)count;
                    m_GraphicsBuffer.SetData(m_CommandData);

                    // Set-up compute buffer.
                    if (m_ElementBuffer == null)
                    {
                        m_ElementBuffer = new ComputeBuffer(count, PhysicsWorld.DrawResults.PolygonGeometryElement.Size());
                    }
                    else if (m_ElementBuffer.count < count)
                    {
                        m_ElementBuffer.Release();
                        m_ElementBuffer = new ComputeBuffer(count, PhysicsWorld.DrawResults.PolygonGeometryElement.Size());
                    }
                    m_ElementBuffer.SetData(polygonGeometryElements);

                    // Set up the material property block.
                    m_ShaderMaterialPropertyBlock.SetBuffer(s_ElementBufferShaderProperty, m_ElementBuffer);
                    m_ShaderMaterialPropertyBlock.SetInteger(s_TransformPlaneShaderProperty, (int)transformPlane);
                    m_ShaderMaterialPropertyBlock.SetMatrix(s_TransformPlaneMatrixShaderProperty, transformPlaneCustomMatrix);
                    m_ShaderMaterialPropertyBlock.SetFloat(s_ThicknessShaderProperty, thickness);
                    m_ShaderMaterialPropertyBlock.SetFloat(s_FillAlphaShaderProperty, fillAlpha);

                    // Draw to the renderer command buffer.
                    rendererCommandBuffer.DrawMeshInstancedIndirect(GetMesh(), 0, m_ShaderMaterial, 0, m_GraphicsBuffer, 0, m_ShaderMaterialPropertyBlock);

                    Profiler.EndSample();
                }
            }

            /// <summary>
            /// Circle Geometry Drawer.
            /// </summary>
            sealed class CircleGeometryDrawer : BaseDrawer
            {
                /// <undoc/>
                public CircleGeometryDrawer()
                {
                    m_ShaderMaterial = PhysicsWorld_GetRenderMaterial("Physics2D/DrawElements/SDF_CircleGeometry.mat", "Hidden/Physics2D/SDF_CircleGeometry");
                    m_ShaderMaterialPropertyBlock = new MaterialPropertyBlock();
                }

                /// <undoc/>
                public override void Draw(CommandBuffer rendererCommandBuffer, ref PhysicsWorld.DrawResults drawResults, float thickness, float fillAlpha, PhysicsWorld.TransformPlane transformPlane, ref Matrix4x4 transformPlaneCustomMatrix)
                {
                    var circleGeometryElements = drawResults.circleGeometryArray;
                    var count = circleGeometryElements.Length;
                    if (count == 0)
                        return;

                    Profiler.BeginSample("PhysicsCore2D.DrawWorlds.CircleCommand");

                    // Set-up command buffer.
                    m_CommandData[0].indexCountPerInstance = GetMesh().GetIndexCount(0);
                    m_CommandData[0].instanceCount = (uint)count;
                    m_GraphicsBuffer.SetData(m_CommandData);

                    // Set-up compute buffer.
                    if (m_ElementBuffer == null)
                    {
                        m_ElementBuffer = new ComputeBuffer(count, PhysicsWorld.DrawResults.CircleGeometryElement.Size());
                    }
                    else if (m_ElementBuffer.count < count)
                    {
                        m_ElementBuffer.Release();
                        m_ElementBuffer = new ComputeBuffer(count, PhysicsWorld.DrawResults.CircleGeometryElement.Size());
                    }
                    m_ElementBuffer.SetData(circleGeometryElements);

                    // Set up the material property block.
                    m_ShaderMaterialPropertyBlock.SetBuffer(s_ElementBufferShaderProperty, m_ElementBuffer);
                    m_ShaderMaterialPropertyBlock.SetInteger(s_TransformPlaneShaderProperty, (int)transformPlane);
                    m_ShaderMaterialPropertyBlock.SetMatrix(s_TransformPlaneMatrixShaderProperty, transformPlaneCustomMatrix);
                    m_ShaderMaterialPropertyBlock.SetFloat(s_ThicknessShaderProperty, thickness);
                    m_ShaderMaterialPropertyBlock.SetFloat(s_FillAlphaShaderProperty, fillAlpha);

                    // Draw to the renderer command buffer.
                    rendererCommandBuffer.DrawMeshInstancedIndirect(GetMesh(), 0, m_ShaderMaterial, 0, m_GraphicsBuffer, 0, m_ShaderMaterialPropertyBlock);

                    Profiler.EndSample();
                }
            }

            /// <summary>
            /// Capsule Geometry Drawer.
            /// </summary>
            sealed class CapsuleGeometryDrawer : BaseDrawer
            {
                /// <undoc/>
                public CapsuleGeometryDrawer()
                {
                    m_ShaderMaterial = PhysicsWorld_GetRenderMaterial("Physics2D/DrawElements/SDF_CapsuleGeometry.mat", "Hidden/Physics2D/SDF_CapsuleGeometry");
                    m_ShaderMaterialPropertyBlock = new MaterialPropertyBlock();
                }

                /// <undoc/>
                public override void Draw(CommandBuffer rendererCommandBuffer, ref PhysicsWorld.DrawResults drawResults, float thickness, float fillAlpha, PhysicsWorld.TransformPlane transformPlane, ref Matrix4x4 transformPlaneCustomMatrix)
                {
                    var capsuleGeometryElements = drawResults.capsuleGeometryArray;
                    var count = capsuleGeometryElements.Length;
                    if (count == 0)
                        return;

                    Profiler.BeginSample("PhysicsCore2D.DrawWorlds.CapsuleCommand");

                    // Set-up command buffer.
                    m_CommandData[0].indexCountPerInstance = GetMesh().GetIndexCount(0);
                    m_CommandData[0].instanceCount = (uint)count;
                    m_GraphicsBuffer.SetData(m_CommandData);

                    // Set-up compute buffer.
                    if (m_ElementBuffer == null)
                    {
                        m_ElementBuffer = new ComputeBuffer(count, PhysicsWorld.DrawResults.CapsuleGeometryElement.Size());
                    }
                    else if (m_ElementBuffer.count < count)
                    {
                        m_ElementBuffer.Release();
                        m_ElementBuffer = new ComputeBuffer(count, PhysicsWorld.DrawResults.CapsuleGeometryElement.Size());
                    }
                    m_ElementBuffer.SetData(capsuleGeometryElements);

                    // Set up the material property block.
                    m_ShaderMaterialPropertyBlock.SetBuffer(s_ElementBufferShaderProperty, m_ElementBuffer);
                    m_ShaderMaterialPropertyBlock.SetInteger(s_TransformPlaneShaderProperty, (int)transformPlane);
                    m_ShaderMaterialPropertyBlock.SetMatrix(s_TransformPlaneMatrixShaderProperty, transformPlaneCustomMatrix);
                    m_ShaderMaterialPropertyBlock.SetFloat(s_ThicknessShaderProperty, thickness);
                    m_ShaderMaterialPropertyBlock.SetFloat(s_FillAlphaShaderProperty, fillAlpha);

                    // Draw to the renderer command buffer.
                    rendererCommandBuffer.DrawMeshInstancedIndirect(GetMesh(), 0, m_ShaderMaterial, 0, m_GraphicsBuffer, 0, m_ShaderMaterialPropertyBlock);

                    Profiler.EndSample();
                }
            }

            /// <summary>
            /// Line Drawer.
            /// </summary>
            sealed class LineDrawer : BaseDrawer
            {
                /// <undoc/>
                public LineDrawer()
                {
                    m_ShaderMaterial = PhysicsWorld_GetRenderMaterial("Physics2D/DrawElements/SDF_Line.mat", "Hidden/Physics2D/SDF_Line");
                    m_ShaderMaterialPropertyBlock = new MaterialPropertyBlock();
                }

                /// <undoc/>
                public override void Draw(CommandBuffer rendererCommandBuffer, ref PhysicsWorld.DrawResults drawResults, float thickness, float fillAlpha, PhysicsWorld.TransformPlane transformPlane, ref Matrix4x4 transformPlaneCustomMatrix)
                {
                    var lineElements = drawResults.lineArray;
                    var count = lineElements.Length;
                    if (count == 0)
                        return;

                    Profiler.BeginSample("PhysicsCore2D.DrawWorlds.LineCommand");

                    // Set-up command buffer.
                    m_CommandData[0].indexCountPerInstance = GetMesh().GetIndexCount(0);
                    m_CommandData[0].instanceCount = (uint)count;
                    m_GraphicsBuffer.SetData(m_CommandData);

                    // Set-up compute buffer.
                    if (m_ElementBuffer == null)
                    {
                        m_ElementBuffer = new ComputeBuffer(count, PhysicsWorld.DrawResults.LineElement.Size());
                    }
                    else if (m_ElementBuffer.count < count)
                    {
                        m_ElementBuffer.Release();
                        m_ElementBuffer = new ComputeBuffer(count, PhysicsWorld.DrawResults.LineElement.Size());
                    }
                    m_ElementBuffer.SetData(lineElements);

                    // Set up the material property block.
                    m_ShaderMaterialPropertyBlock.SetBuffer(s_ElementBufferShaderProperty, m_ElementBuffer);
                    m_ShaderMaterialPropertyBlock.SetInteger(s_TransformPlaneShaderProperty, (int)transformPlane);
                    m_ShaderMaterialPropertyBlock.SetMatrix(s_TransformPlaneMatrixShaderProperty, transformPlaneCustomMatrix);
                    m_ShaderMaterialPropertyBlock.SetFloat(s_ThicknessShaderProperty, thickness);

                    // Draw to the renderer command buffer.
                    rendererCommandBuffer.DrawMeshInstancedIndirect(GetMesh(), 0, m_ShaderMaterial, 0, m_GraphicsBuffer, 0, m_ShaderMaterialPropertyBlock);

                    Profiler.EndSample();
                }
            }

            /// <summary>
            /// Point Drawer.
            /// </summary>
            sealed class PointDrawer : BaseDrawer
            {
                /// <undoc/>
                public PointDrawer()
                {
                    m_ShaderMaterial = PhysicsWorld_GetRenderMaterial("Physics2D/DrawElements/SDF_Point.mat", "Hidden/Physics2D/SDF_Point");
                    m_ShaderMaterialPropertyBlock = new MaterialPropertyBlock();
                }

                /// <undoc/>
                public override void Draw(CommandBuffer rendererCommandBuffer, ref PhysicsWorld.DrawResults drawResults, float thickness, float fillAlpha, PhysicsWorld.TransformPlane transformPlane, ref Matrix4x4 transformPlaneCustomMatrix)
                {
                    var pointElements = drawResults.pointArray;
                    var count = pointElements.Length;
                    if (count == 0)
                        return;

                    Profiler.BeginSample("PhysicsCore2D.DrawWorlds.PointCommand");

                    // Set-up command buffer.
                    m_CommandData[0].indexCountPerInstance = GetMesh().GetIndexCount(0);
                    m_CommandData[0].instanceCount = (uint)count;
                    m_GraphicsBuffer.SetData(m_CommandData);

                    // Set-up compute buffer.
                    if (m_ElementBuffer == null)
                    {
                        m_ElementBuffer = new ComputeBuffer(count, PhysicsWorld.DrawResults.PointElement.Size());
                    }
                    else if (m_ElementBuffer.count < count)
                    {
                        m_ElementBuffer.Release();
                        m_ElementBuffer = new ComputeBuffer(count, PhysicsWorld.DrawResults.PointElement.Size());
                    }
                    m_ElementBuffer.SetData(pointElements);

                    // Set up the material property block.
                    m_ShaderMaterialPropertyBlock.SetBuffer(s_ElementBufferShaderProperty, m_ElementBuffer);
                    m_ShaderMaterialPropertyBlock.SetInteger(s_TransformPlaneShaderProperty, (int)transformPlane);
                    m_ShaderMaterialPropertyBlock.SetMatrix(s_TransformPlaneMatrixShaderProperty, transformPlaneCustomMatrix);

                    // Draw to the renderer command buffer.
                    rendererCommandBuffer.DrawMeshInstancedIndirect(GetMesh(), 0, m_ShaderMaterial, 0, m_GraphicsBuffer, 0, m_ShaderMaterialPropertyBlock);

                    Profiler.EndSample();
                }
            }
        }
    }
}
