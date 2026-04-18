using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private float smoothSpeed    = 3f;
    [SerializeField] private float yOffset        = 1f;   // units above the top block

    private float targetY;
    private float startY;

    void Start()
    {
        startY  = transform.position.y;
        targetY = startY;
    }

    void LateUpdate()
{
    if (TowerManager.Instance == null) return;

    float desiredY = TowerManager.Instance.TopY + yOffset;

    targetY = Mathf.Max(targetY, desiredY);

    float smoothedY = Mathf.Lerp(transform.position.y, targetY, smoothSpeed * Time.deltaTime);

    transform.position = new Vector3(transform.position.x, smoothedY, transform.position.z);
}

    public void ResetCamera()
    {
        targetY = startY;
        transform.position = new Vector3(transform.position.x, startY, transform.position.z);
    }
}
