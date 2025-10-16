// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <undoc/>
    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    static class PhysicsWorldRenderer
    {
        static bool s_IsInitialized = false;
        static bool s_UsingBIRP = false;
        static CommandBuffer s_RendererCommandBuffer = null;
        static DrawerGroup[] s_DrawerGroups;

        /// <undoc/>
        [RequiredByNativeCode]
        static void InitializeRendering()
        {
            // Finish if already initialized.
            if (s_IsInitialized)
                return;

            // Create the drawer groups.
            s_DrawerGroups = new DrawerGroup[PhysicsConstants.MaxWorlds];

            // Flag if using the built-in render pipeline or not.
            s_UsingBIRP = GraphicsSettings.currentRenderPipeline == null;

            // Register render callback.
            if (s_UsingBIRP)
                Camera.onPostRender += BIRP_RenderAllWorlds;
            else
                RenderPipelineManager.endCameraRendering += SRP_RenderAllWorlds;

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

            // Un-register render callback.
            if (s_UsingBIRP)
                Camera.onPostRender -= BIRP_RenderAllWorlds;
            else
                RenderPipelineManager.endCameraRendering -= SRP_RenderAllWorlds;

            // Dispose of the drawer groups.
            if (s_DrawerGroups != null)
            {
                // Dispose of all the drawers.
                foreach (var drawerGroup in s_DrawerGroups)
                {
                    drawerGroup.Dispose();
                }

                s_DrawerGroups = null;
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
        static bool IsCameraTypeValid(Camera camera)
        {
            var cameraType = camera.cameraType;
            return cameraType == CameraType.Game || cameraType == CameraType.SceneView;
        }

        /// <undoc/>
        static void BIRP_RenderAllWorlds(Camera camera)
        {
            // Ensure the camera type is valid.
            if (!IsCameraTypeValid(camera))
                return;

            // Finish if we're bypassing the low-level or rendering is not allowed.
            if (PhysicsWorld.bypassLowLevel || !PhysicsWorld.isRenderingAllowed)
                return;

            // Create the renderer command buffer.
            s_RendererCommandBuffer ??= new CommandBuffer { name = "LowLevelPhysics2D.WorldRenderer" };

            // Draw all the worlds.
            PhysicsWorld.DrawAllWorlds(GetCameraViewAABB(camera));

            // Render the final command buffer.
            Graphics.ExecuteCommandBuffer(s_RendererCommandBuffer);

            // Clear the render command buffer.
            s_RendererCommandBuffer.Clear();
        }

        /// <undoc/>
        static void SRP_RenderAllWorlds(ScriptableRenderContext context, Camera camera)
        {
            // Ensure the camera type is valid.
            if (!IsCameraTypeValid(camera))
                return;

            // Create the renderer command buffer.
            s_RendererCommandBuffer ??= new CommandBuffer { name = "LowLevelPhysics2D.WorldRenderer" };

            // Draw all the worlds.
            PhysicsWorld.DrawAllWorlds(GetCameraViewAABB(camera));

            // Render the final command buffer.
            context.ExecuteCommandBuffer(s_RendererCommandBuffer);
            context.Submit();

            // Clear the render command buffer.
            s_RendererCommandBuffer.Clear();
        }

        /// <undoc/>
        [RequiredByNativeCode]
        static void SendDrawResultsToCommandBuffer(PhysicsWorld physicsWorld, PhysicsWorld.DrawResults drawResults, PhysicsWorld.TransformPlane transformPlane, float thickness, float fillAlpha, int drawCapacity)
        {
            // Sanity.
            if (s_DrawerGroups == null || s_RendererCommandBuffer == null)
                throw new NullReferenceException("PhysicsWorldRenderer is not ready.");

            // Fetch the drawer group.
            ref DrawerGroup drawerGroup = ref s_DrawerGroups[physicsWorld.m_Index1 - 1];

            // Draw the drawer group.
            drawerGroup.Draw(rendererCommandBuffer: s_RendererCommandBuffer, drawResults: ref drawResults, thickness: thickness, fillAlpha: fillAlpha, transformPlane: transformPlane, drawCapacity: drawCapacity);
        }

        /// <undoc/>
        struct DrawerGroup : IDisposable
        {
            BaseDrawer[] m_Drawers;

            /// <undoc/>
            public readonly bool IsValid => m_Drawers != null;

            /// <undoc/>
            public void Draw(CommandBuffer rendererCommandBuffer, ref PhysicsWorld.DrawResults drawResults, float thickness, float fillAlpha, PhysicsWorld.TransformPlane transformPlane, int drawCapacity)
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

                // Draw all the drawers.
                foreach (var drawer in m_Drawers)
                    drawer.Draw(rendererCommandBuffer: rendererCommandBuffer, drawResults: ref drawResults, thickness: thickness, fillAlpha: fillAlpha, transformPlane: transformPlane, drawCapacity: drawCapacity);
            }

            /// <undoc/>
            public void Dispose()
            {
                if (!IsValid)
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

                protected Mesh m_Mesh = null;
                protected GraphicsBuffer m_GraphicsBuffer = new(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
                protected GraphicsBuffer.IndirectDrawIndexedArgs[] m_CommandData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
                protected ComputeBuffer m_ElementBuffer;
                protected Material m_ShaderMaterial;
                protected MaterialPropertyBlock m_ShaderMaterialPropertyBlock;
                protected readonly Bounds m_CullingBounds = new(Vector3.zero, 100000 * Vector3.one);
                protected readonly int m_ElementBufferShaderProperty = Shader.PropertyToID("element_buffer");
                protected readonly int m_TransformPlaneShaderProperty = Shader.PropertyToID("transform_plane");

                /// <undoc/>
                protected Mesh GetMesh()
                {
                    if (m_Mesh == null)
                    {
                        m_Mesh = new()
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

                    return m_Mesh;
                }

                /// <undoc/>
                protected readonly int m_ThicknessShaderProperty = Shader.PropertyToID("thickness");

                /// <undoc/>
                protected readonly int m_FillAlphaShaderProperty = Shader.PropertyToID("fillAlpha");

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

                    if (m_Mesh != null)
                    {
                        Object.DestroyImmediate(m_Mesh);
                        m_Mesh = null;
                    }

                    if (m_ShaderMaterial != null)
                    {
                        Resources.UnloadAsset(m_ShaderMaterial);
                        m_ShaderMaterial = null;
                    }

                    // Flag as disposed.
                    m_Disposed = true;
                }

                /// <undoc/>
                public abstract void Draw(CommandBuffer rendererCommandBuffer, ref PhysicsWorld.DrawResults drawResults, float thickness, float fillAlpha, PhysicsWorld.TransformPlane transformPlane, int drawCapacity);
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
                public override void Draw(CommandBuffer rendererCommandBuffer, ref PhysicsWorld.DrawResults drawResults, float thickness, float fillAlpha, PhysicsWorld.TransformPlane transformPlane, int drawCapacity)
                {
                    var polygonGeometryElements = drawResults.polygonGeometryArray;
                    var count = polygonGeometryElements.Length;
                    if (count == 0)
                        return;

                    // Set-up command buffer.
                    m_CommandData[0].indexCountPerInstance = GetMesh().GetIndexCount(0);
                    m_CommandData[0].instanceCount = (uint)count;
                    m_GraphicsBuffer.SetData(m_CommandData);

                    // Set-up compute buffer.
                    if (m_ElementBuffer == null)
                    {
                        m_ElementBuffer = new ComputeBuffer(Mathf.Max(count, drawCapacity), PhysicsWorld.DrawResults.PolygonGeometryElement.Size());
                    }
                    else if (m_ElementBuffer.count < count)
                    {
                        m_ElementBuffer.Release();
                        m_ElementBuffer = new ComputeBuffer(count, PhysicsWorld.DrawResults.PolygonGeometryElement.Size());
                    }
                    m_ElementBuffer.SetData(polygonGeometryElements);

                    // Set up the material property block.
                    m_ShaderMaterialPropertyBlock.SetBuffer(m_ElementBufferShaderProperty, m_ElementBuffer);
                    m_ShaderMaterialPropertyBlock.SetInteger(m_TransformPlaneShaderProperty, (int)transformPlane);
                    m_ShaderMaterialPropertyBlock.SetFloat(m_ThicknessShaderProperty, thickness);
                    m_ShaderMaterialPropertyBlock.SetFloat(m_FillAlphaShaderProperty, fillAlpha);

                    // Draw to the renderer command buffer.
                    rendererCommandBuffer.DrawMeshInstancedIndirect(GetMesh(), 0, m_ShaderMaterial, 0, m_GraphicsBuffer, 0, m_ShaderMaterialPropertyBlock);
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
                public override void Draw(CommandBuffer rendererCommandBuffer, ref PhysicsWorld.DrawResults drawResults, float thickness, float fillAlpha, PhysicsWorld.TransformPlane transformPlane, int drawCapacity)
                {
                    var circleGeometryElements = drawResults.circleGeometryArray;
                    var count = circleGeometryElements.Length;
                    if (count == 0)
                        return;

                    // Set-up command buffer.
                    m_CommandData[0].indexCountPerInstance = GetMesh().GetIndexCount(0);
                    m_CommandData[0].instanceCount = (uint)count;
                    m_GraphicsBuffer.SetData(m_CommandData);

                    // Set-up compute buffer.
                    if (m_ElementBuffer == null)
                    {
                        m_ElementBuffer = new ComputeBuffer(Mathf.Max(count, drawCapacity), PhysicsWorld.DrawResults.CircleGeometryElement.Size());
                    }
                    else if (m_ElementBuffer.count < count)
                    {
                        m_ElementBuffer.Release();
                        m_ElementBuffer = new ComputeBuffer(count, PhysicsWorld.DrawResults.CircleGeometryElement.Size());
                    }
                    m_ElementBuffer.SetData(circleGeometryElements);

                    // Set up the material property block.
                    m_ShaderMaterialPropertyBlock.SetBuffer(m_ElementBufferShaderProperty, m_ElementBuffer);
                    m_ShaderMaterialPropertyBlock.SetInteger(m_TransformPlaneShaderProperty, (int)transformPlane);
                    m_ShaderMaterialPropertyBlock.SetFloat(m_ThicknessShaderProperty, thickness);
                    m_ShaderMaterialPropertyBlock.SetFloat(m_FillAlphaShaderProperty, fillAlpha);

                    // Draw to the renderer command buffer.
                    rendererCommandBuffer.DrawMeshInstancedIndirect(GetMesh(), 0, m_ShaderMaterial, 0, m_GraphicsBuffer, 0, m_ShaderMaterialPropertyBlock);
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
                public override void Draw(CommandBuffer rendererCommandBuffer, ref PhysicsWorld.DrawResults drawResults, float thickness, float fillAlpha, PhysicsWorld.TransformPlane transformPlane, int drawCapacity)
                {
                    var capsuleGeometryElements = drawResults.capsuleGeometryArray;
                    var count = capsuleGeometryElements.Length;
                    if (count == 0)
                        return;

                    // Set-up command buffer.
                    m_CommandData[0].indexCountPerInstance = GetMesh().GetIndexCount(0);
                    m_CommandData[0].instanceCount = (uint)count;
                    m_GraphicsBuffer.SetData(m_CommandData);

                    // Set-up compute buffer.
                    if (m_ElementBuffer == null)
                    {
                        m_ElementBuffer = new ComputeBuffer(Mathf.Max(count, drawCapacity), PhysicsWorld.DrawResults.CapsuleGeometryElement.Size());
                    }
                    else if (m_ElementBuffer.count < count)
                    {
                        m_ElementBuffer.Release();
                        m_ElementBuffer = new ComputeBuffer(count, PhysicsWorld.DrawResults.CapsuleGeometryElement.Size());
                    }
                    m_ElementBuffer.SetData(capsuleGeometryElements);

                    // Set up the material property block.
                    m_ShaderMaterialPropertyBlock.SetBuffer(m_ElementBufferShaderProperty, m_ElementBuffer);
                    m_ShaderMaterialPropertyBlock.SetInteger(m_TransformPlaneShaderProperty, (int)transformPlane);
                    m_ShaderMaterialPropertyBlock.SetFloat(m_ThicknessShaderProperty, thickness);
                    m_ShaderMaterialPropertyBlock.SetFloat(m_FillAlphaShaderProperty, fillAlpha);

                    // Draw to the renderer command buffer.
                    rendererCommandBuffer.DrawMeshInstancedIndirect(GetMesh(), 0, m_ShaderMaterial, 0, m_GraphicsBuffer, 0, m_ShaderMaterialPropertyBlock);
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
                public override void Draw(CommandBuffer rendererCommandBuffer, ref PhysicsWorld.DrawResults drawResults, float thickness, float fillAlpha, PhysicsWorld.TransformPlane transformPlane, int drawCapacity)
                {
                    var lineElements = drawResults.lineArray;
                    var count = lineElements.Length;
                    if (count == 0)
                        return;

                    // Set-up command buffer.
                    m_CommandData[0].indexCountPerInstance = GetMesh().GetIndexCount(0);
                    m_CommandData[0].instanceCount = (uint)count;
                    m_GraphicsBuffer.SetData(m_CommandData);

                    // Set-up compute buffer.
                    if (m_ElementBuffer == null)
                    {
                        m_ElementBuffer = new ComputeBuffer(Mathf.Max(count, drawCapacity), PhysicsWorld.DrawResults.LineElement.Size());
                    }
                    else if (m_ElementBuffer.count < count)
                    {
                        m_ElementBuffer.Release();
                        m_ElementBuffer = new ComputeBuffer(count, PhysicsWorld.DrawResults.LineElement.Size());
                    }
                    m_ElementBuffer.SetData(lineElements);

                    // Set up the material property block.
                    m_ShaderMaterialPropertyBlock.SetBuffer(m_ElementBufferShaderProperty, m_ElementBuffer);
                    m_ShaderMaterialPropertyBlock.SetInteger(m_TransformPlaneShaderProperty, (int)transformPlane);
                    m_ShaderMaterialPropertyBlock.SetFloat(m_ThicknessShaderProperty, thickness);

                    // Draw to the renderer command buffer.
                    rendererCommandBuffer.DrawMeshInstancedIndirect(GetMesh(), 0, m_ShaderMaterial, 0, m_GraphicsBuffer, 0, m_ShaderMaterialPropertyBlock);
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
                public override void Draw(CommandBuffer rendererCommandBuffer, ref PhysicsWorld.DrawResults drawResults, float thickness, float fillAlpha, PhysicsWorld.TransformPlane transformPlane, int drawCapacity)
                {
                    var pointElements = drawResults.pointArray;
                    var count = pointElements.Length;
                    if (count == 0)
                        return;

                    // Set-up command buffer.
                    m_CommandData[0].indexCountPerInstance = GetMesh().GetIndexCount(0);
                    m_CommandData[0].instanceCount = (uint)count;
                    m_GraphicsBuffer.SetData(m_CommandData);

                    // Set-up compute buffer.
                    if (m_ElementBuffer == null)
                    {
                        m_ElementBuffer = new ComputeBuffer(Mathf.Max(count, drawCapacity), PhysicsWorld.DrawResults.PointElement.Size());
                    }
                    else if (m_ElementBuffer.count < count)
                    {
                        m_ElementBuffer.Release();
                        m_ElementBuffer = new ComputeBuffer(count, PhysicsWorld.DrawResults.PointElement.Size());
                    }
                    m_ElementBuffer.SetData(pointElements);

                    // Set up the material property block.
                    m_ShaderMaterialPropertyBlock.SetBuffer(m_ElementBufferShaderProperty, m_ElementBuffer);
                    m_ShaderMaterialPropertyBlock.SetInteger(m_TransformPlaneShaderProperty, (int)transformPlane);
                    m_ShaderMaterialPropertyBlock.SetFloat(m_ThicknessShaderProperty, thickness);

                    // Draw to the renderer command buffer.
                    rendererCommandBuffer.DrawMeshInstancedIndirect(GetMesh(), 0, m_ShaderMaterial, 0, m_GraphicsBuffer, 0, m_ShaderMaterialPropertyBlock);
                }
            }
        }
    }
}
