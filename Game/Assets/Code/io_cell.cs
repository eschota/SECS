using UnityEngine;

public class io_cell : MonoBehaviour
{
    [SerializeField] public bool possible_to_place = true;
    [SerializeField] public Collider target_collider; 
    [SerializeField] public Transform target_transform;
    [SerializeField] public io_base target_io_base;
    [SerializeField] public MeshRenderer target_mesh_renderer;

    [SerializeField] public Quaternion target_local_rotation = Quaternion.identity;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public Vector3 target_local_position = Vector3.zero;
    void OnValidate()
    {
        target_collider = GetComponent<Collider>();
        target_transform = GetComponent<Transform>();
        if (target_collider.isTrigger == true) target_collider.isTrigger = false;
        if (target_io_base == null)
            target_io_base = GetComponentInParent<io_base>();
        transform.position = new Vector3(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), Mathf.RoundToInt(transform.position.z));
        
    }
    void Awake()
    {
        target_local_position = target_transform.localPosition;
        target_local_rotation = target_transform.localRotation;
    }

    void OnDrawGizmos()
    {
        if(Play.i?.currentState == Play.State.SimulateLocal) return;
        // draw cube by possible to place color
        if (possible_to_place)
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = Color.red;
        }
        transform.localPosition = new Vector3(Mathf.RoundToInt(transform.localPosition.x), Mathf.RoundToInt(transform.localPosition.y), Mathf.RoundToInt(transform.localPosition.z));
        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.1f);
        Gizmos.DrawCube(transform.localPosition, Vector3.one);
    }
    // Update is called once per frame
    
}
