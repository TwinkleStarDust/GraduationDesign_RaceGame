using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AshVP
{
    public class AshSuspension : MonoBehaviour
    {
        float suspensionRestDist, wheelRadius;
        public float springStrength = 40000f, springDamper = 2000f;
        public bool rayDidHit;
        private RaycastHit hit;
        public Rigidbody carRigidBody;
        public Transform wheel;
        //private float wheelRestDistance;
        private float maxWheelTravel = 0.3f;

        void Start()
        {
            maxWheelTravel = carRigidBody.GetComponent<carController>().SuspensionDistance;

            transform.position = wheel.position;
            Vector3 pos = transform.localPosition;
            pos.y = 0;
            transform.localPosition = pos;
            suspensionRestDist = Vector3.Distance(transform.position, wheel.position);

            //wheelRestDistance = Vector3.Distance(transform.position, wheel.position);

            if (wheel.GetComponent<ConfigurableJoint>())
            {
                Destroy(wheel.GetComponent<ConfigurableJoint>());
            }
            if (wheel.GetComponent<SphereCollider>())
            {
                wheelRadius = wheel.GetComponent<SphereCollider>().radius;
                Destroy(wheel.GetComponent<SphereCollider>());
            }
            if (wheel.GetComponent<Rigidbody>())
            {
                Destroy(wheel.GetComponent<Rigidbody>());
            }

            rayDidHit = false;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (grounded())
            {
                rayDidHit = true;
            }
            else
            {
                rayDidHit = false;
            }
            SuspensionLogic();
        }

        public void SuspensionLogic()
        {
            // suspension spring force
            if (rayDidHit)
            {
                Vector3 springDir = transform.up;
                Vector3 tireWorldVel = carRigidBody.GetPointVelocity(transform.position);

                float offset = suspensionRestDist - hit.distance;

                float suspensionVel = Vector3.Dot(springDir, tireWorldVel);
                float desiredvelChange = -suspensionVel;
                float desiredAccel = desiredvelChange / Time.fixedDeltaTime;

                float vel = Vector3.Dot(springDir, tireWorldVel);
                float force = (offset * springStrength) - (vel * springDamper);
                carRigidBody.AddForceAtPosition(springDir * force, transform.position);

                float maxForce = Mathf.Clamp(carRigidBody.mass * 0.25f * desiredAccel, 0, carRigidBody.mass * 0.25f * desiredAccel);
                float unCompressForce = Mathf.Clamp(maxForce - force, 0, maxForce - force);

                if(offset > maxWheelTravel)
                {
                    carRigidBody.AddForceAtPosition(springDir * unCompressForce, transform.position);
                }
            }

            if (rayDidHit)
            {
                wheel.position = transform.position - transform.up * (hit.distance);
            }
            else
            {
                wheel.position = Vector3.Lerp(wheel.position, transform.position - transform.up * suspensionRestDist, 0.1f);
            }

        }

        public bool grounded() //checks for if vehicle is grounded or not
        {
            var direction = -transform.up;
            if (Physics.SphereCast(transform.position, wheelRadius, direction, out hit, suspensionRestDist))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                if (rayDidHit)
                {
                    Gizmos.DrawWireSphere(transform.position - transform.up * (hit.distance), wheelRadius);
                    Gizmos.DrawLine(transform.position, hit.point);
                }
                else
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, wheel.position);
                    Gizmos.DrawWireSphere(wheel.position, wheelRadius);
                }
            }

            //Gizmos.color = Color.red;
            //Vector3 uncompressPos = transform.position - (transform.up * (suspensionRestDist - maxWheelTravel));
            //Gizmos.DrawLine(transform.position, uncompressPos);

        }

    }
}
