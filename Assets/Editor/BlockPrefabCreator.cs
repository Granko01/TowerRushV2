using UnityEngine;
using UnityEditor;
using System.IO;

public class BlockPrefabCreator
{
    [MenuItem("TowerRush/Create Block Prefabs")]
    static void CreateBlockPrefabs()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");

        // Create base white block texture and save as PNG
        string pngPath = Application.dataPath + "/Sprites/BlockBase.png";
        string assetPath = "Assets/Sprites/BlockBase.png";

        if (!File.Exists(pngPath))
        {
            Texture2D tex = new Texture2D(200, 100);
            Color[] pixels = new Color[200 * 100];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            AssetDatabase.Refresh();
        }

        // Configure texture as sprite
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        AssetDatabase.Refresh();
        Sprite baseSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        if (baseSprite == null)
        {
            Debug.LogError("Failed to load base sprite. Try running the menu item again.");
            return;
        }

        // 4 block colors — user can swap sprites later
        Color[] colors = {
            new Color(0.20f, 0.60f, 1.00f),  // Blue
            new Color(1.00f, 0.40f, 0.20f),  // Orange
            new Color(0.30f, 0.90f, 0.40f),  // Green
            new Color(0.90f, 0.30f, 0.80f),  // Purple
        };
        string[] names = { "Block_1", "Block_2", "Block_3", "Block_4" };

        for (int i = 0; i < 4; i++)
        {
            GameObject block = new GameObject(names[i]);

            SpriteRenderer sr = block.AddComponent<SpriteRenderer>();
            sr.sprite = baseSprite;
            sr.color = colors[i];
            sr.sortingOrder = 1;

            BoxCollider2D col = block.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2f, 0.5f);

            string prefabPath = $"Assets/Prefabs/{names[i]}.prefab";
            PrefabUtility.SaveAsPrefabAsset(block, prefabPath);
            Object.DestroyImmediate(block);

            Debug.Log($"Created prefab: {prefabPath}");
        }

        AssetDatabase.Refresh();
        Debug.Log("Done! 4 block prefabs created in Assets/Prefabs/");
    }
}
