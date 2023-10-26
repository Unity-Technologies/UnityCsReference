// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // CollisionFlags is a bitmask returned by CharacterController.Move.
    public enum CollisionFlags
    {
        None = 0,
        Sides = 1,
        Above = 2,
        Below = 4,
        CollidedSides = 1,
        CollidedAbove = 2,
        CollidedBelow = 4
    }

    // ControllerColliderHit is used by CharacterController.OnControllerColliderHit to give detailed information about the collision and how to deal with it.
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public partial class ControllerColliderHit
    {
        internal CharacterController m_Controller;
        internal Collider m_Collider;
        internal Vector3 m_Point;
        internal Vector3 m_Normal;
        internal Vector3 m_MoveDirection;
        internal float m_MoveLength;
        internal int m_Push;

        public CharacterController controller { get { return m_Controller; } }
        public Collider collider { get { return m_Collider; } }
        public Rigidbody rigidbody { get { return m_Collider.attachedRigidbody; } }
        public GameObject gameObject { get { return m_Collider.gameObject; } }
        public Transform transform { get { return m_Collider.transform; } }
        public Vector3 point { get { return m_Point; } }
        public Vector3 normal { get { return m_Normal; } }
        public Vector3 moveDirection { get { return m_MoveDirection; } }
        public float moveLength { get { return m_MoveLength; } }
        private bool push { get { return m_Push != 0; } set { m_Push = value ? 1 : 0; } }
    }

    [NativeHeader("Modules/Physics/CharacterController.h")]
    public class CharacterController : Collider
    {
        extern public bool SimpleMove(Vector3 speed);
        extern public CollisionFlags Move(Vector3 motion);
        extern public Vector3 velocity { get; }
        extern public bool isGrounded { [NativeName("IsGrounded")] get; }
        extern public CollisionFlags collisionFlags { get; }
        extern public float radius { get; set; }
        extern public float height { get; set; }
        extern public Vector3 center { get; set; }
        extern public float slopeLimit { get; set; }
        extern public float stepOffset { get; set; }
        extern public float skinWidth { get; set; }
        extern public float minMoveDistance { get; set; }
        extern public bool detectCollisions { get; set; }
        extern public bool enableOverlapRecovery { get; set; }
        extern internal bool isSupported { get; }
    }
}
