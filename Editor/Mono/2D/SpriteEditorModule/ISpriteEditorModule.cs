// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.U2D
{
    public abstract class SpriteEditorModuleBase
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
