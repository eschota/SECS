using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(io_base))]
public class io_base_editor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        io_base ioBase = (io_base)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Управление анимациями", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Обновить анимации", GUILayout.Height(30)))
        {
            ioBase.RefreshAnimations();
        }
        
        if (GUILayout.Button("Очистить список анимаций", GUILayout.Height(25)))
        {
            SerializedProperty stateAnimationsProp = serializedObject.FindProperty("stateAnimations");
            stateAnimationsProp.ClearArray();
            serializedObject.ApplyModifiedProperties();
            Debug.Log("Список анимаций очищен!");
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Анимации автоматически назначаются по типу animation_type_current компонентов io_base_transform_animation", MessageType.Info);
    }
} 