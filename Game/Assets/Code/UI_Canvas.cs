using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UI_Canvas : MonoBehaviour
{
    public enum UI_State
    {
        None,
        Create,
        Simulate,
        Play,
        Chatting,
        Lobby,
        Queue,
        Match,
        Settings,
        Credits,
        About,
        Exit,
        Help
    }
    
    private UI_State _currentState;
    public UI_State currentState
    {
        get { return _currentState; }
        set
        {
            _currentState = value;
            UI_ChangeState?.Invoke(null);
        }
    }
    [SerializeField] UI_SaveLoadSystem saveLoadSystem;
    [SerializeField] List<RectTransform> CreateSubs;
    [SerializeField] public Transform ui_camera;

    [SerializeField] Color target_Normal_Color_forTypes;
    [SerializeField] public Color target_Normal_Color_forSubs;

    [SerializeField]List<UI_Button> Types_Buttons; 
    
    // Tracking selected buttons
    private UI_Button selectedTypeButton;
    public UI_Button[] selectedSubButtons; // One selected button per sub group
    public List<UI_Button> all_sub_buttons= new List<UI_Button>();
    public static UI_Canvas i;
    [SerializeField]public ui_engine_burst engine_burst;

    public static event System.Action<UI_Button> UI_ChangeState;
    // Update is called once per frame
    void Awake()
    {
        i = this;

        // Expect items under Assets/Resources/items_serialized/{0..4}
        int foldersCount = Types_Buttons.Count;

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
        for (int i = 0; i < CreateSubs.Count; i++)
        {
            if (CreateSubs[i].transform.GetChild(0).childCount == 0) continue;
            GameObject go = CreateSubs[i].GetComponentInChildren<UI_Button>().gameObject;
            if (go != null)
                Destroy(go);
        } 


        // start coroutine to select start sub buttons 
        StartCoroutine(SelectStartSubButtons());
    }

    private IEnumerator SelectStartSubButtons()
    {
        yield return new WaitForSeconds(0.5f);
        all_sub_buttons = transform.GetComponentsInChildren<UI_Button>(true).ToList();
        //clear types from this list
        all_sub_buttons = all_sub_buttons.Where(x => x.buttonType == UI_Button.ButtonType.Sub).ToList();
        foreach (var sub in all_sub_buttons)
            if (sub.transform.parent.childCount > 0)
            {
                if (sub.transform.parent.GetChild(0).gameObject == sub.gameObject)
                {

                    SelectSubButton(sub, sub.subGroupIndex);
                }
            }
            SelectTypeButton(Types_Buttons[0]);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectTypeButton(Types_Buttons[0]);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectTypeButton(Types_Buttons[1]);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SelectTypeButton(Types_Buttons[2]);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SelectTypeButton(Types_Buttons[3]);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SelectTypeButton(Types_Buttons[4]);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            SelectTypeButton(Types_Buttons[5]);
        }
        
    }
    private void BuildButtonsForSub(RectTransform sub, int folderIndex)
    {
        item_SO[] items = LoadItemsForFolder(folderIndex);
//        Debug.Log($"UI_Canvas: Building sub '{sub.name}' from folder index {folderIndex}, items loaded: {items.Length}");
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
            uiButton.subGroupIndex=folderIndex;
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
         //   Debug.Log(sb.ToString());
        }
        return result.ToArray();
    }
    public void ChangeCreateSub(int index)
    {
//        Debug.Log("ChangeCreateSub " + index);
        
        foreach(var sub in CreateSubs)
        {
            sub.gameObject.SetActive(false);
        }
        CreateSubs[index].gameObject.SetActive(true);
    }
    public void MenuButtonClick(UI_Button uI_Button)
    {
        // event action menu button click 
        UI_ChangeState?.Invoke(uI_Button);
    }
    // Setup type buttons
 

    // Select initial buttons (first in each group)

    public void SelectSubButton(UI_Button button, int subGroupIndex)
    {
        foreach (var sub in all_sub_buttons)
            if (sub.subGroupIndex == subGroupIndex)
                ResetButtonColor(sub);

        selectedSubButtons[subGroupIndex] = button;
        SetButtonColor(button, target_Normal_Color_forSubs);
      //  Debug.Log($"Select SubButton {button.name} {subGroupIndex}");
        UI_ChangeState?.Invoke(button);
        
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
        selectedTypeButton = button;
        ChangeCreateSub(button.subGroupIndex);
        SetButtonColor(button, target_Normal_Color_forTypes);
         
                if(selectedSubButtons[button.subGroupIndex] != null)
                SelectSubButton(selectedSubButtons[button.subGroupIndex], button.subGroupIndex);
            
        
    }
    
    // Method to handle sub button selection
 
    
    // Helper method to set button color
    public void SetButtonColor(UI_Button button, Color color)
    {
        if (button == null) return;
        Toggle toggle = button.GetComponent<Toggle>();
        if (toggle != null)
        {
            ColorBlock colors = toggle.colors;
            colors.normalColor = color;
            colors.selectedColor = color;
            colors.highlightedColor = color;
            colors.pressedColor = color;
            colors.disabledColor = color;
            colors.colorMultiplier = 1;
            colors.fadeDuration = 0.1f;
            colors.selectedColor = color;
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
