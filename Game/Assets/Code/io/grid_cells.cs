using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class grid_cells : MonoBehaviour
{
    float timer_to_show_grid_cells = 3;
    [SerializeField] int grid_size;
    [SerializeField] io_base cell;
    [SerializeField] io_base_stair stair_cell;

    [SerializeField]List<io_base> grid_cells_list;

    [ContextMenu("Create Grid Cell")]
    private void CreateGridCell()
    {
        clear_all_grid_cells();
        for (int i = 0; i < grid_size; i++)
        {
            for (int j = 0; j < grid_size; j++)
            {
                io_base io = Instantiate(cell, new Vector3(i - grid_size / 2, 0, j - grid_size / 2), Quaternion.identity).GetComponent<io_base>();
                io.Init(transform);
                io.name = "Grid Cell " + i + " " + j;
                
                io.target_collider.gameObject.name = "Cell_Collider " + i + " " + j;
                grid_cells_list.Add(io);
            }
        }
     
    }
float max_distance_from_center;
    void Awake()
    {
        CreateGridCell();
        max_distance_from_center = 0;
        for (int i = 0; i < grid_cells_list.Count; i++)
        {
            float distance = Vector3.Distance(grid_cells_list[i].transform.position, transform.position);
            if (distance > max_distance_from_center)
            {
                max_distance_from_center = distance;
            }
        }
    }
    float local_timer = 0;

    void Update()
    {
        if(local_timer > timer_to_show_grid_cells) return;
        local_timer += Time.deltaTime;

        // Вычисляем текущую дистанцию волны от центра
        float wave_distance = (local_timer / timer_to_show_grid_cells) * max_distance_from_center;
        
        // Список клеток для удаления
        List<io_base> cells_to_remove = new List<io_base>();
        
        for (int i = 0; i < grid_cells_list.Count; i++)
        {
            // Вычисляем расстояние от центра до текущей клетки
            float cell_distance = Vector3.Distance(grid_cells_list[i].transform.position, transform.position);
            
            // Если волна дошла до этой клетки и она еще не активирована
            if (wave_distance >= cell_distance && !grid_cells_list[i].io_type_stack.Contains(io_base.io_type.on))
            {
                grid_cells_list[i].io_type_stack.Remove(io_base.io_type.off);
                grid_cells_list[i].io_type_stack.Add(io_base.io_type.on);
                
                // Добавляем клетку в список для удаления
                cells_to_remove.Add(grid_cells_list[i]);
            }
        }
        
        // Удаляем активированные клетки из основного списка
        foreach (var cell in cells_to_remove)
        {
            grid_cells_list.Remove(cell);
        }
    }
    private void clear_all_grid_cells()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        grid_cells_list.Clear();
    }
}


