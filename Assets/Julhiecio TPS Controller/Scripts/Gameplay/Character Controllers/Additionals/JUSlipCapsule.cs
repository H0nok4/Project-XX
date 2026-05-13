using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace JUTPS.PhysicsScripts
{

    [AddComponentMenu("JU TPS/Third Person System/Additionals/Slip Capsule")]
    public class JUSlipCapsule : MonoBehaviour
    {
        [SerializeField] private Vector3 Center = new Vector3(0, 0.85f, 0);
        [SerializeField] private float Radius = 0.5f;
        [SerializeField] private float Height = 1.25f;

        private CapsuleCollider defaultCapsuleCollider;
        private CapsuleCollider slipCapsule;
        void Awake()
        {
            defaultCapsuleCollider = GetComponent<CapsuleCollider>();

            GenerateSlipCapsuleCollider(gameObject, Center, Radius, Height, out slipCapsule);
        }
        void Update()
        {
            if (defaultCapsuleCollider == null || slipCapsule == null) return;
            slipCapsule.isTrigger = defaultCapsuleCollider.isTrigger;
            slipCapsule.enabled = defaultCapsuleCollider.enabled;
            slipCapsule.center = Center;
            slipCapsule.radius = Radius;
            slipCapsule.height = Height;
        }
        public static void GenerateSlipCapsuleCollider(GameObject target, Vector3 Center, float Radius, float Height, out CapsuleCollider outCapsuleCollider)
        {
            CapsuleCollider cap = target.AddComponent<CapsuleCollider>();
            cap.center = Center;
            cap.radius = Radius;
            cap.height = Height;
            cap.material = (PhysicsMaterial)Resources.Load("Slip");
            outCapsuleCollider = cap;
        }

    }

}
