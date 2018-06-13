// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TreeEditor
{
    [System.Serializable]
    public class TreeSpline
    {
        // members
        public SplineNode[] nodes = new SplineNode[0];
        public float tension = 0.5f; // 0.0f = linear, 0.5f = catmull-rom spline, 1.0f = over shoot

        public TreeSpline()
        {
        }

        public TreeSpline(TreeSpline o)
        {
            nodes = new SplineNode[o.nodes.Length];
            for (int i = 0; i < o.nodes.Length; i++)
            {
                nodes[i] = new SplineNode(o.nodes[i]);
            }
            tension = o.tension;
        }

        void OnDisable()
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                //    DestroyImmediate(nodes[i],true);
            }
        }

        public void Reset()
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                //   DestroyImmediate(nodes[i],true);
            }
            nodes = new SplineNode[0];
        }

        public int GetNodeCount()
        {
            return nodes.Length;
        }

        public void SetNodeCount(int c)
        {
            if (c < nodes.Length)
            {
                SplineNode[] temp = new SplineNode[c];
                for (int i = 0; i < c; i++)
                {
                    temp[i] = nodes[i];
                }
                for (int i = c; i < nodes.Length; i++)
                {
                    //   DestroyImmediate(nodes[i],true);
                }
                nodes = temp;
            }
        }

        public void RemoveNode(int c)
        {
            if (c < 0 || c >= nodes.Length) return;

            SplineNode[] temp = new SplineNode[nodes.Length - 1];
            int j = 0;
            for (int i = 0; i < nodes.Length; i++)
            {
                if (i != c)
                {
                    temp[j] = nodes[i];
                    j++;
                }
            }

            // DestroyImmediate(nodes[c], true);

            nodes = temp;
        }

        public SplineNode[] GetNodes()
        {
            return nodes;
        }

        public void AddPoint(Vector3 pos, float timeInSeconds)
        {
            SplineNode[] temp = new SplineNode[nodes.Length + 1];
            for (int i = 0; i < nodes.Length; i++)
            {
                temp[i] = nodes[i];
            }
            nodes = temp;

            SplineNode node = new SplineNode(pos, timeInSeconds);
            nodes[nodes.Length - 1] = node;

            // Add to Asset Database
            //   UnityEditor.AssetDatabase.AddObjectToAsset(node, this);
        }

        public float GetApproximateLength()
        {
            if (nodes.Length < 2) return 0.0f;

            float totalDist = 0.0f;
            for (int i = 1; i < nodes.Length; i++)
            {
                float delta = (nodes[i - 1].point - nodes[i].point).magnitude;
                totalDist += delta;
            }
            return totalDist;
        }

        public void UpdateTime()
        {
            if (nodes.Length < 2) return;

            float totalDist = GetApproximateLength();

            float curDist = 0.0f;
            nodes[0].time = curDist;
            for (int i = 1; i < nodes.Length; i++)
            {
                float delta = (nodes[i - 1].point - nodes[i].point).magnitude;
                curDist += delta;
                nodes[i].time = curDist / totalDist;
            }
        }

        public void UpdateRotations()
        {
            if (nodes.Length < 2) return;

            Matrix4x4 mat = Matrix4x4.identity;

            nodes[0].rot = Quaternion.identity;
            nodes[0].tangent = new Vector3(0, 1, 0);
            nodes[0].normal = new Vector3(0, 0, 1);

            for (int i = 1; i < nodes.Length; i++)
            {
                Vector3 upVec;

                if (i == nodes.Length - 1)
                {
                    upVec = nodes[i].point - nodes[i - 1].point;
                }
                else
                {
                    float distA = Vector3.Distance(nodes[i].point, nodes[i - 1].point);
                    float distB = Vector3.Distance(nodes[i].point, nodes[i + 1].point);
                    upVec = (nodes[i].point - nodes[i - 1].point) / distA + (nodes[i + 1].point - nodes[i].point) / distB;
                }
                upVec.Normalize();

                mat.SetColumn(1, upVec);
                if (Mathf.Abs(Vector3.Dot(upVec, mat.GetColumn(0))) > 0.9999f)
                {
                    mat.SetColumn(0, new Vector3(0, 1, 0));
                }
                Vector3 c2 = Vector3.Cross(mat.GetColumn(0), upVec).normalized;
                mat.SetColumn(2, c2);
                mat = MathUtils.OrthogonalizeMatrix(mat);

                nodes[i].rot = MathUtils.QuaternionFromMatrix(mat);
                nodes[i].normal = mat.GetColumn(2);
                nodes[i].tangent = mat.GetColumn(1);

                //
                // Make sure rotation is correct. As the same rotation can be
                // represented by two quaternions, make sure we are using the right one
                // otherwise knots appear as we interpolate over the spline
                //
                if (Quaternion.Dot(nodes[i].rot, nodes[i - 1].rot) < 0.0f)
                {
                    nodes[i].rot.x = -nodes[i].rot.x;
                    nodes[i].rot.y = -nodes[i].rot.y;
                    nodes[i].rot.z = -nodes[i].rot.z;
                    nodes[i].rot.w = -nodes[i].rot.w;
                }
            }
        }

        private Quaternion GetRotationInternal(int idxFirstpoint, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            Quaternion Q0 = nodes[Mathf.Max(idxFirstpoint - 1, 0)].rot;
            Quaternion Q1 = nodes[idxFirstpoint].rot;
            Quaternion Q2 = nodes[idxFirstpoint + 1].rot;
            Quaternion Q3 = nodes[Mathf.Min(idxFirstpoint + 2, nodes.Length - 1)].rot;

            Quaternion T1 = new Quaternion(tension * (Q2.x - Q0.x), tension * (Q2.y - Q0.y), tension * (Q2.z - Q0.z), tension * (Q2.w - Q0.w));
            Quaternion T2 = new Quaternion(tension * (Q3.x - Q1.x), tension * (Q3.y - Q1.y), tension * (Q3.z - Q1.z), tension * (Q3.w - Q1.w));

            float Blend1 = 2 * t3 - 3 * t2 + 1;
            float Blend2 = -2 * t3 + 3 * t2;
            float Blend3 = t3 - 2 * t2 + t;
            float Blend4 = t3 - t2;

            Quaternion q = new Quaternion();
            q.x = Blend1 * Q1.x + Blend2 * Q2.x + Blend3 * T1.x + Blend4 * T2.x;
            q.y = Blend1 * Q1.y + Blend2 * Q2.y + Blend3 * T1.y + Blend4 * T2.y;
            q.z = Blend1 * Q1.z + Blend2 * Q2.z + Blend3 * T1.z + Blend4 * T2.z;
            q.w = Blend1 * Q1.w + Blend2 * Q2.w + Blend3 * T1.w + Blend4 * T2.w;
            float mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            q.x /= mag;
            q.y /= mag;
            q.z /= mag;
            q.w /= mag;
            return q;
        }

        private Vector3 GetPositionInternal(int idxFirstpoint, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            Vector3 P0 = nodes[Mathf.Max(idxFirstpoint - 1, 0)].point;
            Vector3 P1 = nodes[idxFirstpoint].point;
            Vector3 P2 = nodes[idxFirstpoint + 1].point;
            Vector3 P3 = nodes[Mathf.Min(idxFirstpoint + 2, nodes.Length - 1)].point;

            Vector3 T1 = tension * (P2 - P0);
            Vector3 T2 = tension * (P3 - P1);

            float Blend1 = 2 * t3 - 3 * t2 + 1;
            float Blend2 = -2 * t3 + 3 * t2;
            float Blend3 = t3 - 2 * t2 + t;
            float Blend4 = t3 - t2;

            return Blend1 * P1 + Blend2 * P2 + Blend3 * T1 + Blend4 * T2;
        }

        public Quaternion GetRotationAtTime(float timeParam)
        {
            if (nodes.Length < 2) return Quaternion.identity;

            if (timeParam <= nodes[0].time) return nodes[0].rot;
            if (timeParam >= nodes[nodes.Length - 1].time) return nodes[nodes.Length - 1].rot;

            int c;
            for (c = 1; c < nodes.Length; c++)
            {
                if (nodes[c].time > timeParam)
                    break;
            }

            int idx = c - 1;
            float param = (timeParam - nodes[idx].time) / (nodes[idx + 1].time - nodes[idx].time);

            return GetRotationInternal(idx, param);
        }

        public Vector3 GetPositionAtTime(float timeParam)
        {
            if (nodes.Length < 2) return Vector3.zero;

            if (timeParam <= nodes[0].time) return nodes[0].point;
            if (timeParam >= nodes[nodes.Length - 1].time) return nodes[nodes.Length - 1].point;

            int c;
            for (c = 1; c < nodes.Length; c++)
            {
                if (nodes[c].time > timeParam)
                    break;
            }
            int idx = c - 1;
            float param = (timeParam - nodes[idx].time) / (nodes[idx + 1].time - nodes[idx].time);

            return GetPositionInternal(idx, param);
        }
    }
}
