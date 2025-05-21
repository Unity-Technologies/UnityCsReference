// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;

#pragma warning disable 649

namespace UnityEditor
{
    class RagdollBuilder
    {
        public Animator animator; // Reference to the Animator Component

        public Transform pelvis;
        public Transform leftHips, leftKnee, leftFoot;
        public Transform rightHips, rightKnee, rightFoot;
        public Transform leftArm, leftElbow;
        public Transform rightArm, rightElbow;
        public Transform middleSpine, head;

        public float totalMass = 20;
        public float strength = 0.0F;
        public bool flipForward = false;

        public string helpString { get; set; } = "Set up your ragdoll by assigning bones and ensuring a T-stand pose.";
        public string errorString { get; set; } = "";
        public bool isValid { get; set; } = true;
        Vector3 right = Vector3.right;
        Vector3 up = Vector3.up;
        Vector3 forward = Vector3.forward;

        Vector3 worldRight = Vector3.right;
        Vector3 worldUp = Vector3.up;
        Vector3 worldForward = Vector3.forward;

        private bool advancedFoldout = false;

        private static readonly Dictionary<string, HumanBodyBones> boneMapping = new()
        {
            { "Pelvis", HumanBodyBones.Hips },
            { "Left Hips", HumanBodyBones.LeftUpperLeg },
            { "Left Knee", HumanBodyBones.LeftLowerLeg },
            { "Left Foot", HumanBodyBones.LeftFoot },
            { "Right Hips", HumanBodyBones.RightUpperLeg },
            { "Right Knee", HumanBodyBones.RightLowerLeg },
            { "Right Foot", HumanBodyBones.RightFoot },
            { "Left Arm", HumanBodyBones.LeftUpperArm },
            { "Left Elbow", HumanBodyBones.LeftLowerArm },
            { "Right Arm", HumanBodyBones.RightUpperArm },
            { "Right Elbow", HumanBodyBones.RightLowerArm },
            { "Middle Spine", HumanBodyBones.Spine },
            { "Head", HumanBodyBones.Head }
        };

        internal void AutoAssignTransforms()
        {
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
            {
                Debug.LogError("Animator or Humanoid Avatar is missing!");
                return;
            }

            Clear(); // clearing all bones

            foreach (var bonePair in boneMapping)
            {
                Transform boneTransform = animator.GetBoneTransform(bonePair.Value);
                if (boneTransform != null)
                {
                    switch (bonePair.Key)
                    {
                        case "Pelvis": pelvis = boneTransform; break;
                        case "Left Hips": leftHips = boneTransform; break;
                        case "Left Knee": leftKnee = boneTransform; break;
                        case "Left Foot": leftFoot = boneTransform; break;
                        case "Right Hips": rightHips = boneTransform; break;
                        case "Right Knee": rightKnee = boneTransform; break;
                        case "Right Foot": rightFoot = boneTransform; break;
                        case "Left Arm": leftArm = boneTransform; break;
                        case "Left Elbow": leftElbow = boneTransform; break;
                        case "Right Arm": rightArm = boneTransform; break;
                        case "Right Elbow": rightElbow = boneTransform; break;
                        case "Middle Spine": middleSpine = boneTransform; break;
                        case "Head": head = boneTransform; break;
                    }
                }
                else
                {
                    Debug.LogWarning($"Bone '{bonePair.Key}' not found in Avatar.");
                }
            }
        }

        class BoneInfo
        {
            public string name;

            public Transform anchor;
            public CharacterJoint joint;
            public BoneInfo parent;

            public float minLimit;
            public float maxLimit;
            public float swingLimit;

            public Vector3 axis;
            public Vector3 normalAxis;

            public float radiusScale;
            public Type colliderType;

            public ArrayList children = new ArrayList();
            public float density;
            public float summedMass; // The mass of this and all children bodies
        }

        ArrayList bones;
        BoneInfo rootBone;

        internal void Clear()
        {
            pelvis = null;
            leftHips = null;
            leftKnee = null;
            leftFoot = null;
            rightHips = null;
            rightKnee = null;
            rightFoot = null;
            leftArm = null;
            leftElbow = null;
            rightArm = null;
            rightElbow = null;
            middleSpine = null;
            head = null;
        }

        string CheckConsistency()
        {
            PrepareBones();
            Hashtable map = new Hashtable();
            foreach (BoneInfo bone in bones)
            {
                if (bone.anchor)
                {
                    if (map[bone.anchor] != null)
                    {
                        BoneInfo oldBone = (BoneInfo)map[bone.anchor];
                        return String.Format("{0} and {1} may not be assigned to the same bone.", bone.name, oldBone.name);
                    }
                    map[bone.anchor] = bone;
                }
            }

            foreach (BoneInfo bone in bones)
            {
                if (bone.anchor == null)
                    return String.Format("{0} has not been assigned yet.\n", bone.name);
            }

            return "";
        }

