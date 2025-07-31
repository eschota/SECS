using UnityEngine;

[CreateAssetMenu(fileName = "Animation_SO", menuName = "IO/Animation Settings")]
public class io_animation_SO : ScriptableObject



{
    [Header("Animation Type")]
    public io_base.io_type animation_type_current;
    
    [Header("Animation Curve")]
    public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [Header("Transform Targets")]
    public Vector3 targetScale = Vector3.one;
    public Vector3 targetPosition = Vector3.zero;
    public Quaternion targetRotation = Quaternion.identity;
    
    [Header("Material Targets")]
    public Color targetColor = Color.white;
    public Color32 targetEmissionColor = Color.black;
    
    [Header("Animation Duration")]
    public float duration = 0.5f;
} 