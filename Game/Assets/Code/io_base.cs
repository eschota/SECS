using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class io_base : MonoBehaviour
{
    public enum
    io_base_status
    {
        None = 0,
        Creating = 1,
        Deleting = 2,
        Moving = 3,
        Rotating = 4,
        Selected = 5,
        Hovered = 6,
        Dragging = 7,
        Placing = 8,
        Physics = 9,
        PhysicsToTargetPositions = 10,
        Hidden = 11,
        Intersected = 12
    }

    public List<io_base_status> status_list = new List<io_base_status>();
    [SerializeField] public int io_base_cell_type = 0;
    [SerializeField] public io_cell[] target_cells;
    [SerializeField] public Vector3 target_world_position;
    [SerializeField] public Quaternion target_world_rotation;

    [SerializeField] private List<io_base_SO> status_definitions = new List<io_base_SO>();

    [SerializeField] public Rigidbody targetRigidbody;

    private float statusTransitionTimer = 0f;
    private Dictionary<Renderer, Material> cellMaterials = new Dictionary<Renderer, Material>();
    private io_base_SO previousStatusSO;

    [SerializeField] private io_base_status _status = io_base_status.None;
    public io_base_status Status
    {
        get { return _status; }
        set
        {
            if (_status != value)
            {
                previousStatusSO = GetStatusSO(_status);
                _status = value;
                status_list.Add(value);
                statusTransitionTimer = 0f;

                var currentStatusSO = GetStatusSO(_status);
                if (currentStatusSO != null)
                {
                    targetRigidbody.isKinematic = currentStatusSO.isKinematic;
                    TurnColliders(currentStatusSO.collidersEnabled);

                    if (currentStatusSO.meshRenderer)
                    {
                        foreach (var cell in target_cells)
                        {
                            if(cell.target_mesh_renderer != null)
                            cell.target_mesh_renderer.enabled = currentStatusSO.meshRenderer;
                        }
                    }
                    
                    if (currentStatusSO.current_shader != null)
                    {
                        foreach (var entry in cellMaterials)
                        {
                            entry.Key.material.shader = currentStatusSO.current_shader;
                        }
                    }
                }
            }
        }
    }

    float localTimer = 0;

    void OnDestroy()
    {
        if (Creator.instance != null)
        {
            Creator.instance.cells.Remove(this);
        }
        foreach (var material in cellMaterials.Values)
        {
            Destroy(material);
        }
    }

    void OnValidate()
    {
        target_cells = GetComponentsInChildren<io_cell>().OrderBy(c => c.name).ToArray();
        status_definitions.Clear();
        status_definitions = Resources.LoadAll<io_base_SO>("Statuses").OrderBy(s => s.name).ToList();
        if (status_definitions.Count == 0)
        {
            Debug.LogWarning("No status definitions found in Resources/Statuses. Please create them and name them in order (e.g., 0_None, 1_Creating).");
        }
    }

    void Awake()
    {
        if (targetRigidbody == null) targetRigidbody = GetComponent<Rigidbody>();

       
        
        previousStatusSO = GetStatusSO(_status);
        if (previousStatusSO == null)
        {
            Debug.LogError($"Initial status '{_status}' could not find a corresponding ScriptableObject.", this);
        }
    }

    void Update()
    {
        localTimer += Time.deltaTime;

        var currentStatusSO = GetStatusSO(Status);

        float transitionDuration = currentStatusSO.transitionCurve.keys.Length > 1 ? currentStatusSO.transitionCurve.keys.Last().time : 1f;
        statusTransitionTimer += Time.deltaTime;

        float normalizedTime = (transitionDuration > 0) ? Mathf.Clamp01(statusTransitionTimer / transitionDuration) : 1f;
        float curveValue = currentStatusSO.transitionCurve.Evaluate(normalizedTime);

        if (targetRigidbody.isKinematic)
        {
            transform.position = Vector3.Lerp(transform.position, target_world_position, curveValue);
            transform.rotation = Quaternion.Lerp(transform.rotation, target_world_rotation, curveValue);
        }

        Vector3 baseScale = Vector3.Lerp(previousStatusSO.targetLocalScale, currentStatusSO.targetLocalScale, curveValue);

        // Lerp shader properties
        Color diffuseColor = Color.Lerp(previousStatusSO.targetDiffuseColor, currentStatusSO.targetDiffuseColor, curveValue);
        float emissive = Mathf.Lerp(previousStatusSO.selfEmissive, currentStatusSO.selfEmissive, curveValue);
        float transparency = Mathf.Lerp(previousStatusSO.transparency, currentStatusSO.transparency, curveValue);
        float wireframeToggle = Mathf.Lerp(previousStatusSO.wireframeToggle, currentStatusSO.wireframeToggle, curveValue);
        Color wireframeColor = Color.Lerp(previousStatusSO.wireframeColor, currentStatusSO.wireframeColor, curveValue);
        float wireframeThickness = Mathf.Lerp(previousStatusSO.wireframeThickness, currentStatusSO.wireframeThickness, curveValue);
        float smoothness = Mathf.Lerp(previousStatusSO.smoothness, currentStatusSO.smoothness, curveValue);

        foreach (var cell in target_cells)
        {
            Vector3 finalPosition = Vector3.Lerp(previousStatusSO.targetLocalPosition, currentStatusSO.targetLocalPosition, curveValue);
            Quaternion finalRotation = Quaternion.Lerp(previousStatusSO.targetLocalRotation, currentStatusSO.targetLocalRotation, curveValue);
            Vector3 finalScale = baseScale;

            if (currentStatusSO.targetPulse)
            {
                UpdatePulse(ref finalPosition, ref finalScale, ref finalRotation, currentStatusSO);
            }

            if (cell.target_mesh_renderer != null)
            {
                Color finalBaseColor = diffuseColor;
                finalBaseColor.a = transparency;

                var mat = cell.target_mesh_renderer.material;
                mat.SetColor("_BaseColor", finalBaseColor);
                mat.SetColor("_EmissionColor", diffuseColor * emissive);
                mat.SetFloat("_WireframeToggle", wireframeToggle);
                mat.SetColor("_WireframeColor", wireframeColor);
                mat.SetFloat("_WireframeThickness", wireframeThickness);
                mat.SetFloat("_Smoothness", smoothness);
            }
            // ВАЖНО НЕ ТРОГАЙ ЭТОТ КОД!!!
            cell.transform.localPosition = Vector3.Lerp(cell.transform.localPosition, cell.target_local_position, curveValue);
            cell.transform.localRotation = Quaternion.Lerp(cell.transform.localRotation, cell.target_local_rotation, curveValue);
            cell.transform.localScale = Vector3.Lerp(cell.transform.localScale, Vector3.one, curveValue);

          
        }
    }

    void FixedUpdate()
    {
        io_base_SO currentStatusSO = GetStatusSO(Status);
        if (currentStatusSO != null && !targetRigidbody.isKinematic)
        {
            if (currentStatusSO.gravityVector != Vector3.zero)
            {
                targetRigidbody.AddForce(currentStatusSO.gravityVector, currentStatusSO.gravityForceMode);
            }

            if (currentStatusSO.forceToTargetPosition > 0)
            {
                Vector3 directionToTarget = target_world_position - targetRigidbody.worldCenterOfMass;
                targetRigidbody.AddForce(directionToTarget.normalized * currentStatusSO.forceToTargetPosition, currentStatusSO.targetPositionForceMode);
            }
        }
    }

    private void UpdatePulse(ref Vector3 pos, ref Vector3 scale, ref Quaternion rot, io_base_SO so)
    {
        float pulseFactor = (Mathf.Sin(localTimer * so.pulseSpeed) + 1f) / 2f; // 0 to 1 sine wave
        pos = Vector3.Lerp(pos, so.targetPulseLocalPosition, pulseFactor);
        scale = Vector3.Lerp(scale, so.targetPulseLocalScale, pulseFactor);
        rot = Quaternion.Slerp(rot, so.targetPulseLocalRotation, pulseFactor);
    }
    
    private io_base_SO GetStatusSO(io_base_status s)
    {
        int index = (int)s;
        if (index >= 0 && index < status_definitions.Count)
        {
            return status_definitions[index];
        }
        Debug.LogWarning($"Status definition for {s} (index {index}) not found.");
        return null;
    }

    public void TurnColliders(bool value)
    {
        foreach (var cell in target_cells)
        {
            if (cell.target_collider != null)
            {
                cell.target_collider.enabled = value;
            }
        }
    }
}
