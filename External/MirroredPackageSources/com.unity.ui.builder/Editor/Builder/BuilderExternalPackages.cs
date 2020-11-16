using UnityEditor;
using UnityEngine;

namespace Unity.UI.Builder
{
    static class BuilderExternalPackages
    {
        public static bool isVectorGraphicsInstalled
        {
            get
            {
                return false;
            }
        }

        public static bool is2DSpriteEditorInstalled
        {
            get
            {
                return false;
            }
        }

        public static void Open2DSpriteEditor(Object value)
        {
        }
    }
}
