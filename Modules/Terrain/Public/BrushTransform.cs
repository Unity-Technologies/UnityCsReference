// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.TerrainAPI
{
    // represents a linear 2D transform between brush UV space and some other target XY space
    //      xy = u * brushU + v * brushV + brushOrigin
    //      uv = x * targetX + y * targetY + targetOrigin
    public struct BrushTransform
    {
        public Vector2 brushOrigin { get; }     // brush UV origin, in XY space
        public Vector2 brushU { get; }          // brush U vector, in XY space
        public Vector2 brushV { get; }          // brush V vector, in XY space

        public Vector2 targetOrigin { get; }    // XY origin, in brush UV space
        public Vector2 targetX { get; }         // X vector, in brush UV space
        public Vector2 targetY { get; }         // Y vector, in brush UV space

        public BrushTransform(Vector2 brushOrigin, Vector2 brushU, Vector2 brushV)
        {
            // invert the rotation matrix [BrushU, BrushV]
            // this gives us [X, Y] vectors in brush UV space
            // note we run the true inverse, to support non-orthogonal brush axes
            float det = brushU.x * brushV.y - brushU.y * brushV.x;
            float invDet = Mathf.Approximately(det, 0.0f) ? 1.0f : 1.0f / det;      // for non-invert-able matrices, we do 'something'
            Vector2 targetX = new Vector2(brushV.y, -brushU.y) * invDet;
            Vector2 targetY = new Vector2(-brushV.x, brushU.x) * invDet;

            // calculate XY origin in brush UV space
            Vector2 targetOrigin = -brushOrigin.x * targetX - brushOrigin.y * targetY;

            this.brushOrigin = brushOrigin;
            this.brushU = brushU;
            this.brushV = brushV;
            this.targetOrigin = targetOrigin;
            this.targetX = targetX;
            this.targetY = targetY;
        }

        public Rect GetBrushXYBounds()           // get the XY bounding rectangle around the Brush [0,1] UV space
        {
            // compute all four corners of the brush [0,1] UV space
            Vector2 pU = brushOrigin + brushU;
            Vector2 pV = brushOrigin + brushV;
            Vector2 pUV = brushOrigin + brushU + brushV;

            // compute min and max XY coordinates
            float minX = Mathf.Min(Mathf.Min(brushOrigin.x, pU.x), Mathf.Min(pV.x, pUV.x));
            float maxX = Mathf.Max(Mathf.Max(brushOrigin.x, pU.x), Mathf.Max(pV.x, pUV.x));
            float minY = Mathf.Min(Mathf.Min(brushOrigin.y, pU.y), Mathf.Min(pV.y, pUV.y));
            float maxY = Mathf.Max(Mathf.Max(brushOrigin.y, pU.y), Mathf.Max(pV.y, pUV.y));

            // return the XY bounding rectangle
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        public static BrushTransform FromRect(Rect brushRect)
        {
            Vector2 brushOrigin = brushRect.min;
            Vector2 brushU = new Vector2(brushRect.width, 0.0f);
            Vector2 brushV = new Vector2(0.0f, brushRect.height);
            return new BrushTransform(brushOrigin, brushU, brushV);
        }

        public Vector2 ToBrushUV(Vector2 targetXY)
        {
            return targetXY.x * targetX + targetXY.y * targetY + targetOrigin;
        }

        public Vector2 FromBrushUV(Vector2 brushUV)
        {
            return brushUV.x * brushU + brushUV.y * brushV + brushOrigin;
        }
    }
}
