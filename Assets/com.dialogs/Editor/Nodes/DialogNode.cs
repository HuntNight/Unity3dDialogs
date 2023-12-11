using UnityEditor.Experimental.GraphView;

public class DialogNode : Node
{
    public DialogSO.DialogNodeData DialogNodeData { get; private set; }
    public DialogNode(DialogSO.DialogNodeData nodeData)
    {
        DialogNodeData = nodeData;
    }
}