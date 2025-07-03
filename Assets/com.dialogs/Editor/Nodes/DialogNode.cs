using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogNode : Node
{
    public DialogSO.DialogNodeData DialogNodeData { get; private set; }
    public DialogNode(DialogSO.DialogNodeData nodeData)
    {
        DialogNodeData = nodeData;
        styleSheets.Add(Resources.Load<StyleSheet>("Node"));
        AddToClassList("dialog-node");
    }

    public void AnimateIn()
    {
        // Delay applying the "show" class so transition animations play
        schedule.Execute(() => AddToClassList("show")).ExecuteLater(1);
    }
}