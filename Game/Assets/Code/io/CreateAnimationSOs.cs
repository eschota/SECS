using UnityEngine;
using UnityEditor;

public class CreateAnimationSOs : MonoBehaviour
{
    [MenuItem("IO/Create Animation SOs")]
    public static void CreateAllAnimationSOs()
    {
        // Создаем папку если её нет
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Settings"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Settings");
        }

        // Создаем SO для каждого типа анимации
        CreateAnimationSO("Animation_Off_SO", io_base.io_type.off);
        CreateAnimationSO("Animation_On_SO", io_base.io_type.on);
        CreateAnimationSO("Animation_Toggle_SO", io_base.io_type.toggle);
        CreateAnimationSO("Animation_MouseOver_SO", io_base.io_type.mouseOver);
        CreateAnimationSO("Animation_Selected_SO", io_base.io_type.selected);
        CreateAnimationSO("Animation_Clicked_SO", io_base.io_type.clicked);
        CreateAnimationSO("Animation_Deselected_SO", io_base.io_type.deselected);
        CreateAnimationSO("Animation_Drag_SO", io_base.io_type.drag);
        CreateAnimationSO("Animation_FloorUp_SO", io_base.io_type.floor_up);
        CreateAnimationSO("Animation_FloorDown_SO", io_base.io_type.floor_down);
        CreateAnimationSO("Animation_ToRemove_SO", io_base.io_type.ToRemove);

        AssetDatabase.Refresh();
        Debug.Log("All Animation SOs created successfully!");
    }

    private static void CreateAnimationSO(string name, io_base.io_type type)
    {
        string path = $"Assets/Resources/Settings/{name}.asset";
        
        // Проверяем, существует ли уже файл
        if (AssetDatabase.LoadAssetAtPath<io_animation_SO>(path) != null)
        {
            Debug.Log($"SO file {name} already exists, skipping...");
            return;
        }

        io_animation_SO so = ScriptableObject.CreateInstance<io_animation_SO>();
        so.animation_type_current = type;
        
        // Устанавливаем разные параметры в зависимости от типа
        switch (type)
        {
            case io_base.io_type.off:
                so.targetScale = Vector3.one;
                so.targetColor = Color.gray;
                so.curve = AnimationCurve.Linear(0, 0, 1, 1);
                break;
            case io_base.io_type.on:
                so.targetScale = Vector3.one;
                so.targetColor = Color.white;
                so.curve = AnimationCurve.Linear(0, 0, 1, 1);
                break;
            case io_base.io_type.mouseOver:
                so.targetScale = Vector3.one * 1.1f;
                so.targetColor = Color.yellow;
                so.curve = AnimationCurve.Linear(0, 0, 1, 1);
                break;
            case io_base.io_type.selected:
                so.targetScale = Vector3.one * 1.2f;
                so.targetColor = Color.blue;
                so.curve = AnimationCurve.Linear(0, 0, 1, 1);
                break;
            case io_base.io_type.clicked:
                so.targetScale = Vector3.one * 0.9f;
                so.targetColor = Color.green;
                so.curve = AnimationCurve.Linear(0, 0, 1, 1);
                break;
            case io_base.io_type.ToRemove:
                so.targetScale = Vector3.zero;
                so.targetColor = new Color(1, 0, 0, 0);
                so.curve = AnimationCurve.Linear(0, 0, 1, 1);
                break;
            default:
                so.targetScale = Vector3.one;
                so.targetColor = Color.white;
                so.curve = AnimationCurve.Linear(0, 0, 1, 1);
                break;
        }

        AssetDatabase.CreateAsset(so, path);
        Debug.Log($"Created {name} at {path}");
    }
} 