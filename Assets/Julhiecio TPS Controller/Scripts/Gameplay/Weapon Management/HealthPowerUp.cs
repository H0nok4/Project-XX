using UnityEngine;
using JUTPS;

namespace JUTPS.PowerUps
{

    [RequireComponent(typeof(BoxCollider))]
    [AddComponentMenu("JU TPS/Weapon System/Health Power Up")]
    public class HealthPowerUp : MonoBehaviour
    {
        [Header("Health")]
        public float HealthToAdd = 30;
        public GameObject Effect;

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                var juHealth = other.GetComponent<JUHealth>();
                if (juHealth != null)
                {
                    if (!juHealth.TryApplyHealing(HealthToAdd, true)) return;

                    GameObject fx = Instantiate(Effect, transform.position, transform.rotation);
                    Destroy(fx, 5);
                    Destroy(this.gameObject, 0.1f);
                }
            }
        }
    }

}
