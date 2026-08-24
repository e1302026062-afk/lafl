using UnityEngine;

public sealed class B11CameraRigFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float height = 1.33f;

    public void Configure(Transform followTarget, float followHeight)
    {
        target = followTarget;
        height = followHeight;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.position + Vector3.up * height;
    }
}
