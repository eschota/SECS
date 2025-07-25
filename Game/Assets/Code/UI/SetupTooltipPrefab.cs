using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SetupTooltipPrefab : MonoBehaviour
{
    [ContextMenu("Setup ToolTipBase Prefab")]
    public void SetupTooltipPrefabMethod()
    {
#if UNITY_EDITOR
        // Загружаем существующий префаб
        GameObject tooltipPrefab = Resources.Load<GameObject>("UI/ToolTipBase");
        
        if (tooltipPrefab == null)
        {
            Debug.LogError("ToolTipBase prefab not found in Resources/UI/");
            return;
        }
        
        // Создаем временную копию для редактирования
        GameObject tempInstance = Instantiate(tooltipPrefab);
        
        // Добавляем компонент DynamicTooltip если его нет
        DynamicTooltip tooltip = tempInstance.GetComponent<DynamicTooltip>();
        if (tooltip == null)
        {
            tooltip = tempInstance.AddComponent<DynamicTooltip>();
            Debug.Log("Added DynamicTooltip component to ToolTipBase");
        }
        
        // Настраиваем размер RectTransform
        RectTransform rectTransform = tempInstance.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(300, 150);
            Debug.Log("Set RectTransform size to 300x150");
        }
        
        // Создаем UI элементы если их нет
        SetupTooltipUIElements(tempInstance, tooltip);
        
        // Сохраняем изменения в префаб
        PrefabUtility.SaveAsPrefabAsset(tempInstance, "Assets/Resources/UI/ToolTipBase.prefab");
        
        // Удаляем временную копию
        DestroyImmediate(tempInstance);
        
        // Обновляем AssetDatabase
        AssetDatabase.Refresh();
        
        Debug.Log("ToolTipBase prefab setup completed successfully!");
        
        // Выделяем префаб в Project window
        GameObject prefab = Resources.Load<GameObject>("UI/ToolTipBase");
        if (prefab != null)
        {
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
#else
        Debug.LogWarning("SetupTooltipPrefab can only be used in Unity Editor");
#endif
    }
    
    void SetupTooltipUIElements(GameObject tooltipGO, DynamicTooltip tooltip)
    {
        // Проверяем и создаем фон
        GameObject backgroundGO = tooltipGO.transform.Find("Background")?.gameObject;
        if (backgroundGO == null)
        {
            backgroundGO = new GameObject("Background");
            backgroundGO.transform.SetParent(tooltipGO.transform, false);
            
            Image backgroundImage = backgroundGO.AddComponent<Image>();
            backgroundImage.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
            
            RectTransform bgRect = backgroundGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            tooltip.backgroundImage = backgroundImage;
            Debug.Log("Created Background element");
        }
        else
        {
            Image backgroundImage = backgroundGO.GetComponent<Image>();
            if (backgroundImage != null)
            {
                tooltip.backgroundImage = backgroundImage;
            }
        }
        
        // Проверяем и создаем иконку
        GameObject iconGO = tooltipGO.transform.Find("Icon")?.gameObject;
        if (iconGO == null)
        {
            iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(tooltipGO.transform, false);
            
            Image iconImage = iconGO.AddComponent<Image>();
            iconImage.color = Color.white;
            
            RectTransform iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.sizeDelta = new Vector2(40, 40);
            iconRect.anchoredPosition = new Vector2(25, 0);
            
            tooltip.iconImage = iconImage;
            Debug.Log("Created Icon element");
        }
        else
        {
            Image iconImage = iconGO.GetComponent<Image>();
            if (iconImage != null)
            {
                tooltip.iconImage = iconImage;
            }
        }
        
        // Проверяем и создаем заголовок
        GameObject titleGO = tooltipGO.transform.Find("Title")?.gameObject;
        if (titleGO == null)
        {
            titleGO = new GameObject("Title");
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
            Debug.Log("Created Title element");
        }
        else
        {
            TextMeshProUGUI titleText = titleGO.GetComponent<TextMeshProUGUI>();
            if (titleText != null)
            {
                tooltip.titleText = titleText;
            }
        }
        
        // Проверяем и создаем описание
        GameObject descGO = tooltipGO.transform.Find("Description")?.gameObject;
        if (descGO == null)
        {
            descGO = new GameObject("Description");
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
            Debug.Log("Created Description element");
        }
        else
        {
            TextMeshProUGUI descText = descGO.GetComponent<TextMeshProUGUI>();
            if (descText != null)
            {
                tooltip.descriptionText = descText;
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SetupTooltipPrefab))]
public class SetupTooltipPrefabEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        SetupTooltipPrefab setupter = (SetupTooltipPrefab)target;
        
        if (GUILayout.Button("Setup ToolTipBase Prefab"))
        {
            setupter.SetupTooltipPrefabMethod();
        }
        
        EditorGUILayout.HelpBox("Нажмите кнопку выше, чтобы настроить префаб ToolTipBase с компонентом DynamicTooltip и всеми необходимыми UI элементами.", MessageType.Info);
    }
}
#endif 