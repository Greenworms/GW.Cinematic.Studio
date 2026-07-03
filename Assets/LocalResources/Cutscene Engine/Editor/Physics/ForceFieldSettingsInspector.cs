using CutsceneEngine;
using UnityEditor;
using UnityEngine;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(ForceFieldSettings))]
    public class ForceFieldSettingsInspector : Editor
    {
        #region Serialized Properties
        private SerializedProperty shapeProp;
        private SerializedProperty radiusProp;
        private SerializedProperty lengthProp;
        private SerializedProperty boxSizeProp;
        private SerializedProperty forceMagnitudeProp;
        private SerializedProperty forceFalloffProp;
        private SerializedProperty forceOverTimeProp;
        private SerializedProperty dimensionProp;
        private SerializedProperty forceMode3DProp;
        private SerializedProperty forceMode2DProp;
        private SerializedProperty useExplosionForceProp;
        private SerializedProperty explosionRadiusProp;
        private SerializedProperty upwardsModifierProp;
        private SerializedProperty startDelayProp;
        private SerializedProperty durationProp;
        private SerializedProperty targetRootProp;
        #endregion

        #region Constants
        private const float ArrowBodyScale = 0.1f;
        private const float ArrowHeadSize = 0.1f;
        #endregion

        private void OnEnable()
        {
            // 속성을 미리 찾아 캐싱하여 성능 최적화
            shapeProp = serializedObject.FindProperty("shape");
            radiusProp = serializedObject.FindProperty("radius");
            lengthProp = serializedObject.FindProperty("length");
            boxSizeProp = serializedObject.FindProperty("boxSize");
            forceMagnitudeProp = serializedObject.FindProperty("forceMagnitude");
            forceFalloffProp = serializedObject.FindProperty("forceFalloff");
            forceOverTimeProp = serializedObject.FindProperty("forceOverTime");
            dimensionProp = serializedObject.FindProperty("dimension");
            forceMode3DProp = serializedObject.FindProperty("forceMode3D");
            forceMode2DProp = serializedObject.FindProperty("forceMode2D");
            useExplosionForceProp = serializedObject.FindProperty("useExplosionForce");
            explosionRadiusProp = serializedObject.FindProperty("explosionRadius");
            upwardsModifierProp = serializedObject.FindProperty("upwardsModifier");
            startDelayProp = serializedObject.FindProperty("startDelay");
            durationProp = serializedObject.FindProperty("duration");
            targetRootProp = serializedObject.FindProperty("targetRoot");
        }

        #region Inspector GUI
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawTargetSettings();
            EditorGUILayout.Space();
            DrawShapeSettings();
            EditorGUILayout.Space();
            DrawDimensionSettings();
            EditorGUILayout.Space();
            DrawForceSettings();
            EditorGUILayout.Space();
            DrawTimeSettings();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawShapeSettings()
        {
            EditorGUILayout.LabelField("Shape Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(shapeProp);

            ForceFieldShape shape = (ForceFieldShape)shapeProp.enumValueIndex;
            switch (shape)
            {
                case ForceFieldShape.Sphere:
                case ForceFieldShape.Hemisphere:
                    EditorGUILayout.PropertyField(radiusProp);
                    break;
                case ForceFieldShape.Box:
                    EditorGUILayout.PropertyField(boxSizeProp);
                    break;
                case ForceFieldShape.Cylinder:
                    EditorGUILayout.PropertyField(radiusProp);
                    EditorGUILayout.PropertyField(lengthProp);
                    break;
            }
        }

        private void DrawForceSettings()
        {
            EditorGUILayout.LabelField("Force Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(forceMagnitudeProp);

            ForceDimension dim = (ForceDimension)dimensionProp.enumValueIndex;
            if (dim == ForceDimension.Mode3D)
            {
                EditorGUILayout.PropertyField(useExplosionForceProp);
                if (useExplosionForceProp.boolValue)
                {
                    EditorGUILayout.PropertyField(explosionRadiusProp);
                    EditorGUILayout.PropertyField(upwardsModifierProp);
                }
                else
                {
                    EditorGUILayout.PropertyField(forceFalloffProp);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(forceFalloffProp);
            }
            
            EditorGUILayout.PropertyField(forceOverTimeProp);
        }

        private void DrawDimensionSettings()
        {
            EditorGUILayout.LabelField("Dimension Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(dimensionProp);

            ForceDimension dim = (ForceDimension)dimensionProp.enumValueIndex;
            if (dim == ForceDimension.Mode3D)
            {
                EditorGUILayout.PropertyField(forceMode3DProp);
            }
            else
            {
                EditorGUILayout.PropertyField(forceMode2DProp);
            }
        }

        private void DrawTimeSettings()
        {
            EditorGUILayout.LabelField("Time Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(startDelayProp);
            EditorGUILayout.PropertyField(durationProp);
            if (durationProp.floatValue < 0) durationProp.floatValue = 0;
        }

        private void DrawTargetSettings()
        {
            EditorGUILayout.LabelField("Target Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(targetRootProp);
        }
        #endregion

        #region Scene GUI
        private void OnSceneGUI()
        {
            ForceFieldSettings forceField = (ForceFieldSettings)target;
            if (forceField == null || !forceField.enabled) return;

            DrawShapeOutline(forceField);

            if (forceField.dimension == ForceDimension.Mode3D && forceField.useExplosionForce)
            {
                Handles.color = Color.red;
                Vector3 center = forceField.transform.position;
                float r = forceField.explosionRadius;
                
                Handles.DrawWireDisc(center, Vector3.up, r);
                Handles.DrawWireDisc(center, Vector3.right, r);
                Handles.DrawWireDisc(center, Vector3.forward, r);
            }
            else
            {
                DrawForceArrows(forceField);
            }
        }

        private void DrawShapeOutline(ForceFieldSettings forceField)
        {
            Handles.color = Color.yellow;
            Matrix4x4 originalMatrix = Handles.matrix;
            Handles.matrix = forceField.transform.localToWorldMatrix;

            switch (forceField.shape)
            {
                case ForceFieldShape.Sphere:
                    Handles.DrawWireDisc(Vector3.zero, Vector3.up, forceField.radius);
                    Handles.DrawWireDisc(Vector3.zero, Vector3.right, forceField.radius);
                    Handles.DrawWireDisc(Vector3.zero, Vector3.forward, forceField.radius);
                    break;

                case ForceFieldShape.Hemisphere:
                    Handles.DrawWireDisc(Vector3.zero, Vector3.up, forceField.radius); // Base
                    Handles.DrawWireArc(Vector3.zero, Vector3.forward, Vector3.right, -180, forceField.radius); // Vertical arc
                    Handles.DrawWireArc(Vector3.zero, Vector3.right, Vector3.forward, 180, forceField.radius);  // Vertical arc
                    break;

                case ForceFieldShape.Box:
                    Vector3 boxCenter = new Vector3(0f, forceField.boxSize.y * 0.5f, 0f);
                    Handles.DrawWireCube(boxCenter, forceField.boxSize);
                    break;

                case ForceFieldShape.Cylinder:
                    Vector3 bottom = Vector3.zero;
                    Vector3 top = new Vector3(0, forceField.length, 0);
                    Handles.DrawWireDisc(top, Vector3.up, forceField.radius);
                    Handles.DrawWireDisc(bottom, Vector3.up, forceField.radius);
                    Handles.DrawLine(top + new Vector3(forceField.radius, 0, 0), bottom + new Vector3(forceField.radius, 0, 0));
                    Handles.DrawLine(top + new Vector3(-forceField.radius, 0, 0), bottom + new Vector3(-forceField.radius, 0, 0));
                    Handles.DrawLine(top + new Vector3(0, 0, forceField.radius), bottom + new Vector3(0, 0, forceField.radius));
                    Handles.DrawLine(top + new Vector3(0, 0, -forceField.radius), bottom + new Vector3(0, 0, -forceField.radius));
                    break;
            }
            Handles.matrix = originalMatrix;
        }

        private void DrawForceArrows(ForceFieldSettings forceField)
        {
            Handles.color = new Color(1f, 0.6f, 0.1f, 0.9f);

            switch (forceField.shape)
            {
                case ForceFieldShape.Sphere:
                    DrawSphereForceGizmos(forceField);
                    break;
                case ForceFieldShape.Hemisphere:
                    DrawHemisphereForceGizmos(forceField);
                    break;
                case ForceFieldShape.Box:
                    DrawBoxForceGizmos(forceField);
                    break;
                case ForceFieldShape.Cylinder:
                    DrawCylinderForceGizmos(forceField);
                    break;
            }
        }
        #endregion

        #region Gizmo Drawing Methods
        private void DrawSphereForceGizmos(ForceFieldSettings ff)
        {
            Vector3[] points =
            {
                new Vector3(1, 0, 0), new Vector3(-1, 0, 0),
                new Vector3(0, 1, 0), new Vector3(0, -1, 0),
                new Vector3(0, 0, 1), new Vector3(0, 0, -1),
                new Vector3(1, 1, 1).normalized, new Vector3(-1, 1, 1).normalized,
                new Vector3(1, -1, 1).normalized, new Vector3(1, 1, -1).normalized,
                new Vector3(-1, -1, 1).normalized, new Vector3(-1, 1, -1).normalized,
                new Vector3(1, -1, -1).normalized, new Vector3(-1, -1, -1).normalized,
            };
            foreach (var p in points)
            {
                DrawArrow(ff, p * ff.radius);
            }
        }

        private void DrawHemisphereForceGizmos(ForceFieldSettings ff)
        {
            Vector3[] points =
            {
                new Vector3(1, 0, 0), new Vector3(-1, 0, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, 0, 1), new Vector3(0, 0, -1),
                new Vector3(1, 1, 1).normalized, new Vector3(-1, 1, 1).normalized,
                new Vector3(1, 1, -1).normalized, new Vector3(-1, 1, -1).normalized,
                new Vector3(0.7f, 0.7f, 0).normalized, new Vector3(-0.7f, 0.7f, 0).normalized,
            };
            foreach (var p in points)
            {
                if (Vector3.Dot(p, Vector3.up) >= -0.01f)
                {
                    DrawArrow(ff, p * ff.radius);
                }
            }
        }

        private void DrawBoxForceGizmos(ForceFieldSettings ff)
        {
            float halfX = ff.boxSize.x * 0.5f;
            float halfZ = ff.boxSize.z * 0.5f;
            float baseY = 0f;

            Vector3[] samplePoints =
            {
                new Vector3( halfX * 0.5f, baseY,  halfZ * 0.5f),
                new Vector3(-halfX * 0.5f, baseY,  halfZ * 0.5f),
                new Vector3( halfX * 0.5f, baseY, -halfZ * 0.5f),
                new Vector3(-halfX * 0.5f, baseY, -halfZ * 0.5f),
            };

            foreach (var point in samplePoints)
            {
                DrawArrow(ff, point);
            }
        }

        private void DrawCylinderForceGizmos(ForceFieldSettings ff)
        {
            float placementRadius = ff.radius * 0.5f;
            float baseY = 0f;
            DrawArrow(ff, new Vector3(placementRadius, baseY, placementRadius));
            DrawArrow(ff, new Vector3(placementRadius, baseY, -placementRadius));
            DrawArrow(ff, new Vector3(-placementRadius, baseY, placementRadius));
            DrawArrow(ff, new Vector3(-placementRadius, baseY, -placementRadius));
        }
        #endregion

        #region Helper Methods
        private void DrawArrow(ForceFieldSettings ff, Vector3 localPosition)
        {
            Transform transform = ff.transform;
            Vector3 startPoint = transform.TransformPoint(localPosition);
            Vector3 direction = GetForceDirection(startPoint, ff);
            float magnitude = ff.forceMagnitude;

            if (direction.sqrMagnitude < 0.001f || Mathf.Abs(magnitude) < 0.001f) return;

            if (magnitude < 0)
            {
                direction = -direction;
                magnitude = -magnitude;
            }

            float bodyLength = magnitude * ArrowBodyScale;
            Vector3 endPoint = startPoint + direction * bodyLength;

            Handles.DrawLine(startPoint, endPoint);
            Handles.ConeHandleCap(0, endPoint, Quaternion.LookRotation(direction), ArrowHeadSize, EventType.Repaint);
        }

        private Vector3 GetForceDirection(Vector3 worldPosition, ForceFieldSettings ff)
        {
            Vector3 localPosition = ff.transform.InverseTransformPoint(worldPosition);
            Vector3 direction = Vector3.zero;

            switch (ff.shape)
            {
                case ForceFieldShape.Sphere:
                case ForceFieldShape.Hemisphere:
                    direction = localPosition.normalized;
                    break;
                case ForceFieldShape.Box:
                    direction = Vector3.up; // Local Y
                    break;
                case ForceFieldShape.Cylinder:
                    direction = Vector3.up; // Local Y
                    break;
            }

            return ff.transform.TransformDirection(direction);
        }
        #endregion
    }
}
