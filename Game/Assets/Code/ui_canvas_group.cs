using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup))]
public class ui_canvas_group : MonoBehaviour
{
    [SerializeField] List<Play.State> states_to_hide;
    [SerializeField] CanvasGroup canvas_group;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Play.OnPlayStateChange += OnPlayStateChange;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnPlayStateChange(Play.State state)
    {
        Debug.Log($"OnPlayStateChange: {state} gameobject: {gameObject.name}");
        if (states_to_hide.Contains(state))
        {
            canvas_group.alpha = 0;
        }
        else
        {
            canvas_group.alpha = 1;
        }
    }
}
