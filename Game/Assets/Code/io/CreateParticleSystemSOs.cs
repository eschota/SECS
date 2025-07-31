using UnityEngine;
using UnityEditor;

public class CreateParticleSystemSOs : MonoBehaviour
{
    [MenuItem("IO/Create Particle System SOs")]
    public static void CreateAllParticleSystemSOs()
    {
        // Создаем папку если её нет
        if (!AssetDatabase.IsValidFolder("Assets/Resources/settings/particle_systems_SO"))
        {
            AssetDatabase.CreateFolder("Assets/Resources/settings", "particle_systems_SO");
        }

        // Создаем SO для каждого типа клетки
        CreateParticleSystemSO("ParticleSystem_cell_SO", io_base.io_base_cell_type.cell);
        CreateParticleSystemSO("ParticleSystem_stair_SO", io_base.io_base_cell_type.stair);
        CreateParticleSystemSO("ParticleSystem_space_SO", io_base.io_base_cell_type.space);

        AssetDatabase.Refresh();
        Debug.Log("All Particle System SOs created successfully!");
    }

    private static void CreateParticleSystemSO(string name, io_base.io_base_cell_type cellType)
    {
        string path = $"Assets/Resources/settings/particle_systems_SO/{name}.asset";
        
        // Проверяем, существует ли уже файл
        if (AssetDatabase.LoadAssetAtPath<particle_system_SO>(path) != null)
        {
            Debug.Log($"SO file {name} already exists, skipping...");
            return;
        }

        particle_system_SO so = ScriptableObject.CreateInstance<particle_system_SO>();
        so.cellType = cellType;
        
        // Устанавливаем разные параметры в зависимости от типа клетки
        switch (cellType)
        {
            case io_base.io_base_cell_type.cell:
                so.startColor = Color.blue;
                so.startSpeed = new Vector2(3f, 3f);
                so.rateOverTime = new Vector2(15f, 15f);
                so.startLifetime = new Vector2(1.5f, 1.5f);
                break;
            case io_base.io_base_cell_type.stair:
                so.startColor = Color.green;
                so.startSpeed = new Vector2(5f, 5f);
                so.rateOverTime = new Vector2(20f, 20f);
                so.startLifetime = new Vector2(2f, 2f);
                break;
            case io_base.io_base_cell_type.space:
                so.startColor = Color.red;
                so.startSpeed = new Vector2(2f, 2f);
                so.rateOverTime = new Vector2(10f, 10f);
                so.startLifetime = new Vector2(1f, 1f);
                break;
        }

        AssetDatabase.CreateAsset(so, path);
        Debug.Log($"Created {name} at {path}");
    }
} 