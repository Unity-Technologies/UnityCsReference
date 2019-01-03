// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public partial class LineUtility
    {
        public static void Simplify(List<Vector3> points, float tolerance, List<int> pointsToKeep)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            if (pointsToKeep == null)
                throw new ArgumentNullException("pointsToKeep");

            GeneratePointsToKeep3D(points, tolerance, pointsToKeep);
        }

        public static void Simplify(List<Vector3> points, float tolerance, List<Vector3> simplifiedPoints)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            if (simplifiedPoints == null)
                throw new ArgumentNullException("simplifiedPoints");

            GenerateSimplifiedPoints3D(points, tolerance, simplifiedPoints);
        }

        public static void Simplify(List<Vector2> points, float tolerance, List<int> pointsToKeep)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            if (pointsToKeep == null)
                throw new ArgumentNullException("pointsToKeep");

            GeneratePointsToKeep2D(points, tolerance, pointsToKeep);
        }

        public static void Simplify(List<Vector2> points, float tolerance, List<Vector2> simplifiedPoints)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            if (simplifiedPoints == null)
                throw new ArgumentNullException("simplifiedPoints");

            GenerateSimplifiedPoints2D(points, tolerance, simplifiedPoints);
        }
    }
}
