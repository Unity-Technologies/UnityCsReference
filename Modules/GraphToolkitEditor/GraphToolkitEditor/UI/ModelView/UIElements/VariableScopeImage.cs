// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class VariableScopeImage : VisualElement
    {
        /// <summary>
        /// The USS class name added to a <see cref="VariableScopeImage"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-scope-image";

        /// <summary>
        /// The USS class name of the data type added to the scope image.
        /// </summary>
        public static readonly string scopeImageDataTypeClassNamePrefix = ussClassName.WithUssModifier(GraphElementHelper.dataTypeClassUssModifierPrefix);

        /// <summary>
        /// Create an instance of the <see cref="VariableScopeImage"/> class.
        /// </summary>
        public VariableScopeImage()
        {
            generateVisualContent += GenerateVisualContent;
            pickingMode = PickingMode.Ignore;
            AddToClassList(ussClassName);
            this.AddPackageStylesheet("VariableScopeImage.uss");
        }

        VariableScope m_Scope;
        ModifierFlags m_ReadWriteModifiers;
        Color m_Color = Port.DefaultPortColor;

        /// <summary>
        /// The <see cref="VariableScope"/> displayed by the <see cref="VariableScopeImage"/>.
        /// </summary>
        public VariableScope Scope
        {
            get => m_Scope;
            set
            {
                if (m_Scope != value)
                {
                    m_Scope = value;
                    MarkDirtyRepaint();
                }
            }
        }

        /// <summary>
        /// The <see cref="ModifierFlags"/> displayed by the <see cref="VariableScopeImage"/>.
        /// </summary>
        public ModifierFlags ReadWriteModifiers
        {
            get => m_ReadWriteModifiers;
            set
            {
                if (m_ReadWriteModifiers != value)
                {
                    m_ReadWriteModifiers = value;
                    MarkDirtyRepaint();
                }
            }
        }

        /// <summary>
        /// the color if the image.
        /// </summary>
        public Color Color
        {
            get => m_Color;
            set
            {
                if (m_Color != value)
                {
                    m_Color = value;
                    MarkDirtyRepaint();
                }
            }
        }

        void GenerateVisualContent(MeshGenerationContext mgc)
        {
            const float externalBorderWidth = 4;
            const float exposedTriangleSide = 10;
            var bounds = localBound;
            bounds.position = Vector2.zero;
            var p2d = mgc.painter2D;
            p2d.fillColor = Color;

            if (m_Scope == VariableScope.Exposed)
            {
                p2d.BeginPath();

                if (ReadWriteModifiers.HasFlag(ModifierFlags.Write))
                {
                    p2d.MoveTo(new Vector2(bounds.xMax - exposedTriangleSide, bounds.yMin));
                    p2d.LineTo(new Vector2(bounds.xMax, bounds.yMin + exposedTriangleSide));

                    var topRightCornerRadius = parent.resolvedStyle.borderTopRightRadius;
                    if (topRightCornerRadius > 1)
                    {
                        topRightCornerRadius -= 1;
                        p2d.ArcTo(new Vector2(bounds.xMax, bounds.yMin),
                            new Vector2(bounds.xMax - topRightCornerRadius, bounds.yMin),
                            topRightCornerRadius
                        );
                    }
                    else
                    {
                        p2d.LineTo(new Vector2(bounds.xMax, bounds.yMin));
                    }
                }
                else
                {
                    p2d.MoveTo(new Vector2(bounds.xMin + exposedTriangleSide, bounds.yMin));
                    p2d.LineTo(new Vector2(bounds.xMin, bounds.yMin + exposedTriangleSide));

                    var topLeftCornerRadius = parent.resolvedStyle.borderTopLeftRadius;
                    if (topLeftCornerRadius > 1)
                    {
                        topLeftCornerRadius -= 1;
                        p2d.ArcTo(new Vector2(bounds.xMin, bounds.yMin),
                            new Vector2(bounds.xMin + topLeftCornerRadius, bounds.yMin),
                            topLeftCornerRadius
                        );
                    }
                    else
                    {
                        p2d.LineTo(new Vector2(bounds.xMin, bounds.yMin));
                    }
                }

                p2d.ClosePath();
                p2d.Fill();
            }


            if (ReadWriteModifiers.HasFlag(ModifierFlags.Read))
            {
                p2d.BeginPath();
                p2d.MoveTo(new Vector2(bounds.xMin + externalBorderWidth, bounds.yMin));
                p2d.LineTo(new Vector2(bounds.xMin + externalBorderWidth, bounds.yMax));

                var bottomLeftCornerRadius = parent.resolvedStyle.borderBottomLeftRadius;
                if (bottomLeftCornerRadius > 1)
                {
                    bottomLeftCornerRadius -= 1;
                    p2d.ArcTo(new Vector2(bounds.xMin, bounds.yMax),
                        new Vector2(bounds.xMin, bounds.yMax - bottomLeftCornerRadius),
                        bottomLeftCornerRadius
                    );
                }
                else
                {
                    p2d.LineTo(new Vector2(bounds.xMin, bounds.yMax));
                }

                var topLeftCornerRadius = parent.resolvedStyle.borderTopLeftRadius;
                if (topLeftCornerRadius > 1)
                {
                    topLeftCornerRadius -= 1;
                    p2d.ArcTo(new Vector2(bounds.xMin, bounds.yMin),
                        new Vector2(bounds.xMin + topLeftCornerRadius, bounds.yMin),
                        topLeftCornerRadius
                    );
                }
                else
                {
                    p2d.LineTo(new Vector2(bounds.xMin, bounds.yMin));
                }

                p2d.ClosePath();
                p2d.Fill();
            }

            if (ReadWriteModifiers.HasFlag(ModifierFlags.Write))
            {
                p2d.BeginPath();
                p2d.MoveTo(new Vector2(bounds.xMax - externalBorderWidth, bounds.yMin));
                p2d.LineTo(new Vector2(bounds.xMax - externalBorderWidth, bounds.yMax));

                var bottomRightCornerRadius = parent.resolvedStyle.borderBottomRightRadius;
                if (bottomRightCornerRadius > 1)
                {
                    bottomRightCornerRadius -= 1;
                    p2d.ArcTo(new Vector2(bounds.xMax, bounds.yMax),
                        new Vector2(bounds.xMax, bounds.yMax - bottomRightCornerRadius),
                        bottomRightCornerRadius
                    );
                }
                else
                {
                    p2d.LineTo(new Vector2(bounds.xMax, bounds.yMax));
                }

                var topRightCornerRadius = parent.resolvedStyle.borderTopRightRadius;
                if (topRightCornerRadius > 1)
                {
                    topRightCornerRadius -= 1;
                    p2d.ArcTo(new Vector2(bounds.xMax, bounds.yMin),
                        new Vector2(bounds.xMax - topRightCornerRadius, bounds.yMin),
                        topRightCornerRadius
                    );
                }
                else
                {
                    p2d.LineTo(new Vector2(bounds.xMax, bounds.yMin));
                }

                p2d.ClosePath();
                p2d.Fill();
            }
        }
    }
}
