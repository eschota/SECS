using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using UnityEngine.SceneManagement;


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
        LoadUI();
    }
    void Update()
    {
        // Проверяем состояние Play - если в режиме симуляции, не выполняем создание
        if (_play != null && _play.currentState == Play.State.Create)
        {
            CreateCell(); 
        }
    }
    public List<io_base> prefabs = new List<io_base>();
    [SerializeField] public cam _cam;
    [SerializeField] public Play _play;
    public List<io_base> cells = new List<io_base>();
    [SerializeField] private List<Shader> _shaders = new List<Shader>();
    [SerializeField] private List<io_base> _prefabs = new List<io_base>();
    [SerializeField] private List<io_base_SO> _statuses = new List<io_base_SO>();
    [SerializeField] private List<Material> _materials = new List<Material>();
    public bool SnapGrid = true;
    private io_base _current_prefab;
    public io_base current_prefab_to_chabge; 
    public io_base current_prefab
    {
        get
        {
            if (_current_prefab == null)
            {
                
                var a = Instantiate(current_prefab_to_chabge, transform);
                _current_prefab = a;
                _current_prefab.transform.position = _cam.target_pivot_position;
                _current_prefab.name = "Current Create";
                // Убеждаемся, что текущий префаб остается в иерархии Creator
                _current_prefab.transform.SetParent(transform);
                _current_prefab.Status = io_base.io_base_status.Creating;
                cells.Add(_current_prefab);
            }
            return _current_prefab;
        }
        set
        {
            if (value != null)
            { 
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
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit2, 1000, layer_mask))
            {
                return;
            }


            // Создание клетки при нажатии
            if (current_prefab != null && current_prefab.Status != io_base.io_base_status.Intersected)
            {
                AllActions?.Invoke(ActionType.Create, current_prefab);

                // Убеждаемся, что клетка остается в иерархии Creator, а не попадает в пивот
                current_prefab.transform.SetParent(transform);

                // Смещаем пивот к точке создания клетки
                _cam.target_pivot_position = current_prefab.transform.position;
                current_prefab.Status = io_base.io_base_status.Placing;
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
                var cell_to_check = hit.collider.GetComponent<io_cell>();
                if(!cell_to_check.possible_to_place)
                {
                    current_prefab.Status = io_base.io_base_status.Hidden;
                    return;
                }


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
                //find cell of collider
                

              
                current_prefab.target_world_rotation = Quaternion.LookRotation(direction);
                // snap to 90 degrees
                current_prefab.target_world_rotation = Quaternion.Euler(Mathf.Round(current_prefab.target_world_rotation.eulerAngles.x / 90) * 90, Mathf.Round(current_prefab.target_world_rotation.eulerAngles.y / 90) * 90, Mathf.Round(current_prefab.target_world_rotation.eulerAngles.z / 90) * 90);
                // check intersections 
                foreach (var cell in current_prefab.target_cells)
                {
                    // Применяем поворот к локальной позиции клетки
                    Vector3 rotatedLocalPosition = current_prefab.target_world_rotation * cell.target_local_position;
                    Vector3 worldCellPosition = current_prefab.target_world_position + rotatedLocalPosition;

                    foreach (var b in cells)
                    {
                        if (b != current_prefab)
                            foreach (var cell2 in b.target_cells)
                            {
                                Vector3 rotatedLocalPosition2 = b.target_world_rotation * cell2.target_local_position;
                                Vector3 worldCellPosition2 = b.target_world_position + rotatedLocalPosition2;
                                if ((worldCellPosition - worldCellPosition2).sqrMagnitude < 0.1f)
                                {
                                    current_prefab.Status = io_base.io_base_status.Intersected;
                                    return;
                                }
                            }

                    }
                }
                current_prefab.Status = io_base.io_base_status.Creating;

            }
            else
            {
                //  current_prefab.Status = io_base.io_base_status.Hidden;
            }
        }
        else
        {
            if (current_prefab.Status != io_base.io_base_status.Intersected)
                current_prefab.Status = io_base.io_base_status.Hidden;
            // Если луч не попал никуда, можно скрыть объект или разместить в дефолтной позиции

        }
    }

    void LoadPrefabs()
    {
        var prefabs_list = Resources.LoadAll<io_base>("Create");
        foreach (var prefab in prefabs_list)
        {
            prefabs.Add(prefab);
        }
        current_prefab_to_chabge = prefabs[0];
    }
    void CreateCameraWitPivot()
    {

        _cam = gameObject.AddComponent<cam>();
        _play = gameObject.AddComponent<Play>();
    }
    public Vector3 current_prefab_position = Vector3.zero;
     
    void PlacePrefabs()
    {
        foreach (var prefab in prefabs)
        {
            var new_prefab = Instantiate(prefab, transform);
            cells.Add(new_prefab);
            new_prefab.Status = io_base.io_base_status.Placing;
            return;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        _shaders.Clear();
        var shaders_in_folder = Resources.LoadAll<Shader>("Shaders");
        if (_shaders.Count != shaders_in_folder.Length)
        {
            foreach (var shader in shaders_in_folder)
            {
                if (!_shaders.Contains(shader))
                {
                    _shaders.Add(shader);
                }
            }
        }

        _prefabs.Clear();
        var prefabs_in_folder = Resources.LoadAll<io_base>("Create");
        if (_prefabs.Count != prefabs_in_folder.Length)
        {
            foreach (var prefab in prefabs_in_folder)
            {
                if (!_prefabs.Contains(prefab))
                {
                    _prefabs.Add(prefab);
                }
            }
        }

        _statuses.Clear();
        var statuses_in_folder = Resources.LoadAll<io_base_SO>("Statuses");
        if (_statuses.Count != statuses_in_folder.Length)
        {
            foreach (var status in statuses_in_folder)
            {
                if (!_statuses.Contains(status))
                {
                    _statuses.Add(status);
                }
            }
        }
        
        _materials.Clear();
        var materials_in_folder = Resources.LoadAll<Material>("mats");
        if (_materials.Count != materials_in_folder.Length)
        {
            foreach (var material in materials_in_folder)
            {
                if (!_materials.Contains(material))
                {
                    _materials.Add(material);
                }
            }
        }
    }
#endif
    private void LoadUI()
    {
        // load scene UI additively
        SceneManager.LoadScene("UI", LoadSceneMode.Additive);
        StartCoroutine(WaitForUIAndDestroy());
    }

    private System.Collections.IEnumerator WaitForUIAndDestroy()
    {
        // Ждем пока UI_Canvas инициализируется
        while (UI_Canvas.i == null)
        {
            yield return null;
        }

        // Теперь безопасно удаляем камеру UI
        if (UI_Canvas.i.ui_camera != null)
        {
            Destroy(UI_Canvas.i.ui_camera.gameObject);
        }
        UI_Canvas.SubTypeSelected += OnSubTypeSelected; 
    }

    private void OnSubTypeSelected(UI_Button button)
    {
        Debug.Log("OnSubTypeSelected: " + button.name);
        current_prefab_to_chabge = button.Item.prefab;
        current_prefab_position = current_prefab.transform.position;
        Destroy(current_prefab.gameObject);  
        current_prefab = null;
    }
    void OnDestroy()
    {
        UI_Canvas.SubTypeSelected -= OnSubTypeSelected;
    }
    
  
}
