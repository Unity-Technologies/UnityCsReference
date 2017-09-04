// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;


namespace UnityEngine
{
    [Serializable]
    public partial struct Pose
    {
        public Vector3 position;
        public Quaternion rotation;


        public Pose(Vector3 position, Quaternion rotation) { this.position = position; this.rotation = rotation; }

        public override string ToString()
        {
            return string.Format("({0}, {1})", position.ToString(), rotation.ToString());
        }

        public string ToString(string format)
        {
            return string.Format("({0}, {1})", position.ToString(format), rotation.ToString(format));
        }

        public Pose GetTransformedBy(Pose lhs)
        {
            return new Pose
            {
                position = lhs.position + (lhs.rotation * position),
                rotation = lhs.rotation * rotation
            };
        }

        public Pose GetTransformedBy(Transform lhs)
        {
            return new Pose
            {
                position = lhs.TransformPoint(position),
                rotation = lhs.rotation * rotation
            };
        }

        public Vector3 forward
        {
            get { return (rotation * Vector3.forward); }
        }

        public Vector3 right
        {
            get { return (rotation * Vector3.right); }
        }

        public Vector3 up
        {
            get { return (rotation * Vector3.up); }
        }

        static readonly Pose k_Identity = new Pose(Vector3.zero, Quaternion.identity);
        public static Pose identity
        {
            get
            {
                return k_Identity;
            }
        }
    }
}
