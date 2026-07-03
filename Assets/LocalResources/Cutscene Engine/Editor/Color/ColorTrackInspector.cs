using System;
using System.Linq;
using CutsceneEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditor.UIElements;
using UnityEngine;
#if URP
using UnityEngine.Rendering.Universal;
#endif
#if HDRP
using UnityEngine.Rendering.HighDefinition;
#endif
using UnityEngine.Timeline;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using Toggle = UnityEngine.UIElements.Toggle;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(ColorTrack))]
    [CanEditMultipleObjects]
    public class ColorTrackInspector : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var director = TimelineEditor.inspectedDirector;
            if (!director) return null;

            var root = new VisualElement();
            
            var tintField = new PropertyField(serializedObject.FindProperty(nameof(ColorTrack.isTint)));
            root.Add(tintField);

            var binding = director.GetGenericBinding(target as TrackAsset) as GameObject;
            if (!binding)
            {
                root.Add(new Label("Bind a GameObject to this track.\n\n" +
                                   "The following Components are available:\n" +
                                   "- Renderer (MeshRenderer, SkinnedMeshRenderer, LineRenderer, etc.)\n" +
                                   "- SpriteRenderer\n" +
                                   "- DecalProjector\n" +
                                   "- UGUI Graphic component\n" +
                                   "- UIDocument (UIElement)\n" +
                                   "- ParticleSystem" +
                                   "- VFX (VisualEffect Graph)"));
            }
            else
            {
                if (binding.TryGetComponent(out Graphic g))
                {
                    var applyToMaterialProp = serializedObject.FindProperty(nameof(ColorTrack.applyToMaterialProperty));
                    var applyToMaterialPropertyField = new Toggle(applyToMaterialProp.displayName);
                    applyToMaterialPropertyField.BindProperty(applyToMaterialProp);
                    applyToMaterialPropertyField.labelElement.style.width = Length.Percent(40);
                    root.Add(applyToMaterialPropertyField);
                    
                    var propertyNameField = new PropertyField(serializedObject.FindProperty(nameof(ColorTrack.propertyName)));
                    propertyNameField.BindVisible(applyToMaterialPropertyField, true);
                    root.Add(propertyNameField);
                }
                else if (binding.TryGetComponent(out UIDocument doc))
                {
                    var elementPathField = new PropertyField(serializedObject.FindProperty(nameof(ColorTrack.elementName)));
                    root.Add(elementPathField);

                    var elementColorTargetField = new PropertyField(serializedObject.FindProperty(nameof(ColorTrack.uiElementColorTarget)));
                    root.Add(elementColorTargetField);
                }
#if URP || HDRP
                else if (binding.TryGetComponent(out DecalProjector decal))
#else
                else if (binding.TryGetComponent(out Projector decal))
#endif
                {
                    var propertyNameField = new PropertyField(serializedObject.FindProperty(nameof(ColorTrack.propertyName)));
                    root.Add(propertyNameField);

#if URP || HDRP
                    var alphaToOpacityField = new PropertyField(serializedObject.FindProperty(nameof(ColorTrack.applyAlphaToDecalOpacity)));
                    root.Add(alphaToOpacityField);
#endif
                    
                }
#if VFX
                else if (binding.TryGetComponent(out VisualEffect vfx))
                {
                    var propertyNameField = new PropertyField(serializedObject.FindProperty(nameof(ColorTrack.propertyName)));
                    root.Add(propertyNameField);
                }
#endif
                else if (binding.TryGetComponent(out SpriteRenderer sr))
                {
                    var applyToMaterialProp = serializedObject.FindProperty(nameof(ColorTrack.applyToMaterialProperty));
                    var applyToMaterialPropertyField = new Toggle(applyToMaterialProp.displayName);
                    applyToMaterialPropertyField.BindProperty(applyToMaterialProp);
                    applyToMaterialPropertyField.labelElement.style.width = Length.Percent(40);
                    root.Add(applyToMaterialPropertyField);
                    
                    var propertyNameField = new PropertyField(serializedObject.FindProperty(nameof(ColorTrack.propertyName)));
                    propertyNameField.BindVisible(applyToMaterialPropertyField, true);
                    root.Add(propertyNameField);
                }
                else if (binding.TryGetComponent(out Renderer r))
                {
                    var materialIndexField = new PropertyField(serializedObject.FindProperty(nameof(ColorTrack.materialIndex)));
                    materialIndexField.tooltip = "Target material's index of the renderer.\n" +
                                                 "If this value is less than 0, all materials will be the target.";
                    root.Add(materialIndexField);
                    
                    var propertyNameField = new PropertyField(serializedObject.FindProperty(nameof(ColorTrack.propertyName)));
                    root.Add(propertyNameField);
                }
                else
                {
                    // invalid
                }
            }

            return root;
        }
    }
}