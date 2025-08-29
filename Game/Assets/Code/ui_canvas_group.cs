using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup))]
public class ui_canvas_group : MonoBehaviour
{
    [SerializeField] List<Play.State> states_to_hide;
    [SerializeField] CanvasGroup canvas_group;
    private RectTransform rectTransform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Play.OnPlayStateChange += OnPlayStateChange;
        rectTransform = GetComponent<RectTransform>();
        rectTransform.anchoredPosition = Vector2.zero;
        OnPlayStateChange(Play.i.currentState);
    }

    // Update is called once per frame
    void Update()
    {

    }
    void OnDestroy()
    {
        Play.OnPlayStateChange -= OnPlayStateChange;
    }
    void OnPlayStateChange(Play.State state)
    {
        // Debug.Log($"gameobject: {gameObject.name}");
        // Debug.Log($"OnPlayStateChange: {state}");
        if (states_to_hide.Contains(state))
        {
            canvas_group.alpha = 0;
            canvas_group.interactable = false;
            canvas_group.blocksRaycasts = false;
        }
        else
        {
            canvas_group.alpha = 1;
            canvas_group.interactable = true;
            canvas_group.blocksRaycasts = true;
        }
    }
}
