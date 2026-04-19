using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TowerManager : MonoBehaviour
{
    public static TowerManager Instance { get; private set; }

    [Header("Block Prefabs (Block_1 … Block_4)")]
    [SerializeField] private GameObject[] blockPrefabs;

    [Header("Tower")]
    public float BlockHeight = 2.083f;
    [SerializeField] private float missThreshold = 0.0f;
    public GameObject BlockHolder;
    [SerializeField] private float towerBaseY = -5f;

    [Header("Escape Line")]
    [SerializeField] private Sprite escapeLineSprite;

    public GameObject[] Towers;
    public int ShowTower = 0;

    // Public
    public float TopY { get; private set; }

    // Private
    private List<GameObject> placedBlocks = new List<GameObject>();
    private float currentWidth;
    private float currentCenterX;
    private int prefabIndex = 0;

    [Header("Timer")]
    [SerializeField] private float startTime = 60f; // assign in Inspector
    [SerializeField] private Text[] timerText;

    private float currentTime;
    private bool isCounting = false;
    public Text ReachText;

    // ─── Unity ───────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    public void StartTower()
    {
        ClearTower();
        Towers[ShowTower].gameObject.SetActive(true);
        var sr = blockPrefabs[0].GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            Vector3 s = blockPrefabs[0].transform.localScale;
            currentWidth = sr.sprite.bounds.size.x * s.x;
            BlockHeight = sr.sprite.bounds.size.y * s.y;
        }

        currentCenterX = 0f;

        float camBottom = Camera.main.transform.position.y - Camera.main.orthographicSize;
        TopY = camBottom + BlockHeight * 0.5f;

        PlaceEscapeLine(TopY + GameManager.Instance.EscapeFloor * BlockHeight);
        SpawnBlockOnCrane();

        currentTime = startTime;
        isCounting = true;
    }

    void Update()
    {
        if (!isCounting) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            isCounting = false;
            GameManager.Instance.TriggerGameOver();
        }

        UpdateTimerUI();
    }
    void UpdateTimerUI()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        timerText[0].text = $"{minutes:00}:{seconds:00}";
        timerText[1].text = $"{minutes:00}:{seconds:00}";
    }
    public void ClearTower()
    {
        StopAllCoroutines();

        if (CraneController.Instance != null)
            CraneController.Instance.ClearBlock();

        foreach (var b in placedBlocks)
            if (b) Destroy(b);

        placedBlocks.Clear();
        prefabIndex = 0;
    }

    // Called by BlockController when it hits the tower top
    public void OnBlockLanded(BlockController block)
    {

        if (ShowTower > 3)
        {
            ShowTower = 0;
            Towers[ShowTower].gameObject.SetActive(true);
            return;
        }
        else if (ShowTower <= 3)
        {
            for (int i = 0; i < Towers.Length; i++)
            {
                Towers[i].gameObject.SetActive(false);
            }
            ShowTower++;
            if (ShowTower > 3)
                ShowTower = 0;
            Towers[ShowTower].gameObject.SetActive(true);
        }


        float dropX = block.transform.position.x;
        float dropW = block.CurrentWidth;
        Color blockColor = block.GetComponent<SpriteRenderer>().color;

        float prevLeft = currentCenterX - currentWidth / 2f;
        float prevRight = currentCenterX + currentWidth / 2f;
        float dropLeft = dropX - dropW / 2f;
        float dropRight = dropX + dropW / 2f;

        float overlapLeft = Mathf.Max(prevLeft, dropLeft);
        float overlapRight = Mathf.Min(prevRight, dropRight);
        float overlapWidth = overlapRight - overlapLeft;

        // ── Complete miss — no overlap at all ────────────────────────────────
        if (overlapWidth <= missThreshold)
        {
            SpawnFallingPiece(dropX, block.transform.position.y, currentWidth, blockColor);
            Destroy(block.gameObject);
            GameManager.Instance.TriggerGameOver();
            return;
        }

        // ── Land — freeze the dropped block exactly in place ────────────────
        // Refresh block dimensions from the actual landed block's sprite
        var landedSR = block.GetComponent<SpriteRenderer>();
        if (landedSR != null && landedSR.sprite != null)
        {
            currentWidth = landedSR.sprite.bounds.size.x * block.transform.localScale.x;
            BlockHeight = landedSR.sprite.bounds.size.y * block.transform.localScale.y;
        }

        block.transform.position = new Vector3(dropX, TopY + BlockHeight * 0.5f, 0f);
        block.transform.rotation = Quaternion.identity;
        placedBlocks.Add(block.gameObject);
        StartCoroutine(SquishBlock(block.transform));

        currentCenterX = dropX;
        TopY += BlockHeight;

        GameManager.Instance.RegisterFloor();
        if (GameManager.Instance.State == GameManager.GameState.Playing)
            CraneController.Instance.TriggerExit(() => SpawnBlockOnCrane());
    }

    // ─── Spawning ─────────────────────────────────────────────────────────────

    void SpawnBlockOnCrane()
    {
        // Rope extends down from crane top, bringing the new block with it
        CraneController.Instance.TriggerEntry();

        // Always use blockPrefabs[0] until all prefabs have art assigned
        GameObject go = Instantiate(NextPrefab(), CraneController.Instance.HookPosition, Quaternion.identity);
        BlockController bc = go.AddComponent<BlockController>();
        bc.Init(currentWidth);
        CraneController.Instance.AttachBlock(bc);
    }

    GameObject SpawnPlaced(float cx, float bottomY, float width)
    {
        float cy = bottomY + BlockHeight * 0.5f;
        GameObject go = Instantiate(NextPrefab(), new Vector3(cx, cy, 0f), Quaternion.identity);
        // Scale comes from the prefab — don't override it
        placedBlocks.Add(go);
        return go;
    }

    void SpawnFallingPiece(float cx, float centerY, float width, Color color)
    {
        Towers[ShowTower].gameObject.SetActive(false);
        GameObject go = Instantiate(NextPrefab(), new Vector3(cx, centerY, 0f), Quaternion.identity);
        // Scale from prefab
        go.GetComponent<SpriteRenderer>().color = color;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2.5f;
        rb.AddTorque(Random.Range(-90f, 90f));

        Destroy(go, 2.5f);
    }

    // ─── Animations ───────────────────────────────────────────────────────────

    IEnumerator SquishBlock(Transform t)
    {
        if (t == null) yield break;
        Vector3 baseScale = t.localScale;

        // Squish down
        float dur = 0.07f, elapsed = 0f;
        while (elapsed < dur)
        {
            if (t == null) yield break;
            elapsed += Time.deltaTime;
            float p = elapsed / dur;
            t.localScale = new Vector3(
                Mathf.Lerp(baseScale.x, baseScale.x * 1.06f, p),
                Mathf.Lerp(baseScale.y, baseScale.y * 0.70f, p),
                1f);
            yield return null;
        }

        // Bounce back
        elapsed = 0f; dur = 0.12f;
        while (elapsed < dur)
        {
            if (t == null) yield break;
            elapsed += Time.deltaTime;
            float p = elapsed / dur;
            t.localScale = new Vector3(
                Mathf.Lerp(baseScale.x * 1.06f, baseScale.x, p),
                Mathf.Lerp(baseScale.y * 0.70f, baseScale.y, p),
                1f);
            yield return null;
        }

        if (t != null) t.localScale = baseScale;
    }

    // ─── Escape Line ─────────────────────────────────────────────────────────

    void PlaceEscapeLine(float y)
    {
        Transform old = transform.Find("EscapeLineRoot");
        if (old != null) Destroy(old.gameObject);

        GameObject root = new GameObject("EscapeLineRoot");
        root.transform.SetParent(transform);

        if (escapeLineSprite != null)
        {
            GameObject lineGO = new GameObject("LineSprite");
            lineGO.transform.SetParent(root.transform);
            lineGO.transform.position = new Vector3(0f, y, 0f);

            SpriteRenderer sr = lineGO.AddComponent<SpriteRenderer>();
            sr.sprite = escapeLineSprite;
            sr.sortingOrder = 10;

            float screenWidth = Camera.main.orthographicSize * Camera.main.aspect * 2f;
            float spriteWidth = escapeLineSprite.bounds.size.x;
            lineGO.transform.localScale = new Vector3(screenWidth / spriteWidth, 1f, 1f);
        }

        GameObject labelGO = new GameObject("EscapeLabel");
        labelGO.transform.SetParent(root.transform);
        labelGO.transform.position = new Vector3(0f, y + 0.7f, 0f);

        TextMeshPro tmp = labelGO.AddComponent<TextMeshPro>();
        tmp.text = "ESCAPE HEIGHT";
        tmp.fontSize = 5.2f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 11;
        labelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(12f, 1.5f);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    GameObject NextPrefab() =>
        blockPrefabs[prefabIndex++ % blockPrefabs.Length];
}
