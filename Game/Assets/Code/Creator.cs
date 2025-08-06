using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using TMPro.EditorUtilities;

public class Creator : MonoBehaviour
{
    public static Creator instance;
    public enum ActionType
    {
        Create,
        Delete,
        Move,
        Rotate,
    }
    public static event Action<ActionType, io_base> AllActions;
    void Start()
    {

    }
    void Awake()
    {
        instance = this;
        LoadPrefabs();
        PlacePrefabs();
        CreateCameraWitPivot();
    }
    void Update()
    {
        // Проверяем состояние Play - если в режиме симуляции, не выполняем создание
        if (_play != null && _play.currentState == Play.GameState.stateCreate)
        {
            CreateCell();
            ChangeCurrentCellType();
        }
    }
    public List<io_base> prefabs = new List<io_base>();
    [SerializeField] public cam _cam;
    [SerializeField] public Play _play;
    public List<io_base> cells = new List<io_base>();
    public bool SnapGrid = true;
    private io_base _current_prefab;
    public int current_prefab_index = 0;
    public io_base current_prefab
    {
        get
        {
            if (_current_prefab == null)
            {
                var a = Instantiate(prefabs[current_prefab_index], transform);
                _current_prefab = a;
                _current_prefab.transform.position = current_prefab_position;
                _current_prefab.name = "Current Create";
                _current_prefab.TurnColliders(false);
                // Убеждаемся, что текущий префаб остается в иерархии Creator
                _current_prefab.transform.SetParent(transform);
                cells.Add(_current_prefab);
            }
            return _current_prefab;
        }
        set
        {
            if (value != null)
            {
                current_prefab_index = value.io_base_cell_type;
                _current_prefab = null;
            }
            else
            {
                _current_prefab = null;
            }
            
        }
    }
    void CreateCell()
    {
        LayerMask layer_mask = LayerMask.GetMask("io_base");

        // Обработка нажатия кнопки мыши
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Mouse button down detected!");

            // Создание клетки при нажатии
            if (current_prefab != null)
            {
                current_prefab.speed = 10;
                AllActions?.Invoke(ActionType.Create, current_prefab);

                // Убеждаемся, что клетка остается в иерархии Creator, а не попадает в пивот
                current_prefab.transform.SetParent(transform); 

                // Смещаем пивот к точке создания клетки
                _cam.target_pivot_position = current_prefab.transform.position;
                current_prefab.TurnColliders(true); 
                current_prefab.targetRigidbody.isKinematic = true;           
                
                current_prefab = null;
            }
        }

        // Обработка отпускания кнопки мыши
        if (Input.GetMouseButtonUp(0))
        {
            Debug.Log("Mouse button up detected!");
        }

        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 1000, layer_mask))
        {
            if (SnapGrid)
            {
                // Определяем направление касания (normal)
                Vector3 hitNormal = hit.normal;

                // Получаем позицию центра коллайдера объекта, который мы касаемся
                // Коллайдер гарантированно стоит в сетке 1x1x1
                Vector3 colliderCenter = hit.collider.transform.position;

                // Вычисляем смещение в зависимости от направления касания
                Vector3 offset = Vector3.zero;

                // Определяем, с какой стороны куба мы касаемся
                if (Mathf.Abs(hitNormal.x) > 0.5f) // Касаемся боковой грани по X
                {
                    offset.x = hitNormal.x > 0 ? 1f : -1f;
                }
                else if (Mathf.Abs(hitNormal.y) > 0.5f) // Касаемся грани по Y
                {
                    offset.y = hitNormal.y > 0 ? 1f : -1f;
                }
                else if (Mathf.Abs(hitNormal.z) > 0.5f) // Касаемся грани по Z
                {
                    offset.z = hitNormal.z > 0 ? 1f : -1f;
                }

                // Размещаем объект в позиции, кратной 1x1x1, относительно центра коллайдера
                Vector3 targetPosition = colliderCenter + offset;
                targetPosition = new Vector3(Mathf.Round(targetPosition.x), Mathf.Round(targetPosition.y), Mathf.Round(targetPosition.z));
                current_prefab.target_world_position = targetPosition;
                // check direction of hitNormal and rotate current_prefab to this direction
                Vector3 direction = hitNormal;
                current_prefab.target_world_rotation = Quaternion.LookRotation(direction);
                // snap to 90 degrees
                current_prefab.target_world_rotation = Quaternion.Euler(Mathf.Round(current_prefab.target_world_rotation.eulerAngles.x / 90) * 90, Mathf.Round(current_prefab.target_world_rotation.eulerAngles.y / 90) * 90, Mathf.Round(current_prefab.target_world_rotation.eulerAngles.z / 90) * 90);

                current_prefab.speed = 20;
            }
            else
            {
                //current_prefab.target_transform.position = hit.point;
            }
        }
        else
        {
            // Если луч не попал никуда, можно скрыть объект или разместить в дефолтной позиции
            //current_prefab.target_transform.position = Vector3.zero;
        }
    }
    
void LoadPrefabs()
{
    var prefabs_list = Resources.LoadAll<io_base>("Create");
    foreach (var prefab in prefabs_list)
    {
        prefabs.Add(prefab);
    }
}
    void CreateCameraWitPivot()
    {

        _cam = gameObject.AddComponent<cam>();
        _play = gameObject.AddComponent<Play>();
    }
    public Vector3 current_prefab_position  = Vector3.zero;
    public void ChangeCurrentCellType()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (current_prefab_index != 0)
            {
                current_prefab_position = current_prefab.transform.position;
                Destroy(current_prefab.gameObject);
                current_prefab_index = 0;
                Destroy(current_prefab);
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            current_prefab_position = current_prefab.transform.position;
            if (current_prefab_index != 1)
            {
                Destroy(current_prefab.gameObject);
                current_prefab_index = 1;
                Destroy(current_prefab);
            }
        }
    }
     
void PlacePrefabs()
{
        foreach (var prefab in prefabs)
        {
            var new_prefab = Instantiate(prefab, transform);  
            cells.Add(new_prefab);
            return;
        }       
    }
}
