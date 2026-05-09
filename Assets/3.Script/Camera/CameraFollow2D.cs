using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = new(0f, 1.2f);
    [SerializeField] private Vector2 minPosition = new(-13.5f, -2f);
    [SerializeField] private Vector2 maxPosition = new(13.5f, 4f);
    [SerializeField] private float smoothTime = 0.12f;

    private Vector3 velocity;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desired = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
        desired.x = Mathf.Clamp(desired.x, minPosition.x, maxPosition.x);
        desired.y = Mathf.Clamp(desired.y, minPosition.y, maxPosition.y);

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
