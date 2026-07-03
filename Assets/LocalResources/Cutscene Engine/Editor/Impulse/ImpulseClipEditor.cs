using System;

using CutsceneEngine;
#if CINEMACHINE_3_OR_NEWER
using Unity.Cinemachine;
#elif CINEMACHINE
using Cinemachine;
#endif
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace CutsceneEngineEditor
{
    [CustomTimelineEditor(typeof(ImpulseClip))]
    public class ImpulseClipEditor : ClipEditor
    {
        static AnimationCurve[] DefaultImpulsedShapes;

        static Gradient CurveGradient = new Gradient()
        {
            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0),
                new GradientColorKey(Color.yellow, 0.5f),
                new GradientColorKey(Color.red, 1)
            }
        };

        static ImpulseClipEditor()
        {
#if CINEMACHINE_2_8_OR_NEWER
            int max = 0;
            var iter = Enum.GetValues(typeof(CinemachineImpulseDefinition.ImpulseShapes)).GetEnumerator();
            while (iter.MoveNext())
                max = Mathf.Max(max, (int)iter.Current);
            DefaultImpulsedShapes = new AnimationCurve[max + 1];

            DefaultImpulsedShapes[(int)CinemachineImpulseDefinition.ImpulseShapes.Recoil] = new AnimationCurve(new Keyframe[]
            {
                new Keyframe(0, 1, -3.2f, -3.2f),
                new Keyframe(1, 0, 0, 0)
            });

            DefaultImpulsedShapes[(int)CinemachineImpulseDefinition.ImpulseShapes.Bump] = new AnimationCurve(new Keyframe[]
            {
                new Keyframe(0, 0, -4.9f, -4.9f),
                new Keyframe(0.2f, 0, 8.25f, 8.25f),
                new Keyframe(1, 0, -0.25f, -0.25f)
            });

            DefaultImpulsedShapes[(int)CinemachineImpulseDefinition.ImpulseShapes.Explosion] = new AnimationCurve(new Keyframe[]
            {
                new Keyframe(0, -1.4f, -7.9f, -7.9f),
                new Keyframe(0.27f, 0.78f, 23.4f, 23.4f),
                new Keyframe(0.54f, -0.12f, 22.6f, 22.6f),
                new Keyframe(0.75f, 0.042f, 9.23f, 9.23f),
                new Keyframe(0.9f, -0.02f, 5.8f, 5.8f),
                new Keyframe(0.95f, -0.006f, -3.0f, -3.0f),
                new Keyframe(1, 0, 0, 0)
            });

            DefaultImpulsedShapes[(int)CinemachineImpulseDefinition.ImpulseShapes.Rumble] = new AnimationCurve(new Keyframe[]
            {
                new Keyframe(0, 0, 0, 0),
                new Keyframe(0.1f, 0.25f, 0, 0),
                new Keyframe(0.2f, 0, 0, 0),
                new Keyframe(0.3f, 0.75f, 0, 0),
                new Keyframe(0.4f, 0, 0, 0),
                new Keyframe(0.5f, 1, 0, 0),
                new Keyframe(0.6f, 0, 0, 0),
                new Keyframe(0.7f, 0.75f, 0, 0),
                new Keyframe(0.8f, 0, 0, 0),
                new Keyframe(0.9f, 0.25f, 0, 0),
                new Keyframe(1, 0, 0, 0)
            });
#elif CINEMACHINE

#endif
        }

        public override void OnClipChanged(TimelineClip clip)
        {
#if CINEMACHINE_3_OR_NEWER
            var impulseClip = clip.asset as ImpulseClip;
            if(impulseClip.impulseDefinition.ImpulseType == CinemachineImpulseDefinition.ImpulseTypes.Legacy)
                clip.duration = impulseClip.impulseDefinition.TimeEnvelope.Duration;    
            else 
                clip.duration = impulseClip.impulseDefinition.ImpulseDuration;
            
#elif CINEMACHINE_2_8_OR_NEWER
            var impulseClip = clip.asset as ImpulseClip;
            if(impulseClip.impulseDefinition.m_ImpulseType == CinemachineImpulseDefinition.ImpulseTypes.Legacy)
                clip.duration = impulseClip.impulseDefinition.m_TimeEnvelope.Duration;    
            else 
                clip.duration = impulseClip.impulseDefinition.m_ImpulseDuration;
#elif CINEMACHINE
            var impulseClip = clip.asset as ImpulseClip;
            clip.duration = impulseClip.impulseDefinition.m_TimeEnvelope.Duration;
#endif
        }

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            var impulseClip = clip.asset as ImpulseClip;

            var velocityToColor = CurveGradient.Evaluate(impulseClip.velocity.magnitude / 2f);

            var rect = region.position;

#if CINEMACHINE_3_OR_NEWER
            if(impulseClip.impulseDefinition.ImpulseType != CinemachineImpulseDefinition.ImpulseTypes.Legacy)
            {
                if (impulseClip.impulseDefinition.ImpulseShape == CinemachineImpulseDefinition.ImpulseShapes.Custom)
                    EditorUtil.DrawCurve(rect, impulseClip.impulseDefinition.CustomImpulseShape, velocityToColor);
                else
                    EditorUtil.DrawCurve(rect, DefaultImpulsedShapes[(int)impulseClip.impulseDefinition.ImpulseShape], velocityToColor);
            }
#elif CINEMACHINE_2_8_OR_NEWER
            if(impulseClip.impulseDefinition.m_ImpulseType != CinemachineImpulseDefinition.ImpulseTypes.Legacy)
            {
                if (impulseClip.impulseDefinition.m_ImpulseShape == CinemachineImpulseDefinition.ImpulseShapes.Custom)
                    EditorUtil.DrawCurve(rect, impulseClip.impulseDefinition.m_CustomImpulseShape, velocityToColor);
                else
                    EditorUtil.DrawCurve(rect, DefaultImpulsedShapes[(int)impulseClip.impulseDefinition.m_ImpulseShape], velocityToColor);
            }
#elif CINEMACHINE
            
#endif
            
        }
    }
}