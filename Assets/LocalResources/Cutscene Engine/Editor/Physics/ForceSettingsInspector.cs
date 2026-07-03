using System;
using UnityEngine;
using UnityEditor;
using CutsceneEngine;

namespace CutsceneEngineEditor
{
    [CustomEditor(typeof(ForceSettings))]
    public class ForceSettingsInspector : Editor
    {
        SerializedProperty forcesProperty;

        void OnEnable()
        {
            forcesProperty = serializedObject.FindProperty("forces");

        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var forceSettings = (ForceSettings)target;

            DrawRigidbodyInfo(forceSettings);
            DrawForceConfiguration();
            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            var forceSettings = (ForceSettings)target;
            foreach (var data in forceSettings.forces)
            {
                DrawForceGizmos(forceSettings, data);
            }
        }


        void DrawForceGizmos(ForceSettings force, ForceData data)
        {
            if (!data.showGizmos) return;
            
            // Transform force and torque to world space if using local space
            var forceToShow = data.useLocalForce ? force.transform.TransformDirection(data.force) : data.force;
            var torqueToShow = data.useLocalTorque ? force.transform.TransformDirection(data.torque) : data.torque;
            
            if (force.rb2D)
            {
                DrawForceGizmo2D(force.transform.position, forceToShow, data.forceGizmoColor);
                DrawTorqueGizmo2D(force.transform.position, torqueToShow.z, data.torqueGizmoColor);
            }

            if (force.rb3D)
            {
                DrawForceGizmo3D(force.transform.position, forceToShow, data.forceGizmoColor);
                DrawTorqueGizmo3D(force.transform.position, torqueToShow, data.torqueGizmoColor);
            }
        }

        void DrawForceGizmo3D(Vector3 position, Vector3 force, Color color)
        {
            if (force.magnitude < 0.001f) return;

            Handles.color = color;
            Handles.ArrowHandleCap(0, position, Quaternion.LookRotation(force.normalized),
                force.magnitude * 0.5f, EventType.Repaint);
        }

        void DrawForceGizmo2D(Vector3 position, Vector3 force, Color color)
        {
            if (force.magnitude < 0.001f) return;

            Handles.color = color;
            float angle = Mathf.Atan2(force.y, force.x) * Mathf.Rad2Deg;
            Handles.ArrowHandleCap(0, position, Quaternion.Euler(0, 0, angle),
                force.magnitude * 0.5f, EventType.Repaint);
        }

        void DrawTorqueGizmo3D(Vector3 position, Vector3 torque, Color color)
        {
            if (torque.magnitude < 0.001f) return;

            Handles.color = color;
            Vector3 axis = torque.normalized;
            float magnitude = torque.magnitude;
            float radius = 1.0f;

            // Calculate perpendicular vector for the rotation plane
            Vector3 perpendicular = Vector3.Cross(axis, Vector3.up);
            if (perpendicular.magnitude < 0.1f)
                perpendicular = Vector3.Cross(axis, Vector3.right);
            perpendicular = perpendicular.normalized;

            // Calculate arc angle proportional to torque magnitude
            float arcAngle = Mathf.Min(magnitude * 30f, 270f);

            // Draw the arc
            Handles.DrawWireArc(position, axis, perpendicular, arcAngle, radius);

            // Draw arrow at the end of the arc
            Vector3 arcEnd = position + Quaternion.AngleAxis(arcAngle, axis) * perpendicular * radius;
            Vector3 tangent = Vector3.Cross(axis, (arcEnd - position).normalized);

            float arrowSize = 0.2f;
            Vector3 arrowDir1 = Quaternion.AngleAxis(150f, axis) * tangent * arrowSize;
            Vector3 arrowDir2 = Quaternion.AngleAxis(-150f, axis) * tangent * arrowSize;

            Handles.DrawLine(arcEnd, arcEnd + arrowDir1);
            Handles.DrawLine(arcEnd, arcEnd + arrowDir2);
        }

        void DrawTorqueGizmo2D(Vector3 position, float torque, Color color)
        {
            if (Mathf.Abs(torque) < 0.001f) return;

            Handles.color = color;
            float radius = 1.0f;
            Vector3 normal = Vector3.forward;

            float direction = Mathf.Sign(torque);
            float arcAngle = Mathf.Min(Mathf.Abs(torque) * 30f, 270f);

            Vector3 startDir = Vector3.right;
            if (direction < 0) arcAngle = -arcAngle;

            Handles.DrawWireArc(position, normal, startDir, arcAngle, radius);

            // Draw arrow at the end of the arc
            Vector3 arcEnd = position + Quaternion.AngleAxis(arcAngle, normal) * startDir * radius;
            Vector3 tangent = Vector3.Cross(normal, (arcEnd - position).normalized) * direction;

            float arrowSize = 0.2f;
            Vector3 arrowDir1 = Quaternion.AngleAxis(150f, normal) * tangent * arrowSize;
            Vector3 arrowDir2 = Quaternion.AngleAxis(-150f, normal) * tangent * arrowSize;

            Handles.DrawLine(arcEnd, arcEnd + arrowDir1);
            Handles.DrawLine(arcEnd, arcEnd + arrowDir2);
        }

