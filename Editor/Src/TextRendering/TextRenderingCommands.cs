// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    static class TextRenderingCommands
    {
        [MenuItem("GameObject/3D Object/3D Text", priority = 4000)]
        static void Create3DText(MenuCommand command)
        {
            var parent = command.context as GameObject;
            var go = GOCreationCommands.CreateGameObject(parent, "New Text", typeof(MeshRenderer), typeof(TextMesh));

            var font = Selection.activeObject as Font ?? Font.GetDefault();
            TextMesh tm = go.GetComponent<TextMesh>();
            tm.text = "Hello World";
            tm.font = font;
            go.GetComponent<MeshRenderer>().material = font.material;

            GOCreationCommands.Place(go, parent);
        }
    }
}
