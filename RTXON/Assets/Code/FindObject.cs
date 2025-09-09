using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.InputSystem;

public class FindObject : MonoBehaviour
{
    private const string PrefKey_LastQuery = "FindObject.LastQuery";
    private const string PrefKey_LastCategory = "FindObject.LastCategory";
    private const string PrefKey_LastSubCategory = "FindObject.LastSubCategory";
    [SerializeField] TMP_InputField prompt;
    [SerializeField] Button buttonStart;
    [SerializeField] Button[] previewButtons;
    [SerializeField] ImportGLBbyURL importer; // Target importer that instantiates the model as a child

    private string[] previewUrls;
    private string[] glbUrls;
    private Dictionary<int, bool> imageDownloaded;
    private Dictionary<int, Coroutine> downloadCoroutines;

    private string lastSubmittedQuery = "";
    private bool inputDirty = false;
    private Vector2 angularVelocity = Vector2.zero;
    private float rotationSensitivity = 5f; // doubled sensitivity (degrees per pixel)
    private float inertiaDamping = 8f; // higher = stronger damping
    private float scrollScaleSensitivity = 0.03f; // doubled sensitivity (scale per scroll unit)
    private float autoSearchDebounceSec = 0.5f; // delay after typing before auto-search
    private float lastEditTime = -1f;
    [SerializeField] public TMP_Text textTotalCount ;
    [SerializeField] public TMP_Dropdown dropdownCategory;
    [SerializeField] public TMP_Dropdown dropdownSubCategory;

    // Category model
    [System.Serializable]
    private class SubcategoryDef { public int id; public string name; public SubcategoryDef(int i, string n){ id=i; name=n; } }
    [System.Serializable]
    private class CategoryDef { public int id; public string name; public List<SubcategoryDef> subs = new List<SubcategoryDef>(); public CategoryDef(int i,string n){ id=i; name=n; } }

