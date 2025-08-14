using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UI_Canvas : MonoBehaviour
{
    [SerializeField] List<RectTransform> CreateSubs;

    // Update is called once per frame
    void Awake()
    {
        // Expect items under Assets/Resources/items_serialized/{0..4}
        const int foldersCount = 5;

        // 1) Duplicate base CreateSub 4 times alongside the original
        for (int i = 1; i < foldersCount; i++)
        {
            RectTransform clone = Instantiate(CreateSubs[0], CreateSubs[0].parent);
            clone.name = CreateSubs[0].name + "_" + i;
            CreateSubs.Add(clone);
        }

        // 2) For each sub, duplicate Button template for each SO in its corresponding folder
        for (int folderIndex = 0; folderIndex < foldersCount; folderIndex++)
        {
            BuildButtonsForSub(CreateSubs[folderIndex], folderIndex);
        }

        // 3) Reset visibility/positions and enable only the first
        for (int i = 0; i < CreateSubs.Count; i++)
        {
            var sub = CreateSubs[i];
            sub.gameObject.SetActive(i == 0);
            sub.localPosition = new Vector3(sub.localPosition.x, 0, 0);
        }
    }

    private void BuildButtonsForSub(RectTransform sub, int folderIndex)
    {
        item_SO[] items = LoadItemsForFolder(folderIndex);
        Debug.Log($"UI_Canvas: Building sub '{sub.name}' from folder index {folderIndex}, items loaded: {items.Length}");
        UI_Button template = sub.GetComponentInChildren<UI_Button>(true);
        if (template == null)
        {
            Debug.LogError("UI_Button template not found inside sub: " + sub.name);
            return;
        }

        Transform buttonParent = template.transform.parent;
        // Do not delete any existing buttons; use the first found as the template
        // Keep the template even if there are no items; do not destroy it

        for (int i = 0; i < items.Length; i++)
        {
            UI_Button uiButton = i == 0 ? template : Instantiate(template, buttonParent);
            uiButton.name = $"ButtonSub_{folderIndex}_{i}";

            var item = items[i];

            // Assign reference
            uiButton.Item = item;

            // Replace sprite on target image (prefer explicit Targetimage, otherwise find any Image in children)
            Image target = uiButton.Targetimage != null ? uiButton.Targetimage : uiButton.GetComponentInChildren<Image>(true);
            if (target != null)
            {
                target.sprite = item.icon;
            }
            // Ensure button is active; subs visibility handled separately
            if (!uiButton.gameObject.activeSelf)
            {
                uiButton.gameObject.SetActive(true);
            }
            // Buttons stay enabled; only parent subs are toggled
        }
    }

    private item_SO[] LoadItemsForFolder(int folderIndex)
    {
        // Collect from multiple sources and merge unique instances
        List<item_SO> result = new List<item_SO>();

        // 1) Resources (non-recursive)
        item_SO[] resItems = Resources.LoadAll<item_SO>("items_serialized/" + folderIndex);
        if (resItems != null && resItems.Length > 0)
        {
            for (int i = 0; i < resItems.Length; i++)
            {
                if (resItems[i] != null && !result.Contains(resItems[i])) result.Add(resItems[i]);
            }
        }
#if UNITY_EDITOR
        // 2) Editor: recursively load from Assets/Resources/items_serialized/{index}
        string resFolder = "Assets/Resources/items_serialized/" + folderIndex;
        if (AssetDatabase.IsValidFolder(resFolder))
        {
            string[] guidsRes = AssetDatabase.FindAssets("t:item_SO", new[] { resFolder });
            for (int i = 0; i < guidsRes.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guidsRes[i]);
                var so = AssetDatabase.LoadAssetAtPath<item_SO>(path);
                if (so != null && !result.Contains(so)) result.Add(so);
            }
        }

        // 3) Editor: recursively load from Assets/items_serialized/{index}
        string assetsFolder = "Assets/items_serialized/" + folderIndex;
        if (AssetDatabase.IsValidFolder(assetsFolder))
        {
            string[] guids = AssetDatabase.FindAssets("t:item_SO", new[] { assetsFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var so = AssetDatabase.LoadAssetAtPath<item_SO>(path);
                if (so != null && !result.Contains(so)) result.Add(so);
            }
        }
#endif
        // Log debug list for diagnostics
        if (result.Count > 0)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append($"Loaded {result.Count} item_SO for folder {folderIndex}: ");
            for (int i = 0; i < result.Count; i++)
            {
                sb.Append(result[i].name);
                if (i < result.Count - 1) sb.Append(", ");
            }
            Debug.Log(sb.ToString());
        }
        return result.ToArray();
    }
    public void ChangeCreateSub(int index)
    {
        Debug.Log("ChangeCreateSub " + index);
        
        foreach(var sub in CreateSubs)
        {
            sub.gameObject.SetActive(false);
        }
        CreateSubs[index].gameObject.SetActive(true);
    }
}
