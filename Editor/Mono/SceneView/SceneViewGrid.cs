// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEditor;
using System.Collections;

namespace UnityEditor
{
    [System.Serializable]
    internal class SceneViewGrid
    {
        static PrefColor kViewGridColor = new PrefColor("Scene/Grid", .5f, .5f, .5f, .4f);

        public void Register(SceneView source)
        {
            // hook up the anims, so repainting can work correctly
            xGrid.valueChanged.AddListener(source.Repaint);
            yGrid.valueChanged.AddListener(source.Repaint);
            zGrid.valueChanged.AddListener(source.Repaint);
        }

        [SerializeField]
        AnimBool xGrid = new AnimBool();
        [SerializeField]
        AnimBool yGrid = new AnimBool();
        [SerializeField]
        AnimBool zGrid = new AnimBool();

        public DrawGridParameters PrepareGridRender(Camera camera, Vector3 pivot, Quaternion rotation,
            float size, bool orthoMode, bool gridVisible
            )
        {
            bool _xGrid = false, _yGrid = false, _zGrid = false;
            if (gridVisible)
            {
                if (orthoMode)
                {
                    Vector3 fwd = rotation * Vector3.forward;
                    // Show horizontal grid as long as angle is not too small
                    if (Mathf.Abs(fwd.y) > 0.2f)
                        _yGrid = true;
                    // Show xy and zy planes only when straight on
                    else if (fwd == Vector3.left || fwd == Vector3.right)
                        _xGrid = true;
                    else if (fwd == Vector3.forward || fwd == Vector3.back)
                        _zGrid = true;
                }
                else
                {
                    _yGrid = true;
                }
            }

            xGrid.target = _xGrid;
            yGrid.target = _yGrid;
            zGrid.target = _zGrid;

            DrawGridParameters parameters;
            parameters.pivot  = pivot;
            parameters.color  = kViewGridColor;
            parameters.size   = size;
            parameters.alphaX = xGrid.faded;
            parameters.alphaY = yGrid.faded;
            parameters.alphaZ = zGrid.faded;


            return parameters;
        }
    }
} // namespace
