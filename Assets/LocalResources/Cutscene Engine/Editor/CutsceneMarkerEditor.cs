using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace CutsceneEngineEditor
{
    [CustomTimelineEditor(typeof(CutsceneMarker))]
    public class CutsceneMarkerEditor : MarkerEditor
    {
        static Texture2D _markerHeader;
        static Texture2D _collapsedMarkerHeader;
        static Texture2D _dashedLine;
        static Texture2D _dottedLine;
        static Texture2D _waveLine;
        static Texture2D _zigzagLine;
        static Texture2D _markedLine;
        
        public override void DrawOverlay(IMarker marker, MarkerUIStates uiState, MarkerOverlayRegion region)
        {
            var simpleMarker = marker as CutsceneMarker;
            if (!simpleMarker) return;

            if (simpleMarker.showName) DrawNameOverlay(simpleMarker, region, uiState); 
            
            DrawLineOverlay(simpleMarker, region, uiState);
            DrawColorOverlay(simpleMarker, region, uiState);
        }

        void DrawNameOverlay(CutsceneMarker marker, MarkerOverlayRegion region, MarkerUIStates uiState)
        {
            if(uiState == MarkerUIStates.Collapsed) return;
            var color = marker.color;
            var overlayLineColor = uiState == MarkerUIStates.Selected ? color : new Color(color.r, color.g, color.b, color.a * 0.5f);
            GUI.contentColor = overlayLineColor;

            var rect = region.markerRegion;
            rect.x += rect.width;
            rect.xMax += marker.name.Length * 10;
            
            GUI.Label(rect, marker.name, EditorStyles.miniLabel);
            GUI.contentColor = Color.white;
        }

        void DrawLineOverlay(CutsceneMarker marker, MarkerOverlayRegion region, MarkerUIStates uiState)
        {
            var markerRegionCenterX = region.markerRegion.xMin + region.markerRegion.width / 2.0f;

            var lineRect = new Rect(markerRegionCenterX, region.timelineRegion.y, 0, region.timelineRegion.height);
            lineRect.yMin = region.markerRegion.yMax;
            
            var top = lineRect.max;
            var bottom = lineRect.min;

            var color = marker.color;
            var overlayLineColor = uiState == MarkerUIStates.Selected ? color : new Color(color.r, color.g, color.b, color.a * 0.5f);

            if (marker.lineStyle == CutsceneMarker.LineStyle.Plain)
            {
                var c = Handles.color;
                Handles.color = overlayLineColor;
                Handles.DrawLine(top, bottom, 2);
                Handles.color = c;
            }
            else
            {
                var texture = GetLineTexture(marker.lineStyle);
                if(texture == null) return;
                
                var c = GUI.color;
                GUI.color = overlayLineColor;
                lineRect.width = 16;
                lineRect.x -= lineRect.width / 2.0f;
                GUI.DrawTextureWithTexCoords(lineRect, texture, new Rect(0, 0, 1, lineRect.height / marker.lineTextureSize));
                GUI.color = c;
            }

        }

        static void DrawColorOverlay(CutsceneMarker marker, MarkerOverlayRegion region, MarkerUIStates state)
        {
            var oldColor = GUI.color;
            GUI.color = state.HasFlag(MarkerUIStates.Selected) ? marker.color : marker.color * new Color(0.9f, 0.9f, 0.9f, 0.5f); 
            
            if(!_markerHeader) _markerHeader = Resources.Load<Texture2D>("cutscene marker");
            if(!_collapsedMarkerHeader) _collapsedMarkerHeader = Resources.Load<Texture2D>("cutscene marker_collapsed");
            
            if (state.HasFlag(MarkerUIStates.Collapsed))
            {
                GUI.DrawTexture(region.markerRegion, _collapsedMarkerHeader);
            }
            else if (state.HasFlag(MarkerUIStates.None))
            {
                GUI.DrawTexture(region.markerRegion, _markerHeader);
            }
            
            GUI.color = oldColor;
        }

        static Texture2D GetLineTexture(CutsceneMarker.LineStyle style)
        {
            if (!_dashedLine) _dashedLine = Resources.Load<Texture2D>("cutscene marker dashed line");
            if (!_dottedLine) _dottedLine = Resources.Load<Texture2D>("cutscene marker dotted line");
            if (!_waveLine) _waveLine = Resources.Load<Texture2D>("cutscene marker wave line");
            if (!_zigzagLine) _zigzagLine = Resources.Load<Texture2D>("cutscene marker zigzag line");
            if (!_markedLine) _markedLine = Resources.Load<Texture2D>("cutscene marker marked line");
            
            return style switch
            {
                CutsceneMarker.LineStyle.Dashed => _dashedLine,
                CutsceneMarker.LineStyle.Dotted => _dottedLine,
                CutsceneMarker.LineStyle.Wave => _waveLine,
                CutsceneMarker.LineStyle.Zigzag => _zigzagLine,
                CutsceneMarker.LineStyle.Makred => _markedLine,
                _ => null
            };
        }
    }
}