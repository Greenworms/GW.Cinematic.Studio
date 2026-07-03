using System;
using UnityEngine;
using UnityEngine.Playables;

namespace CutsceneEngine
{
    /// <summary>
    /// Playable behaviour that handles loop execution logic.
    /// Manages loop counting, escape conditions, and timeline navigation.
    /// </summary>
    public class LoopBehaviour : PlayableBehaviour
    {
        /// <summary>Gets or sets the PlayableDirector controlling this loop.</summary>
        public PlayableDirector director { get; set; }
        
        /// <summary>Gets the start time of the loop in timeline seconds.</summary>
        public double start { get; internal set; }
        
        /// <summary>Gets the end time of the loop in timeline seconds.</summary>
        public double end { get; internal set; }
        
        /// <summary>Gets whether this loop ends at the timeline end.</summary>
        public bool endAtTimelineEnd { get; internal set; }
        
        /// <summary>Gets the current number of completed loops.</summary>
        public int loopCount { get; private set; }
        
        /// <summary>Gets the total elapsed time since the loop started.</summary>
        public float elapsed { get; private set; }
        
        /// <summary>Gets whether an escape from the loop has been requested.</summary>
        public bool escapeRequested { get; private set; }
        
        /// <summary>Gets whether the loop has finished executing.</summary>
        public bool isFinished { get; private set; }

        public bool resetAfterEscape;
        public LoopClip.EscapeMethod escapeMethod;
        public double duration => end - start;
        
        [Tooltip("The number of times the loop will occur. This number of times the clip will loop back to the beginning and then repeat.")]
        public ushort targetLoopCount = 1;
        [Tooltip("Minimum time to satisfy the loop escape condition. " +
                 "At least this amount of time must elapse " +
                 "since the loop clip was in progress before the loop can be exited.")]
        public float minElapseTime = 3f;

        bool _shouldReset;
        bool _shouldLoopAtTheEnd;
        Cutscene _cutscene;
        public override void OnGraphStart(Playable playable)
        {
            if(!Application.isPlaying)
            {
                Reset();
            }

            _cutscene = director.GetComponent<Cutscene>();

            _shouldLoopAtTheEnd = end.IsApproximately(director.duration);
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            Reset();
        }
        
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (info.evaluationType == FrameData.EvaluationType.Playback)
            {
                // OnBehaviourPause에서는 LoopClip이 타임라인의 끝 부분에 스냅되어있을때 루프를 실행하지 못 하는 문제가 있음.
                // 이 경우에는 PrepareFrame에서 루프를 처리함
                if(!_shouldLoopAtTheEnd)
                {
                    if(playable.GetTime().IsGreaterThanOrEqual(playable.GetDuration()))
                    {
                        if (ShouldEscape())
                        {
                            Escape(false);
                        }
                        else
                        {
                            Return();
                        }
                    }
                }
            }
            else
            {
                if(!Application.isPlaying)
                {
                    if (info.effectiveWeight <= 0)
                    {
                        Reset();
                    }
                }
                else
                {
                    if(_shouldReset) Reset();
                }
            }
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            // ProcessFrame에서는 evaluationType이 Playback으로만 설정됨. 그래서 PrepareFrame에서 EvaluationType을 평가함.
            if(info.evaluationType == FrameData.EvaluationType.Playback)
            {
                // end가 director.duration과 같은 경우에만 PrepareFrame에서 루프를 처리하고, 나머지는 OnBehaviourPause에서 처리함.
                if(_shouldLoopAtTheEnd)
                {
                    if(playable.GetTime().IsGreaterThanOrEqual(playable.GetDuration() - info.deltaTime - 0.02))
                    {
                        if (ShouldEscape())
                        {
                            Escape(false);
                        }
                        else
                        {
                            Return();
                        }
                    }
                }
                elapsed += info.deltaTime;
            }
        }
        

        /// <summary>
        /// Determines whether the loop should be escaped based on the current escape method.
        /// </summary>
        /// <returns>True if the loop should be escaped.</returns>
        public bool ShouldEscape()
        {
            if(escapeRequested) return true;
            switch (escapeMethod)
            {
                case LoopClip.EscapeMethod.Manual:
                    return escapeRequested;
                case LoopClip.EscapeMethod.LoopCount:
                    return loopCount >= targetLoopCount;
                case LoopClip.EscapeMethod.Elapsed:
                    return elapsed >= minElapseTime;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Escapes from the loop, optionally jumping to the end.
        /// </summary>
        /// <param name="toEnd">If true, jumps to the end of the loop. If false, continues from current position.</param>
        public void Escape(bool toEnd)
        {
            escapeRequested = true;
            if (toEnd)
            {
                director.time = end;
            }

            isFinished = true;
            if (resetAfterEscape) _shouldReset = true;
        }
        
        void Reset()
        {
            loopCount = 0;
            elapsed = 0;
            isFinished = false;
            escapeRequested = false;   
        }
        void Return()
        {
            loopCount++;
            director.time = start;
            if(_cutscene != null) _cutscene.OnLoopedByClip(end, start);
        }
    }
}
