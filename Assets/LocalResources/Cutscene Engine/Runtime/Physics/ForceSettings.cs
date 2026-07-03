using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CutsceneEngine
{
    /// <summary>
    /// Data structure containing force and torque settings for physics simulation.
    /// </summary>
    [Serializable]
    public class ForceData
    {
        [Tooltip("The force applied to the Rigidbody. This is the argument input to the AddForce method.")]
        public Vector3 force = Vector3.zero;
        
        [Tooltip("The torque applied to the Rigidbody. This is the argument input to the AddForce method.")]
        public Vector3 torque = Vector3.zero;
        
        [Min(0)]
        [Tooltip("Delay before force is applied. After the simulation starts, the force is applied after this amount of time.")]
        public float startDelay;

        [Tooltip("How the force and torque are applied. Only used when the object has a Rigidbody component.")]
        public ForceMode forceMode;
        
        [Tooltip("How the force and torque are applied. Only used when the object has a Rigidbody2D component.")]
        public ForceMode2D forceMode2D;

        [Min(0)]
        [Tooltip("The time to apply the force. If this value is 0, the force will only be applied for one simulation step. " +
                 "If you need to apply the force continuously, like a car or a rocket, set this value to greater than 0.")]
        public float duration;
        
        [Tooltip("Represents the value by which the force applied over the time duration is multiplied. " +
                 "The duration applied to this curve is normalized to a time between 0 and 1.")]
        public AnimationCurve forceCurve;
        
        [Tooltip("Represents the value by which the torque applied over the time duration is multiplied." +
                 "The duration applied to this curve is normalized to a time between 0 and 1.")]
        public AnimationCurve torqueCurve;
        
        [Tooltip("Turns the gizmo showing the direction of forces and torques in the scene view on or off.")]
        public bool showGizmos = true;
        
        [Tooltip("Color for force gizmo visualization.")]
        public Color forceGizmoColor;
        
        [Tooltip("Color for torque gizmo visualization.")]
        public Color torqueGizmoColor;
        
        [Tooltip("Whether to apply force in local space relative to the object's transform.")]
        public bool useLocalForce = false;
        
        [Tooltip("Whether to apply torque in local space relative to the object's transform.")]
        public bool useLocalTorque = false;

        [NonSerialized] public bool applied;
        
        [HideInInspector] [SerializeField] bool _initialized;
        
        /// <summary>
        /// Initializes the ForceData with default values.
        /// </summary>
        public void Init()
        {
            if(_initialized) return;
            force = Vector3.zero;
            torque = Vector3.zero;
            startDelay = 0;
            duration = 0;
            forceMode = ForceMode.Impulse;
            forceMode2D = ForceMode2D.Impulse;
            
            forceCurve = AnimationCurve.Linear(0, 1, 1, 1);
            torqueCurve = AnimationCurve.Linear(0, 1, 1, 1);
            
            showGizmos = true;
            forceGizmoColor = Color.red;
            torqueGizmoColor = Color.blue;
            useLocalForce = false;
            useLocalTorque = false;
            _initialized = true;
        }
        
        /// <summary>
        /// Checks if this force data has meaningful force or torque values.
        /// </summary>
        /// <returns>True if force or torque magnitude is greater than 0.001f.</returns>
        public bool IsValid()
        {
            return force.magnitude > 0.001f || torque.magnitude > 0.001f;
        }
    }

    /// <summary>
    /// Component that manages force application to rigidbodies during physics simulation.
    /// Supports both 2D and 3D rigidbodies with customizable force curves and timing.
    /// </summary>
    [AddComponentMenu("Cutscene Engine/Force Settings (Cutscene Engine)")]
    public class ForceSettings : MonoBehaviour
    {
        /// <summary>
        /// Gets the 3D Rigidbody component attached to this GameObject.
        /// </summary>
        public Rigidbody rb3D => _rb3D;
        
        /// <summary>
        /// Gets the 2D Rigidbody component attached to this GameObject.
        /// </summary>
        public Rigidbody2D rb2D => _rb2D;
        
        /// <summary>
        /// Array of force data configurations to apply during simulation.
        /// </summary>
        public ForceData[] forces = new ForceData[1];

        [SerializeField] Rigidbody _rb3D;
        [SerializeField] Rigidbody2D _rb2D;
        [SerializeField] [HideInInspector] int _arraySize;
        Coroutine _applyForcesCoroutine;
        void Reset()
        {
            _rb3D = GetComponent<Rigidbody>();
            _rb2D = GetComponent<Rigidbody2D>();
        }

        void OnValidate()
        {
#if UNITY_EDITOR
            if (forces.Length > _arraySize)
            {
                for (int i = 0; i < forces.Length; i++)
                {
                    if (forces[i] == null) forces[i] = new ForceData();
                    forces[i].Init();
                }
            }
#endif
            Initialize();
        }
        
        public void Initialize()
        {
            _rb3D = GetComponent<Rigidbody>();
            _rb2D = GetComponent<Rigidbody2D>();
            if (forces != null)
            {
                foreach (var f in forces)
                {
                    if (f != null) f.applied = false;
                }
            }
        }
        /// <summary>
        /// Applys the configured force via coroutine.
        /// </summary>
        public void ApplyForces()
        {
            if(_applyForcesCoroutine != null) StopCoroutine(_applyForcesCoroutine);
            _applyForcesCoroutine = StartCoroutine(ApplyForcesCoroutine());
        }
        /// <summary>
        /// Applys the configured force based on elapsed time.
        /// It's called by the Physics Simulator.
        /// </summary>
        /// <param name="elapsedTime"></param>
        public void ApplyForces(float elapsedTime)
        {
            if(_applyForcesCoroutine != null)
            {
                StopCoroutine(_applyForcesCoroutine);
                _applyForcesCoroutine = null;
            }
            foreach (var forceData in forces)
            {
                if (forceData.IsValid())
                {
                    ApplyForceOverTime(forceData, elapsedTime);
                }
            }
        }

        void ApplyForceOverTime(ForceData forceData, float elapsedTime)
        {
            // Wait for start delay
            if (elapsedTime < forceData.startDelay) return;

            float currentDuration = Mathf.Max(0, forceData.duration);

            if (currentDuration <= 0f)
            {
                if (forceData.applied) return;
                forceData.applied = true;
            }
            else
            {
                // Check if duration has ended
                if (elapsedTime > forceData.startDelay + currentDuration) return;
            }

            float adjustedTime = elapsedTime - forceData.startDelay;
            var normalizedTime = currentDuration > 0 ? adjustedTime / currentDuration : 0;
            var forceMult = currentDuration > 0 ? forceData.forceCurve.Evaluate(normalizedTime) : 1;
            var torqueMult = currentDuration > 0 ? forceData.torqueCurve.Evaluate(normalizedTime) : 1;

            var currentForce = forceData.force * forceMult;
            var currentTorque = forceData.torque * torqueMult;
            
            // Transform force and torque to world space if using local space
            if (forceData.useLocalForce)
            {
                currentForce = transform.TransformDirection(currentForce);
            }
            
            if (forceData.useLocalTorque)
            {
                currentTorque = transform.TransformDirection(currentTorque);
            }
                
            // Apply force based on rigidbody type
            if (_rb3D)
            {
                _rb3D.AddForce(currentForce, forceData.forceMode);
                _rb3D.AddTorque(currentTorque, forceData.forceMode);
            }
            else if (_rb2D)
            {
                _rb2D.AddForce(currentForce, forceData.forceMode2D);
                _rb2D.AddTorque(currentTorque.z, forceData.forceMode2D);
            }
        }
        
        /// <summary>
        /// Checks if this GameObject has a 3D Rigidbody component.
        /// </summary>
        /// <returns>True if a 3D Rigidbody is attached.</returns>
        public bool Has3DRigidbody()
        {
            return _rb3D != null;
        }
        
        /// <summary>
        /// Checks if this GameObject has a 2D Rigidbody component.
        /// </summary>
        /// <returns>True if a 2D Rigidbody is attached.</returns>
        public bool Has2DRigidbody()
        {
            return _rb2D != null;
        }
        
        IEnumerator ApplyForcesCoroutine()
        {
            var fixedUpdate = new WaitForFixedUpdate();
            var totalDuration = forces.Length > 0 ? forces.Max(x => x.startDelay + Mathf.Max(0, x.duration)) : 0f;
            float f = 0f;
            while (f <= totalDuration)
            {
                ApplyForces(f);
                yield return fixedUpdate;
                f += Time.fixedDeltaTime;
            }
            ApplyForces(totalDuration);
            _applyForcesCoroutine = null;
        }
    }
}
