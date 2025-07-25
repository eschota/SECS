using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(io_base))]
public class io_base_editor : Editor
{
    public override void OnInspectorGUI()
    {
        io_base ioBase = (io_base)target;
        
        // Рисуем стандартные поля
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("State Animations", EditorStyles.boldLabel);
        
        // Получаем сериализованное свойство для stateAnimations
        SerializedProperty stateAnimationsProp = serializedObject.FindProperty("stateAnimations");
        
        if (stateAnimationsProp != null)
        {
            EditorGUI.indentLevel++;
            
            // Показываем текущие настройки анимаций
            for (int i = 0; i < stateAnimationsProp.arraySize; i++)
            {
                SerializedProperty element = stateAnimationsProp.GetArrayElementAtIndex(i);
                SerializedProperty stateProp = element.FindPropertyRelative("state");
                SerializedProperty animationProp = element.FindPropertyRelative("animation");
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(stateProp, GUIContent.none, GUILayout.Width(100));
                EditorGUILayout.PropertyField(animationProp, GUIContent.none);
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space();
            
            // Кнопка для добавления новой анимации
            if (GUILayout.Button("Add State Animation"))
            {
                stateAnimationsProp.arraySize++;
                serializedObject.ApplyModifiedProperties();
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }
} 