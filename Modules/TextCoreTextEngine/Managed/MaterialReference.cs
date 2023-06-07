// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.TextCore.Text
{
    struct MaterialReference
    {
        public int index;
        public FontAsset fontAsset;
        public SpriteAsset spriteAsset;
        public Material material;
        public bool isFallbackMaterial;
        public Material fallbackMaterial;
        public float padding;
        public int referenceCount;

        /// <summary>
        /// Constructor for new Material Reference.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="fontAsset"></param>
        /// <param name="spriteAsset"></param>
        /// <param name="material"></param>
        /// <param name="padding"></param>
        public MaterialReference(int index, FontAsset fontAsset, SpriteAsset spriteAsset, Material material, float padding)
        {
            this.index = index;
            this.fontAsset = fontAsset;
            this.spriteAsset = spriteAsset;
            this.material = material;
            isFallbackMaterial = false;
            fallbackMaterial = null;
            this.padding = padding;
            referenceCount = 0;
        }

        /// <summary>
        /// Function to check if a certain font asset is contained in the material reference array.
        /// </summary>
        /// <param name="materialReferences"></param>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        public static bool Contains(MaterialReference[] materialReferences, FontAsset fontAsset)
        {
            int id = fontAsset.GetHashCode();

            for (int i = 0; i < materialReferences.Length && materialReferences[i].fontAsset != null; i++)
            {
                if (materialReferences[i].fontAsset.GetHashCode() == id)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Function to add a new material reference and returning its index in the material reference array.
        /// </summary>
        /// <param name="material"></param>
        /// <param name="fontAsset"></param>
        /// <param name="materialReferences"></param>
        /// <param name="materialReferenceIndexLookup"></param>
        /// <returns></returns>
        public static int AddMaterialReference(Material material, FontAsset fontAsset, ref MaterialReference[] materialReferences, Dictionary<int, int> materialReferenceIndexLookup)
        {
            int materialId = material.GetHashCode();
            int index;

            if (materialReferenceIndexLookup.TryGetValue(materialId, out index))
            {
                return index;
            }

            index = materialReferenceIndexLookup.Count;

            // Add new reference index
            materialReferenceIndexLookup[materialId] = index;

            if (index >= materialReferences.Length)
                Array.Resize(ref materialReferences, Mathf.NextPowerOfTwo(index + 1));

            materialReferences[index].index = index;
            materialReferences[index].fontAsset = fontAsset;
            materialReferences[index].spriteAsset = null;
            materialReferences[index].material = material;
            materialReferences[index].referenceCount = 0;

            return index;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="material"></param>
        /// <param name="spriteAsset"></param>
        /// <param name="materialReferences"></param>
        /// <param name="materialReferenceIndexLookup"></param>
        /// <returns></returns>
        public static int AddMaterialReference(Material material, SpriteAsset spriteAsset, ref MaterialReference[] materialReferences, Dictionary<int, int> materialReferenceIndexLookup)
        {
            int materialId = material.GetHashCode();
            int index;

            if (materialReferenceIndexLookup.TryGetValue(materialId, out index))
                return index;

            index = materialReferenceIndexLookup.Count;

            // Add new reference index
            materialReferenceIndexLookup[materialId] = index;

            if (index >= materialReferences.Length)
                Array.Resize(ref materialReferences, Mathf.NextPowerOfTwo(index + 1));

            materialReferences[index].index = index;
            materialReferences[index].fontAsset = materialReferences[0].fontAsset;
            materialReferences[index].spriteAsset = spriteAsset;
            materialReferences[index].material = material;
            materialReferences[index].referenceCount = 0;

            return index;
        }
    }
}
