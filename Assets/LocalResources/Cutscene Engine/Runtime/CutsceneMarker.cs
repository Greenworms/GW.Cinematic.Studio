using UnityEngine;
using UnityEngine.Timeline;

namespace CutsceneEngine
{
    [CustomStyle("CutsceneMarker")]
    public class CutsceneMarker : Marker
    {
        public enum LineStyle
        {
            None,
            Plain,
            Dashed,
            Dotted,
            Wave,
            Zigzag,
            Makred
        }
        [Tooltip("The color of the marker.")]
        public Color color = new Color(0.6f, 0.8f, 0.8f);
        [Tooltip("The style of the line to be displayed on the timeline.")]
        public LineStyle lineStyle = LineStyle.Dashed;
        [Tooltip("Whether to show the name of the marker on the timeline.")]
        public bool showName = true;
        [Tooltip("The texture size of the line.")]
        [Range(5, 50)] public int lineTextureSize = 10; 
        [Tooltip("The description of the marker.")]
        [TextArea(3, 5)] public string description;
    }
}