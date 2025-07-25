using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

public class TooltipPrefabCreator : MonoBehaviour
{
    [MenuItem("Tools/Create Tooltip Prefab")]
    public static void CreateTooltipPrefab()
    {
        // Создаем Canvas для тултипа
        GameObject canvasGO = new GameObject("TooltipCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // Высокий приоритет
        
        // Добавляем CanvasScaler
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Добавляем GraphicRaycaster
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Создаем тултип
        GameObject tooltipGO = new GameObject("Base_Tooltip_Prefab");
        tooltipGO.transform.SetParent(canvasGO.transform, false);
        
        // Добавляем RectTransform (обязательно для UI элементов)
        RectTransform rectTransform = tooltipGO.AddComponent<RectTransform>();
        
        // Добавляем компоненты тултипа
        DynamicTooltip tooltip = tooltipGO.AddComponent<DynamicTooltip>();
        
        // Создаем UI элементы
        CreateTooltipUIElements(tooltipGO, tooltip);
        
        // Валидируем созданный тултип
        bool isValid = tooltip.ValidateTooltip();
        if (!isValid)
        {
            Debug.LogWarning("Tooltip prefab created with auto-fixes");
        }
        
        // Создаем папку Resources если её нет
        string resourcesPath = "Assets/Resources";
        if (!System.IO.Directory.Exists(resourcesPath))
        {
            System.IO.Directory.CreateDirectory(resourcesPath);
        }
        
        // Создаем папку UI если её нет
        string uiPath = "Assets/Resources/UI";
        if (!System.IO.Directory.Exists(uiPath))
        {
            System.IO.Directory.CreateDirectory(uiPath);
        }
        
        // Сохраняем префаб
        string prefabPath = "Assets/Resources/UI/Base_Tooltip_Prefab.prefab";
        
        // Удаляем старый префаб если существует
        if (System.IO.File.Exists(prefabPath))
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }
        
        // Создаем префаб
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);
        
        // Удаляем временный объект из сцены
        DestroyImmediate(canvasGO);
        
        // Обновляем AssetDatabase
        AssetDatabase.Refresh();
        
        Debug.Log($"Tooltip prefab created successfully at: {prefabPath}");
        
        // Выделяем созданный префаб в Project window
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
    }
    
    static void CreateTooltipUIElements(GameObject tooltipGO, DynamicTooltip tooltip)
    {
        RectTransform rectTransform = tooltipGO.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(300, 150);
        
        // Создаем фон
        GameObject backgroundGO = new GameObject("Background");
        backgroundGO.transform.SetParent(tooltipGO.transform, false);
        
        Image backgroundImage = backgroundGO.AddComponent<Image>();
        backgroundImage.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
        
        RectTransform bgRect = backgroundGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        tooltip.backgroundImage = backgroundImage;
        
        // Создаем иконку
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(tooltipGO.transform, false);
        
        Image iconImage = iconGO.AddComponent<Image>();
        iconImage.color = Color.white;
        
        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.sizeDelta = new Vector2(40, 40);
        iconRect.anchoredPosition = new Vector2(25, 0);
        
        tooltip.iconImage = iconImage;
        
        // Создаем заголовок
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(tooltipGO.transform, false);
        
        TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "Информация";
        titleText.fontSize = 16;
        titleText.color = Color.black;
        titleText.fontStyle = FontStyles.Bold;
        
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.5f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = new Vector2(80, 10);
        titleRect.offsetMax = new Vector2(-10, -10);
        
        tooltip.titleText = titleText;
        
        // Создаем описание
        GameObject descGO = new GameObject("Description");
        descGO.transform.SetParent(tooltipGO.transform, false);
        
        TextMeshProUGUI descText = descGO.AddComponent<TextMeshProUGUI>();
        descText.text = "Описание объекта";
        descText.fontSize = 12;
        descText.color = Color.black;
        
        RectTransform descRect = descGO.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0);
        descRect.anchorMax = new Vector2(1, 0.5f);
        descRect.offsetMin = new Vector2(10, 10);
        descRect.offsetMax = new Vector2(-10, -10);
        
        tooltip.descriptionText = descText;
    }
}
#endif 