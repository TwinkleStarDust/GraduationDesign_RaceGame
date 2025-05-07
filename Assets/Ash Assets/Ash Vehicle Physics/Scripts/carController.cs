using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AshVP
{
    public class carController : MonoBehaviour
    {
        #region Variables

        [Header("Suspension")]
        [Range(0, 5)] public float SuspensionDistance = 0.2f;
        public float suspensionForce = 30000f;
        public float suspensionDamper = 200f;
        public Transform groundCheck;
        public Transform fricAt;
        public Transform CenterOfMass;

        private Rigidbody rb;

        [Header("Car Stats")]
        public float accelerationForce = 200f;
        public float turnTorque = 100f;
        public float brakeForce = 150f;
        public float frictionForce = 70f;
        public float dragAmount = 4f;
        public float TurnAngle = 30f;

        public float maxRayLength = 0.8f, slerpTime = 0.2f;
        [HideInInspector] public bool grounded;

        [Header("Visuals")]
        public Transform[] TireMeshes;
        public Transform[] TurnTires;

        [Header("Curves")]
        public AnimationCurve frictionCurve;
        public AnimationCurve accelerationCurve;
        public bool separateReverseCurve = false;
        public AnimationCurve ReverseCurve;
        public AnimationCurve turnCurve;
        public AnimationCurve driftCurve;
        public AnimationCurve engineCurve;

        private float speedValue, fricValue, turnValue, curveVelocity, brakeValue;
        private float accelerationInput, steerInput, brakeInput;
        [HideInInspector] public Vector3 carVelocity;
        [HideInInspector] public RaycastHit hit;

        [Header("Other Settings")]
        public AudioSource[] engineSounds;
        public bool airDrag;
        public float SkidEnable = 20f;
        public float skidWidth = 0.12f;
        private float frictionAngle;

        [HideInInspector] public Vector3 normalDir;

        private float VehicleGravity = -30;
        private Vector3 centerOfMass_ground;
        private float raycast_boxWidth, raycast_boxLength;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            InitializeCar();
        }


        private void FixedUpdate()
        {
            UpdateCarVelocity();
            HandleInputs();
            GroundCheck();

            if (grounded)
            {
                ApplyGroundedPhysics();
            }
            else
            {
                ApplyAirPhysics();
            }
        }

        private void Update()
        {
            UpdateTireVisuals();
            UpdateAudio();
        }

        #endregion

        #region Initialization Methods

        private void InitializeCar()
        {
            rb = GetComponent<Rigidbody>();
            grounded = false;
            engineSounds[1].mute = true;
            rb.centerOfMass = CenterOfMass.localPosition;

            CalculateCenterOfMass();
            SetVehicleGravity();
            CalculateRaycastBoxSize();
        }

        private void CalculateCenterOfMass()
        {
            Vector3 centerOfMass_ground_temp = Vector3.zero;
            for (int i = 0; i < TireMeshes.Length; i++)
            {
                centerOfMass_ground_temp += TireMeshes[i].parent.parent.localPosition;
            }

            centerOfMass_ground_temp.y = 0;
            centerOfMass_ground = TireMeshes.Length < 3 ? centerOfMass_ground_temp / 2 : centerOfMass_ground_temp / 4;
        }

        private void SetVehicleGravity()
        {
            if (GetComponent<GravityCustom>())
            {
                VehicleGravity = GetComponent<GravityCustom>().gravity;
            }
            else
            {
                VehicleGravity = Physics.gravity.y;
            }
        }

        private void CalculateRaycastBoxSize()
        {
            if (TireMeshes.Length < 3)
            {
                raycast_boxLength = Vector3.Distance(TireMeshes[0].position, TireMeshes[1].position);
                raycast_boxWidth = 0.1f;
            }
            else
            {
                raycast_boxLength = Vector3.Distance(TireMeshes[0].position, TireMeshes[2].position);
                raycast_boxWidth = Vector3.Distance(TireMeshes[0].position, TireMeshes[1].position);
            }
        }

        #endregion

        #region Physics Handling

        private void UpdateCarVelocity()
        {
            carVelocity = transform.InverseTransformDirection(rb.linearVelocity);
            curveVelocity = Mathf.Abs(carVelocity.magnitude) / 100;
        }


        private void ApplyGroundedPhysics()
        {
            AccelerationLogic();
            TurningLogic();
            FrictionLogic();
            BrakeLogic();
            ApplyDragAndCenterOfMass();
        }

        private void ApplyAirPhysics()
        {
            rb.linearDamping = 0.1f;
            rb.centerOfMass = CenterOfMass.localPosition;
            if (!airDrag)
            {
                rb.angularDamping = 0.1f;
            }
        }

        private void ApplyDragAndCenterOfMass()
        {
            rb.angularDamping = dragAmount * driftCurve.Evaluate(Mathf.Abs(carVelocity.x) / 70);
            normalDir = hit.normal;

            if (Vector3.Angle(transform.up, normalDir) < 45f)
            {
                rb.centerOfMass = centerOfMass_ground;
            }
            else
            {
                rb.centerOfMass = CenterOfMass.localPosition;
            }

            Debug.DrawLine(groundCheck.position, hit.point, Color.green);
        }

        public void GroundCheck()
        {
            int wheelsLayerMask = 1 << LayerMask.NameToLayer("wheels");
            int layerMask = ~wheelsLayerMask;
            float rayMultiplier = 1 / Mathf.Clamp(Mathf.Cos(transform.rotation.eulerAngles.z), 0.8f, 1);
            Vector3 boxSize = new Vector3(raycast_boxWidth / 2, 0.01f, raycast_boxLength / 2);

            if (Physics.BoxCast(groundCheck.position, boxSize, -transform.up, out hit, transform.rotation, maxRayLength * rayMultiplier, layerMask))
            {
                grounded = true;
                DrawDebugBox(groundCheck.position - transform.up * (hit.distance - 0.5f * boxSize.y), 2 * boxSize, Color.blue);
            }
            else
            {
                grounded = false;
                DrawDebugBox(groundCheck.position - transform.up * maxRayLength * rayMultiplier, 2 * boxSize, Color.red);
            }
        }

        #endregion

        #region Input Handling

        public void ProvideInputs(float _accelerationInput, float _steerInput, float _brakeInput)
        {
            accelerationInput = Mathf.Clamp(_accelerationInput, -1, 1);
            steerInput = Mathf.Clamp(_steerInput, -1, 1);
            brakeInput = Mathf.Clamp(_brakeInput, 0, 1);
        }

        private void HandleInputs()
        {
            brakeValue = brakeForce * brakeInput * Time.fixedDeltaTime * 1000;

            speedValue = accelerationForce * accelerationInput * Time.fixedDeltaTime * 1000 * accelerationCurve.Evaluate(Mathf.Abs(carVelocity.z) / 100);
            if (separateReverseCurve && carVelocity.z < 0 && accelerationInput < 0)
            {
                speedValue = accelerationForce * accelerationInput * Time.fixedDeltaTime * 1000 * ReverseCurve.Evaluate(Mathf.Abs(carVelocity.z) / 100);
            }
            turnValue = turnTorque * steerInput * Time.fixedDeltaTime * 1000 * turnCurve.Evaluate(carVelocity.magnitude / 100);
        }

        #endregion

        #region Car Physics Logic

        public void AccelerationLogic()
        {
            if (accelerationInput != 0)
            {
                rb.AddForceAtPosition(transform.forward * speedValue, groundCheck.position);
            }
        }

        public void TurningLogic()
        {
            if (carVelocity.z > 0.1f || accelerationInput > 0.1f)
            {
                rb.AddTorque(transform.up * turnValue);
            }
            else if (carVelocity.z < -0.1f)
            {
                rb.AddTorque(transform.up * -turnValue);
            }
        }

        public void FrictionLogic()
        {
            fricValue = frictionForce * frictionCurve.Evaluate(carVelocity.magnitude / 100);
            Vector3 sideVelocity = carVelocity.x * transform.right;
            Vector3 contactDesiredAccel = -sideVelocity / Time.fixedDeltaTime;
            float clampedFrictionForce = rb.mass * contactDesiredAccel.magnitude;
            Vector3 gravityForce = VehicleGravity * rb.mass * Vector3.up;
            Vector3 gravityFriction = -Vector3.Project(gravityForce, transform.right);
            Vector3 maxfrictionForce = Vector3.ClampMagnitude(fricValue * 50 * (-sideVelocity.normalized), clampedFrictionForce);

            rb.AddForceAtPosition(maxfrictionForce + gravityFriction, fricAt.position);
        }

        public void BrakeLogic()
        {
            Vector3 forwardVelocity = carVelocity.z * transform.forward;
            Vector3 DesiredAccel = -forwardVelocity / Time.fixedDeltaTime;
            float clampedBrakeForce = rb.mass * DesiredAccel.magnitude;
            Vector3 maxBrakeForce = Vector3.ClampMagnitude(brakeValue * (-forwardVelocity.normalized), clampedBrakeForce);

            rb.AddForceAtPosition(maxBrakeForce, groundCheck.position);

            float brakeSlideAngle = 30f;
            float currentSlideAngle = Vector3.Angle(Vector3.up, transform.up);
            Vector3 gravitySideForce = Vector3.ProjectOnPlane(VehicleGravity * rb.mass * Vector3.up, transform.up);

            if (carVelocity.magnitude < 1 && brakeValue > 0 && currentSlideAngle < brakeSlideAngle)
            {
                rb.AddForce(-gravitySideForce);
            }

            rb.linearDamping = carVelocity.magnitude < 1 ? 5f : 0.1f;
        }

        #endregion

        #region Tire Visuals & Audio Control

        public void UpdateTireVisuals()
        {
            foreach (Transform mesh in TireMeshes)
            {
                mesh.transform.RotateAround(mesh.transform.position, mesh.transform.right, carVelocity.z / 3);
                mesh.transform.localPosition = Vector3.zero;
            }

            foreach (Transform FM in TurnTires)
            {
                FM.localRotation = Quaternion.Slerp(FM.localRotation,
                    Quaternion.Euler(FM.localRotation.eulerAngles.x, TurnAngle * steerInput, FM.localRotation.eulerAngles.z), slerpTime);
            }
        }

        public void UpdateAudio()
        {
            if (grounded)
            {
                engineSounds[1].mute = Mathf.Abs(carVelocity.x) > SkidEnable - 0.1f ? false : true;
            }
            else
            {
                engineSounds[1].mute = true;
            }

            engineSounds[1].pitch = 1f;
            engineSounds[0].pitch = 2 * engineCurve.Evaluate(curveVelocity);
            if (engineSounds.Length > 2)
            {
                engineSounds[2].pitch = 2 * engineCurve.Evaluate(curveVelocity);
            }
        }

        #endregion

        #region Debug Drawing

        private void DrawDebugBox(Vector3 center, Vector3 size, Color color)
        {
            Vector3 halfSize = size * 0.5f;

            Vector3[] vertices = new Vector3[]
            {
                center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
                center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z),
                center + new Vector3(halfSize.x, -halfSize.y, halfSize.z),
                center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z),
                center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z),
                center + new Vector3(-halfSize.x, halfSize.y, halfSize.z),
                center + new Vector3(halfSize.x, halfSize.y, halfSize.z),
                center + new Vector3(halfSize.x, halfSize.y, -halfSize.z)
            };

            // Draw the bottom lines of the box
            Debug.DrawLine(vertices[0], vertices[1], color);
            Debug.DrawLine(vertices[1], vertices[2], color);
            Debug.DrawLine(vertices[2], vertices[3], color);
            Debug.DrawLine(vertices[3], vertices[0], color);

            // Draw the top lines of the box
            Debug.DrawLine(vertices[4], vertices[5], color);
            Debug.DrawLine(vertices[5], vertices[6], color);
            Debug.DrawLine(vertices[6], vertices[7], color);
            Debug.DrawLine(vertices[7], vertices[4], color);

            // Draw the connecting lines between top and bottom
            Debug.DrawLine(vertices[0], vertices[4], color);
            Debug.DrawLine(vertices[1], vertices[5], color);
            Debug.DrawLine(vertices[2], vertices[6], color);
            Debug.DrawLine(vertices[3], vertices[7], color);
        }

        #endregion

        #region Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.DrawSphere(transform.position + centerOfMass_ground, 0.05f);

            if (!Application.isPlaying)
            {
                DrawGroundCheckGizmos();
                DrawColliderGizmos();
                DrawSuspensionGizmos();
                UpdateRayLength();
            }
        }

        private void DrawGroundCheckGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position - maxRayLength * groundCheck.up);
            Gizmos.DrawWireCube(groundCheck.position - maxRayLength * groundCheck.up, new Vector3(5, 0.02f, 10));
        }

        private void DrawColliderGizmos()
        {
            Gizmos.color = Color.magenta;
            if (GetComponent<BoxCollider>())
            {
                Gizmos.DrawWireCube(GetComponent<BoxCollider>().bounds.center, GetComponent<BoxCollider>().size);
            }
            else if (GetComponent<CapsuleCollider>())
            {
                Gizmos.DrawWireCube(GetComponent<CapsuleCollider>().bounds.center, GetComponent<CapsuleCollider>().bounds.size);
            }
        }

        private void DrawSuspensionGizmos()
        {
            Gizmos.color = Color.red;
            foreach (Transform mesh in TireMeshes)
            {
                ConfigurableJoint joint = mesh.parent.parent.GetComponent<ConfigurableJoint>();

                var ydrive = joint.yDrive;
                ydrive.positionDamper = suspensionDamper;
                ydrive.positionSpring = suspensionForce;
                joint.yDrive = ydrive;

                var jointLimit = joint.linearLimit;
                jointLimit.limit = SuspensionDistance;
                joint.linearLimit = jointLimit;

                Handles.ArrowHandleCap(0, mesh.position, mesh.rotation * Quaternion.LookRotation(Vector3.up), jointLimit.limit, EventType.Repaint);
                Handles.ArrowHandleCap(0, mesh.position, mesh.rotation * Quaternion.LookRotation(Vector3.down), jointLimit.limit, EventType.Repaint);
            }
        }

        private void UpdateRayLength()
        {
            float wheelRadius = TurnTires[0].parent.GetComponent<SphereCollider>().radius;
            float wheelYPosition = TurnTires[0].parent.parent.localPosition.y + TurnTires[0].parent.localPosition.y;
            maxRayLength = (groundCheck.localPosition.y - wheelYPosition + (0.05f + wheelRadius));
        }
#endif

        #endregion
    }
}
