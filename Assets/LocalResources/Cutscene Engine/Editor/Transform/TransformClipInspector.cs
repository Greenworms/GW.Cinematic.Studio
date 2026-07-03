using System;
using System.Collections.Generic;
using System.Linq;
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UIElements;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(TransformClip))]
    [CanEditMultipleObjects]
    public class TransformClipInspector : Editor
    {
        static Material wireframeMaterial;
        static TransformClipInspector _active;
        Vector3 initialPos;
        Quaternion initialRot;
        Vector3 initialScale;
        
        void OnEnable()
        {
            if (_active)
            {
                DestroyImmediate(_active);
            }
            SceneView.duringSceneGui += DuringSceneGUI;
            
            var director = TimelineEditor.inspectedDirector;
            if (director)
            {
                var track = director.GetTrackOf<TransformTrack>((TransformClip)target);
                if (track)
                {
                    var binding = director.GetGenericBinding(track) as Transform;
                    if (binding)
                    {
                        initialPos = track.initialPos;
                        initialRot = track.initialRot;
                        initialScale = track.initialScale;
                    }
                }
            }

            _active = this;
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
            if(wireframeMaterial) DestroyImmediate(wireframeMaterial);
        }

#if UNITY_6000_0_OR_NEWER
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var sourceTransformField = new IMGUIContainer(() =>
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TransformClip.sourceTransform)), new GUIContent("Source Transform"));
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
            });
            sourceTransformField.tooltip = "A single transform to reference position, rotation and scale values. " +
                                           "Used only when the Method of each property is set to 'Transform'.";
            root.Add(sourceTransformField);
            
            root.AddSpace();
            
            // movement
            var positionMethodProp = serializedObject.FindProperty(nameof(TransformClip.positionMethod));
            var positionMethodField = new EnumField(positionMethodProp.displayName, (TransformMethod)positionMethodProp.enumValueIndex);
            positionMethodField.BindProperty(positionMethodProp);
            root.Add(positionMethodField);
            
            var applyPositionOffsetField = new PropertyField(serializedObject.FindProperty(nameof(TransformClip.applyPositionOffset)));
            applyPositionOffsetField.BindVisible(positionMethodField, TransformMethod.Value);
            applyPositionOffsetField.style.Indent(1);
            applyPositionOffsetField.tooltip = "When PositionMethod is Value, determines whether to add an offset to the start position of the transform.";
            root.Add(applyPositionOffsetField);
            
            var endPositionField = new PropertyField(serializedObject.FindProperty(nameof(TransformClip.position)));
            endPositionField.BindVisible(positionMethodField, TransformMethod.Value);
            endPositionField.style.Indent(1);
            root.Add(endPositionField);

            var positionTransformLocalOffsetField = new PropertyField(serializedObject.FindProperty(nameof(TransformClip.positionTransformLocalOffset)));
            positionTransformLocalOffsetField.BindVisible(positionMethodField, TransformMethod.Transform);
            positionTransformLocalOffsetField.style.Indent(1);
            positionTransformLocalOffsetField.tooltip = "Offset applied along the referenced Transform's local axes when Method is Transform.";
            root.Add(positionTransformLocalOffsetField);

            var positionTransformWorldOffsetField = new PropertyField(serializedObject.FindProperty(nameof(TransformClip.positionTransformWorldOffset)));
            positionTransformWorldOffsetField.BindVisible(positionMethodField, TransformMethod.Transform);
            positionTransformWorldOffsetField.style.Indent(1);
            positionTransformWorldOffsetField.tooltip = "Offset applied in world space when Method is Transform.";
            root.Add(positionTransformWorldOffsetField);
            
            // rotation
            var rotationMethodProp = serializedObject.FindProperty(nameof(TransformClip.rotationMethod));
            var rotationMethodField = new EnumField(rotationMethodProp.displayName, (TransformMethod)rotationMethodProp.enumValueIndex);
            rotationMethodField.BindProperty(rotationMethodProp);
            root.Add(rotationMethodField);

            var applyRotationOffsetField = new PropertyField(serializedObject.FindProperty(nameof(TransformClip.applyRotationOffset)));
            applyRotationOffsetField.BindVisible(rotationMethodField, TransformMethod.Value);
            applyRotationOffsetField.style.Indent(1);
            applyRotationOffsetField.tooltip = "When RotationMethod is Value, determines whether to add the start rotation of the transform as an offset.";
            root.Add(applyRotationOffsetField);
            
            var rotationField = new PropertyField(serializedObject.FindProperty(nameof(TransformClip.rotation)));
            rotationField.BindVisible(rotationMethodField, TransformMethod.Value);
            rotationField.style.Indent(1);
            root.Add(rotationField);

            var rotationTransformLocalOffsetField = new PropertyField(serializedObject.FindProperty(nameof(TransformClip.rotationTransformLocalOffset)));
            rotationTransformLocalOffsetField.BindVisible(rotationMethodField, TransformMethod.Transform);
            rotationTransformLocalOffsetField.style.Indent(1);
            rotationTransformLocalOffsetField.tooltip = "Offset applied relative to the referenced Transform's local rotation when Method is Transform.";
            root.Add(rotationTransformLocalOffsetField);

            var rotationTransformWorldOffsetField = new PropertyField(serializedObject.FindProperty(nameof(TransformClip.rotationTransformWorldOffset)));
            rotationTransformWorldOffsetField.BindVisible(rotationMethodField, TransformMethod.Transform);
            rotationTransformWorldOffsetField.style.Indent(1);
            rotationTransformWorldOffsetField.tooltip = "Offset applied in world rotation when Method is Transform.";
            root.Add(rotationTransformWorldOffsetField);


            // scale
            var scaleMethodProp = serializedObject.FindProperty(nameof(TransformClip.scaleMethod));
            var scaleMethodField = new EnumField(scaleMethodProp.displayName, (TransformMethod)scaleMethodProp.enumValueIndex);
            scaleMethodField.BindProperty(scaleMethodProp);
            root.Add(scaleMethodField);
            
            var applyScaleOffsetField = new PropertyField(serializedObject.FindProperty(nameof(TransformClip.applyScaleOffset)));
            applyScaleOffsetField.BindVisible(scaleMethodField, TransformMethod.Value);
            applyScaleOffsetField.style.Indent(1);
            applyScaleOffsetField.tooltip = "When ScaleMethod is Value, determines whether to add the start scale of the transform as an offset.";
            root.Add(applyScaleOffsetField);

            
            var scaleField = new PropertyField(serializedObject.FindProperty(nameof(TransformClip.scale)));
            scaleField.BindVisible(scaleMethodField, TransformMethod.Value);
            scaleField.style.Indent(1);
            root.Add(scaleField);

            var scaleTransformLocalOffsetField = new PropertyField(serializedObject.FindProperty(nameof(TransformClip.scaleTransformLocalOffset)));
            scaleTransformLocalOffsetField.BindVisible(scaleMethodField, TransformMethod.Transform);
            scaleTransformLocalOffsetField.style.Indent(1);
            scaleTransformLocalOffsetField.tooltip = "Offset applied in local scale after copying the referenced Transform when Method is Transform.";
            root.Add(scaleTransformLocalOffsetField);

            var scaleTransformWorldOffsetField = new PropertyField(serializedObject.FindProperty(nameof(TransformClip.scaleTransformWorldOffset)));
            scaleTransformWorldOffsetField.BindVisible(scaleMethodField, TransformMethod.Transform);
            scaleTransformWorldOffsetField.style.Indent(1);
            scaleTransformWorldOffsetField.tooltip = "Offset applied to the world scale difference when Method is Transform.";
            root.Add(scaleTransformWorldOffsetField);
            
            
            return root;

        }
