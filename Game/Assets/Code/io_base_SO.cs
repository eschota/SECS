using UnityEngine;

[CreateAssetMenu(fileName = "new_status", menuName = "IO/Base Status")]
public class io_base_SO : ScriptableObject
{ 
    public bool isKinematic;

    public bool meshRenderer = true;
    public Vector3 targetLocalPosition;
    public Quaternion targetLocalRotation = Quaternion.identity;
    public Vector3 targetLocalScale = Vector3.one;
    
    // Physics & Collider properties
    public bool collidersEnabled = true;
    public Vector3 gravityVector = Vector3.zero;
    public ForceMode gravityForceMode = ForceMode.Force;
    public float forceToTargetPosition = 0f;
    public ForceMode targetPositionForceMode = ForceMode.Force;

    // Pulse properties
    public bool targetPulse = false;
    public float pulseSpeed = 1f;
    public Vector3 targetPulseLocalScale = Vector3.one;
    public Vector3 targetPulseLocalPosition = Vector3.zero;
    public Quaternion targetPulseLocalRotation = Quaternion.identity;
    
    // Material & Shader properties
    public Shader current_shader;
    [ColorUsage(true, true)] public Color targetDiffuseColor = Color.white;
    public float selfEmissive = 0f;
    public float transparency = 1f;
    [Range(0.0f, 1.0f)] public float wireframeToggle = 0.0f;
    [ColorUsage(true, true)] public Color wireframeColor = Color.black;
    public float wireframeThickness = 1.0f;
    [Range(0.0f, 1.0f)] public float smoothness = 0.5f;

    public AnimationCurve transitionCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
}
