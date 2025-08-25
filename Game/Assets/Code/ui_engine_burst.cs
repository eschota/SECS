using UnityEngine.UI;
using UnityEngine;

public class ui_engine_burst : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] Image engine_capacity;
    [SerializeField] float disable_speed = 1;
    [SerializeField] float capacity_speed = 0.25f;
    [SerializeField] float disable_time = 3;
    [Header("Colors")]
    [SerializeField] Color full_color = Color.green;
    [SerializeField] Color empty_color = Color.red;
    [SerializeField] Color full_overcharge_color = Color.yellow;
    
    void Start()
    {
        engine_capacity.fillAmount = 1;
        engine_capacity.color = full_color;
    }
    
    float DisableTimer = 0;
    bool isOverheated = false;
    float blinkTimer = 0;
    bool iconVisible = true;
    bool capacityVisible = true;
    
    // Для моргания при полном заполнении
    bool wasFullCapacity = true;
    float fullCapacityBlinkTimer = 0;
    int fullCapacityBlinkCount = 0;
    bool isFullCapacityBlinking = false;
    
    // Update is called once per frame
    void Update()
    {
        if (DisableTimer > 0)
        {
            DisableTimer -= Time.deltaTime*disable_speed;
            
            // Моргание иконки и engine_capacity при перегреве
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= 0.2f)
            {
                blinkTimer = 0;
                iconVisible = !iconVisible;
                capacityVisible = !capacityVisible;
                icon.enabled = iconVisible;
                engine_capacity.enabled = capacityVisible;
            }
            
            // Уменьшение цвета overcharge по таймеру
            float overchargeProgress = DisableTimer / disable_speed;
            engine_capacity.color = Color.Lerp(full_color, full_overcharge_color, overchargeProgress);
            
            return;            
        }
        
        // Сброс состояния перегрева
        if (isOverheated)
        {
            isOverheated = false;
            icon.enabled = true;
            engine_capacity.enabled = true;
            engine_capacity.fillAmount = 0;
        }
        
        if (Play.i?.currentState == Play.State.SimulateOnline)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                engine_capacity.fillAmount -= Time.deltaTime * capacity_speed;
                if (engine_capacity.fillAmount <= 0)
                {
                    DisableTimer = disable_time;
                    isOverheated = true;
                    engine_capacity.fillAmount = 1;
                    engine_capacity.color = full_overcharge_color;
                }
            }
            else
            {
                engine_capacity.fillAmount += Time.deltaTime * 0.05f; // В 2 раза медленнее
                if (engine_capacity.fillAmount >= 1)
                {
                    engine_capacity.fillAmount = 1;
                    
                    // Проверяем, только что ли заполнилась емкость
                    if (!wasFullCapacity && !isFullCapacityBlinking)
                    {
                        isFullCapacityBlinking = true;
                        fullCapacityBlinkCount = 0;
                        fullCapacityBlinkTimer = 0;
                    }
                }
            }
            
            // Лерпинг цвета от empty до full
            if (!isOverheated)
            {
                engine_capacity.color = Color.Lerp(empty_color, full_color, engine_capacity.fillAmount);
            }
            
            // Моргание при полном заполнении
            if (isFullCapacityBlinking)
            {
                fullCapacityBlinkTimer += Time.deltaTime;
                if (fullCapacityBlinkTimer >= 0.3f)
                {
                    fullCapacityBlinkTimer = 0;
                    fullCapacityBlinkCount++;
                    
                    if (fullCapacityBlinkCount >= 6) // 3 раза моргнуть = 6 переключений
                    {
                        isFullCapacityBlinking = false;
                        engine_capacity.enabled = true;
                    }
                    else
                    {
                        engine_capacity.enabled = !engine_capacity.enabled;
                    }
                }
            }
            
            wasFullCapacity = engine_capacity.fillAmount >= 1;
        }
    }
}
