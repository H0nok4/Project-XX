using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAimPointResolver : MonoBehaviour
{
    public readonly struct AimPointSnapshot
    {
        public readonly Camera SourceCamera;
        public readonly Vector3 RayOrigin;
        public readonly Vector3 RayDirection;
        public readonly Vector3 AimWorldPoint;
        public readonly bool HasHit;
        public readonly RaycastHit Hit;

        public AimPointSnapshot(
            Camera sourceCamera,
            Vector3 rayOrigin,
            Vector3 rayDirection,
            Vector3 aimWorldPoint,
            bool hasHit,
            RaycastHit hit)
        {
            SourceCamera = sourceCamera;
            RayOrigin = rayOrigin;
            RayDirection = rayDirection;
            AimWorldPoint = aimWorldPoint;
            HasHit = hasHit;
            Hit = hit;
        }

        public Ray AimRay => new Ray(RayOrigin, RayDirection);
    }

    [Header("References")]
    [SerializeField] private PlayerAnimationRigRefs rigRefs;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Camera renderCamera;

    [Header("Query")]
    [SerializeField] private float maxAimDistance = 120f;
    [SerializeField] private LayerMask aimQueryMask = Physics.DefaultRaycastLayers;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    private int lastResolvedFrame = -1;
    private AimPointSnapshot currentSnapshot;

    public Vector3 CurrentAimWorldPoint => GetAimSnapshot().AimWorldPoint;
    public Vector3 CurrentAimDirection => GetAimSnapshot().RayDirection;
    public Vector3 CurrentRayOrigin => GetAimSnapshot().RayOrigin;
    public bool HasAimHit => GetAimSnapshot().HasHit;
    public Ray CurrentAimRay => GetAimSnapshot().AimRay;
    public Camera ActiveAimCamera => GetAimSnapshot().SourceCamera;

    private void Awake()
    {
        ResolveReferences();
        ClampSettings();
    }

    private void OnValidate()
    {
        ResolveReferences();
        ClampSettings();
        lastResolvedFrame = -1;
    }

    public AimPointSnapshot GetAimSnapshot()
    {
        ResolveAimSnapshot();
        return currentSnapshot;
    }

    public Vector3 GetDirectionFrom(Vector3 origin)
    {
        AimPointSnapshot snapshot = GetAimSnapshot();
        Vector3 direction = snapshot.AimWorldPoint - origin;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return snapshot.RayDirection.sqrMagnitude > 0.0001f
                ? snapshot.RayDirection
                : transform.forward;
        }

        return direction.normalized;
    }

    public float GetDistanceFrom(Vector3 origin, float fallbackDistance)
    {
        AimPointSnapshot snapshot = GetAimSnapshot();
        float maxDistance = Mathf.Max(0.01f, fallbackDistance);
        if (!snapshot.HasHit)
        {
            return maxDistance;
        }

        return Mathf.Min(maxDistance, Vector3.Distance(origin, snapshot.AimWorldPoint));
    }

    private void ResolveAimSnapshot()
    {
        if (lastResolvedFrame == Time.frameCount)
        {
            return;
        }

        ResolveReferences();

        Camera sourceCamera = ResolveAimCamera();
        Transform sourceTransform = sourceCamera != null ? sourceCamera.transform : transform;
        Vector3 rayOrigin = sourceTransform.position;
        Vector3 rayDirection = sourceTransform.forward;
        if (rayDirection.sqrMagnitude <= 0.0001f)
        {
            rayDirection = transform.forward;
        }

        rayDirection.Normalize();

        RaycastHit hit;
        bool hasHit = TryGetAimHit(rayOrigin, rayDirection, out hit);
        Vector3 aimWorldPoint = hasHit
            ? hit.point
            : rayOrigin + rayDirection * maxAimDistance;

        currentSnapshot = new AimPointSnapshot(sourceCamera, rayOrigin, rayDirection, aimWorldPoint, hasHit, hit);
        lastResolvedFrame = Time.frameCount;
    }

    private bool TryGetAimHit(Vector3 origin, Vector3 direction, out RaycastHit hit)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxAimDistance, aimQueryMask, triggerInteraction);
        if (hits == null || hits.Length == 0)
        {
            hit = default;
            return false;
        }

        Array.Sort(hits, CompareHitDistance);
        foreach (RaycastHit candidate in hits)
        {
            if (!IsValidAimHit(candidate))
            {
                continue;
            }

            hit = candidate;
            return true;
        }

        hit = default;
        return false;
    }

    private bool IsValidAimHit(RaycastHit candidate)
    {
        if (candidate.collider == null)
        {
            return false;
        }

        Transform hitTransform = candidate.collider.transform;
        if (hitTransform == transform || hitTransform.IsChildOf(transform))
        {
            return false;
        }

        if (!candidate.collider.isTrigger)
        {
            return true;
        }

        return HasAimRelevantTriggerTarget(candidate.collider);
    }

    private static bool HasAimRelevantTriggerTarget(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return false;
        }

        if (hitCollider.GetComponent<PrototypeUnitHitbox>() != null
            || hitCollider.GetComponentInParent<PrototypeUnitHitbox>() != null
            || hitCollider.GetComponent<PrototypeBreakable>() != null
            || hitCollider.GetComponentInParent<PrototypeBreakable>() != null)
        {
            return true;
        }

        MonoBehaviour[] localComponents = hitCollider.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in localComponents)
        {
            if (component is IInteractable)
            {
                return true;
            }
        }

        MonoBehaviour[] parentComponents = hitCollider.GetComponentsInParent<MonoBehaviour>(true);
        foreach (MonoBehaviour component in parentComponents)
        {
            if (component is IInteractable)
            {
                return true;
            }
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (rigRefs == null)
        {
            rigRefs = GetComponent<PlayerAnimationRigRefs>();
        }

        if (gameplayCamera == null)
        {
            gameplayCamera = rigRefs != null ? rigRefs.ViewCamera : GetComponentInChildren<Camera>();
        }

        if (renderCamera == null)
        {
            renderCamera = rigRefs != null ? rigRefs.RenderCamera : null;
        }
    }

    private void ClampSettings()
    {
        maxAimDistance = Mathf.Max(1f, maxAimDistance);
        if (aimQueryMask.value == 0 || aimQueryMask.value == ~0)
        {
            aimQueryMask = Physics.DefaultRaycastLayers;
        }
    }

    private Camera ResolveAimCamera()
    {
        if (renderCamera != null && renderCamera.gameObject.activeInHierarchy && renderCamera.enabled)
        {
            return renderCamera;
        }

        if (gameplayCamera != null)
        {
            return gameplayCamera;
        }

        return renderCamera;
    }

    private static int CompareHitDistance(RaycastHit left, RaycastHit right)
    {
        return left.distance.CompareTo(right.distance);
    }
}
