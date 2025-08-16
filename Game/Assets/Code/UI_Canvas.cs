using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UI_Canvas : MonoBehaviour
{
    [SerializeField] List<RectTransform> CreateSubs;
    [SerializeField] public Transform ui_camera;

    [SerializeField] Color target_Normal_Color_forTypes;
    [SerializeField] Color target_Normal_Color_forSubs;

    [SerializeField]List<UI_Button> Types_Buttons; 
    
    // Tracking selected buttons
    private UI_Button selectedTypeButton;
    public UI_Button[] selectedSubButtons; // One selected button per sub group
    public static UI_Canvas i;

    public static event System.Action<UI_Button> SubTypeSelected;
    // Update is called once per frame
    void Awake()
    {
        i = this;
        
        // Expect items under Assets/Resources/items_serialized/{0..4}
        const int foldersCount = 5;
        
        // Initialize selected buttons tracking
        selectedSubButtons = new UI_Button[foldersCount];

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
          Destroy(CreateSubs[0].GetComponentInChildren<UI_Button>().gameObject);
        // 4) Setup type buttons if they exist
        SetupTypeButtons();
        
        // 5) Select first elements in each group
        SelectInitialButtons();
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
        
        // If no items, destroy template and return
        if (items.Length == 0)
        {
            DestroyImmediate(template.gameObject);
            return;
        }
        
        // Create buttons for all items
        for (int i = 0; i < items.Length; i++)
        {
            UI_Button uiButton = Instantiate(template, buttonParent);
            uiButton.name = $"ButtonSub_{folderIndex}_{i}";

            var item = items[i];

            // Assign reference
            uiButton.Item = item;
            
            // Set up button for sub group selection
            uiButton.SetupAsSubButton(folderIndex);

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
    
    // Setup type buttons
    private void SetupTypeButtons()
    {
        if (Types_Buttons != null)
        {
            for (int i = 0; i < Types_Buttons.Count; i++)
            {
                if (Types_Buttons[i] != null)
                {
                    Types_Buttons[i].SetupAsTypeButton();
                }
            }
        }
    }
    
    // Select initial buttons (first in each group)
    private void SelectInitialButtons()
    {
        // Select first type button if exists
        if (Types_Buttons != null && Types_Buttons.Count > 0 && Types_Buttons[0] != null)
        {
            SelectTypeButton(Types_Buttons[0]);
        }
        
        // Select first button in each sub group
        for (int i = 0; i < CreateSubs.Count; i++)
        {
            UI_Button firstButton = GetFirstButtonInSub(CreateSubs[i]);
            if (firstButton != null)
            {
                SelectSubButton(firstButton, i);
            }
        }
    }
    
    // Helper to get first UI_Button in a sub
    private UI_Button GetFirstButtonInSub(RectTransform sub)
    {
        UI_Button[] buttons = sub.GetComponentsInChildren<UI_Button>(true);
        return buttons.Length > 0 ? buttons[0] : null;
    }
    
    // Method to handle type button selection
    public void SelectTypeButton(UI_Button button)
    {
        // Reset previous selection
        if (selectedTypeButton != null)
        {
            ResetButtonColor(selectedTypeButton);
            SetToggleState(selectedTypeButton, false);
        }
        
        // Set new selection
        selectedTypeButton = button;
        SetButtonColor(button, target_Normal_Color_forTypes);
        SetToggleState(button, true);
    }
    
    // Method to handle sub button selection
    public void SelectSubButton(UI_Button button, int subGroupIndex)
    {
        if (subGroupIndex < 0 || subGroupIndex >= selectedSubButtons.Length) return;
        
        // Reset previous selection in this sub group
        if (selectedSubButtons[subGroupIndex] != null)
        {
            ResetButtonColor(selectedSubButtons[subGroupIndex]);
            SetToggleState(selectedSubButtons[subGroupIndex], false);
        }
        
        // Set new selection
        selectedSubButtons[subGroupIndex] = button;
        SetButtonColor(button, target_Normal_Color_forSubs);
        SetToggleState(button, true);
        SubTypeSelected?.Invoke(button);
    }
    
    // Helper method to set button color
    private void SetButtonColor(UI_Button button, Color color)
    {
        if (button == null) return;
        Toggle toggle = button.GetComponent<Toggle>();
        if (toggle != null)
        {
            ColorBlock colors = toggle.colors;
            colors.normalColor = color;
            colors.selectedColor = color;
            toggle.colors = colors;
        }
    }
    
    // Helper method to reset button color to white
    private void ResetButtonColor(UI_Button button)
    {
        if (button == null) return;
        Toggle toggle = button.GetComponent<Toggle>();
        if (toggle != null)
        {
            ColorBlock colors = toggle.colors;
            colors.normalColor = new Color(0,0,0,0);
            toggle.colors = colors;
        }
    }
    
    // Helper method to set toggle state
    private void SetToggleState(UI_Button button, bool isOn)
    {
        if (button == null) return;
        Toggle toggle = button.GetComponent<Toggle>();
        if (toggle != null)
        {
            toggle.isOn = isOn;
        }
    }
}
