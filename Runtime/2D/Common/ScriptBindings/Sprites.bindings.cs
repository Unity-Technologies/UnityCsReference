// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/2D/Common/ScriptBindings/SpritesMarshalling.h")]
    [NativeType("Runtime/Graphics/SpriteFrame.h")]
    public sealed partial class Sprite
    {
        // The number of pixels in one unit. Note: The C++ side still uses the name pixelsToUnits which is misleading,
        // but has not been changed yet to minimize merge conflicts.
        public extern float pixelsPerUnit
        {
            [NativeMethod("GetPixelsToUnits")]
            get;
        }

        public extern int GetPhysicsShapeCount();

        public int GetPhysicsShapePointCount(int shapeIdx)
        {
            int physicsShapeCount = GetPhysicsShapeCount();
            if (shapeIdx < 0 || shapeIdx >= physicsShapeCount)
                throw new IndexOutOfRangeException(String.Format("Index({0}) is out of bounds(0 - {1})", shapeIdx, physicsShapeCount - 1));

            return Internal_GetPhysicsShapePointCount(shapeIdx);
        }

        [NativeMethod("GetPhysicsShapePointCount")]
        private extern int Internal_GetPhysicsShapePointCount(int shapeIdx);

        public int GetPhysicsShape(int shapeIdx, List<Vector2> physicsShape)
        {
            int physicsShapeCount = GetPhysicsShapeCount();
            if (shapeIdx < 0 || shapeIdx >= physicsShapeCount)
                throw new IndexOutOfRangeException(String.Format("Index({0}) is out of bounds(0 - {1})", shapeIdx, physicsShapeCount - 1));

            GetPhysicsShapeImpl(this, shapeIdx, physicsShape);
            return physicsShape.Count;
        }

        [FreeFunction("SpritesBindings::GetPhysicsShape", ThrowsException = true)]
        private extern static void GetPhysicsShapeImpl(Sprite sprite, int shapeIdx, List<Vector2> physicsShape);

        public void OverridePhysicsShape(IList<Vector2[]> physicsShapes)
        {
            for (int i = 0; i < physicsShapes.Count; ++i)
            {
                var physicsShape = physicsShapes[i];
                if (physicsShape == null)
                {
                    throw new ArgumentNullException(String.Format("Physics Shape at {0} is null.", i));
                }
                if (physicsShape.Length < 3)
                {
                    throw new ArgumentException(String.Format("Physics Shape at {0} has less than 3 vertices ({1}).", i, physicsShape.Length));
                }
            }

            OverridePhysicsShapeCount(this, physicsShapes.Count);
            for (int idx = 0; idx < physicsShapes.Count; ++idx)
                OverridePhysicsShape(this, physicsShapes[idx], idx);
        }

        [FreeFunction("SpritesBindings::OverridePhysicsShapeCount")]
        private extern static void OverridePhysicsShapeCount(Sprite sprite, int physicsShapeCount);

        [FreeFunction("SpritesBindings::OverridePhysicsShape", ThrowsException = true)]
        private extern static void OverridePhysicsShape(Sprite sprite, Vector2[] physicsShape, int idx);
    }
}