        void DrawRigidbodyInfo(ForceSettings forceSettings)
        {
            EditorGUILayout.LabelField("Rigidbody Info", EditorStyles.boldLabel);

            var has3D = forceSettings.Has3DRigidbody();
            var has2D = forceSettings.Has2DRigidbody();

            if (has3D)
            {
                EditorGUILayout.HelpBox("3D Rigidbody detected - Using 3D force modes", MessageType.Info);
            }
            else if (has2D)
            {
                EditorGUILayout.HelpBox("2D Rigidbody detected - Using 2D force modes", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("No Rigidbody component found! Add a Rigidbody or Rigidbody2D component.", MessageType.Warning);
            }

            EditorGUILayout.Space();
        }

        void DrawForceConfiguration()
        {
            EditorGUILayout.LabelField("Force Configuration", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(forcesProperty, new GUIContent("Forces"), true);
            EditorGUILayout.Space();
        }
    }

    [CustomPropertyDrawer(typeof(ForceData))]
    public class ForceDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw foldout header
            var foldout = property.isExpanded;
            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, foldout, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var yOffset = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                // Force and Torque
                var force = property.FindPropertyRelative("force");
                var forceRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(forceRect, force);
                yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;


                var forceMagnitudeRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                var forceValue = force.vector3Value;
                EditorGUI.indentLevel++;
                var mag = EditorGUI.FloatField(forceMagnitudeRect, "Force Magnitude", forceValue.magnitude);
                forceValue = forceValue.normalized * mag;
                force.vector3Value = forceValue;
                yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.indentLevel--;


                var torqueRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(torqueRect, property.FindPropertyRelative("torque"));
                yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                // Local/World space toggles
                var useLocalForceRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(useLocalForceRect, property.FindPropertyRelative("useLocalForce"));
                yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                var useLocalTorqueRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(useLocalTorqueRect, property.FindPropertyRelative("useLocalTorque"));
                yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                // Timing
                var delayRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(delayRect, property.FindPropertyRelative("startDelay"));
                yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                var durationRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                var durationProp = property.FindPropertyRelative("duration");
                EditorGUI.PropertyField(durationRect, durationProp);
                if (durationProp.floatValue < 0) durationProp.floatValue = 0;
                yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                // Force Mode (detect if 2D or 3D)
                var target = property.serializedObject.targetObject as ForceSettings;
                if (target != null)
                {
                    var forceModeRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);

                    if (target.Has2DRigidbody())
                    {
                        EditorGUI.PropertyField(forceModeRect, property.FindPropertyRelative("forceMode2D"), new GUIContent("Force Mode"));
                    }
                    else
                    {
                        EditorGUI.PropertyField(forceModeRect, property.FindPropertyRelative("forceMode"), new GUIContent("Force Mode"));
                    }

                    yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                // Animation Curves
                var forceCurveRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(forceCurveRect, property.FindPropertyRelative("forceCurve"));
                yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                var torqueCurveRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(torqueCurveRect, property.FindPropertyRelative("torqueCurve"));
                
                
                var showGizmosProperty = property.FindPropertyRelative("showGizmos");
                var forceGizmoColorProperty = property.FindPropertyRelative("forceGizmoColor");
                var torqueGizmoColorProperty = property.FindPropertyRelative("torqueGizmoColor");
                
                yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                var showGizmosRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(showGizmosRect, showGizmosProperty);
                yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                if (showGizmosProperty.boolValue)
                {
                    EditorGUI.indentLevel++;
                    var forceGizmoColorRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(forceGizmoColorRect, forceGizmoColorProperty);
                    yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                    var torqueGizmoColorRect = new Rect(position.x, position.y + yOffset, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(torqueGizmoColorRect, torqueGizmoColorProperty);
                    yOffset += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            // Calculate height for all expanded properties
            var height = EditorGUIUtility.singleLineHeight; // Foldout header
            height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 11; // Added 2 for useLocalForce and useLocalTorque
            
            var showGizmosProperty = property.FindPropertyRelative("showGizmos");
            if (showGizmosProperty.boolValue)
            {
                height += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
            }

            return height;
        }
    }
}