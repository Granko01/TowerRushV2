using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundScroller : MonoBehaviour
{
    [Tooltip("How fast the image pans to reveal sky as camera rises. 0 = no pan, 1 = full pan per camera unit.")]
    [SerializeField] private float panSpeed = 0.5f;

    private Camera mainCam;
    private SpriteRenderer sr;
    private float startCamY;
    private float maxLocalY;

    void Start()
    {
        mainCam = Camera.main;
        sr = GetComponent<SpriteRenderer>();
        sr.sortingOrder = -100;

        transform.SetParent(mainCam.transform);
        transform.localRotation = Quaternion.identity;

        float screenW = mainCam.orthographicSize * 2f * mainCam.aspect;
        float screenH = mainCam.orthographicSize * 2f;
        float spriteW = sr.sprite.bounds.size.x;
        float spriteH = sr.sprite.bounds.size.y;

        // Fill screen completely — no black bars
        float scale = Mathf.Max(screenW / spriteW, screenH / spriteH);
        transform.localScale = new Vector3(scale, scale, 1f);

        // How many local units we can shift before hitting the edge of the sprite
        maxLocalY = Mathf.Max(0f, (spriteH * scale - screenH) * 0.5f);

        // Start centered — sky is already visible in the upper half
        transform.localPosition = new Vector3(0f, 0f, 15f);

        startCamY = mainCam.transform.position.y;
    }

    void LateUpdate()
    {
        float camDelta = mainCam.transform.position.y - startCamY;

        // Camera goes up → localY goes negative → sprite shifts down → more sky fills the top
        float localY = Mathf.Clamp(-camDelta * panSpeed, -maxLocalY, maxLocalY);
        transform.localPosition = new Vector3(0f, localY, 15f);
    }
}
