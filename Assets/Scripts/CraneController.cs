using System.Collections;
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
    [SerializeField] private float maxRopeLength  = 4.5f;
    [SerializeField] private float slingLength    = 0.6f;  // vertical drop from hook to block top corners
    [SerializeField] private float hookGap        = 1.0f;  // extra space between hook bottom and block top
    [SerializeField] private float slingEdgeRatio = 0.6f;  // 0 = centre, 1 = full edge

    public float SlingLength => slingLength + hookGap;

    [Header("Swing")]
    [SerializeField] private float maxXOffset     = 2.5f;
    [SerializeField] private float baseSwingSpeed = 1.4f;
    [SerializeField] private float speedPerFloor  = 0.07f;

    [Header("Visuals — assign in Inspector")]
    [SerializeField] private Sprite         gantrySprite;
    [SerializeField] private Sprite         cabSprite;
    [SerializeField] private Sprite         hookSprite;
    [SerializeField] public Sprite         hookAttachSprite;  // optional; falls back to a solid rect
    [SerializeField] private Texture2D      craneLineTexture;

    [SerializeField] private Transform hookAttachPoint;   // drag this to the tip of your hook sprite

    [HideInInspector] [SerializeField] private SpriteRenderer gantryRenderer;
    [HideInInspector] [SerializeField] private SpriteRenderer cabRenderer;
    [HideInInspector] [SerializeField] private SpriteRenderer hookRenderer;
    [HideInInspector] [SerializeField] private Transform      hookVisual;

    // pixels → world units (assumes 100 PPU)
    private const float HookAttachW = 10f / 100f;   // 0.10 units
    private const float HookAttachH = 30f / 100f;   // 0.30 units

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

    public UIManager uIManager;

    // ─── Awake ───────────────────────────────────────────────────────────────

    void Awake()
    {
        uIManager = UIManager.FindAnyObjectByType<UIManager>();
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
        SetupHookAttachVisual();
    }

    void SetupHookAttachVisual()
    {
        if (hookAttachPoint == null) return;

        SpriteRenderer sr = hookAttachPoint.GetComponent<SpriteRenderer>();
        if (sr == null) sr = hookAttachPoint.gameObject.AddComponent<SpriteRenderer>();

        if (hookAttachSprite != null)
        {
            sr.sprite = hookAttachSprite;
        }
        else
        {
            // Generate a plain white 1×1 pixel sprite as fallback
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        sr.color        = new Color(0.25f, 0.25f, 0.25f); // dark grey hook colour
        sr.sortingOrder = 5;
        hookAttachPoint.localScale = new Vector3(HookAttachW, HookAttachH, 1f);
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
        if (craneLineTexture != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.mainTexture = craneLineTexture;
            mat.mainTextureScale = new Vector2(1f, 4f);
            lr.material = mat;
            lr.textureMode = LineTextureMode.Tile;
        }
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
            // Slings attach at slingEdgeRatio of the half-width (0=centre, 1=full edge)
            float hw = activeBlock.CurrentWidth * 0.5f * slingEdgeRatio;
            float hh = TowerManager.Instance.BlockHeight * 0.5f;
            Quaternion rot   = activeBlock.transform.rotation;
            Vector3 topLeft  = activeBlock.transform.position + rot * new Vector3(-hw,  hh, 0f);
            Vector3 topRight = activeBlock.transform.position + rot * new Vector3( hw,  hh, 0f);

            Vector3 attachPt = hookAttachPoint != null
                ? hookAttachPoint.position + Vector3.down * (HookAttachH / 2f)
                : HookPosition;

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
        // if (canSwing)
        // {
        //     bool tapped = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        //                || (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        //                || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
        //     if (tapped) DropBlock();
        // }
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

    private bool _noCementRunning = false;

    public void DropBlock()
    {
        bool canDrop = !isEntering && !isExiting
                    && GameManager.Instance != null
                    && GameManager.Instance.State == GameManager.GameState.Playing;
        if (!canDrop) return;

        if (activeBlock == null) return;
        if (activeBlock.State != BlockController.BlockState.OnCrane) return;

        if (uIManager.CementAmount <= 0)
        {
            if (!_noCementRunning)
                StartCoroutine(NoCementMeth());
            return;
        }

        activeBlock.Drop();
        AudioManager.Instance?.PlayCementUse();
        activeBlock = null;
        uIManager.CementAmount--;
        uIManager.SetCement();
        uIManager.UpdateUI(uIManager.CementText, uIManager.CementAmount);
    }

    IEnumerator NoCementMeth()
    {
        _noCementRunning = true;

        GameObject obj = uIManager.NoCement;
        RectTransform rt = obj.GetComponent<RectTransform>();
        Vector2 shownPos  = rt.anchoredPosition;
        Vector2 hiddenPos = shownPos + Vector2.down * 120f;

        rt.anchoredPosition = hiddenPos;
        obj.SetActive(true);

        // slide in
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 5f;
            rt.anchoredPosition = Vector2.Lerp(hiddenPos, shownPos, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        rt.anchoredPosition = shownPos;

        yield return new WaitForSecondsRealtime(1.5f);

        // slide out
        t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 5f;
            rt.anchoredPosition = Vector2.Lerp(shownPos, hiddenPos, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        obj.SetActive(false);
        rt.anchoredPosition = shownPos;
        _noCementRunning = false;

        uIManager.ShowLeaveButton();
    }
}
