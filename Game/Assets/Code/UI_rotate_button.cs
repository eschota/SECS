using UnityEngine;

public class UI_rotate_button : MonoBehaviour
{
    [SerializeField] CanvasGroup canvas_group;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Play.i?.currentState == Play.State.Create)
        {
            if (Creator.instance.current_prefab.Status == io_base.io_base_status.Creating
                || Creator.instance.current_prefab.Status == io_base.io_base_status.Intersected)
            {
                canvas_group.alpha = 1;
                return;
            }
            else
            {
                canvas_group.alpha = 0;
            }
        }
        else
        {
            canvas_group.alpha = 0;
        }
    }
}