    private static readonly List<CategoryDef> CATEGORIES = new List<CategoryDef>
    {
        new CategoryDef(1, "Aircraft") { subs = new List<SubcategoryDef>{ new SubcategoryDef(2,"Part"), new SubcategoryDef(3,"Commercial"), new SubcategoryDef(4,"Helicopter"), new SubcategoryDef(5,"Historic"), new SubcategoryDef(6,"Jet"), new SubcategoryDef(7,"Military"), new SubcategoryDef(8,"Other"), new SubcategoryDef(9,"Private") }},
        new CategoryDef(10, "Animals") { subs = new List<SubcategoryDef>{ new SubcategoryDef(11,"Bird"), new SubcategoryDef(12,"Dinosaur"), new SubcategoryDef(13,"Fish"), new SubcategoryDef(14,"Insect"), new SubcategoryDef(15,"Mammal"), new SubcategoryDef(16,"Other"), new SubcategoryDef(17,"Reptile") }},
        new CategoryDef(18, "Architectural") { subs = new List<SubcategoryDef>{ new SubcategoryDef(19,"Engineering"), new SubcategoryDef(20,"Decoration"), new SubcategoryDef(21,"Door"), new SubcategoryDef(22,"Fixture"), new SubcategoryDef(23,"Floor"), new SubcategoryDef(24,"Lighting"), new SubcategoryDef(25,"Other"), new SubcategoryDef(26,"Street"), new SubcategoryDef(847,"Window") }},
        new CategoryDef(27, "Exterior") { subs = new List<SubcategoryDef>{ new SubcategoryDef(28,"Stadium"), new SubcategoryDef(29,"Cityscape"), new SubcategoryDef(30,"Office"), new SubcategoryDef(31,"Historic"), new SubcategoryDef(32,"House"), new SubcategoryDef(33,"Industrial"), new SubcategoryDef(34,"Landmark"), new SubcategoryDef(35,"Landscape"), new SubcategoryDef(36,"Other"), new SubcategoryDef(37,"Sci-fi"), new SubcategoryDef(38,"Skyscraper"), new SubcategoryDef(39,"Street"), new SubcategoryDef(155,"Public") }},
        new CategoryDef(40, "Interior") { subs = new List<SubcategoryDef>{ new SubcategoryDef(41,"Bathroom"), new SubcategoryDef(42,"Bedroom"), new SubcategoryDef(43,"Hall"), new SubcategoryDef(44,"House"), new SubcategoryDef(46,"Kitchen"), new SubcategoryDef(47,"Living Room"), new SubcategoryDef(48,"Office"), new SubcategoryDef(49,"Other") }},
        new CategoryDef(50, "Car") { subs = new List<SubcategoryDef>{ new SubcategoryDef(51,"Antique"), new SubcategoryDef(52,"Concept"), new SubcategoryDef(53,"SUV"), new SubcategoryDef(54,"Luxury"), new SubcategoryDef(55,"Racing"), new SubcategoryDef(56,"Sport"), new SubcategoryDef(57,"Standard") }},
        new CategoryDef(58, "Character") { subs = new List<SubcategoryDef>{ new SubcategoryDef(59,"Anatomy"), new SubcategoryDef(60,"Child"), new SubcategoryDef(61,"Clothing"), new SubcategoryDef(62,"Fantasy"), new SubcategoryDef(63,"Man"), new SubcategoryDef(64,"Other"), new SubcategoryDef(65,"Sci-Fi"), new SubcategoryDef(66,"Woman") }},
        new CategoryDef(67, "Electronics") { subs = new List<SubcategoryDef>{ new SubcategoryDef(68,"Audio"), new SubcategoryDef(69,"Computer"), new SubcategoryDef(70,"Other"), new SubcategoryDef(71,"Phone"), new SubcategoryDef(72,"Video") }},
        new CategoryDef(73, "Food") { subs = new List<SubcategoryDef>{ new SubcategoryDef(74,"Beverage"), new SubcategoryDef(75,"Fruit"), new SubcategoryDef(76,"Other"), new SubcategoryDef(77,"Vegetable") }},
        new CategoryDef(78, "Furniture") { subs = new List<SubcategoryDef>{ new SubcategoryDef(79,"Appliance"), new SubcategoryDef(80,"Bed"), new SubcategoryDef(81,"Cabinet"), new SubcategoryDef(82,"Chair"), new SubcategoryDef(83,"Outdoor"), new SubcategoryDef(84,"Kitchen"), new SubcategoryDef(85,"Lamp"), new SubcategoryDef(86,"Other"), new SubcategoryDef(87,"Sofa"), new SubcategoryDef(88,"Table"), new SubcategoryDef(89,"Tableware"), new SubcategoryDef(90,"Furniture Set") }},
        new CategoryDef(98, "Household") { subs = new List<SubcategoryDef>{ new SubcategoryDef(99,"Kitchenware"), new SubcategoryDef(100,"Other"), new SubcategoryDef(101,"Tools") }},
        new CategoryDef(102, "Industrial") { subs = new List<SubcategoryDef>{ new SubcategoryDef(103,"Machine"), new SubcategoryDef(104,"Other"), new SubcategoryDef(105,"Part"), new SubcategoryDef(106,"Tool") }},
        new CategoryDef(107, "Plant") { subs = new List<SubcategoryDef>{ new SubcategoryDef(108,"Conifer"), new SubcategoryDef(109,"Flower"), new SubcategoryDef(110,"Grass"), new SubcategoryDef(111,"Leaf"), new SubcategoryDef(112,"Other"), new SubcategoryDef(113,"Pot Plant"), new SubcategoryDef(114,"Bush") }},
        new CategoryDef(115, "Science") { subs = new List<SubcategoryDef>{ new SubcategoryDef(116,"Laboratory"), new SubcategoryDef(117,"Medical"), new SubcategoryDef(118,"Other") }},
        new CategoryDef(119, "Space") { subs = new List<SubcategoryDef>{ new SubcategoryDef(120,"Other"), new SubcategoryDef(121,"Planet"), new SubcategoryDef(122,"Spaceship") }},
        new CategoryDef(124, "Sports") { subs = new List<SubcategoryDef>{ new SubcategoryDef(125,"Game"), new SubcategoryDef(126,"Book"), new SubcategoryDef(127,"Equipment"), new SubcategoryDef(128,"Music"), new SubcategoryDef(129,"Toy") }},
        new CategoryDef(130, "Vehicle") { subs = new List<SubcategoryDef>{ new SubcategoryDef(131,"Bicycle"), new SubcategoryDef(132,"Bus"), new SubcategoryDef(133,"Industrial"), new SubcategoryDef(134,"Military"), new SubcategoryDef(135,"Motorcycle"), new SubcategoryDef(136,"Other"), new SubcategoryDef(137,"Part"), new SubcategoryDef(138,"Sci-Fi"), new SubcategoryDef(139,"Train"), new SubcategoryDef(140,"Truck") }},
        new CategoryDef(141, "Watercraft") { subs = new List<SubcategoryDef>{ new SubcategoryDef(142,"Historic"), new SubcategoryDef(143,"Industrial"), new SubcategoryDef(144,"Military"), new SubcategoryDef(145,"Other"), new SubcategoryDef(146,"Recreational") }},
        new CategoryDef(147, "Military") { subs = new List<SubcategoryDef>{ new SubcategoryDef(148,"Armor"), new SubcategoryDef(149,"Character"), new SubcategoryDef(150,"Gun"), new SubcategoryDef(151,"Melee"), new SubcategoryDef(152,"Other"), new SubcategoryDef(153,"Rocketry"), new SubcategoryDef(154,"Vehicle") }},
        new CategoryDef(228, "Scanned 3D Models") { subs = new List<SubcategoryDef>{ new SubcategoryDef(229,"Various") }},
        new CategoryDef(230, "Scripts / Plugins") { subs = new List<SubcategoryDef>{ new SubcategoryDef(231,"Modelling"), new SubcategoryDef(232,"Animation"), new SubcategoryDef(233,"Rendering"), new SubcategoryDef(234,"Lighting"), new SubcategoryDef(235,"Texturing"), new SubcategoryDef(236,"VFX") }},
        new CategoryDef(237, "Engineering Parts"),
        new CategoryDef(315, "Various") { subs = new List<SubcategoryDef>{ new SubcategoryDef(316, "Various models") }},
        new CategoryDef(849, "Textures") { subs = new List<SubcategoryDef>{ new SubcategoryDef(850,"Architectural"), new SubcategoryDef(851,"Natural"), new SubcategoryDef(852,"Decal"), new SubcategoryDef(853,"Miscellaneous") }}
    };

