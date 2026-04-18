using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(LineRenderer))]
public class CraneController : MonoBehaviour
{
    public static CraneController Instance { get; private set; }

    [Header("Position")]
    [SerializeField] private float restBelowTop = 0f;     // units below camera top at rest
    [SerializeField] private float animSpeed    = 3f;     // lerp speed (higher = faster)

    [Header("Rope")]
    [SerializeField] private float maxRopeLength = 4.5f;
    [SerializeField] private float slingLength   = 0.6f;  // vertical drop from hook to block top corners
    [SerializeField] private float hookGap       = 1.0f;  // extra space between hook bottom and block top

    public float SlingLength => slingLength + hookGap;

    [Header("Swing")]
    [SerializeField] private float maxXOffset     = 2.5f;
    [SerializeField] private float baseSwingSpeed = 1.4f;
    [SerializeField] private float speedPerFloor  = 0.07f;

    [Header("Visuals — assign in Inspector")]
    [SerializeField] private Sprite         gantrySprite;
    [SerializeField] private Sprite         cabSprite;
    [SerializeField] private Sprite         hookSprite;

    [SerializeField] private Transform hookAttachPoint;   // drag this to the tip of your hook sprite

    [HideInInspector] [SerializeField] private SpriteRenderer gantryRenderer;
    [HideInInspector] [SerializeField] private SpriteRenderer cabRenderer;
    [HideInInspector] [SerializeField] private SpriteRenderer hookRenderer;
    [HideInInspector] [SerializeField] private Transform      hookVisual;

    // Public
    public Vector3 HookPosition { get; private set; }

    // Components
    private LineRenderer rope;
    private LineRenderer leftSling;
    private LineRenderer rightSling;

    // State
    private BlockController activeBlock;
    private float           swingTime  = 0f;
    private bool            isEntering = false;
    private bool            isExiting  = false;
    private Vector3         animTo;
    private int             side       = 1;      // 1 or -1, alternates each round
    private System.Action   exitCallback;

