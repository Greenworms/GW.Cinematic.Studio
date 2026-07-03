using UnityEngine;
using UnityEngine.Playables;

namespace CutsceneEngine
{
    public class TimeScaleBehaviour : PlayableBehaviour
    {
        public float timeScale;
        public AnimationCurve multiplier = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    }
}
