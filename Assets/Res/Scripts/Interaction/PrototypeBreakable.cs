using UnityEngine;

public enum PrototypeBreakableMaterialType
{
    Glass = 0,
    ThinWood = 1
}

[DisallowMultipleComponent]
public class PrototypeBreakable : MonoBehaviour
{
    [SerializeField] private string breakableLabel = "Breakable";
    [SerializeField] private PrototypeBreakableMaterialType materialType = PrototypeBreakableMaterialType.Glass;
    [Min(1f)]
    [SerializeField] private float durability = 18f;
    [SerializeField] private bool disableObjectOnBreak;
    [SerializeField] private Collider[] colliders;
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Rigidbody attachedRigidbody;

    private float currentDurability;
    private bool isBroken;

    public string BreakableLabel => string.IsNullOrWhiteSpace(breakableLabel) ? name : breakableLabel.Trim();
    public bool IsBroken => isBroken;

    private void Awake()
    {
        ResolveReferences();
        currentDurability = Mathf.Max(1f, durability);
    }

    private void OnValidate()
    {
        durability = Mathf.Max(1f, durability);
        ResolveReferences();
    }

    public void Configure(PrototypeBreakableMaterialType type, float maxDurability)
    {
        materialType = type;
        durability = Mathf.Max(1f, maxDurability);
        currentDurability = durability;
        ResolveReferences();
    }

    public void ApplyDamage(PrototypeUnitVitals.DamageInfo damageInfo, Vector3 impactPoint, Vector3 impactDirection, float impactForce)
    {
        if (isBroken)
        {
            return;
        }

        float incomingDamage = Mathf.Max(0f, damageInfo.damage);
        if (incomingDamage <= 0f)
        {
            return;
        }

        float materialMultiplier = materialType == PrototypeBreakableMaterialType.Glass ? 1.35f : 0.92f;
        currentDurability = Mathf.Max(0f, currentDurability - incomingDamage * materialMultiplier);

        if (attachedRigidbody != null && impactForce > 0f)
        {
            attachedRigidbody.AddForceAtPosition(impactDirection.normalized * impactForce, impactPoint, ForceMode.Impulse);
        }

        if (currentDurability <= 0f)
        {
            Break();
        }
    }

    public void Break()
    {
        if (isBroken)
        {
            return;
        }

        isBroken = true;
        PrototypeCombatNoiseSystem.ReportNoise(transform.position, materialType == PrototypeBreakableMaterialType.Glass ? 12f : 7f, gameObject);

        if (colliders != null)
        {
            for (int index = 0; index < colliders.Length; index++)
            {
                if (colliders[index] != null)
                {
                    colliders[index].enabled = false;
                }
            }
        }

        if (renderers != null)
        {
            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] != null)
                {
                    renderers[index].enabled = false;
                }
            }
        }

        if (disableObjectOnBreak)
        {
            gameObject.SetActive(false);
        }
    }

    private void ResolveReferences()
    {
        if (colliders == null || colliders.Length == 0)
        {
            colliders = GetComponentsInChildren<Collider>(true);
        }

        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        if (attachedRigidbody == null)
        {
            attachedRigidbody = GetComponent<Rigidbody>();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = materialType == PrototypeBreakableMaterialType.Glass
            ? new Color(0.45f, 0.85f, 1f, 0.35f)
            : new Color(0.82f, 0.58f, 0.21f, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}