        void OnDrawGizmos()
        {
            if (pelvis)
            {
                Gizmos.color = Color.red;   Gizmos.DrawRay(pelvis.position, pelvis.TransformDirection(right));
                Gizmos.color = Color.green; Gizmos.DrawRay(pelvis.position, pelvis.TransformDirection(up));
                Gizmos.color = Color.blue;  Gizmos.DrawRay(pelvis.position, pelvis.TransformDirection(forward));
            }
        }

        internal void OnGUI()
        {
            EditorGUILayout.LabelField("Use Animator component to automatically assign ragdoll bones.");

            // Animator & Auto-Fill on the same line
            EditorGUILayout.BeginHorizontal();
            animator = (Animator)EditorGUILayout.ObjectField("Animator", animator, typeof(Animator), true);
            if (GUILayout.Button("Auto-Fill", GUILayout.Width(100)))
            {
                AutoAssignTransforms();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Parameters (Total mass and strength)
            totalMass = EditorGUILayout.FloatField("Total Mass", totalMass);
            strength = EditorGUILayout.FloatField("Strength", strength);

            // Flip axis toggle (for adjusting the forward direction)
            flipForward = EditorGUILayout.Toggle("Flip Forward Axis", flipForward);

            GUILayout.Space(10);

            // Advanced Bone Setup Foldout
            advancedFoldout = EditorGUILayout.Foldout(advancedFoldout, "Advanced Bone Setup", true);
            if (advancedFoldout)
            {
                if (!string.IsNullOrEmpty(errorString))
                {
                    EditorGUILayout.HelpBox(errorString, MessageType.Error);
                }

                GUILayout.Space(10);

                // Pelvis
                pelvis = EditorGUILayout.ObjectField("Pelvis", pelvis, typeof(Transform), true) as Transform;

                // Left Leg
                leftHips = EditorGUILayout.ObjectField("Left Hips", leftHips, typeof(Transform), true) as Transform;
                leftKnee = EditorGUILayout.ObjectField("Left Knee", leftKnee, typeof(Transform), true) as Transform;
                leftFoot = EditorGUILayout.ObjectField("Left Foot", leftFoot, typeof(Transform), true) as Transform;

                // Right Leg
                rightHips = EditorGUILayout.ObjectField("Right Hips", rightHips, typeof(Transform), true) as Transform;
                rightKnee = EditorGUILayout.ObjectField("Right Knee", rightKnee, typeof(Transform), true) as Transform;
                rightFoot = EditorGUILayout.ObjectField("Right Foot", rightFoot, typeof(Transform), true) as Transform;

                // Left Arm
                leftArm = EditorGUILayout.ObjectField("Left Arm", leftArm, typeof(Transform), true) as Transform;
                leftElbow = EditorGUILayout.ObjectField("Left Elbow", leftElbow, typeof(Transform), true) as Transform;

                // Right Arm
                rightArm = EditorGUILayout.ObjectField("Right Arm", rightArm, typeof(Transform), true) as Transform;
                rightElbow = EditorGUILayout.ObjectField("Right Elbow", rightElbow, typeof(Transform), true) as Transform;

                // Spine and Head
                middleSpine = EditorGUILayout.ObjectField("Middle Spine", middleSpine, typeof(Transform), true) as Transform;
                head = EditorGUILayout.ObjectField("Head", head, typeof(Transform), true) as Transform;
            }
        }

        void DecomposeVector(out Vector3 normalCompo, out Vector3 tangentCompo, Vector3 outwardDir, Vector3 outwardNormal)
        {
            outwardNormal = outwardNormal.normalized;
            normalCompo = outwardNormal * Vector3.Dot(outwardDir, outwardNormal);
            tangentCompo = outwardDir - normalCompo;
        }

        void CalculateAxes()
        {
            if (head != null && pelvis != null)
                up = CalculateDirectionAxis(pelvis.InverseTransformPoint(head.position));
            if (rightElbow != null && pelvis != null)
            {
                Vector3 removed, temp;
                DecomposeVector(out temp, out removed, pelvis.InverseTransformPoint(rightElbow.position), up);
                right = CalculateDirectionAxis(removed);
            }

            forward = Vector3.Cross(right, up);
            if (flipForward)
                forward = -forward;
        }

        internal void OnWizardUpdate()
        {
            errorString = CheckConsistency();
            CalculateAxes();

            if (errorString.Length != 0)
            {
                helpString = "Drag Animator component to auto-fill all the ragdoll bones.\nOtherwise, manually drag all bones from the hierarchy into their slots.\nMake sure your character is in T-Stand.";
        		isValid = false;
            }
            else
            {
                helpString = "Make sure your character is in T-Stand.\nMake sure the blue axis faces in the same direction the chracter is looking.\nUse flipForward to flip the direction.";
                isValid = true;
			}
        }

        public bool hasAnyBonesAssigned => head != null || pelvis != null || leftHips != null || leftKnee != null || leftFoot != null ||
        rightHips != null || rightKnee != null || rightFoot != null || leftArm != null ||
        leftElbow != null || rightArm != null || rightElbow != null || middleSpine != null;

        void PrepareBones()
        {
            if (pelvis)
            {
                worldRight = pelvis.TransformDirection(right);
                worldUp = pelvis.TransformDirection(up);
                worldForward = pelvis.TransformDirection(forward);
            }

            bones = new ArrayList();

            rootBone = new BoneInfo();
            rootBone.name = "Pelvis";
            rootBone.anchor = pelvis;
            rootBone.parent = null;
            rootBone.density = 2.5F;
            bones.Add(rootBone);

            AddMirroredJoint("Hips", leftHips, rightHips, "Pelvis", worldRight, worldForward, -20, 70, 30, typeof(CapsuleCollider), 0.3F, 1.5F);
            AddMirroredJoint("Knee", leftKnee, rightKnee, "Hips", worldRight, worldForward, -80, 0, 0, typeof(CapsuleCollider), 0.25F, 1.5F);

            AddJoint("Middle Spine", middleSpine, "Pelvis", worldRight, worldForward, -20, 20, 10, null, 1, 2.5F);

            AddMirroredJoint("Arm", leftArm, rightArm, "Middle Spine", worldUp, worldForward, -70, 10, 50, typeof(CapsuleCollider), 0.25F, 1.0F);
            AddMirroredJoint("Elbow", leftElbow, rightElbow, "Arm", worldForward, worldUp, -90, 0, 0, typeof(CapsuleCollider), 0.20F, 1.0F);

            AddJoint("Head", head, "Middle Spine", worldRight, worldForward, -40, 25, 25, null, 1, 1.0F);
        }

        internal void OnWizardCreate()
        {
            Cleanup();
            BuildCapsules();
            AddBreastColliders();
            AddHeadCollider();

            BuildBodies();
            BuildJoints();
            CalculateMass();
        }

        BoneInfo FindBone(string name)
        {
            foreach (BoneInfo bone in bones)
            {
                if (bone.name == name)
                    return bone;
            }
            return null;
        }

        void AddMirroredJoint(string name, Transform leftAnchor, Transform rightAnchor, string parent, Vector3 worldTwistAxis, Vector3 worldSwingAxis, float minLimit, float maxLimit, float swingLimit, Type colliderType, float radiusScale, float density)
        {
            AddJoint("Left " + name, leftAnchor, parent, worldTwistAxis, worldSwingAxis, minLimit, maxLimit, swingLimit, colliderType, radiusScale, density);
            AddJoint("Right " + name, rightAnchor, parent, worldTwistAxis, worldSwingAxis, minLimit, maxLimit, swingLimit, colliderType, radiusScale, density);
        }

        void AddJoint(string name, Transform anchor, string parent, Vector3 worldTwistAxis, Vector3 worldSwingAxis, float minLimit, float maxLimit, float swingLimit, Type colliderType, float radiusScale, float density)
        {
            BoneInfo bone = new BoneInfo();
            bone.name = name;
            bone.anchor = anchor;
            bone.axis = worldTwistAxis;
            bone.normalAxis = worldSwingAxis;
            bone.minLimit = minLimit;
            bone.maxLimit = maxLimit;
            bone.swingLimit = swingLimit;
            bone.density = density;
            bone.colliderType = colliderType;
            bone.radiusScale = radiusScale;

            if (FindBone(parent) != null)
                bone.parent = FindBone(parent);
            else if (name.StartsWith("Left"))
                bone.parent = FindBone("Left " + parent);
            else if (name.StartsWith("Right"))
                bone.parent = FindBone("Right " + parent);


            bone.parent.children.Add(bone);
            bones.Add(bone);
        }

        void BuildCapsules()
        {
            foreach (BoneInfo bone in bones)
            {
                if (bone.colliderType != typeof(CapsuleCollider))
                    continue;

                int direction;
                float distance;
                if (bone.children.Count == 1)
                {
                    BoneInfo childBone = (BoneInfo)bone.children[0];
                    Vector3 endPoint = childBone.anchor.position;
                    CalculateDirection(bone.anchor.InverseTransformPoint(endPoint), out direction, out distance);
                }
                else
                {
                    Vector3 endPoint = (bone.anchor.position - bone.parent.anchor.position) + bone.anchor.position;
                    CalculateDirection(bone.anchor.InverseTransformPoint(endPoint), out direction, out distance);

                    if (bone.anchor.GetComponentsInChildren(typeof(Transform)).Length > 1)
                    {
                        Bounds bounds = new Bounds();
                        foreach (Transform child in bone.anchor.GetComponentsInChildren(typeof(Transform)))
                        {
                            bounds.Encapsulate(bone.anchor.InverseTransformPoint(child.position));
                        }

                        if (distance > 0)
                            distance = bounds.max[direction];
                        else
                            distance = bounds.min[direction];
                    }
                }

                CapsuleCollider collider = Undo.AddComponent<CapsuleCollider>(bone.anchor.gameObject);
                collider.direction = direction;

                Vector3 center = Vector3.zero;
                center[direction] = distance * 0.5F;
                collider.center = center;
                collider.height = Mathf.Abs(distance);
                collider.radius = Mathf.Abs(distance * bone.radiusScale);
            }
        }

        void Cleanup()
        {
            foreach (BoneInfo bone in bones)
            {
                if (!bone.anchor)
                    continue;

                Component[] joints = bone.anchor.GetComponentsInChildren(typeof(Joint));
                foreach (Joint joint in joints)
                    Undo.DestroyObjectImmediate(joint);

                Component[] bodies = bone.anchor.GetComponentsInChildren(typeof(Rigidbody));
                foreach (Rigidbody body in bodies)
                    Undo.DestroyObjectImmediate(body);

                Component[] colliders = bone.anchor.GetComponentsInChildren(typeof(Collider));
                foreach (Collider collider in colliders)
                    Undo.DestroyObjectImmediate(collider);
            }
        }

        void BuildBodies()
        {
            foreach (BoneInfo bone in bones)
            {
                Undo.AddComponent<Rigidbody>(bone.anchor.gameObject);
                bone.anchor.GetComponent<Rigidbody>().mass = bone.density;
            }
        }

        void BuildJoints()
        {
            foreach (BoneInfo bone in bones)
            {
                if (bone.parent == null)
                    continue;

                CharacterJoint joint = Undo.AddComponent<CharacterJoint>(bone.anchor.gameObject);
                bone.joint = joint;

                // Setup connection and axis
                joint.axis = CalculateDirectionAxis(bone.anchor.InverseTransformDirection(bone.axis));
                joint.swingAxis = CalculateDirectionAxis(bone.anchor.InverseTransformDirection(bone.normalAxis));
                joint.anchor = Vector3.zero;
                joint.connectedBody = bone.parent.anchor.GetComponent<Rigidbody>();
                joint.enablePreprocessing = false; // turn off to handle degenerated scenarios, like spawning inside geometry.

                // Setup limits
                SoftJointLimit limit = new SoftJointLimit();
                limit.contactDistance = 0; // default to zero, which automatically sets contact distance.

                limit.limit = bone.minLimit;
                joint.lowTwistLimit = limit;

                limit.limit = bone.maxLimit;
                joint.highTwistLimit = limit;

                limit.limit = bone.swingLimit;
                joint.swing1Limit = limit;

                limit.limit = 0;
                joint.swing2Limit = limit;
            }
        }

        void CalculateMassRecurse(BoneInfo bone)
        {
            float mass = bone.anchor.GetComponent<Rigidbody>().mass;
            foreach (BoneInfo child in bone.children)
            {
                CalculateMassRecurse(child);
                mass += child.summedMass;
            }
            bone.summedMass = mass;
        }

        void CalculateMass()
        {
            // Calculate allChildMass by summing all bodies
            CalculateMassRecurse(rootBone);

            // Rescale the mass so that the whole character weights totalMass
            float massScale = totalMass / rootBone.summedMass;
            foreach (BoneInfo bone in bones)
                bone.anchor.GetComponent<Rigidbody>().mass *= massScale;

            // Recalculate allChildMass by summing all bodies
            CalculateMassRecurse(rootBone);
        }

        static void CalculateDirection(Vector3 point, out int direction, out float distance)
        {
            // Calculate longest axis
            direction = 0;
            if (Mathf.Abs(point[1]) > Mathf.Abs(point[0]))
                direction = 1;
            if (Mathf.Abs(point[2]) > Mathf.Abs(point[direction]))
                direction = 2;

            distance = point[direction];
        }

        static Vector3 CalculateDirectionAxis(Vector3 point)
        {
            int direction = 0;
            float distance;
            CalculateDirection(point, out direction, out distance);
            Vector3 axis = Vector3.zero;
            if (distance > 0)
                axis[direction] = 1.0F;
            else
                axis[direction] = -1.0F;
            return axis;
        }

        static int SmallestComponent(Vector3 point)
        {
            int direction = 0;
            if (Mathf.Abs(point[1]) < Mathf.Abs(point[0]))
                direction = 1;
            if (Mathf.Abs(point[2]) < Mathf.Abs(point[direction]))
                direction = 2;
            return direction;
        }

        static int LargestComponent(Vector3 point)
        {
            int direction = 0;
            if (Mathf.Abs(point[1]) > Mathf.Abs(point[0]))
                direction = 1;
            if (Mathf.Abs(point[2]) > Mathf.Abs(point[direction]))
                direction = 2;
            return direction;
        }

        Bounds Clip(Bounds bounds, Transform relativeTo, Transform clipTransform, bool below)
        {
            int axis = LargestComponent(bounds.size);

            if (Vector3.Dot(worldUp, relativeTo.TransformPoint(bounds.max)) > Vector3.Dot(worldUp, relativeTo.TransformPoint(bounds.min)) == below)
            {
                Vector3 min = bounds.min;
                min[axis] = relativeTo.InverseTransformPoint(clipTransform.position)[axis];
                bounds.min = min;
            }
            else
            {
                Vector3 max = bounds.max;
                max[axis] = relativeTo.InverseTransformPoint(clipTransform.position)[axis];
                bounds.max = max;
            }
            return bounds;
        }

        Bounds GetBreastBounds(Transform relativeTo)
        {
            // Pelvis bounds
            Bounds bounds = new Bounds();
            bounds.Encapsulate(relativeTo.InverseTransformPoint(leftHips.position));
            bounds.Encapsulate(relativeTo.InverseTransformPoint(rightHips.position));
            bounds.Encapsulate(relativeTo.InverseTransformPoint(leftArm.position));
            bounds.Encapsulate(relativeTo.InverseTransformPoint(rightArm.position));
            Vector3 size = bounds.size;
            size[SmallestComponent(bounds.size)] = size[LargestComponent(bounds.size)] / 2.0F;
            bounds.size = size;
            return bounds;
        }

        void AddBreastColliders()
        {
            // Middle spine and pelvis
            if (middleSpine != null && pelvis != null)
            {
                Bounds bounds;
                BoxCollider box;

                // Middle spine bounds
                bounds = Clip(GetBreastBounds(pelvis), pelvis, middleSpine, false);
                box = Undo.AddComponent<BoxCollider>(pelvis.gameObject);
                box.center = bounds.center;
                box.size = bounds.size;

                bounds = Clip(GetBreastBounds(middleSpine), middleSpine, middleSpine, true);
                box = Undo.AddComponent<BoxCollider>(middleSpine.gameObject);
                box.center = bounds.center;
                box.size = bounds.size;
            }
            // Only pelvis
            else
            {
                Bounds bounds = new Bounds();
                bounds.Encapsulate(pelvis.InverseTransformPoint(leftHips.position));
                bounds.Encapsulate(pelvis.InverseTransformPoint(rightHips.position));
                bounds.Encapsulate(pelvis.InverseTransformPoint(leftArm.position));
                bounds.Encapsulate(pelvis.InverseTransformPoint(rightArm.position));

                Vector3 size = bounds.size;
                size[SmallestComponent(bounds.size)] = size[LargestComponent(bounds.size)] / 2.0F;

                BoxCollider box = Undo.AddComponent<BoxCollider>(pelvis.gameObject);
                box.center = bounds.center;
                box.size = size;
            }
        }

        void AddHeadCollider()
        {
            if (head.GetComponent<Collider>())
                Undo.DestroyObjectImmediate(head.GetComponent<Collider>());

            float radius = Vector3.Distance(leftArm.transform.position, rightArm.transform.position);
            radius /= 4;

            SphereCollider sphere = Undo.AddComponent<SphereCollider>(head.gameObject);
            sphere.radius = radius;
            Vector3 center = Vector3.zero;

            int direction;
            float distance;
            CalculateDirection(head.InverseTransformPoint(pelvis.position), out direction, out distance);
            if (distance > 0)
                center[direction] = -radius;
            else
                center[direction] = radius;
            sphere.center = center;
        }
    }
}
