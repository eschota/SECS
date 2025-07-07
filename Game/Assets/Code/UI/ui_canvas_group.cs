using Unity.VisualScripting;
using UnityEngine;


public class ui_canvas_group : MonoBehaviour
{

    [SerializeField] CanvasGroup canvasGroup;

    public main.State thisState;
    [SerializeField] float fadeDuration = 0.5f;
    bool isFade = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        main.ChangeState += OnChangeState;
    }

    private void OnChangeState(main.State state)
    {
        isFade = (state == thisState);
    }
    private void OnDestroy()
    {
        main.ChangeState -= OnChangeState;
    }
    void Update()
    {
        if (isFade)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1, Time.deltaTime / fadeDuration);
        }
        else
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0, Time.deltaTime / fadeDuration);
        }
    }
}
