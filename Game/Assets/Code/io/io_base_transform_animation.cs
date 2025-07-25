using UnityEngine;

public class io_base_transform_animation : MonoBehaviour
{
    public float duration = 0.5f;
    public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);

    public Vector3 targetScale;
    public Vector3 targetPosition;
    public Quaternion targetRotation;
    public Color targetColor;
    public Color32 targetEmissionColor;
}
