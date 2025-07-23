using UnityEngine;
using UnityEngine.UI;

public class ship_ui_button : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private int targetStateIndex = 0;
    
    void Start()
    {
        // Если кнопка не назначена, пытаемся найти её на этом GameObject
        if (button == null)
        {
            button = GetComponent<Button>();
        }
        
        // Подписываемся на событие нажатия кнопки
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
        else
        {
            Debug.LogError("[ship_ui_button] Button component not found on GameObject: " + gameObject.name);
        }
    }
    
    private void OnButtonClick()
    {
        if (SHIP_UI.Instance != null)
        {
            // Получаем все состояния из enum
            SHIP_UI.State[] states = (SHIP_UI.State[])System.Enum.GetValues(typeof(SHIP_UI.State));
            
            // Проверяем, что индекс находится в допустимых пределах
            if (targetStateIndex >= 0 && targetStateIndex < states.Length)
            {
                SHIP_UI.State targetState = states[targetStateIndex];
                Debug.Log($"[ship_ui_button] Switching to state: {targetState} (index: {targetStateIndex})");
                SHIP_UI.Instance.SetState(targetState);
            }
            else
            {
                Debug.LogError($"[ship_ui_button] Invalid state index: {targetStateIndex}. Available states: 0-{states.Length - 1}");
            }
        }
        else
        {
            Debug.LogError("[ship_ui_button] SHIP_UI.Instance is null!");
        }
    }
    
    private void OnDestroy()
    {
        // Отписываемся от события при уничтожении объекта
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
    
    // Метод для программного изменения целевого состояния
    public void SetTargetState(int newStateIndex)
    {
        targetStateIndex = newStateIndex;
    }
    
    // Метод для получения текущего целевого состояния
    public int GetTargetState()
    {
        return targetStateIndex;
    }
} 