using System.Collections.Generic;
using UnityEngine;

namespace CutsceneEngine
{
    internal static class AnimationCurveOptimizer
    {
        public static AnimationCurve Optimize(AnimationCurve sourceCurve, float optimizationValue)
        {
            if (sourceCurve == null || sourceCurve.keys.Length < 3 || optimizationValue <= 0f)
            {
                return sourceCurve;
            }

            optimizationValue = Mathf.Pow(Mathf.Clamp01(optimizationValue), 4f);

            var keys = new List<Keyframe>(sourceCurve.keys);
            var optimizedKeys = new List<Keyframe> { keys[0] };

            Simplify(keys, 0, keys.Count - 1, optimizationValue, optimizedKeys);
            optimizedKeys.Add(keys[keys.Count - 1]);

            return new AnimationCurve(optimizedKeys.ToArray());
        }

        static void Simplify(List<Keyframe> sourceKeys, int firstIndex, int lastIndex, float tolerance, List<Keyframe> result)
        {
            float maxError = 0f;
            int maxErrorIndex = -1;

            var startKey = sourceKeys[firstIndex];
            var endKey = sourceKeys[lastIndex];

            for (int i = firstIndex + 1; i < lastIndex; i++)
            {
                float currentTime = sourceKeys[i].time;
                float originalValue = sourceKeys[i].value;
                float evaluatedValue = EvaluateHermite(startKey, endKey, currentTime);
                float error = Mathf.Abs(originalValue - evaluatedValue);

                if (error > maxError)
                {
                    maxError = error;
                    maxErrorIndex = i;
                }
            }

            if (maxError <= tolerance)
            {
                return;
            }

            Simplify(sourceKeys, firstIndex, maxErrorIndex, tolerance, result);
            result.Add(sourceKeys[maxErrorIndex]);
            Simplify(sourceKeys, maxErrorIndex, lastIndex, tolerance, result);
        }

        static float EvaluateHermite(Keyframe k1, Keyframe k2, float time)
        {
            float dx = k2.time - k1.time;
            if (dx < 0.00001f)
            {
                return k1.value;
            }

            float t = (time - k1.time) / dx;
            float t2 = t * t;
            float t3 = t2 * t;

            float h00 = 2f * t3 - 3f * t2 + 1f;
            float h10 = t3 - 2f * t2 + t;
            float h01 = -2f * t3 + 3f * t2;
            float h11 = t3 - t2;

            float m0 = k1.outTangent * dx;
            float m1 = k2.inTangent * dx;

            return h00 * k1.value + h10 * m0 + h01 * k2.value + h11 * m1;
        }
    }
}