// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor.Animations;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class AnimationClipInfoProperties
    {
        SerializedProperty m_Property;

        private SerializedProperty Get(string property) { return m_Property.FindPropertyRelative(property); }

        public AnimationClipInfoProperties(SerializedProperty prop) { m_Property = prop; }

        public string name      { get { return Get("name").stringValue; }     set { Get("name").stringValue = value; } }
        public string takeName  { get { return Get("takeName").stringValue; }  set { Get("takeName").stringValue = value; } }
        public float firstFrame   { get { return Get("firstFrame").floatValue; } set { Get("firstFrame").floatValue = value; } }
        public float lastFrame  { get { return Get("lastFrame").floatValue; }  set { Get("lastFrame").floatValue = value; } }
        public int wrapMode    { get { return Get("wrapMode").intValue; }    set { Get("wrapMode").intValue = value; } }
        public bool loop          { get { return Get("loop").boolValue; }       set { Get("loop").boolValue = value; } }

        // Mecanim animation properties
        public float orientationOffsetY   { get { return Get("orientationOffsetY").floatValue; }     set { Get("orientationOffsetY").floatValue = value; } }
        public float level                 { get { return Get("level").floatValue; }                  set { Get("level").floatValue = value; } }
        public float cycleOffset             { get { return Get("cycleOffset").floatValue; }            set { Get("cycleOffset").floatValue = value; } }
        public float additiveReferencePoseFrame { get { return Get("additiveReferencePoseFrame").floatValue; } set { Get("additiveReferencePoseFrame").floatValue = value; } }
        public bool hasAdditiveReferencePose { get { return Get("hasAdditiveReferencePose").boolValue; } set { Get("hasAdditiveReferencePose").boolValue = value; } }
        public bool loopTime { get { return Get("loopTime").boolValue; } set { Get("loopTime").boolValue = value; } }
        public bool loopBlend { get { return Get("loopBlend").boolValue; } set { Get("loopBlend").boolValue = value; } }
        public bool  loopBlendOrientation   { get { return Get("loopBlendOrientation").boolValue; } set { Get("loopBlendOrientation").boolValue = value; } }
        public bool  loopBlendPositionY   { get { return Get("loopBlendPositionY").boolValue; }   set { Get("loopBlendPositionY").boolValue = value; } }
        public bool  loopBlendPositionXZ     { get { return Get("loopBlendPositionXZ").boolValue; }  set { Get("loopBlendPositionXZ").boolValue = value; } }
        public bool  keepOriginalOrientation { get { return Get("keepOriginalOrientation").boolValue; } set { Get("keepOriginalOrientation").boolValue = value; } }
        public bool  keepOriginalPositionY   { get { return Get("keepOriginalPositionY").boolValue; }   set { Get("keepOriginalPositionY").boolValue = value; } }
        public bool  keepOriginalPositionXZ  { get { return Get("keepOriginalPositionXZ").boolValue; }  set { Get("keepOriginalPositionXZ").boolValue = value; } }
        public bool  heightFromFeet       { get { return Get("heightFromFeet").boolValue; }       set { Get("heightFromFeet").boolValue = value; } }
        public bool  mirror { get { return Get("mirror").boolValue; } set { Get("mirror").boolValue = value; } }
        public ClipAnimationMaskType maskType { get { return (ClipAnimationMaskType)Get("maskType").intValue; } set { Get("maskType").intValue = (int)value; } }
        public SerializedProperty maskTypeProperty { get { return Get("maskType"); } }
        public AvatarMask maskSource { get { return Get("maskSource").objectReferenceValue as AvatarMask; } set { Get("maskSource").objectReferenceValue = value; } }
        public SerializedProperty maskSourceProperty { get { return Get("maskSource"); } }
        public SerializedProperty bodyMaskProperty { get { return Get("bodyMask"); } }
        public SerializedProperty transformMaskProperty { get { return Get("transformMask"); } }

        public bool MaskNeedsUpdating()
        {
            AvatarMask mask = maskSource;
            if (mask == null)
                return false;

            SerializedProperty bodyMask = Get("bodyMask");

            if (bodyMask != null && bodyMask.isArray)
            {
                for (AvatarMaskBodyPart i = 0; i < AvatarMaskBodyPart.LastBodyPart; i++)
                {
                    if (mask.GetHumanoidBodyPartActive(i) != (bodyMask.GetArrayElementAtIndex((int)i).intValue != 0))
                        return true;
                }
            }
            else
            {
                return true;
            }

            SerializedProperty transformMask = Get("transformMask");

            if (transformMask != null && transformMask.isArray)
            {
                if (transformMask.arraySize > 0 && (mask.transformCount != transformMask.arraySize))
                    return true;

                int size = transformMask.arraySize;
                for (int i = 0; i < size; i++)
                {
                    SerializedProperty pathProperty = transformMask.GetArrayElementAtIndex(i).FindPropertyRelative("m_Path");
                    SerializedProperty weightProperty = transformMask.GetArrayElementAtIndex(i).FindPropertyRelative("m_Weight");

                    if (mask.GetTransformPath(i) != pathProperty.stringValue ||
                        mask.GetTransformActive(i) != (weightProperty.floatValue > 0.5f))
                        return true;
                }
            }
            else
            {
                return true;
            }

            return false;
        }

        public void MaskFromClip(AvatarMask mask)
        {
            SerializedProperty bodyMask = Get("bodyMask");

            if (bodyMask != null && bodyMask.isArray)
            {
                for (AvatarMaskBodyPart i = 0; i < AvatarMaskBodyPart.LastBodyPart; i++)
                {
                    mask.SetHumanoidBodyPartActive(i, bodyMask.GetArrayElementAtIndex((int)i).intValue != 0);
                }
            }

            SerializedProperty transformMask = Get("transformMask");

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
            SerializedProperty bodyMask = Get("bodyMask");

            if (bodyMask != null && bodyMask.isArray)
            {
                for (AvatarMaskBodyPart i = 0; i < AvatarMaskBodyPart.LastBodyPart; i++)
                {
                    if ((int)i >= bodyMask.arraySize) bodyMask.InsertArrayElementAtIndex((int)i);
                    bodyMask.GetArrayElementAtIndex((int)i).intValue = mask.GetHumanoidBodyPartActive(i) ? 1 : 0;
                }
            }

            SerializedProperty transformMask = Get("transformMask");
            ModelImporter.UpdateTransformMask(mask, transformMask);
        }

        public void ClearCurves()
        {
            SerializedProperty curves = Get("curves");

            if (curves != null && curves.isArray)
            {
                curves.ClearArray();
            }
        }

        public int GetCurveCount()
        {
            int ret = 0;

            SerializedProperty curves = Get("curves");

            if (curves != null && curves.isArray)
            {
                ret = curves.arraySize;
            }

            return ret;
        }

        public SerializedProperty GetCurveProperty(int index)
        {
            SerializedProperty ret = null;

            SerializedProperty curves = Get("curves");

            if (curves != null && curves.isArray)
            {
                ret = curves.GetArrayElementAtIndex(index).FindPropertyRelative("curve");
            }

            return ret;
        }

        public string GetCurveName(int index)
        {
            string ret = "";

            SerializedProperty curves = Get("curves");

            if (curves != null && curves.isArray)
            {
                ret = curves.GetArrayElementAtIndex(index).FindPropertyRelative("name").stringValue;
            }

            return ret;
        }

        public void SetCurveName(int index, string name)
        {
            SerializedProperty curves = Get("curves");

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
            SerializedProperty curves = Get("curves");

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
            SerializedProperty curves = Get("curves");

            if (curves != null && curves.isArray)
            {
                curves.DeleteArrayElementAtIndex(index);
            }
        }

        public AnimationEvent GetEvent(int index)
        {
            AnimationEvent evt = new AnimationEvent();
            SerializedProperty events = Get("events");

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
            SerializedProperty events = Get("events");

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
            SerializedProperty events = Get("events");

            if (events != null && events.isArray)
            {
                events.ClearArray();
            }
        }

        public int GetEventCount()
        {
            int ret = 0;

            SerializedProperty curves = Get("events");

            if (curves != null && curves.isArray)
            {
                ret = curves.arraySize;
            }

            return ret;
        }

        public void AddEvent(float time)
        {
            SerializedProperty events = Get("events");

            if (events != null && events.isArray)
            {
                events.InsertArrayElementAtIndex(events.arraySize);
                events.GetArrayElementAtIndex(events.arraySize - 1).FindPropertyRelative("functionName").stringValue = "NewEvent";
                events.GetArrayElementAtIndex(events.arraySize - 1).FindPropertyRelative("time").floatValue = time;
            }
        }

        public void RemoveEvent(int index)
        {
            SerializedProperty events = Get("events");

            if (events != null && events.isArray)
            {
                events.DeleteArrayElementAtIndex(index);
            }
        }

        public void SetEvents(AnimationEvent[] newEvents)
        {
            SerializedProperty events = Get("events");

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
            SerializedProperty events = Get("events");

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
