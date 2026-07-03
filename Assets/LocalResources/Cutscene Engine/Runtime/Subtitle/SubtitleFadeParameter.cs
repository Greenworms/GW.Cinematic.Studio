using System;
using UnityEngine;

namespace CutsceneEngine
{
    [Serializable]
    public class SubtitleFadeParameter
    {
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float fadeInTime = 0.15f;
        public float fadeOutTime = 0.25f;
    }
}