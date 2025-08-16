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
        target_io_base = GetComponent<io_base>();

        target_io_base = GetComponentInParent<io_base>();
    }
    void Awake()
    {
        target_local_position = target_transform.localPosition;
        target_local_rotation = target_transform.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
