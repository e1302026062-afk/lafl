using UnityEngine;

public sealed class B11ThirdPersonCameraCollision : MonoBehaviour
{
    [SerializeField] private Transform targetPivot;
    [SerializeField] private Transform ignoreRoot;
    [SerializeField] private float surfacePadding = 0.25f;
    [SerializeField] private LayerMask collisionMask = Physics.DefaultRaycastLayers;

    private Vector3 defaultLocalPosition;
    private Transform targetRoot;

    private void Awake()
    {
        defaultLocalPosition = transform.localPosition;
        targetRoot = ignoreRoot != null ? ignoreRoot.root : (targetPivot != null ? targetPivot.root : transform.root);
    }

    private void LateUpdate()
    {
        if (targetPivot == null) return;

        Vector3 origin = targetPivot.position;
        Vector3 desiredPosition = targetPivot.TransformPoint(defaultLocalPosition);
        Vector3 offset = desiredPosition - origin;
        float distance = offset.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            transform.position = desiredPosition;
            return;
        }

        Vector3 direction = offset / distance;
        float nearestHitDistance = distance;
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, collisionMask, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.transform.IsChildOf(targetRoot)) continue;
            nearestHitDistance = Mathf.Min(nearestHitDistance, hit.distance - surfacePadding);
        }

        transform.position = origin + direction * Mathf.Max(0.1f, nearestHitDistance);
    }
}
