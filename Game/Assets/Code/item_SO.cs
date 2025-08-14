using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "item_SO", menuName = "Game/item_SO", order = 0)]
public class item_SO : ScriptableObject
{
    public Sprite icon;
    public string Title;
    [TextArea]
    public string Description;

    public enum ToolTipType { Simple, Move }
    public ToolTipType currentToolTipType;

    public enum ToolTipTextColor { Default, Success, Warning, Error }
    public ToolTipTextColor toolTipTextColor = ToolTipTextColor.Default;
}


