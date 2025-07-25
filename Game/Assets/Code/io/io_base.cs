using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

[System.Serializable]
public struct StateAnimation
{
    public io_base.io_type state;
    public io_base_transform_animation animation;
}

[RequireComponent(typeof(Transform))]
public class io_base : MonoBehaviour
{
    [SerializeField] Transform target_transform;
    [SerializeField] public Collider target_collider;
    public int floor = 0;
    Vector3 originalScale;
    Vector3 originalPosition;
    quaternion originalRotation;
    [SerializeField] public MeshRenderer[] target_mesh_renderer;

    [SerializeField] private List<StateAnimation> stateAnimations = new List<StateAnimation>();
    [SerializeField] private io_base_transform_animation defaultAnimation;

    public float localTimer = 0;
    
    // ObservableCollection автоматически уведомляет об изменениях
    private ObservableCollection<io_type> _io_type_stack;
    
    // Публичное свойство для доступа к списку
    public ObservableCollection<io_type> io_type_stack
    {
        get { return _io_type_stack; }
    }

    public enum io_type
    {
        off,
        on,
        toggle,
        mouseOver,
        selected,
        clicked,
        drag,
        floor_up,
        floor_down
    }

    protected virtual void Awake()
    {
        
    }

    public virtual void Init(Transform parent)
    {
        transform.parent = parent;
        // Инициализируем ObservableCollection с обработчиком изменений
        _io_type_stack = new ObservableCollection<io_type>();
        _io_type_stack.CollectionChanged += (sender, e) => 
        {
            // Обнуляем таймер при любом изменении списка
            localTimer = 0;
        };
        
        _io_type_stack.Add(io_type.off); 
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        
        // Инициализируем target_transform только если есть анимации
        if (stateAnimations != null && stateAnimations.Count > 0 && stateAnimations[0].animation != null)
        {
            target_transform.localScale = stateAnimations[0].animation.targetScale;
            target_transform.localPosition = stateAnimations[0].animation.targetPosition;
        }
        else
        {
            // Если анимаций нет, используем текущие значения
            target_transform.localScale = originalScale;
            target_transform.localPosition = originalPosition;
        }
    }
    // Update is called once per frame
    void Update()
    {
        
        
        io_base_transform_animation current_animation = GetAnimationForState();
        if (current_animation == null) return;

        localTimer += Time.deltaTime;
        //clamp minimal localScale to 0.01
        target_transform.localScale = Vector3.Lerp(target_transform.localScale, current_animation.targetScale, current_animation.curve.Evaluate(localTimer / current_animation.duration));
        target_transform.localScale = new Vector3(Mathf.Max(target_transform.localScale.x, 0.01f), Mathf.Max(target_transform.localScale.y, 0.01f), Mathf.Max(target_transform.localScale.z, 0.01f));
        target_transform.localPosition = Vector3.Lerp(target_transform.localPosition, current_animation.targetPosition, current_animation.curve.Evaluate(localTimer / current_animation.duration));
        // Validate quaternions before interpolation to avoid assertion errors
        Quaternion startRotation = originalRotation;
        Quaternion endRotation = current_animation.targetRotation;
        for (int i = 0; i < target_mesh_renderer.Length; i++)
        {
            target_mesh_renderer[i].material.color = Color.Lerp(target_mesh_renderer[i].material.color, current_animation.targetColor, current_animation.curve.Evaluate(localTimer / current_animation.duration));
            target_mesh_renderer[i].material.SetColor("_EmissionColor", Color32.Lerp(target_mesh_renderer[i].material.GetColor("_EmissionColor"), current_animation.targetEmissionColor, current_animation.curve.Evaluate(localTimer / current_animation.duration)));
        }
        // Check if quaternions are valid (not zero magnitude)
        if (startRotation.x == 0 && startRotation.y == 0 && startRotation.z == 0 && startRotation.w == 0)
        {
            startRotation = Quaternion.identity;
        }
        if (endRotation.x == 0 && endRotation.y == 0 && endRotation.z == 0 && endRotation.w == 0)
        {
            endRotation = Quaternion.identity;
        }
        
        target_transform.localRotation = Quaternion.Slerp(startRotation, endRotation, current_animation.curve.Evaluate(localTimer / current_animation.duration));
    }

    private io_base_transform_animation GetAnimationForState()
    {
        if (stateAnimations == null || stateAnimations.Count == 0)
        {
            return defaultAnimation;
        }

        var currentState = _io_type_stack.LastOrDefault();
        var stateAnimation = stateAnimations.FirstOrDefault(sa => sa.state == currentState);
        
        return stateAnimation.animation != null ? stateAnimation.animation : defaultAnimation;
    }
}