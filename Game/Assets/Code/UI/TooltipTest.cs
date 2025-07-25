using UnityEngine;

public class TooltipTest : MonoBehaviour
{
    void Start()
    {
        // Создаем TooltipManager если его нет
        if (TooltipManager.Instance == null)
        {
            GameObject tooltipManagerGO = new GameObject("TooltipManager");
            tooltipManagerGO.AddComponent<TooltipManager>();
            Debug.Log("TooltipManager created from TooltipTest");
        }
    }
    
    void Update()
    {
        // Тестируем тултип по нажатию клавиши T
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (TooltipManager.Instance != null && TooltipManager.Instance.tooltip != null)
            {
                TooltipManager.Instance.tooltip.Show("Test Object", "Selected Object");
                Debug.Log("Tooltip test activated");
            }
        }
        
        // Скрываем тултип по нажатию клавиши H
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (TooltipManager.Instance != null && TooltipManager.Instance.tooltip != null)
            {
                TooltipManager.Instance.tooltip.Hide();
                Debug.Log("Tooltip hidden");
            }
        }
    }
} 