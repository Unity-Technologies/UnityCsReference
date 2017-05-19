// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEngine.U2D.Interface
{
    internal interface IGL
    {
        void PushMatrix();
        void PopMatrix();
        void MultMatrix(Matrix4x4 m);
        void Begin(int mode);
        void End();
        void Color(Color c);
        void Vertex(Vector3 v);
    }

    internal class GLSystem : IGL
    {
        static IGL m_GLSystem;
        internal static void SetSystem(IGL system)
        {
            m_GLSystem = system;
        }

        internal static IGL GetSystem()
        {
            if (m_GLSystem == null)
                m_GLSystem = new GLSystem();
            return m_GLSystem;
        }

        public void PushMatrix()
        {
            GL.PushMatrix();
        }

        public void PopMatrix()
        {
            GL.PopMatrix();
        }

        public void MultMatrix(Matrix4x4 m)
        {
            GL.MultMatrix(m);
        }

        public void Begin(int mode)
        {
            GL.Begin(mode);
        }

        public void End()
        {
            GL.End();
        }

        public void Color(Color c)
        {
            GL.Color(c);
        }

        public void Vertex(Vector3 v)
        {
            GL.Vertex(v);
        }
    }
}
