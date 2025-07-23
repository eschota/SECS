using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;

public class ship_ui_canvas_group : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;

    public List<SHIP_UI.State> thisState;
    [SerializeField] float fadeDuration = 0.5f;
    bool isFade = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SHIP_UI.ChangeState += OnChangeState;
    }

    private void OnChangeState(SHIP_UI.State state)
    {
        isFade = thisState.Contains(state);
    }
    
    private void OnDestroy()
    {
        SHIP_UI.ChangeState -= OnChangeState;
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