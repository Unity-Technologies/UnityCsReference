// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class AnimationClipInfoProperties
    {
        SerializedProperty m_Property;

        public AnimationClipInfoProperties(SerializedProperty prop)
        {
            m_Property = prop;
            Editor.AssignCachedProperties(this, prop);
        }

#pragma warning disable 0649
        [CacheProperty("name")]
        SerializedProperty m_Name;
        [CacheProperty("takeName")]
        SerializedProperty m_TakeName;
        [CacheProperty("internalID")]
        SerializedProperty m_InternalId;
        [CacheProperty("firstFrame")]
        SerializedProperty m_FirstFrame;
        [CacheProperty("lastFrame")]
        SerializedProperty m_LastFrame;
        [CacheProperty("wrapMode")]
        SerializedProperty m_WrapMode;
        [CacheProperty("loop")]
        SerializedProperty m_Loop;
        [CacheProperty("orientationOffsetY")]
        SerializedProperty m_OrientationOffsetY;
        [CacheProperty("level")]
        SerializedProperty m_Level;
        [CacheProperty("cycleOffset")]
        SerializedProperty m_CycleOffset;
        [CacheProperty("additiveReferencePoseFrame")]
        SerializedProperty m_AdditiveReferencePoseFrame;
        [CacheProperty("hasAdditiveReferencePose")]
        SerializedProperty m_HasAdditiveReferencePose;
        [CacheProperty("loopTime")]
        SerializedProperty m_LoopTime;
        [CacheProperty("loopBlend")]
        SerializedProperty m_LoopBlend;
        [CacheProperty("loopBlendOrientation")]
        SerializedProperty m_LoopBlendOrientation;
        [CacheProperty("loopBlendPositionY")]
        SerializedProperty m_LoopBlendPositionY;
        [CacheProperty("loopBlendPositionXZ")]
        SerializedProperty m_LoopBlendPositionXZ;
        [CacheProperty("keepOriginalOrientation")]
        SerializedProperty m_KeepOriginalOrientation;
        [CacheProperty("keepOriginalPositionY")]
        SerializedProperty m_KeepOriginalPositionY;
        [CacheProperty("keepOriginalPositionXZ")]
        SerializedProperty m_KeepOriginalPositionXZ;
        [CacheProperty("heightFromFeet")]
        SerializedProperty m_HeightFromFeet;
        [CacheProperty("mirror")]
        SerializedProperty m_Mirror;
        [CacheProperty("maskType")]
        SerializedProperty m_MaskType;
        [CacheProperty("maskSource")]
        SerializedProperty m_MaskSource;
        [CacheProperty("bodyMask")]
        SerializedProperty m_BodyMask;
        [CacheProperty("transformMask")]
        SerializedProperty m_TransformMask;
        [CacheProperty("curves")]
        SerializedProperty m_Curves;
        [CacheProperty("events")]
        SerializedProperty m_Events;
#pragma warning restore 0649

        public string name      { get { return m_Name.stringValue;      } set { m_Name.stringValue = value;      } }
        public string takeName  { get { return m_TakeName.stringValue;  } set { m_TakeName.stringValue = value;  } }
        public long internalID  { get { return m_InternalId.longValue;  } set { m_InternalId.longValue = value;  } }
        public float firstFrame { get { return m_FirstFrame.floatValue; } set { m_FirstFrame.floatValue = value; } }
        public float lastFrame  { get { return m_LastFrame.floatValue;  } set { m_LastFrame.floatValue = value;  } }
        public int wrapMode     { get { return m_WrapMode.intValue;     } set { m_WrapMode.intValue = value;     } }
        public bool loop        { get { return m_Loop.boolValue;        } set { m_Loop.boolValue = value;        } }

        // Mecanim animation properties
        public float orientationOffsetY         { get { return m_OrientationOffsetY.floatValue;         } set { m_OrientationOffsetY.floatValue = value;         } }
        public float level                      { get { return m_Level.floatValue;                      } set { m_Level.floatValue = value;                      } }
        public float cycleOffset                { get { return m_CycleOffset.floatValue;                } set { m_CycleOffset.floatValue = value;                } }
        public float additiveReferencePoseFrame { get { return m_AdditiveReferencePoseFrame.floatValue; } set { m_AdditiveReferencePoseFrame.floatValue = value; } }
        public bool  hasAdditiveReferencePose   { get { return m_HasAdditiveReferencePose.boolValue;    } set { m_HasAdditiveReferencePose.boolValue = value;    } }
        public bool  loopTime                   { get { return m_LoopTime.boolValue;                    } set { m_LoopTime.boolValue = value;                    } }
        public bool  loopBlend                  { get { return m_LoopBlend.boolValue;                   } set { m_LoopBlend.boolValue = value;                   } }
        public bool  loopBlendOrientation       { get { return m_LoopBlendOrientation.boolValue;        } set { m_LoopBlendOrientation.boolValue = value;        } }
        public bool  loopBlendPositionY         { get { return m_LoopBlendPositionY.boolValue;          } set { m_LoopBlendPositionY.boolValue = value;          } }
        public bool  loopBlendPositionXZ        { get { return m_LoopBlendPositionXZ.boolValue;         } set { m_LoopBlendPositionXZ.boolValue = value;         } }
        public bool  keepOriginalOrientation    { get { return m_KeepOriginalOrientation.boolValue;     } set { m_KeepOriginalOrientation.boolValue = value;     } }
        public bool  keepOriginalPositionY      { get { return m_KeepOriginalPositionY.boolValue;       } set { m_KeepOriginalPositionY.boolValue = value;       } }
        public bool  keepOriginalPositionXZ     { get { return m_KeepOriginalPositionXZ.boolValue;      } set { m_KeepOriginalPositionXZ.boolValue = value;      } }
        public bool  heightFromFeet             { get { return m_HeightFromFeet.boolValue;              } set { m_HeightFromFeet.boolValue = value;              } }
        public bool  mirror                     { get { return m_Mirror.boolValue;                      } set { m_Mirror.boolValue = value;                      } }
        public ClipAnimationMaskType maskType   { get { return (ClipAnimationMaskType)m_MaskType.intValue;    } set { m_MaskType.intValue = (int)value;          } }
        public AvatarMask maskSource            { get { return (AvatarMask)m_MaskSource.objectReferenceValue; } set { m_MaskSource.objectReferenceValue = value; } }

        public SerializedProperty maskTypeProperty      => m_MaskType;
        public SerializedProperty maskSourceProperty    => m_MaskSource;
        public SerializedProperty bodyMaskProperty      => m_BodyMask;
        public SerializedProperty transformMaskProperty => m_TransformMask;

        public void ApplyModifiedProperties()
        {
            m_Property.serializedObject.ApplyModifiedProperties();
        }

        public bool MaskNeedsUpdating()
        {
            AvatarMask mask = maskSource;
            if (mask == null)
                return false;

            using (SerializedObject source = new SerializedObject(mask))
            {
                if (!SerializedProperty.DataEquals(bodyMaskProperty, source.FindProperty("m_Mask")))
                    return true;

                if (!SerializedProperty.DataEquals(transformMaskProperty, source.FindProperty("m_Elements")))
                    return true;
            }

            return false;
        }

        public void MaskFromClip(AvatarMask mask)
        {
            SerializedProperty bodyMask = bodyMaskProperty;

            if (bodyMask != null && bodyMask.isArray)
            {
                for (AvatarMaskBodyPart i = 0; i < AvatarMaskBodyPart.LastBodyPart; i++)
                {
                    mask.SetHumanoidBodyPartActive(i, bodyMask.GetArrayElementAtIndex((int)i).intValue != 0);
                }
            }

            var transformMask = transformMaskProperty;

            if (transformMask != null && transformMask.isArray)
            {
                if (transformMask.arraySize > 0 && (mask.transformCount != transformMask.arraySize))
                    mask.transformCount = transformMask.arraySize;

                int size = transformMask.arraySize;
                if (size == 0)
                    return;

                SerializedProperty prop = transformMask.GetArrayElementAtIndex(0);

                for (int i = 0; i < size; i++)
                {
                    SerializedProperty pathProperty = prop.FindPropertyRelative("m_Path");
                    SerializedProperty weightProperty = prop.FindPropertyRelative("m_Weight");
                    mask.SetTransformPath(i, pathProperty.stringValue);
                    mask.SetTransformActive(i, weightProperty.floatValue > 0.5);
                    prop.Next(false);
                }
            }
        }

        public void MaskToClip(AvatarMask mask)
        {
            SerializedProperty bodyMask = bodyMaskProperty;

            if (bodyMask != null && bodyMask.isArray)
            {
                for (AvatarMaskBodyPart i = 0; i < AvatarMaskBodyPart.LastBodyPart; i++)
                {
                    if ((int)i >= bodyMask.arraySize) bodyMask.InsertArrayElementAtIndex((int)i);
                    bodyMask.GetArrayElementAtIndex((int)i).intValue = mask.GetHumanoidBodyPartActive(i) ? 1 : 0;
                }
            }

            SerializedProperty transformMask = transformMaskProperty;
            ModelImporter.UpdateTransformMask(mask, transformMask);
        }

        public void ClearCurves()
        {
            SerializedProperty curves = m_Curves;

            if (curves != null && curves.isArray)
            {
                curves.ClearArray();
            }
        }

        public int GetCurveCount()
        {
            int ret = 0;

            SerializedProperty curves = m_Curves;

            if (curves != null && curves.isArray)
            {
                ret = curves.arraySize;
            }

            return ret;
        }

        public SerializedProperty GetCurveProperty(int index)
        {
            SerializedProperty ret = null;

            SerializedProperty curves = m_Curves;

            if (curves != null && curves.isArray)
            {
                ret = curves.GetArrayElementAtIndex(index).FindPropertyRelative("curve");
            }

            return ret;
        }

        public string GetCurveName(int index)
        {
            string ret = "";

            SerializedProperty curves = m_Curves;

            if (curves != null && curves.isArray)
            {
                ret = curves.GetArrayElementAtIndex(index).FindPropertyRelative("name").stringValue;
            }

            return ret;
        }

        public void SetCurveName(int index, string name)
        {
            SerializedProperty curves = m_Curves;

            if (curves != null && curves.isArray)
            {
                curves.GetArrayElementAtIndex(index).FindPropertyRelative("name").stringValue = name;
            }
        }

        public AnimationCurve GetCurve(int index)
        {
            AnimationCurve ret = null;

            SerializedProperty curve = GetCurveProperty(index);

            if (curve != null)
            {
                ret = curve.animationCurveValue;
            }

            return ret;
        }

        public void SetCurve(int index, AnimationCurve curveValue)
        {
            SerializedProperty curve = GetCurveProperty(index);

            if (curve != null)
            {
                curve.animationCurveValue = curveValue;
            }
        }

        public void AddCurve()
        {
            SerializedProperty curves = m_Curves;

            if (curves != null && curves.isArray)
            {
                curves.InsertArrayElementAtIndex(curves.arraySize);
                curves.GetArrayElementAtIndex(curves.arraySize - 1).FindPropertyRelative("name").stringValue = "Curve";
                AnimationCurve newCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));
                newCurve.preWrapMode = WrapMode.Default;
                newCurve.postWrapMode = WrapMode.Default;
                curves.GetArrayElementAtIndex(curves.arraySize - 1).FindPropertyRelative("curve").animationCurveValue = newCurve;
            }
        }

        public void RemoveCurve(int index)
        {
            SerializedProperty curves = m_Curves;

            if (curves != null && curves.isArray)
            {
                curves.DeleteArrayElementAtIndex(index);
            }
        }

        public AnimationEvent GetEvent(int index)
        {
            AnimationEvent evt = new AnimationEvent();
            SerializedProperty events = m_Events;

            if (events != null && events.isArray)
            {
                if (index < events.arraySize)
                {
                    evt.floatParameter = events.GetArrayElementAtIndex(index).FindPropertyRelative("floatParameter").floatValue;
                    evt.functionName = events.GetArrayElementAtIndex(index).FindPropertyRelative("functionName").stringValue;
                    evt.intParameter = events.GetArrayElementAtIndex(index).FindPropertyRelative("intParameter").intValue;
                    evt.objectReferenceParameter = events.GetArrayElementAtIndex(index).FindPropertyRelative("objectReferenceParameter").objectReferenceValue;
                    evt.stringParameter = events.GetArrayElementAtIndex(index).FindPropertyRelative("data").stringValue;
                    evt.time = events.GetArrayElementAtIndex(index).FindPropertyRelative("time").floatValue;
                }
                else
                {
                    Debug.LogWarning("Invalid Event Index");
                }
            }

            return evt;
        }

        public void SetEvent(int index, AnimationEvent animationEvent)
        {
            SerializedProperty events = m_Events;

            if (events != null && events.isArray)
            {
                if (index < events.arraySize)
                {
                    events.GetArrayElementAtIndex(index).FindPropertyRelative("floatParameter").floatValue = animationEvent.floatParameter;
                    events.GetArrayElementAtIndex(index).FindPropertyRelative("functionName").stringValue = animationEvent.functionName;
                    events.GetArrayElementAtIndex(index).FindPropertyRelative("intParameter").intValue = animationEvent.intParameter;
                    events.GetArrayElementAtIndex(index).FindPropertyRelative("objectReferenceParameter").objectReferenceValue = animationEvent.objectReferenceParameter;
                    events.GetArrayElementAtIndex(index).FindPropertyRelative("data").stringValue = animationEvent.stringParameter;
                    events.GetArrayElementAtIndex(index).FindPropertyRelative("time").floatValue = animationEvent.time;
                }
                else
                {
                    Debug.LogWarning("Invalid Event Index");
                }
            }
        }

        public void ClearEvents()
        {
            SerializedProperty events = m_Events;

            if (events != null && events.isArray)
            {
                events.ClearArray();
            }
        }

        public int GetEventCount()
        {
            int ret = 0;

            SerializedProperty curves = m_Events;

            if (curves != null && curves.isArray)
            {
                ret = curves.arraySize;
            }

            return ret;
        }

        public void AddEvent(float time)
        {
            SerializedProperty events = m_Events;

            if (events != null && events.isArray)
            {
                events.InsertArrayElementAtIndex(events.arraySize);
                events.GetArrayElementAtIndex(events.arraySize - 1).FindPropertyRelative("functionName").stringValue = "NewEvent";
                events.GetArrayElementAtIndex(events.arraySize - 1).FindPropertyRelative("time").floatValue = time;
            }
        }

        public void RemoveEvent(int index)
        {
            SerializedProperty events = m_Events;

            if (events != null && events.isArray)
            {
                events.DeleteArrayElementAtIndex(index);
            }
        }

        public void SetEvents(AnimationEvent[] newEvents)
        {
            SerializedProperty events = m_Events;

            if (events != null && events.isArray)
            {
                events.ClearArray();

                foreach (AnimationEvent evt in newEvents)
                {
                    events.InsertArrayElementAtIndex(events.arraySize);
                    SetEvent(events.arraySize - 1, evt);
                }
            }
        }

        public AnimationEvent[] GetEvents()
        {
            AnimationEvent[]  ret  = new AnimationEvent[GetEventCount()];
            SerializedProperty events = m_Events;

            if (events != null && events.isArray)
            {
                for (int i = 0; i < GetEventCount(); ++i)
                {
                    ret[i] = GetEvent(i);
                }
            }

            return ret;
        }

        public void AssignToPreviewClip(AnimationClip clip)
        {
            AnimationClipSettings info = new AnimationClipSettings();

            info.startTime = firstFrame / clip.frameRate;
            info.stopTime = lastFrame / clip.frameRate;
            info.orientationOffsetY = orientationOffsetY;
            info.level = level;
            info.cycleOffset = cycleOffset;
            info.loopTime = loopTime;
            info.loopBlend = loopBlend;
            info.loopBlendOrientation = loopBlendOrientation;
            info.loopBlendPositionY = loopBlendPositionY;
            info.loopBlendPositionXZ = loopBlendPositionXZ;
            info.keepOriginalOrientation = keepOriginalOrientation;
            info.keepOriginalPositionY = keepOriginalPositionY;
            info.keepOriginalPositionXZ = keepOriginalPositionXZ;
            info.heightFromFeet = heightFromFeet;
            info.mirror = mirror;
            info.hasAdditiveReferencePose = hasAdditiveReferencePose;
            info.additiveReferencePoseTime = additiveReferencePoseFrame / clip.frameRate;

            AnimationUtility.SetAnimationClipSettingsNoDirty(clip, info);
        }

        private float FixPrecisionErrors(float f)
        {
            float rounded = Mathf.Round(f);
            if (Mathf.Abs(f - rounded) < 0.0001f)
                return rounded;
            return f;
        }

        public void ExtractFromPreviewClip(AnimationClip clip)
        {
            AnimationClipSettings info = AnimationUtility.GetAnimationClipSettings(clip);

            // Ensure that we don't accidentally dirty settings due to floating point precision in the multiply / divide
            if (firstFrame / clip.frameRate != info.startTime)
                firstFrame = FixPrecisionErrors(info.startTime * clip.frameRate);
            if (lastFrame / clip.frameRate != info.stopTime)
                lastFrame = FixPrecisionErrors(info.stopTime * clip.frameRate);

            orientationOffsetY = info.orientationOffsetY;
            level = info.level;
            cycleOffset = info.cycleOffset;
            loopTime = info.loopTime;
            loopBlend = info.loopBlend;
            loopBlendOrientation = info.loopBlendOrientation;
            loopBlendPositionY = info.loopBlendPositionY;
            loopBlendPositionXZ = info.loopBlendPositionXZ;
            keepOriginalOrientation = info.keepOriginalOrientation;
            keepOriginalPositionY = info.keepOriginalPositionY;
            keepOriginalPositionXZ = info.keepOriginalPositionXZ;
            heightFromFeet = info.heightFromFeet;
            mirror = info.mirror;

            hasAdditiveReferencePose = info.hasAdditiveReferencePose;

            if (additiveReferencePoseFrame / clip.frameRate != info.additiveReferencePoseTime)
                additiveReferencePoseFrame = FixPrecisionErrors(info.additiveReferencePoseTime * clip.frameRate);
        }
    }
}