#else
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawSourceTransformField();

            EditorGUILayout.Space();

            DrawPositionSection();
            EditorGUILayout.Space();
            DrawRotationSection();
            EditorGUILayout.Space();
            DrawScaleSection();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawSourceTransformField()
        {
            var prop = serializedObject.FindProperty(nameof(TransformClip.sourceTransform));
            EditorGUILayout.PropertyField(prop, SourceTransformLabel);
        }

        void DrawPositionSection()
        {
            var methodProp = serializedObject.FindProperty(nameof(TransformClip.positionMethod));
            EditorGUILayout.PropertyField(methodProp);

            using (new EditorGUI.IndentLevelScope())
            {
                var method = (TransformMethod)methodProp.enumValueIndex;
                if (method == TransformMethod.Value)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TransformClip.applyPositionOffset)),
                        ApplyPositionOffsetLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TransformClip.position)));
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TransformClip.positionTransformLocalOffset)),
                        PositionLocalOffsetLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TransformClip.positionTransformWorldOffset)),
                        PositionWorldOffsetLabel);
                }
            }
        }

        void DrawRotationSection()
        {
            var methodProp = serializedObject.FindProperty(nameof(TransformClip.rotationMethod));
            EditorGUILayout.PropertyField(methodProp);

            using (new EditorGUI.IndentLevelScope())
            {
                var method = (TransformMethod)methodProp.enumValueIndex;
                if (method == TransformMethod.Value)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TransformClip.applyRotationOffset)),
                        ApplyRotationOffsetLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TransformClip.rotation)));
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TransformClip.rotationTransformLocalOffset)),
                        RotationLocalOffsetLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TransformClip.rotationTransformWorldOffset)),
                        RotationWorldOffsetLabel);
                }
            }
        }

        void DrawScaleSection()
        {
            var methodProp = serializedObject.FindProperty(nameof(TransformClip.scaleMethod));
            EditorGUILayout.PropertyField(methodProp);

            using (new EditorGUI.IndentLevelScope())
            {
                var method = (TransformMethod)methodProp.enumValueIndex;
                if (method == TransformMethod.Value)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TransformClip.applyScaleOffset)),
                        ApplyScaleOffsetLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TransformClip.scale)));
                }
                else
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TransformClip.scaleTransformLocalOffset)),
                        ScaleLocalOffsetLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TransformClip.scaleTransformWorldOffset)),
                        ScaleWorldOffsetLabel);
                }
            }
        }

        static readonly GUIContent SourceTransformLabel = new("Source Transform",
            "A single transform to reference position, rotation and scale values. Used only when the Method of each property is set to 'Transform'.");
        static readonly GUIContent ApplyPositionOffsetLabel = new("Apply Position Offset",
            "When PositionMethod is Value, determines whether to add an offset to the start position of the transform.");
        static readonly GUIContent PositionLocalOffsetLabel = new("Local Offset",
            "Offset applied along the referenced Transform's local axes when Method is Transform.");
        static readonly GUIContent PositionWorldOffsetLabel = new("World Offset",
            "Offset applied in world space when Method is Transform.");
        static readonly GUIContent ApplyRotationOffsetLabel = new("Apply Rotation Offset",
            "When RotationMethod is Value, determines whether to add an offset using the start rotation of the transform.");
        static readonly GUIContent RotationLocalOffsetLabel = new("Local Offset",
            "Offset applied after copying the referenced Transform's rotation when Method is Transform.");
        static readonly GUIContent RotationWorldOffsetLabel = new("World Offset",
            "Offset applied before the referenced Transform's rotation when Method is Transform.");
        static readonly GUIContent ApplyScaleOffsetLabel = new("Apply Scale Offset",
            "When ScaleMethod is Value, determines whether to add the start scale of the transform as an offset.");
        static readonly GUIContent ScaleLocalOffsetLabel = new("Local Offset",
            "Offset applied in local scale after copying the referenced Transform when Method is Transform.");
        static readonly GUIContent ScaleWorldOffsetLabel = new("World Offset",
            "Offset applied to the world scale difference when Method is Transform.");
