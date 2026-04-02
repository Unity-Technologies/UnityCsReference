// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View
{
    partial class TransitionElement : VisualElement
    {
        // [UxmlElement] does no codegen in trunk (6000.2); we have to provide the generated UxmlSerializedData manually.
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData),
                    new UxmlAttributeNames[]
                    {
                        new(nameof(blendType), "blend-type"),
                    }, true);
            }

#pragma warning disable 649
            [SerializeField] Type blendType;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags blendType_UxmlAttributeFlags;
#pragma warning restore 649

            public override object CreateInstance() => new TransitionElement();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (TransitionElement)obj;
                if (ShouldWriteAttributeValue(blendType_UxmlAttributeFlags))
                    e.type = blendType;
            }
        }
        public enum Type
        {
            In,
            Out
        }

        public Type type { get; set; }

        public Action<MeshGenerationContext> generateBackGround;

        static readonly CustomStyleProperty<float> k_BlendLineWidth = new CustomStyleProperty<float>("--transition-line-width");
        static readonly CustomStyleProperty<Color> k_BlendLineColor = new CustomStyleProperty<Color>("--transition-line-color");
        static readonly CustomStyleProperty<Color> k_BlendColorUpper = new CustomStyleProperty<Color>("--transition-color-upper");
        static readonly CustomStyleProperty<Color> k_BlendColorLower = new CustomStyleProperty<Color>("--transition-color-lower");

        public TransitionElement(Type type) : this()
        {
            this.type = type;
        }

        public TransitionElement()
        {
            generateVisualContent += GenerateVisualContent;
            style.overflow = new StyleEnum<Overflow>(Overflow.Hidden);
        }

        void GenerateVisualContent(MeshGenerationContext context)
        {
            if (type == Type.In)
                DrawInBlend(context);
            else if (type == Type.Out)
                DrawOutBlend(context);
        }

        void DrawInBlend(MeshGenerationContext context)
        {
            Rect localRect = contentRect;
            var topRight = new Vector2(localRect.xMax, localRect.yMin);
            var bottomLeft = new Vector2(localRect.xMin, localRect.yMax);
            DrawBlend(context, topRight, bottomLeft, localRect.max, localRect.min);
        }

        void DrawOutBlend(MeshGenerationContext context)
        {
            Rect localRect = contentRect;
            var bottomLeft = new Vector2(localRect.xMin, localRect.yMax);
            var topRight = new Vector2(localRect.xMax, localRect.yMin);
            DrawBlend(context, localRect.max, localRect.min, bottomLeft, topRight);
        }

        void DrawBlend(MeshGenerationContext context, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            if (customStyle.TryGetValue(k_BlendColorLower, out Color lowerColor))
            {
                context.DrawTriangle(a, c, b, lowerColor);
            }

            if (customStyle.TryGetValue(k_BlendColorUpper, out Color upperColor))
            {
                context.DrawTriangle(a, b, d, upperColor);
            }

            generateBackGround?.Invoke(context);

            if (customStyle.TryGetValue(k_BlendLineWidth, out float width))
            {
                customStyle.TryGetValue(k_BlendLineColor, out Color color);
                context.DrawLine2D(a, b, width, color);
            }
        }
    }
}
