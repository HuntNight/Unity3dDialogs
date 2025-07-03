using System;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

public class DialogEditorWindow : EditorWindow
{
    private DialogGraph _dialogGraph;
    private DialogSO _dialogSo;

    public static void Open(DialogSO dialogSo)
    {
        var window = GetWindow<DialogEditorWindow>();
        window._dialogSo = dialogSo;
        window.SetupWindow();
    }

    private void OnEnable()
    {
        if (_dialogSo == null)
            return;
        
        SetupWindow();
    }

    private void SetupWindow()
    {
        rootVisualElement.Clear();
        rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("DialogStyle"));
        titleContent = new GUIContent(_dialogSo.name);
        var graph = new DialogGraph();
        graph.StretchToParentSize();
        rootVisualElement.Add(graph);
        _dialogGraph = graph;
        _dialogGraph.Load(_dialogSo.Dialog);
        CreateToolBar();
    }
    private enum GraphAction
    {
        Save,
        Load
    }

    private void CreateToolBar()
    {
        var toolbar = new Toolbar();
        toolbar.AddToClassList("dialog-toolbar");

        foreach (GraphAction graphAction in Enum.GetValues(typeof(GraphAction)))
        {
            var button = new Button(() => ToolBarActionHandler(graphAction)) { text = graphAction.ToString() };
            button.AddToClassList("dialog-toolbar-button");
            toolbar.Add(button);
        }

        rootVisualElement.Add(toolbar);
    }

    private void ToolBarActionHandler(GraphAction graphAction)
    {
        if (graphAction == GraphAction.Save)
            Save();
        else if (graphAction == GraphAction.Load)
            SetupWindow();
    }

    private void Save()
    {
        var nodes = _dialogGraph.nodes.Cast<DialogNode>().ToList();
        foreach (var edge in _dialogGraph.edges)
        {
            var outDialogNode = edge.output.node as DialogNode;
            var inDialogNode = edge.input.node as DialogNode;
            Assert.IsNotNull(outDialogNode);
            Assert.IsNotNull(inDialogNode);
            
            if (outDialogNode.DialogNodeData.Id == 0 && !outDialogNode.DialogNodeData.Transitions.Any())
                 outDialogNode.DialogNodeData.Transitions.Add(new DialogSO.DialogTransitionData{TransitionName = "Start", NodeTranslationId = inDialogNode.DialogNodeData.Id});
            
            var targetTransition = outDialogNode.DialogNodeData.Transitions.First(transition => transition.TransitionName.Equals(edge.output.portName));
            targetTransition.NodeTranslationId = inDialogNode.DialogNodeData.Id;
        }

        foreach (var node in nodes)
            node.DialogNodeData.Position = node.GetPosition().position;
        
        _dialogSo.Dialog.nodes = nodes.Select(node => node.DialogNodeData).ToList();
        EditorUtility.SetDirty(_dialogSo);
    }
}