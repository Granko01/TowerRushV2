using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerManager : MonoBehaviour
{
    public static TowerManager Instance { get; private set; }

    [Header("Block Prefabs (Block_1 … Block_4)")]
    [SerializeField] private GameObject[] blockPrefabs;

    [Header("Tower")]
    public  float BlockHeight     = 2.083f;
    [SerializeField] private float missThreshold = 0.0f;
    public GameObject BlockHolder;
    [SerializeField] private float towerBaseY = -5f;

// Public
    public float TopY { get; private set; }

    // Private
    private List<GameObject> placedBlocks  = new List<GameObject>();
    private float            currentWidth;
    private float            currentCenterX;
    private int              prefabIndex = 0;

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

        var sr = blockPrefabs[0].GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            Vector3 s    = blockPrefabs[0].transform.localScale;
            currentWidth = sr.sprite.bounds.size.x * s.x;
            BlockHeight  = sr.sprite.bounds.size.y * s.y;
        }

        currentCenterX = 0f;

       float camBottom = Camera.main.transform.position.y - Camera.main.orthographicSize;
       TopY = camBottom + BlockHeight * 0.5f;

        SpawnBlockOnCrane();
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
        float dropX = block.transform.position.x;
        float dropW = block.CurrentWidth;
        Color blockColor = block.GetComponent<SpriteRenderer>().color;

        float prevLeft  = currentCenterX - currentWidth / 2f;
        float prevRight = currentCenterX + currentWidth / 2f;
        float dropLeft  = dropX - dropW / 2f;
        float dropRight = dropX + dropW / 2f;

        float overlapLeft  = Mathf.Max(prevLeft,  dropLeft);
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
            BlockHeight  = landedSR.sprite.bounds.size.y * block.transform.localScale.y;
        }

        block.transform.position = new Vector3(dropX, TopY + BlockHeight * 0.5f, 0f);
        block.transform.rotation = Quaternion.identity;
        placedBlocks.Add(block.gameObject);
        StartCoroutine(SquishBlock(block.transform));

        currentCenterX = dropX;
        TopY          += BlockHeight;

        GameManager.Instance.RegisterFloor();
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
        float cy = bottomY + BlockHeight * 0.5f;  // center = bottom edge + half height
        GameObject go = Instantiate(NextPrefab(), new Vector3(cx, cy, 0f), Quaternion.identity);
        // Scale comes from the prefab — don't override it
        placedBlocks.Add(go);
        return go;
    }

    void SpawnFallingPiece(float cx, float centerY, float width, Color color)
    {
        GameObject go = Instantiate(NextPrefab(), new Vector3(cx, centerY, 0f), Quaternion.identity);
        // Scale from prefab
        go.GetComponent<SpriteRenderer>().color = color;

        Rigidbody2D rb  = go.AddComponent<Rigidbody2D>();
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

    // ─── Helpers ─────────────────────────────────────────────────────────────

    GameObject NextPrefab() =>
        blockPrefabs[prefabIndex++ % blockPrefabs.Length];
}
