using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    /// <summary>
    /// Timeline clip that creates looping behavior within cutscenes.
    /// Allows sections of the timeline to repeat based on various escape conditions.
    /// </summary>
    [Serializable]
    [DisplayName("Loop Clip")]
    public class LoopClip : PlayableAsset, ITimelineClipAsset
    {
        /// <summary>
        /// Methods for escaping from the loop.
        /// </summary>
        public enum EscapeMethod
        {
            /// <summary>Escapes when the escape method is called manually after repeating infinitely.</summary>
            Manual,
            /// <summary>Escape after repeating a certain number of times.</summary>
            LoopCount,
            /// <summary>Escape after repeating until a certain amount of time has elapsed.</summary>
            Elapsed
        }
        
        public ClipCaps clipCaps => ClipCaps.None;
        public double start { get; internal set; }
        public double end { get; internal set; }
        public bool endAtTimelineEnd { get; internal set; }
        [Tooltip("After the loop ends, the loop clip will be in the isFinished state, so the clip will be ignored when the loop is entered again. \n" +
                 "However, if this value is true, the state will be reset when the loop ends, so that the loop clip can be used again.")]
        public bool resetAfterEscape;

        [Tooltip("Defines how to escape the loop.\n\n" +
                 "Manual: Escapes when the escape method is called manually after repeating infinitely.\n" +
                 "Loop Count: Escape after repeating a certain number of times.\n" +
                 "Elapsed: Escape after repeating until a certain amount of time has elapsed.\n")]
        public EscapeMethod escapeMethod;
        [Tooltip("The number of times to repeat the loop when Escape Method is Loop Count.")]
        public ushort targetLoopCount = 1;
        [Tooltip("The amount of time to repeat the loop when Escape Method is Elapsed.")]
        public float minElapseTime = 3f;
        
        [Tooltip("The description of the loop.")]
        [TextArea(3, 5)] public string description;
        
        public LoopBehaviour behaviour { get; private set; }
        
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<LoopBehaviour>.Create(graph);
            behaviour = playable.GetBehaviour();
            
            behaviour.director = owner.GetComponent<PlayableDirector>();
            behaviour.start = start;
            behaviour.end = end;
            behaviour.endAtTimelineEnd = endAtTimelineEnd;
            
            behaviour.resetAfterEscape = resetAfterEscape;
            behaviour.escapeMethod = escapeMethod;
            behaviour.targetLoopCount = targetLoopCount;
            behaviour.minElapseTime = minElapseTime;

            return playable;
        }
    }
}