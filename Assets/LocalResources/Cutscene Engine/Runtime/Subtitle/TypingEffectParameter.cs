using System;
using System.Collections.Generic;
using UnityEngine;

namespace CutsceneEngine
{
    [Serializable]
    public class TypingEffectParameter
    {
        public enum TimeMethod
        {
            PerChar,
            Total
        }

        public TimeMethod timeMethod;
        [Min(0)]public float timePerChar = 0.05f;
        [Min(0)]public float totalDuration = 1.5f;
        [Min(0)]public float fadeOutTime = 0.25f;
        public AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0,0,1,1);

        public List<CharTimeOverride> additionalCharSettings = new List<CharTimeOverride>()
        {
            new CharTimeOverride(',', 0.2f),
            new CharTimeOverride('.', 0.3f),
            new CharTimeOverride('…', 0.5f),
            new CharTimeOverride('·', 0.2f),
            new CharTimeOverride('⋯', 0.3f),
        };
        
        public bool HasAdditionalCharSetting(char c, out float time)
        {
            var settings = additionalCharSettings;
            for (int i = 0; i < settings.Count; i++)
            {
                if (settings[i].c == c)
                {
                    time = settings[i].time;
                    return true;
                }
            }

            time = timePerChar;
            return false;
        }
    }

    [Serializable]
    public struct CharTimeOverride
    {
        public char c;
        public float time;

        public CharTimeOverride(char c, float time)
        {
            this.c = c;
            this.time = time;
        }
    }
}