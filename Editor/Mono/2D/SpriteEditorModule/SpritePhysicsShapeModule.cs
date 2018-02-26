// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine.U2D.Interface;
using UnityEditor.U2D.Interface;

namespace UnityEditor.U2D
{
    internal class SpritePhysicsShapeModule : SpriteOutlineModule
    {
        private readonly float kDefaultPhysicsTessellationDetail = 0.25f;
        private readonly byte kDefaultPhysicsAlphaTolerance = 200;

        public SpritePhysicsShapeModule(ISpriteEditor sem, IEventSystem ege, IUndoSystem us, IAssetDatabase ad, IGUIUtility gu, IShapeEditorFactory sef, ITexture2D outlineTexture)
            : base(sem, ege, us, ad, gu, sef, outlineTexture)
        {
            spriteEditorWindow = sem;
        }

        public override string moduleName
        {
            get { return "Custom Physics Shape"; }
        }

        private ISpriteEditor spriteEditorWindow
        {
            get; set;
        }

        protected override List<SpriteOutline> selectedShapeOutline
        {
            get { return m_Selected.physicsShape; }
            set { m_Selected.physicsShape = value; }
        }

        protected override bool HasShapeOutline(SpriteRect spriteRect)
        {
            return (spriteRect.physicsShape != null);
        }

        protected override void SetupShapeEditorOutline(SpriteRect spriteRect)
        {
            spriteRect.physicsShape = GenerateSpriteRectOutline(spriteRect.rect, spriteEditorWindow.selectedTexture,
                    Math.Abs(spriteRect.tessellationDetail - (-1f)) < Mathf.Epsilon ? kDefaultPhysicsTessellationDetail : spriteRect.tessellationDetail,
                    kDefaultPhysicsAlphaTolerance, spriteEditorWindow.spriteEditorDataProvider);
            spriteEditorWindow.SetDataModified();
        }
    }
}