#endif

        void DuringSceneGUI(SceneView sceneView)
        {
            foreach (var o in targets)
            {
                var clip = (TransformClip)o;
                DrawSceneGUI(clip);
            }

            var clipsPerTrack = new Dictionary<TrackAsset, List<TimelineClip>>();

            foreach (var clip in TimelineEditor.selectedClips)
            {
                clipsPerTrack.TryAdd(clip.GetParentTrack(), new List<TimelineClip>());
                clipsPerTrack[clip.GetParentTrack()].Add(clip);
            }
            
            foreach (var kv in clipsPerTrack)
            {
                var clips = kv.Value
                    .Where(c => c?.asset is TransformClip)
                    .OrderBy(c => c.start)
                    .ThenBy(c => c.end)
                    .ToList();

                for (int i = 0; i < clips.Count - 1; i++)
                {
                    var start = clips[i].asset as TransformClip;
                    var end = clips[i + 1].asset as TransformClip;
                    if (!start || !end) continue;

                    Vector3 startPos;
                    switch (start.positionMethod)
                    {
                        case TransformMethod.Value:
                            startPos = start.position;
                            if (start.applyPositionOffset) startPos += initialPos;
                            break;
                        case TransformMethod.Transform:
                            var startTarget = start.sourceTransform.Resolve(TimelineEditor.inspectedDirector.playableGraph.GetResolver());
                            if (startTarget)
                            {
                                startPos = startTarget.position;
                                if (start.positionTransformLocalOffset != Vector3.zero)
                                {
                                    startPos += startTarget.TransformVector(start.positionTransformLocalOffset);
                                }

                                startPos += start.positionTransformWorldOffset;
                            }
                            else
                            {
                                startPos = Vector3.zero;
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    Vector3 endPos;
                    switch (end.positionMethod)
                    {
                        case TransformMethod.Value:
                            endPos = end.position;
                            if (end.applyPositionOffset) endPos += initialPos;
                            break;
                        case TransformMethod.Transform:
                            var endTarget = end.sourceTransform.Resolve(TimelineEditor.inspectedDirector.playableGraph.GetResolver());
                            if (endTarget)
                            {
                                endPos = endTarget.position;
                                if (end.positionTransformLocalOffset != Vector3.zero)
                                {
                                    endPos += endTarget.TransformVector(end.positionTransformLocalOffset);
                                }

                                endPos += end.positionTransformWorldOffset;
                            }
                            else
                            {
                                endPos = Vector3.zero;
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                
                    DrawPath(startPos, endPos);
                }
            }

        }

        void DrawSceneGUI(TransformClip clip)
        {
            if (!clip) return;
            var director = TimelineEditor.inspectedDirector;
            if (!director) return;
            
            var track = director.GetTrackOf<TransformTrack>(clip);
            if (!track) return;
            var binding = director.GetGenericBinding(track) as Transform;
            if (!binding) return;
            
            if (!director.playableGraph.IsValid()) director.RebuildGraph();

            // 렌더러 컴포넌트들 가져오기
            var meshFilters = binding.GetComponentsInChildren<MeshFilter>();
            var skinnedMeshRenderers = binding.GetComponentsInChildren<SkinnedMeshRenderer>();


            var position = Vector3.zero;
            var rotation = Quaternion.identity;
            var scale = Vector3.one;

            var destination = clip.sourceTransform.Resolve(director.playableGraph.GetResolver());

            switch (clip.positionMethod)
            {
                case TransformMethod.Value:
                    position = clip.position;
                    if (clip.applyPositionOffset)
                    {
                        position += initialPos;
                    }
                    break;
                case TransformMethod.Transform:
                    if (destination)
                    {
                        position = destination.position;
                        if (clip.positionTransformLocalOffset != Vector3.zero)
                        {
                            position += destination.TransformVector(clip.positionTransformLocalOffset);
                        }

                        position += clip.positionTransformWorldOffset;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (clip.rotationMethod)
            {
                case TransformMethod.Value:
                    if (clip.applyRotationOffset)
                    {
                        rotation = Quaternion.Euler(clip.rotation + initialRot.eulerAngles);
                    }
                    else
                    {
                        rotation = Quaternion.Euler(clip.rotation);    
                    }
                    break;
                case TransformMethod.Transform:
                    if (destination)
                    {
                        rotation = destination.rotation;
                        if (clip.rotationTransformWorldOffset != Vector3.zero)
                        {
                            rotation = Quaternion.Euler(clip.rotationTransformWorldOffset) * rotation;
                        }

                        if (clip.rotationTransformLocalOffset != Vector3.zero)
                        {
                            rotation *= Quaternion.Euler(clip.rotationTransformLocalOffset);
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (clip.scaleMethod)
            {
                case TransformMethod.Value:
                    scale = clip.scale;
                    if (clip.applyScaleOffset)
                    {
                        scale += initialScale;
                    }
                    break;
                case TransformMethod.Transform:
                    if (destination)
                    {
                        scale = destination.localScale;
                        if (clip.scaleTransformWorldOffset != Vector3.zero)
                        {
                            scale += ConvertWorldScaleOffsetToLocal(binding, clip.scaleTransformWorldOffset);
                        }

                        if (clip.scaleTransformLocalOffset != Vector3.zero)
                        {
                            scale += clip.scaleTransformLocalOffset;
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            
            Matrix4x4 matrix = Matrix4x4.TRS(position, rotation.normalized, scale * 1.0001f);
            
            Matrix4x4 targetRootInverseMatrix = binding.worldToLocalMatrix;

            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter && meshFilter.sharedMesh)
                {
                    Matrix4x4 finalMatrix = matrix * targetRootInverseMatrix * meshFilter.transform.localToWorldMatrix;
                    DrawMesh(meshFilter.sharedMesh, finalMatrix);
                }
            }

            foreach (var renderer in skinnedMeshRenderers)
            {
                if (renderer.sharedMesh != null)
                {
                    Matrix4x4 finalMatrix = matrix * targetRootInverseMatrix * renderer.transform.localToWorldMatrix;
                    DrawMesh(renderer.sharedMesh, finalMatrix);
                }
            }
        }

        Vector3 ConvertWorldScaleOffsetToLocal(Transform binding, Vector3 worldOffset)
        {
            if (!binding) return worldOffset;
            var parent = binding.parent;
            if (!parent) return worldOffset;

            var parentLossyScale = parent.lossyScale;
            if (parentLossyScale == Vector3.zero)
            {
                return worldOffset;
            }

            float ConvertAxis(float offset, float parentAxis)
            {
                return Mathf.Approximately(parentAxis, 0f) ? offset : offset / parentAxis;
            }

            return new Vector3(
                ConvertAxis(worldOffset.x, parentLossyScale.x),
                ConvertAxis(worldOffset.y, parentLossyScale.y),
                ConvertAxis(worldOffset.z, parentLossyScale.z)
            );
        }


        void DrawMesh(Mesh mesh, Matrix4x4 matrix)
        {
            if (!wireframeMaterial)
            {
                var shader = Shader.Find("VR/SpatialMapping/Wireframe");
                wireframeMaterial = new Material(shader);
                wireframeMaterial.SetFloat("_WireThickness", 500);
                wireframeMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
                
            wireframeMaterial.SetPass(0);
                
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                Graphics.DrawMeshNow(mesh, matrix);
            }
            
        }

        void DrawPath(Vector3 start, Vector3 end)
        {
            var startGUIPos = HandleUtility.WorldToGUIPointWithDepth(start);
            var endGUIPos = HandleUtility.WorldToGUIPointWithDepth(end);
            var middleGUIPos = Vector3.Lerp(startGUIPos, endGUIPos, 0.5f);
            var guiPosDir = (endGUIPos - startGUIPos).normalized;
            var perp = Vector2.Perpendicular(guiPosDir);

            Handles.color = new Color(1f, 0f, 0.25f);
            Handles.DrawLine(start, end, 4);
            Handles.BeginGUI();
            if(startGUIPos.z >= 0)Handles.DrawSolidDisc((Vector2)startGUIPos, Vector3.forward, 6);
            if(endGUIPos.z >= 0)Handles.DrawSolidDisc((Vector2)endGUIPos, Vector3.forward, 6);
            
            if(middleGUIPos.z >= 0) Handles.DrawAAConvexPolygon(
                (Vector2)middleGUIPos, 
                (Vector2)middleGUIPos + perp * 8 - (Vector2)guiPosDir * 10,
                (Vector2)middleGUIPos - perp * 8 - (Vector2)guiPosDir * 10);
            Handles.EndGUI();
            Handles.color = Color.white;
        }
    }
}
