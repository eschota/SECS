using UnityEngine;
using System;
public class io_base : MonoBehaviour
{

    [SerializeField] public int io_base_cell_type = 0;
    [SerializeField] public io_cell[] target_cells;
    [SerializeField] public Vector3 target_world_position;
    [SerializeField] public Quaternion target_world_rotation;
    [SerializeField] public float speed = 1;

    [SerializeField] public Rigidbody targetRigidbody;
    void OnDestroy()
    {
        Creator.instance.cells.Remove(this);
    }
    void OnValidate()
    {        
        target_cells = GetComponentsInChildren<io_cell>();
    }

    void Awake()
    {
        targetRigidbody.useGravity = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (targetRigidbody.isKinematic == false) return;
        transform.position = Vector3.Lerp(transform.position, target_world_position, Time.deltaTime * speed);
        foreach (var cell in target_cells)
        {
            cell.transform.localPosition = Vector3.Lerp(cell.transform.localPosition, cell.target_local_position, Time.deltaTime * speed);
            cell.transform.localRotation = Quaternion.Lerp(cell.transform.localRotation, cell.target_local_rotation, Time.deltaTime * speed);
        }
        transform.rotation = Quaternion.Lerp(transform.rotation, target_world_rotation, Time.deltaTime * speed);
        foreach (var cell in target_cells)
        {
            cell.transform.localRotation = Quaternion.Lerp(cell.transform.localRotation, cell.target_local_rotation, Time.deltaTime * speed);
        }
    }
    public void TurnColliders(bool value)
    {
        foreach (var cell in target_cells)
        {
            cell.target_collider.enabled = value;
        }
    }   
}
