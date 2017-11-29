// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.U2D
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
        void DoMainGUI();
        // Draw user tool bar
        void DoToolbarGUI(Rect drawArea);
        // Any last GUI draw. This is in the SpriteEditorWindow's space.
        // Any GUI draw will appear on top
        void DoPostGUI();
        // If return false, the module will not be shown for selection
        bool CanBeActivated();
        // User triggers data apply. Return true to indicate the asset needs a reimport
        bool ApplyRevert(bool apply);
    }

    public abstract class SpriteEditorModuleBase : ISpriteEditorModule
    {
        public ISpriteEditor spriteEditor { get; internal set; }

        public abstract string moduleName { get; }

        public abstract bool CanBeActivated();
        public abstract void DoMainGUI();
        public abstract void DoToolbarGUI(Rect drawArea);
        public abstract void OnModuleActivate();
        public abstract void OnModuleDeactivate();
        public abstract void DoPostGUI();
        public abstract bool ApplyRevert(bool apply);
    }

    public interface ISpriteEditor
    {
        List<SpriteRect> spriteRects { set; }
        SpriteRect selectedSpriteRect { get; set; }
        bool enableMouseMoveEvent { set; }
        bool editingDisabled { get; }
        Rect windowDimension { get; }
        T GetDataProvider<T>() where T : class;

        bool HandleSpriteSelection();
        void RequestRepaint();
        void SetDataModified();
        void ApplyOrRevertModification(bool apply);
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class SpriteEditorModuleAssetPostProcessAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class RequireSpriteDataProviderAttribute : Attribute
    {
        Type[] m_Types;

        public RequireSpriteDataProviderAttribute(params Type[] types)
        {
            m_Types = types;
        }

        internal bool ContainsAllType(ISpriteEditorDataProvider provider)
        {
            return provider == null ? false : m_Types.Where(x =>
                {
                    return provider.HasDataProvider(x);
                }).Count() == m_Types.Length;
        }
    }
}
