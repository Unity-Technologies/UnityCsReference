// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace TreeEditor
{
    [System.Serializable]
    public class TreeNode
    {
        public TreeSpline spline = null;

        public int seed;
        public float animSeed;

        public bool visible;

        public int triStart;
        public int triEnd;
        public int vertStart;
        public int vertEnd;

        // Branches only..
        public float capRange;
        public float breakOffset;

        // Generic..
        public float size; // size or radius
        public float scale; // scale according to
        public float offset;
        public float baseAngle;
        public float angle;
        public float pitch;
        public Quaternion rotation;

        public Matrix4x4 matrix;
        public int parentID;
        public int groupID;

        // Only for internal use!
        [System.NonSerialized]
        internal TreeNode parent;
        [System.NonSerialized]
        internal TreeGroup group;

        [SerializeField]
        private int _uniqueID = -1;
        public int uniqueID
        {
            get
            {
                return _uniqueID;
            }
            set
            {
                // only allow setting if un-initialized
                if (_uniqueID == -1)
                {
                    _uniqueID = value;
                }
            }
        }

        public TreeNode()
        {
            spline = null;
            parentID = 0;
            groupID = 0;
            parent = null;
            group = null;
            seed = 1234;
            breakOffset = 1.0f;
            visible = true;
            animSeed = 0.0f;
            scale = 1.0f;
            rotation = Quaternion.identity;
            matrix = Matrix4x4.identity;
        }

        public float GetScale()
        {
            float sp = 1.0f;
            if (parent != null)
            {
                sp = parent.GetScale();
            }
            return scale * sp;
        }

        //
        // Computes the surface angle deviation, from the splines orientation.. Funky stuff!
        // ! In degrees !
        public float GetSurfaceAngleAtTime(float time)
        {
            if (spline == null)
            {
                return 0.0f;
            }
            float angle = 0.0f;

            Vector3 pos0 = spline.GetPositionAtTime(time);
            float rad0 = group.GetRadiusAtTime(this, time, false);
            if (time < 0.5f)
            {
                float difPos = (spline.GetPositionAtTime(time + 0.01f) - pos0).magnitude;
                float difRad = group.GetRadiusAtTime(this, time + 0.01f, false) - rad0;
                angle = Mathf.Atan2(difRad, difPos);
            }
            else
            {
                float disPos = (pos0 - spline.GetPositionAtTime(time - 0.01f)).magnitude;
                float difRad = rad0 - group.GetRadiusAtTime(this, time - 0.01f, false);
                angle = Mathf.Atan2(difRad, disPos);
            }

            return (angle * Mathf.Rad2Deg);
        }

        public float GetRadiusAtTime(float time)
        {
            return group.GetRadiusAtTime(this, time, false);
        }

        public void GetPropertiesAtTime(float time, out Vector3 pos, out Quaternion rot, out float rad)
        {
            if (spline == null)
            {
                pos = Vector3.zero;
                rot = Quaternion.identity;
            }
            else
            {
                pos = spline.GetPositionAtTime(time);
                rot = spline.GetRotationAtTime(time);
            }
            rad = group.GetRadiusAtTime(this, time, false);
        }

        public Matrix4x4 GetLocalMatrixAtTime(float time)
        {
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            float rad = 0.0f;

            GetPropertiesAtTime(time, out pos, out rot, out rad);

            return Matrix4x4.TRS(pos, rot, Vector3.one);
        }
    }
}
