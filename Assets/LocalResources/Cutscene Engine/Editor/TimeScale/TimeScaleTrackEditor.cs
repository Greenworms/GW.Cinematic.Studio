using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CutsceneEngineEditor
{
    [CustomTimelineEditor(typeof(TimeScaleTrack))]
    public class TimeScaleTrackEditor : TrackEditor
    {
        public override TrackDrawOptions GetTrackOptions(TrackAsset track, Object binding)
        {
            var options = base.GetTrackOptions(track, binding);
            options.icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Cutscene Engine/Editor/Icons/TimeScaleControl Icon.png");
            
            var director = TimelineEditor.inspectedDirector;
            if(director)
            {
                if(director.timeUpdateMode != DirectorUpdateMode.GameTime) 
                    options.errorText = "If the PlayableDirector's timeUpdateMode is not GameTime, the actual timeline playback speed will not change even if Time.TimeScale is changed. \n\n" +
                                        "If this is not the intended behavior, It's recommended setting the PlayableDirector's timeUpdateMode to GameTime.";
            }
            
            return options;
        }
    }
}