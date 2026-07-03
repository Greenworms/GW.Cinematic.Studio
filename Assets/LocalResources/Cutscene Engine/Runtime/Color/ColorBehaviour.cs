using System;
using UnityEngine;
using UnityEngine.Playables;

namespace CutsceneEngine
{
    public class ColorBehaviour : PlayableBehaviour
    {
        public ColorType colorType;
        public Color color;
        public Gradient gradient;

        internal double start;
        internal double end;
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