    // Counts fetched from API
    private Dictionary<int, int> categoryIdToCount = new Dictionary<int, int>();
    private Dictionary<int, Dictionary<int,int>> categoryIdToSubCounts = new Dictionary<int, Dictionary<int,int>>();
    private readonly List<int> categoryOptionIds = new List<int>(); // 0 = All, others = category ids by index
    private readonly List<int> subcategoryOptionIds = new List<int>(); // 0 = All, others = sub ids by index
    private int currentCategoryId = -1; // -1 means All
    private int currentSubCategoryId = -1; // -1 means All

    private void Start()
    {
        if (buttonStart != null)
        {
            buttonStart.onClick.AddListener(StartSearch);
        }

        if (prompt != null)
        {
            // Track text changes to require Enter/Tab only after modification
            prompt.onValueChanged.AddListener(OnPromptChanged);
        }

        previewButtons = GetComponentsInChildren<Button>();
        for (int i = 0; i < previewButtons.Length; i++)
        {
            int idx = i;
            if (previewButtons[idx] != null)
            {
                previewButtons[idx].onClick.AddListener(() => OnPreviewClicked(idx));
                // Add debug remap for first 3 buttons to cycle mask presets
                if (idx == 0)
                {
                    previewButtons[idx].onClick.AddListener(() => OnRemapMaskClicked(1));
                }
                else if (idx == 1)
                {
                    previewButtons[idx].onClick.AddListener(() => OnRemapMaskClicked(2));
                }
                else if (idx == 2)
                {
                    previewButtons[idx].onClick.AddListener(() => OnRemapMaskClicked(3));
                }
            }
        }

        previewUrls = new string[previewButtons.Length];
        glbUrls = new string[previewButtons.Length];
        imageDownloaded = new Dictionary<int, bool>();
        downloadCoroutines = new Dictionary<int, Coroutine>();

        // Load last successful query and search immediately
        if (prompt != null)
        {
            string saved = PlayerPrefs.GetString(PrefKey_LastQuery, "");
            if (!string.IsNullOrEmpty(saved))
            {
                prompt.text = saved;
                inputDirty = false;
                lastSubmittedQuery = saved;
                // search will run after dropdowns init
            }
        }

        // Restore last category selection
        currentCategoryId = PlayerPrefs.GetInt(PrefKey_LastCategory, -1);
        currentSubCategoryId = PlayerPrefs.GetInt(PrefKey_LastSubCategory, -1);

        StartCoroutine(RefreshCountsAndPopulateUI());
    }

    private void StartSearch()
    {
        string query = prompt != null ? prompt.text : "";
        Debug.Log($"FindObject: StartSearch -> '{query}'");
        StartCoroutine(SearchAndPopulate(query));
        inputDirty = false;
        lastSubmittedQuery = query;
    }

