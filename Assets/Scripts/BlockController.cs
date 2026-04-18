using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BlockController : MonoBehaviour
{
    public enum BlockState { OnCrane, Falling, Placed }

    public BlockState State        { get; private set; } = BlockState.OnCrane;
    public float      CurrentWidth { get; private set; }

    [SerializeField] private float fallGravity  = -26f;
    [SerializeField] private float maxTiltDeg   = 15f;   // max visual tilt while swinging
    [SerializeField] private float tiltSmooth   = 6f;    // how fast tilt transitions

    private float fallVelocity = 0f;
    private float currentTilt  = 0f;
    private float prevHookX    = 0f;
    private bool  firstFrame   = true;

    // ─── Init ─────────────────────────────────────────────────────────────────

    public void Init(float width)
    {
        var sr = GetComponent<SpriteRenderer>();
        CurrentWidth = (sr != null && sr.sprite != null)
            ? sr.sprite.bounds.size.x * transform.localScale.x
            : width;
    }

    // ─── State transitions ────────────────────────────────────────────────────

    public void AttachToCrane()
    {
        State      = BlockState.OnCrane;
        currentTilt = 0f;
        firstFrame  = true;
    }

    public void Drop()
    {
        if (State != BlockState.OnCrane) return;
        State              = BlockState.Falling;
        fallVelocity       = 0f;
        transform.rotation = Quaternion.identity;
    }

    // ─── Update ───────────────────────────────────────────────────────────────

    void Update()
    {
        if (State == BlockState.OnCrane)
        {
            UpdateOnCrane();
            return;
        }

        if (State != BlockState.Falling) return;

        fallVelocity       += fallGravity * Time.deltaTime;
        transform.position += Vector3.up * fallVelocity * Time.deltaTime;

        float halfH = TowerManager.Instance.BlockHeight * 0.5f;
        float landY = TowerManager.Instance.TopY + halfH;
        if (transform.position.y <= landY)
        {
            transform.position = new Vector3(transform.position.x, landY, 0f);
            State = BlockState.Placed;
            TowerManager.Instance.OnBlockLanded(this);
        }
    }

    // ─── On Crane ─────────────────────────────────────────────────────────────

    void UpdateOnCrane()
    {
        Vector3 hookPos = CraneController.Instance.HookPosition;
        float   L       = CraneController.Instance.SlingLength + CurrentWidth * 0.5f;

        // Block hangs directly below hook
        transform.position = new Vector3(hookPos.x, hookPos.y - L, 0f);

        // Tilt based on hook horizontal speed — moving right → lean left
        if (firstFrame) { prevHookX = hookPos.x; firstFrame = false; }
        float hookSpeed  = (hookPos.x - prevHookX) / Time.deltaTime;
        prevHookX        = hookPos.x;

        float targetTilt = Mathf.Clamp(-hookSpeed * 0.9f, -maxTiltDeg, maxTiltDeg);
        currentTilt      = Mathf.Lerp(currentTilt, targetTilt, tiltSmooth * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, currentTilt);
    }
}
