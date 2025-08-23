using UnityEngine;

public class UI_rotate_button : MonoBehaviour
{
    [SerializeField] CanvasGroup canvas_group;
    void Start()
    {
        
    }

    float local_timer = -1;
    void Update()
    {
        local_timer += Time.deltaTime;
        if (Play.i?.currentState == Play.State.Create && local_timer > 0)
        {
            if (Creator.instance.current_prefab.Status == io_base.io_base_status.Creating
                || Creator.instance.current_prefab.Status == io_base.io_base_status.Intersected)
            {
                canvas_group.alpha = Mathf.Lerp(canvas_group.alpha, 1, Time.deltaTime * 10);
                return;
            }
            else
            {
                local_timer = -1;
                canvas_group.alpha = Mathf.Lerp(canvas_group.alpha, 0, Time.deltaTime * 10);
            }
        }
        else
        {
            canvas_group.alpha = Mathf.Lerp(canvas_group.alpha, 0, Time.deltaTime * 10);
        }
    }
}
