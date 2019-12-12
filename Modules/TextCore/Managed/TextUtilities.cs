// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.TextCore
{
    static class TextUtilities
    {
        static Vector3[] s_RectWorldCorners = new Vector3[4];

        /// <summary>
        /// Function used to determine if the position intersects with the RectTransform.
        /// </summary>
        /// <param name="rectTransform">A reference to the RectTranform of the text object.</param>
        /// <param name="position">Position to check for intersection.</param>
        /// <param name="camera">The scene camera which may be assigned to a Canvas using ScreenSpace Camera or WorldSpace render mode. Set to null is using ScreenSpace Overlay.</param>
        /// <returns></returns>
        public static bool IsIntersectingRectTransform(RectTransform rectTransform, Vector3 position, Camera camera)
        {
            // Convert position into Worldspace coordinates
            ScreenPointToWorldPointInRectangle(rectTransform, position, camera, out position);

            rectTransform.GetWorldCorners(s_RectWorldCorners);

            return PointIntersectRectangle(position, s_RectWorldCorners[0], s_RectWorldCorners[1], s_RectWorldCorners[2], s_RectWorldCorners[3]);
        }

        /// <summary>
        /// Function to check if a Point is contained within a Rectangle.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        static bool PointIntersectRectangle(Vector3 m, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 ab = b - a;
            Vector3 am = m - a;
            Vector3 bc = c - b;
            Vector3 bm = m - b;

            float abamDot = Vector3.Dot(ab, am);
            float bcbmDot = Vector3.Dot(bc, bm);

            return 0 <= abamDot && abamDot <= Vector3.Dot(ab, ab) && 0 <= bcbmDot && bcbmDot <= Vector3.Dot(bc, bc);
        }

        /// <summary>
        /// Method to convert ScreenPoint to WorldPoint aligned with Rectangle
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="screenPoint"></param>
        /// <param name="cam"></param>
        /// <param name="worldPoint"></param>
        /// <returns></returns>
        public static bool ScreenPointToWorldPointInRectangle(Transform transform, Vector2 screenPoint, Camera cam, out Vector3 worldPoint)
        {
            worldPoint = Vector3.zero;
            Ray ray = cam.ScreenPointToRay(screenPoint);
            float enter;
            if (!new Plane(transform.rotation * Vector3.back, transform.position).Raycast(ray, out enter))
                return false;
            worldPoint = ray.GetPoint(enter);
            return true;
        }

        struct LineSegment
        {
            public Vector3 Point1;
            public Vector3 Point2;

            public LineSegment(Vector3 p1, Vector3 p2)
            {
                Point1 = p1;
                Point2 = p2;
            }
        }


        /// <summary>
        /// Function returning the point of intersection between a line and a plane.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="point"></param>
        /// <param name="normal"></param>
        /// <param name="intersectingPoint"></param>
        /// <returns></returns>
        static bool IntersectLinePlane(LineSegment line, Vector3 point, Vector3 normal, out Vector3 intersectingPoint)
        {
            intersectingPoint = Vector3.zero;
            Vector3 u = line.Point2 - line.Point1;
            Vector3 w = line.Point1 - point;

            float d = Vector3.Dot(normal, u);
            float n = -Vector3.Dot(normal, w);

            if (Mathf.Abs(d) < Mathf.Epsilon)   // if line is parallel & co-planar to plane
            {
                return n == 0;
            }

            float sI = n / d;

            if (sI < 0 || sI > 1) // Line parallel to plane
                return false;

            intersectingPoint = line.Point1 + sI * u;

            return true;
        }

        /// <summary>
        /// Function returning the Square Distance from a Point to a Line.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static float DistanceToLine(Vector3 a, Vector3 b, Vector3 point)
        {
            Vector3 n = b - a;
            Vector3 pa = a - point;

            float c = Vector3.Dot(n, pa);

            // Closest point is a
            if (c > 0.0f)
                return Vector3.Dot(pa, pa);

            Vector3 bp = point - b;

            // Closest point is b
            if (Vector3.Dot(n, bp) > 0.0f)
                return Vector3.Dot(bp, bp);

            // Closest point is between a and b
            Vector3 e = pa - n * (c / Vector3.Dot(n, n));

            return Vector3.Dot(e, e);
        }

        /// <summary>
        /// Table used to convert character to lowercase.
        /// </summary>
        const string k_LookupStringL = "-------------------------------- !-#$%&-()*+,-./0123456789:;<=>?@abcdefghijklmnopqrstuvwxyz[-]^_`abcdefghijklmnopqrstuvwxyz{|}~-";

        /// <summary>
        /// Table used to convert character to uppercase.
        /// </summary>
        const string k_LookupStringU = "-------------------------------- !-#$%&-()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[-]^_`ABCDEFGHIJKLMNOPQRSTUVWXYZ{|}~-";


        /// <summary>
        /// Get lowercase version of this ASCII character.
        /// </summary>
        public static char ToLowerFast(char c)
        {
            if (c > k_LookupStringL.Length - 1)
                return c;

            return k_LookupStringL[c];
        }

        /// <summary>
        /// Get uppercase version of this ASCII character.
        /// </summary>
        public static char ToUpperFast(char c)
        {
            if (c > k_LookupStringU.Length - 1)
                return c;

            return k_LookupStringU[c];
        }

        /// <summary>
        /// Get uppercase version of this ASCII character.
        /// </summary>
        public static uint ToUpperASCIIFast(uint c)
        {
            if (c > k_LookupStringU.Length - 1)
                return c;

            return k_LookupStringU[(int)c];
        }

        /// <summary>
        /// Get lowercase version of this ASCII character.
        /// </summary>
        public static uint ToLowerASCIIFast(uint c)
        {
            if (c > k_LookupStringL.Length - 1)
                return c;

            return k_LookupStringL[(int)c];
        }

        /// <summary>
        /// Function which returns a simple hashcode from a string.
        /// </summary>
        /// <returns></returns>
        public static int GetHashCodeCaseSensitive(string s)
        {
            int hashCode = 0;

            for (int i = 0; i < s.Length; i++)
                hashCode = (hashCode << 5) + hashCode ^ s[i];

            return hashCode;
        }

        public static int GetHashCodeCaseInSensitive(string s)
        {
            int hashCode = 0;

            for (int i = 0; i < s.Length; i++)
                hashCode = (hashCode << 5) + hashCode ^ (int)ToUpperASCIIFast(s[i]);

            return hashCode;
        }

        /// <summary>
        /// Function which returns a simple hashcode from a string converted to lowercase.
        /// </summary>
        /// <returns></returns>
        public static uint GetSimpleHashCodeLowercase(string s)
        {
            uint hashCode = 5381;

            for (int i = 0; i < s.Length; i++)
                hashCode = (hashCode << 5) + hashCode ^ ToLowerFast(s[i]);

            return hashCode;
        }

        /// <summary>
        /// Function to convert Hex to Int
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static int HexToInt(char hex)
        {
            switch (hex)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'A': return 10;
                case 'B': return 11;
                case 'C': return 12;
                case 'D': return 13;
                case 'E': return 14;
                case 'F': return 15;
                case 'a': return 10;
                case 'b': return 11;
                case 'c': return 12;
                case 'd': return 13;
                case 'e': return 14;
                case 'f': return 15;
            }
            return 15;
        }

        /// <summary>
        /// Function to convert a properly formatted string which contains an hex value to its decimal value.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int StringHexToInt(string s)
        {
            int value = 0;

            for (int i = 0; i < s.Length; i++)
            {
                value += HexToInt(s[i]) * (int)Mathf.Pow(16, (s.Length - 1) - i);
            }

            return value;
        }
    }
}
