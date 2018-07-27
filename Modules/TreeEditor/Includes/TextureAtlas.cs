// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace TreeEditor
{
    public class TextureAtlas
    {
        public int atlasWidth;
        public int atlasHeight;
        public int atlasPadding;

        public class TextureNode
        {
            public string name;
            public Texture2D diffuseTexture;
            public Color diffuseColor;
            public Texture2D normalTexture;
            public Texture2D glossTexture;
            public Texture2D translucencyTexture;
            public Texture2D shadowOffsetTexture;
            public float shininess;

            public Vector2 scale = new Vector2(1.0f, 1.0f);
            public bool tileV = false;
            public Vector2 uvTiling;

            public Rect sourceRect = new Rect(0, 0, 0, 0);
            public Rect packedRect = new Rect(0, 0, 0, 0);
            public Rect uvRect = new Rect(0, 0, 0, 0);

            public static bool Overlap(TextureNode a, TextureNode b)
            {
                if (a.tileV || b.tileV)
                {
                    return (!((a.packedRect.x > (b.packedRect.x + b.packedRect.width)) ||
                        ((a.packedRect.x + a.packedRect.width) < b.packedRect.x)));
                }
                else
                {
                    return (!((a.packedRect.x > (b.packedRect.x + b.packedRect.width)) ||
                        ((a.packedRect.x + a.packedRect.width) < b.packedRect.x) ||
                        (a.packedRect.y > (b.packedRect.y + b.packedRect.height)) ||
                        ((a.packedRect.y + a.packedRect.height) < b.packedRect.y)));
                }
            }

            public int CompareTo(TextureNode b)
            {
                if (tileV && b.tileV) return (-packedRect.width.CompareTo(b.packedRect.width));
                if (tileV) return -1;
                if (b.tileV) return 1;
                return -packedRect.height.CompareTo(b.packedRect.height);
            }
        }

        public List<TextureNode> nodes = new List<TextureNode>();

        override public int GetHashCode()
        {
            int hash = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                hash ^= nodes[i].uvRect.GetHashCode();
            }

            return hash;
        }

        public void AddTexture(string name, Texture2D diffuse, Color diffuseColor, Texture2D normal, Texture2D gloss, Texture2D transtex, Texture2D shadowOffsetTex, float shininess, Vector2 scale, bool tileV, Vector2 uvTiling)
        {
            TextureNode node = new TextureNode();
            node.name = name;
            node.diffuseTexture = diffuse;
            node.diffuseColor = diffuseColor;
            node.normalTexture = normal;
            node.glossTexture = gloss;
            node.translucencyTexture = transtex;
            node.shadowOffsetTexture = shadowOffsetTex;
            node.shininess = shininess;
            node.scale = scale;
            node.tileV = tileV;
            node.uvTiling = uvTiling;

            if (diffuse)
            {
                node.sourceRect.width = diffuse.width;
                node.sourceRect.height = diffuse.height;
            }
            else
            {
                node.sourceRect.width = 64;
                node.sourceRect.height = 64;
                node.scale = new Vector2(1.0f, 1.0f);
            }

            nodes.Add(node);
        }

        public Vector2 GetTexTiling(string name)
        {
            //
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].name == name)
                {
                    return nodes[i].uvTiling;
                }
            }

            // No rect for this texture..
            return new Vector2(1, 1);
        }

        public Rect GetUVRect(string name)
        {
            //
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].name == name)
                {
                    return nodes[i].uvRect;
                }
            }

            // No rect for this texture..
            return new Rect(0, 0, 0, 0);
        }

        public void Pack(ref int targetWidth, int targetHeight, int padding, bool correctPow2)
        {
            //
            // Very simple packing.. top->bottom left->right
            // Uses fixed height packing to ensure, that textures can tile vertically..
            // Allows scale factor of individual textures..
            // Works best with pow2 textures..
            //

            if ((padding % 2) != 0)
            {
                Debug.LogWarning("Padding not an even number");
                padding += 1;
            }


            // Maximal height of a node, to ensure V tiling is possible
            int maxHeight = targetHeight;

            // Set corrected size according to padding and scale..
            for (int i = 0; i < nodes.Count; i++)
            {
                TextureNode node = nodes[i];

                node.packedRect.x = 0;
                node.packedRect.y = 0;
                node.packedRect.width = Mathf.Round(node.sourceRect.width * node.scale.x);
                node.packedRect.height = Mathf.Min(maxHeight, Mathf.Round(node.sourceRect.height * node.scale.y));

                if (node.tileV)
                {
                    node.packedRect.height = maxHeight;
                }

                if (correctPow2)
                {
                    node.packedRect.width = (float)Mathf.ClosestPowerOfTwo((int)node.packedRect.width);
                    node.packedRect.height = (float)Mathf.ClosestPowerOfTwo((int)node.packedRect.height);
                    //    Debug.Log("Corrected size: "+node.packedRect.width+","+node.packedRect.height);
                }
            }

            //
            // Sort nodes according to corrected size..
            nodes.Sort(delegate(TextureNode a, TextureNode b) { return a.CompareTo(b); });

            int interiorw = 0;
            int interiorh = 0;

            for (int i = 0; i < nodes.Count; i++)
            {
                TextureNode node = nodes[i];
                bool good = false;

                // left - right first
                for (int x = 0; x < interiorw; x++)
                {
                    node.packedRect.x = x;
                    node.packedRect.y = 0;
                    good = true;

                    // top - bottom
                    for (int y = 0; y <= interiorh; y++)
                    {
                        good = true;
                        node.packedRect.y = y;

                        for (int j = 0; j < i; j++)
                        {
                            TextureNode node2 = nodes[j];
                            if (TextureNode.Overlap(node, node2))
                            {
                                good = false;
                                // No point in searching for free place in top - bottom if node2.tileV is true, so exit loop 'top - bottom'
                                if (node2.tileV) y = interiorh;
                                else y = (int)(node2.packedRect.y + node2.packedRect.height);
                                break;
                            }
                        }

                        if (good) break;
                    }
                    if (good) break;
                }

                if (!good)
                {
                    // no good position inside, so push onto the right hand side at the top
                    node.packedRect.x = interiorw;
                    node.packedRect.y = 0;
                }

                interiorw = Mathf.Max(interiorw, (int)(node.packedRect.x + node.packedRect.width));
                interiorh = Mathf.Max(interiorh, (int)(node.packedRect.y + node.packedRect.height));
            }

            //
            // Minimal width is padding*2, or 64
            //
            int minimalWidth = Mathf.Max(Mathf.ClosestPowerOfTwo(padding * 2), 64);
            int finalWidth = Mathf.Clamp(Mathf.ClosestPowerOfTwo(interiorw), minimalWidth, targetWidth);

            // Best match..
            targetWidth = finalWidth;

            //
            // copy values
            atlasWidth = targetWidth;
            atlasHeight = targetHeight;
            atlasPadding = padding;

            //
            // Scale to fit
            //
            float scaleU = targetWidth / ((float)interiorw);
            float scaleV = targetHeight / ((float)interiorh);

            for (int i = 0; i < nodes.Count; i++)
            {
                TextureNode node = nodes[i];

                // packed rect
                node.packedRect.x *= scaleU;
                node.packedRect.y *= scaleV;
                node.packedRect.width *= scaleU;
                node.packedRect.height *= scaleV;

                // padding is done post scaling, to ensure margin is enough
                if (node.tileV)
                {
                    node.packedRect.y = 0.0f;
                    node.packedRect.height = targetHeight;
                    node.packedRect.x += padding / 2;
                    node.packedRect.width -= padding;
                }
                else
                {
                    node.packedRect.x += padding / 2;
                    node.packedRect.y += padding / 2;
                    node.packedRect.width -= padding;
                    node.packedRect.height -= padding;
                }

                if (node.packedRect.width < 1) node.packedRect.width = 1;
                if (node.packedRect.height < 1) node.packedRect.height = 1;

                // round to clean pixel values
                node.packedRect.x = Mathf.Round(node.packedRect.x);// - 0.5f;// +0.5f;
                node.packedRect.y = Mathf.Round(node.packedRect.y);// - 0.5f;// +0.5f;
                node.packedRect.width = Mathf.Round(node.packedRect.width);// - 0.5f;// +1.0f;// +0.5f;
                node.packedRect.height = Mathf.Round(node.packedRect.height);// - 0.5f;// +1.0f;// +0.5f;

                // uv rect
                node.uvRect.x = node.packedRect.x / targetWidth;
                node.uvRect.y = node.packedRect.y / targetHeight;
                node.uvRect.width = node.packedRect.width / targetWidth;
                node.uvRect.height = node.packedRect.height / targetHeight;
            }
        }
    }
}