    private IEnumerator SearchAndPopulate(string query)
    {
        Debug.Log("FindObject: SearchAndPopulate starting...");
        // Stop any in-flight image downloads
        foreach (var kv in downloadCoroutines)
        {
            if (kv.Value != null) StopCoroutine(kv.Value);
        }
        downloadCoroutines.Clear();
        imageDownloaded.Clear();

        // Clear old sprites
        for (int i = 0; i < previewButtons.Length; i++)
        {
            if (previewButtons[i] != null)
            {
                previewButtons[i].image.sprite = null;
            }
            previewUrls[i] = null;
            glbUrls[i] = null;
        }

        int limit = (previewButtons != null) ? Mathf.Max(0, previewButtons.Length) : 0;
        if (limit <= 0)
        {
            Debug.LogWarning("FindObject: No preview buttons found to display results.");
            yield break;
        }
        string url = BuildSearchUrl(query, limit);
        Debug.Log($"FindObject: GET {url} (limit={limit})");

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = www.downloadHandler.text;
                Debug.Log($"FindObject: Search response length={response?.Length}");
                ParseSearchResponse(response);
            }
            else
            {
                Debug.LogError("FindObject: Search request failed -> " + www.error);
                yield break;
            }
        }
    }

    [System.Serializable]
    private class SearchItem
    {
        public string asset_guid;
        public string title;
        public bool is_character;
        public string[] keywords;
        public int category_id;
        public int sub_category_id;
        public string preview_url;
        public string glb_url;
        public string metadata_url;
    }

    [System.Serializable]
    private class SearchResponse
    {
        public SearchItem[] items;
        public int total;
    }

    private void ParseSearchResponse(string response)
    {
        try
        {
            var data = JsonUtility.FromJson<SearchResponse>(response);
            if (data == null || data.items == null || data.items.Length == 0)
            {
                Debug.LogWarning("FindObject: No items in search response");
                return;
            }

            int buttonSlots = (previewButtons != null) ? previewButtons.Length : 0;
            int count = Mathf.Min(data.items.Length, buttonSlots);
            Debug.Log($"FindObject: Parsed {data.items.Length} items, showing {count} to match {buttonSlots} buttons");
            for (int i = 0; i < count; i++)
            {
                var item = data.items[i];
                previewUrls[i] = item.preview_url;
                glbUrls[i] = item.glb_url;
                imageDownloaded[i] = false;
                downloadCoroutines[i] = StartCoroutine(DownloadPreviewWithRetry(i));
            }

            // Persist last successful query
            if (!string.IsNullOrEmpty(lastSubmittedQuery))
            {
                PlayerPrefs.SetString(PrefKey_LastQuery, lastSubmittedQuery);
                PlayerPrefs.Save();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("FindObject: Failed to parse search response -> " + e.Message);
        }
    }

    private IEnumerator DownloadPreviewWithRetry(int index)
    {
        float interval = 2.5f;
        int attempts = 0;
        const int maxAttempts = 6;

        while (!imageDownloaded.ContainsKey(index) || (imageDownloaded.ContainsKey(index) && imageDownloaded[index] == false))
        {
            if (attempts++ >= maxAttempts) yield break;

            if (!string.IsNullOrEmpty(previewUrls[index]))
            {
                yield return StartCoroutine(DownloadPreview(index));
                if (imageDownloaded[index]) yield break;
            }
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator DownloadPreview(int index)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(previewUrls[index]))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                if (previewButtons[index] != null)
                {
                    previewButtons[index].image.sprite = sprite;
                }
                imageDownloaded[index] = true;
            }
            else
            {
                Debug.LogWarning($"FindObject: Failed to download preview {index} -> {www.error}");
            }
        }
    }

    private void OnPreviewClicked(int index)
    {
        if (index < 0 || index >= glbUrls.Length) return;
        string url = glbUrls[index];
        if (string.IsNullOrEmpty(url)) return;

        if (importer == null)
        {
            importer = GetComponent<ImportGLBbyURL>();
        }
        if (importer != null)
        {
            importer.ImportFromUrl(url, true);
        }
        else
        {
            Debug.LogError("FindObject: ImportGLBbyURL component not assigned/found");
        }
    }

    // Debug helper: regenerate mask map with preset 1..3 after model is already loaded
    private void OnRemapMaskClicked(int preset)
    {
        if (importer == null) return;
        importer.RegenerateMaskMap(preset);
    }

    private void Update()
    {
        // Trigger search on Enter/Tab when text changed
        var keyboard = Keyboard.current;
        if (prompt != null && keyboard != null && prompt.isFocused)
        {
            if ((keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.tabKey.wasPressedThisFrame))
            {
                if (inputDirty || !string.Equals(lastSubmittedQuery, prompt.text))
                {
                    Debug.Log("FindObject: Enter/Tab detected, launching search");
                    StartSearch();
                }
            }
        }

        // Auto-search debounce without needing Enter/Tab
        if (prompt != null && inputDirty && lastEditTime > 0f)
        {
            if (Time.unscaledTime - lastEditTime >= autoSearchDebounceSec)
            {
                Debug.Log("FindObject: Debounced auto-search firing");
                StartSearch();
            }
        }

        // Mouse-driven rotation (RMB drag) with inertia and scroll-based scale
        if (importer == null) return;
        Transform target = importer.transform;
        if (target == null) return;

        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                Vector2 desired = new Vector2(delta.x * rotationSensitivity, delta.y * rotationSensitivity);
                angularVelocity = Vector2.Lerp(angularVelocity, desired, 0.5f);
            }
            else
            {
                angularVelocity = Vector2.Lerp(angularVelocity, Vector2.zero, inertiaDamping * Time.deltaTime);
            }

            if (angularVelocity.sqrMagnitude > 0.0001f)
            {
                // Inverted axes: vertical drag -> positive X rotation, horizontal drag -> negative Y rotation
                target.Rotate(angularVelocity.y * Time.deltaTime, -angularVelocity.x * Time.deltaTime, 0f, Space.World);
            }

            float scroll = mouse.scroll.ReadValue().y;
            // Disable scaling when mouse X is in the left 25% of the screen
            float mouseX = mouse.position.ReadValue().x;
            if (mouseX >= Screen.width * 0.25f)
            {
                if (Mathf.Abs(scroll) > 0.0001f)
                {
                    float factor = 1f + scroll * scrollScaleSensitivity;
                    factor = Mathf.Clamp(factor, 0.1f, 10f);
                    Vector3 newScale = target.localScale * factor;
                    newScale = ClampVector3(newScale, 0.01f, 100f);
                    target.localScale = newScale;
                }
            }
        }
    }

    private void OnPromptChanged(string _)
    {
        inputDirty = true;
        lastEditTime = Time.unscaledTime;
    }

    private Vector3 ClampVector3(Vector3 v, float min, float max)
    {
        return new Vector3(
            Mathf.Clamp(v.x, min, max),
            Mathf.Clamp(v.y, min, max),
            Mathf.Clamp(v.z, min, max)
        );
    }

    // ---------- Categories / Counts / Dropdowns ----------
    [System.Serializable]
    private class CountByCategoryResponse { public CountByCategoryItem[] items; public int total; }
    [System.Serializable]
    private class CountByCategoryItem { public int category_id; public int total; public CountBySubItem[] by_sub_category; }
    [System.Serializable]
    private class CountBySubItem { public int sub_category_id; public int total; }
    [System.Serializable]
    private class CountResponse { public int total; }

    private IEnumerator RefreshCountsAndPopulateUI()
    {
        // Total count
        using (UnityWebRequest www = UnityWebRequest.Get("https://renderfin.com/api-game-assets/count"))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                var resp = JsonUtility.FromJson<CountResponse>(www.downloadHandler.text);
                if (resp != null && textTotalCount != null)
                {
                    textTotalCount.text = "Total Models: " + resp.total;
                }
            }
        }

        // Count by category
        using (UnityWebRequest www = UnityWebRequest.Get("https://renderfin.com/api-game-assets/count-by-category"))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                var resp = JsonUtility.FromJson<CountByCategoryResponse>(www.downloadHandler.text);
                categoryIdToCount.Clear();
                categoryIdToSubCounts.Clear();
                if (resp != null && resp.items != null)
                {
                    foreach (var item in resp.items)
                    {
                        categoryIdToCount[item.category_id] = item.total;
                        if (item.by_sub_category != null)
                        {
                            var dict = new Dictionary<int,int>();
                            foreach (var s in item.by_sub_category) dict[s.sub_category_id] = s.total;
                            categoryIdToSubCounts[item.category_id] = dict;
                        }
                    }
                }
            }
        }

        PopulateCategoryDropdown();
        PopulateSubCategoryDropdown(currentCategoryId);

        // Hook change listeners
        if (dropdownCategory != null)
        {
            dropdownCategory.onValueChanged.RemoveListener(OnCategoryDropdownChanged);
            dropdownCategory.onValueChanged.AddListener(OnCategoryDropdownChanged);
        }
        if (dropdownSubCategory != null)
        {
            dropdownSubCategory.onValueChanged.RemoveListener(OnSubCategoryDropdownChanged);
            dropdownSubCategory.onValueChanged.AddListener(OnSubCategoryDropdownChanged);
        }

        // Initial search after UI ready
        StartSearch();
    }

    private void PopulateCategoryDropdown()
    {
        if (dropdownCategory == null) return;
        dropdownCategory.ClearOptions();
        categoryOptionIds.Clear();
        var options = new List<TMP_Dropdown.OptionData>();

        // All option
        options.Add(new TMP_Dropdown.OptionData("All"));
        categoryOptionIds.Add(-1);

        // Populate
        foreach (var cat in CATEGORIES)
        {
            int count = categoryIdToCount.ContainsKey(cat.id) ? categoryIdToCount[cat.id] : 0;
            options.Add(new TMP_Dropdown.OptionData($"{cat.name} ({count})"));
            categoryOptionIds.Add(cat.id);
        }
        dropdownCategory.AddOptions(options);

        // Select saved
        int index = Mathf.Max(0, categoryOptionIds.IndexOf(currentCategoryId));
        dropdownCategory.SetValueWithoutNotify(index < 0 ? 0 : index);
    }

    private void PopulateSubCategoryDropdown(int categoryId)
    {
        if (dropdownSubCategory == null) return;
        dropdownSubCategory.ClearOptions();
        subcategoryOptionIds.Clear();
        var options = new List<TMP_Dropdown.OptionData>();

        // All option
        options.Add(new TMP_Dropdown.OptionData("All"));
        subcategoryOptionIds.Add(-1);

        var cat = CATEGORIES.Find(c => c.id == categoryId);
        if (cat != null)
        {
            Dictionary<int,int> subcounts = categoryIdToSubCounts.ContainsKey(categoryId) ? categoryIdToSubCounts[categoryId] : null;
            foreach (var s in cat.subs)
            {
                int cnt = (subcounts != null && subcounts.ContainsKey(s.id)) ? subcounts[s.id] : 0;
                options.Add(new TMP_Dropdown.OptionData($"{s.name} ({cnt})"));
                subcategoryOptionIds.Add(s.id);
            }
        }

        dropdownSubCategory.AddOptions(options);

        int index = Mathf.Max(0, subcategoryOptionIds.IndexOf(currentSubCategoryId));
        dropdownSubCategory.SetValueWithoutNotify(index < 0 ? 0 : index);
    }

    private void OnCategoryDropdownChanged(int idx)
    {
        if (idx < 0 || idx >= categoryOptionIds.Count) return;
        currentCategoryId = categoryOptionIds[idx];
        PlayerPrefs.SetInt(PrefKey_LastCategory, currentCategoryId);
        PlayerPrefs.Save();

        // Reset subcategory if category changed
        currentSubCategoryId = -1;
        PlayerPrefs.SetInt(PrefKey_LastSubCategory, currentSubCategoryId);
        PopulateSubCategoryDropdown(currentCategoryId);

        StartSearch();
    }

    private void OnSubCategoryDropdownChanged(int idx)
    {
        if (idx < 0 || idx >= subcategoryOptionIds.Count) return;
        currentSubCategoryId = subcategoryOptionIds[idx];
        PlayerPrefs.SetInt(PrefKey_LastSubCategory, currentSubCategoryId);
        PlayerPrefs.Save();
        StartSearch();
    }

    private string BuildSearchUrl(string query, int limit)
    {
        var sb = new System.Text.StringBuilder("https://renderfin.com/api-game-assets/search?");
        bool first = true;
        if (!string.IsNullOrEmpty(query))
        {
            sb.Append("q=").Append(UnityWebRequest.EscapeURL(query));
            first = false;
        }
        if (currentCategoryId > 0)
        {
            if (!first) sb.Append('&');
            sb.Append("category_id=").Append(currentCategoryId);
            first = false;
        }
        if (currentSubCategoryId > 0)
        {
            if (!first) sb.Append('&');
            sb.Append("sub_category_id=").Append(currentSubCategoryId);
            first = false;
        }
        if (!first) sb.Append('&');
        sb.Append("limit=").Append(limit).Append("&offset=0");
        return sb.ToString();
    }
}