    // ─── Awake ───────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        rope = GetComponent<LineRenderer>();
        ConfigureRope(rope);
        leftSling  = CreateSling("LeftSling");
        rightSling = CreateSling("RightSling");
        ApplySprites();
    }

    void ApplySprites()
    {
        ApplySprite(gantryRenderer, gantrySprite);
        ApplySprite(cabRenderer,    cabSprite);
        ApplySprite(hookRenderer,   hookSprite);
    }

    void ApplySprite(SpriteRenderer sr, Sprite sprite)
    {
        if (sr == null || sprite == null) return;
        sr.sprite = sprite;
        sr.color  = Color.white;                      // remove placeholder tint
        sr.transform.localScale = Vector3.one;        // use sprite's natural size
    }

    void ConfigureRope(LineRenderer lr)
    {
        lr.positionCount = 2;
        lr.startWidth    = lr.endWidth = 0.06f;
        lr.useWorldSpace = true;
        lr.sortingOrder  = 4;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.startColor    = lr.endColor = new Color(0.80f, 0.80f, 0.80f);
    }

    LineRenderer CreateSling(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        ConfigureRope(lr);
        lr.enabled = false;
        return lr;
    }

    // ─── Update ───────────────────────────────────────────────────────────────

    void Update()
    {
        float camTop   = Camera.main.transform.position.y + Camera.main.orthographicSize;
        float restY    = camTop - restBelowTop;
        float camHalfW = Camera.main.orthographicSize * Camera.main.aspect;
        float offX     = camHalfW + 3f;    // fully off screen horizontally

        // ── Entry / Exit: lerp toward target ─────────────────────────────────
        if (isEntering || isExiting)
        {
            transform.position = Vector3.Lerp(transform.position, animTo, animSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, animTo) < 0.08f)
            {
                transform.position = animTo;

                if (isExiting)
                {
                    isExiting    = false;
                    var cb       = exitCallback;
                    exitCallback = null;
                    cb?.Invoke();
                }
                else
                {
                    isEntering = false;
                }
            }
        }
        // ── Idle / Playing: crane slides left-right, hook hangs straight down ──
        else
        {
            bool playing = GameManager.Instance != null
                        && GameManager.Instance.State == GameManager.GameState.Playing;

            float craneX = 0f;
            if (playing)
            {
                float speed = baseSwingSpeed + GameManager.Instance.Floor * speedPerFloor;
                swingTime  += Time.deltaTime * speed;
                craneX      = maxXOffset * Mathf.Sin(swingTime);
            }
            transform.position = new Vector3(craneX, restY, 0f);
        }

        bool canSwing = !isEntering && !isExiting
                     && GameManager.Instance != null
                     && GameManager.Instance.State == GameManager.GameState.Playing;

        // ── Hook hangs straight down below crane ─────────────────────────────
        HookPosition = new Vector3(transform.position.x, transform.position.y - maxRopeLength, 0f);

        rope.SetPosition(0, transform.position);
        rope.SetPosition(1, HookPosition);

        if (hookVisual != null)
            hookVisual.position = HookPosition;

        // Sling ropes — block position is driven by pendulum physics in BlockController
        if (activeBlock != null && activeBlock.State == BlockController.BlockState.OnCrane)
        {
            // Slings attach at the actual rotated top-left / top-right corners of the block
            var blockSR = activeBlock.GetComponent<SpriteRenderer>();
            float lhw = blockSR != null && blockSR.sprite != null ? blockSR.sprite.bounds.extents.x : 0.5f;
            float lhh = blockSR != null && blockSR.sprite != null ? blockSR.sprite.bounds.extents.y : 0.5f;
            Vector3 topLeft  = activeBlock.transform.TransformPoint(new Vector3(-lhw,  lhh, 0f));
            Vector3 topRight = activeBlock.transform.TransformPoint(new Vector3( lhw,  lhh, 0f));

            Vector3 attachPt = hookAttachPoint != null ? hookAttachPoint.position : HookPosition;

            leftSling.enabled  = true;
            rightSling.enabled = true;
            leftSling.SetPosition(0,  attachPt);
            leftSling.SetPosition(1,  topLeft);
            rightSling.SetPosition(0, attachPt);
            rightSling.SetPosition(1, topRight);
        }
        else
        {
            leftSling.enabled  = false;
            rightSling.enabled = false;
        }

        // Drop input
        if (canSwing)
        {
            bool tapped = (Mouse.current       != null && Mouse.current.leftButton.wasPressedThisFrame)
                       || (Keyboard.current    != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                       || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
            if (tapped) DropBlock();
        }
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>Crane lerps from centre up to the top-side corner, then calls onComplete.</summary>
    public void TriggerExit(System.Action onComplete)
    {
        float camTop   = Camera.main.transform.position.y + Camera.main.orthographicSize;
        float restY    = camTop - restBelowTop;
        float camHalfW = Camera.main.orthographicSize * Camera.main.aspect;

        animTo       = new Vector3(side * (camHalfW + 3f), camTop + maxRopeLength + 1f, 0f);
        side         = -side;   // next entry comes from the other side
        isEntering   = false;
        isExiting    = true;
        exitCallback = onComplete;
    }

    /// <summary>Crane lerps from the top-side corner down to centre.</summary>
    public void TriggerEntry()
    {
        float camTop   = Camera.main.transform.position.y + Camera.main.orthographicSize;
        float restY    = camTop - restBelowTop;
        float camHalfW = Camera.main.orthographicSize * Camera.main.aspect;

        animTo     = new Vector3(0f, restY, 0f);
        swingTime  = 0f;
        isExiting  = false;
        isEntering = true;

        transform.position = new Vector3(-side * (camHalfW + 3f), camTop + maxRopeLength + 1f, 0f);

        // Sync HookPosition immediately so spawned block starts at the right place
        HookPosition = new Vector3(transform.position.x, transform.position.y - maxRopeLength, 0f);
    }

    public void AttachBlock(BlockController block)
    {
        activeBlock = block;
        block.AttachToCrane();
    }

    public void ClearBlock()
    {
        if (activeBlock == null) return;
        Destroy(activeBlock.gameObject);
        activeBlock = null;
    }

    // ─── Private ─────────────────────────────────────────────────────────────

    void DropBlock()
    {
        if (activeBlock == null) return;
        if (activeBlock.State != BlockController.BlockState.OnCrane) return;
        activeBlock.Drop();
        activeBlock = null;
    }
}
