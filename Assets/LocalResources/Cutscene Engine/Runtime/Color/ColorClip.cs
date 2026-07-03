using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace CutsceneEngine
{
    public class ColorClip : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.Extrapolation;
        [Tooltip("The type of color to use. Default uses a single color, Gradient uses a gradient.")]
        public ColorType colorType;
        [Tooltip("The color to apply.")]
        [ColorUsage(true, true)]public Color color = Color.white;
        [Tooltip("The gradient to apply.")]
        [GradientUsage(true, ColorSpace.Gamma)]public Gradient gradient;
        internal double start;
        internal double end;
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<ColorBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.colorType = colorType;
            behaviour.color = color;
            behaviour.gradient = gradient;

            behaviour.start = start;
            behaviour.end = end;
            
            return playable;
        }

        public Color Evaluate(float t)
        {
            switch (colorType)
            {
                case ColorType.Default:
                    return color;
                case ColorType.Gradient:
                    return gradient.Evaluate(t);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}