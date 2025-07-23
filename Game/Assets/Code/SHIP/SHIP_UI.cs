using UnityEngine;
using System;

public class SHIP_UI : MonoBehaviour
{
    public static SHIP_UI Instance { get; private set; }
    
    public static event Action<State> ChangeState;
    
    public enum State
    {
        _ship_state_editor_base,
        _ship_state_editor_main_module_0,
        _ship_state_editor_main_module_1,
        _ship_state_editor_main_module_2,
        _ship_state_editor_main_module_3,
        _ship_state_editor_main_module_4,
        _ship_state_editor_main_module_5,
        _ship_state_editor_main_module_6,
        _ship_state_editor_main_module_7,       
        _ship_state_space      // Корабль в режиме нахождения в космосе
    }
    
    [SerializeField] public State currentState;
    
    // Публичное свойство для доступа к текущему состоянию
    public State CurrentState => currentState;
    
    public void SetState(State state)
    {
        currentState = state;
        ChangeState?.Invoke(state);
        
        switch (state)
        {
            case State._ship_state_editor_base:
                OnEditorState();
                break;
            case State._ship_state_editor_main_module_0:
                OnEditorState();
                break;
            case State._ship_state_editor_main_module_1:
                OnEditorState();
                break;
            case State._ship_state_editor_main_module_2:
                OnEditorState();
                break;
        }
    }
    
    private void OnEditorState()
    {
        Debug.Log("[SHIP_UI] Переключение в режим редактора корабля");
        // TODO: Инициализация UI редактора корабля
        // - Показать панель инструментов строительства
        // - Активировать режим размещения блоков
        // - Показать инвентарь компонентов
    }
    
    private void OnSpaceState()
    {
        Debug.Log("[SHIP_UI] Переключение в режим космоса");
        // TODO: Инициализация UI космического режима
        // - Показать HUD корабля
        // - Активировать элементы управления полетом
        // - Скрыть элементы редактора
    }
    
    void Awake()
    {
        Instance = this;
        
        // Устанавливаем начальное состояние - редактор корабля
        SetState(State._ship_state_editor_base);
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
