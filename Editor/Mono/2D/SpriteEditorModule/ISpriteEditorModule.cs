// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Experimental.U2D;
using UnityEngine;
using UnityEditor.U2D.Interface;
using UnityEngine.U2D.Interface;

namespace UnityEditor
{
    internal interface ISpriteEditorModule
    {
        // The module name to display in UI
        string moduleName { get; }
        // Called when the module is activated by user
        void OnModuleActivate();
        // Called when user switched to another module
        void OnModuleDeactivate();
        // Called after SpriteEditorWindow drawed the sprite.
        // Handles call are in texture space
        void DoTextureGUI();
        // Draw user tool bar
        void DrawToolbarGUI(Rect drawArea);
        // Any last GUI draw. This is in the SpriteEditorWindow's space.
        // Any GUI draw will appear on top
        void OnPostGUI();
        // Called when Sprite selection changed.
        // If return false, the module will not be shown for selection
        bool CanBeActivated();
    }

    internal interface ISpriteEditor
    {
        ISpriteRectCache spriteRects { get; }
        SpriteRect selectedSpriteRect { get; set; }
        bool enableMouseMoveEvent { set; }
        bool editingDisabled { get; }
        Rect windowDimension { get; }
        ITexture2D selectedTexture { get; }
        ITexture2D previewTexture { get; }
        ISpriteEditorDataProvider spriteEditorDataProvider { get; }

        void HandleSpriteSelection();
        void RequestRepaint();
        void SetDataModified();
        void DisplayProgressBar(string title, string content, float progress);
        void ClearProgressBar();
        ITexture2D GetReadableTexture2D();
        void ApplyOrRevertModification(bool apply);
    }

    internal interface ISpriteRectCache : IUndoableObject
    {
        int Count { get; }

        SpriteRect RectAt(int i);
        void AddRect(SpriteRect r);
        void RemoveRect(SpriteRect r);
        void ClearAll();
        int GetIndex(SpriteRect spriteRect);
        bool Contains(SpriteRect spriteRect);
    }
}
