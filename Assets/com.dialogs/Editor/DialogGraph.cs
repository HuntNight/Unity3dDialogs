using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogGraph : GraphView
{
    private List<DialogNode> _dialogNodes = new List<DialogNode>();
    public DialogGraph()
    {
        styleSheets.Add(Resources.Load<StyleSheet>("DialogStyle"));
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        this.AddManipulator(new FreehandSelector());
        this.AddManipulator(new ClickSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();
        CreateSearchWindow();
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        var startPortView = startPort;

        foreach (var port in ports)
        {
            if (startPortView != port && startPortView.node != port.node)
                compatiblePorts.Add(port);
        }

        return compatiblePorts;
    }

    private void CreateSearchWindow()
    {
        var searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
        searchWindow.Configure(this);
        nodeCreationRequest = context =>
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
    }

    public void Load(DialogSO.DialogData dialogData)
    {
        _dialogNodes.Clear();
        foreach (var nodeData in dialogData.nodes)
        {
            var node = CreateNode(nodeData);
            _dialogNodes.Add(node);
        }

        foreach (var node in _dialogNodes)
            LinkNode(node);

        foreach (var node in _dialogNodes)
        {
            AddElement(node);
            node.AnimateIn();
            node.RefreshExpandedState();
            node.RefreshPorts();
        }
    }

    public void CreateNode(Vector2 position)
    {
        var id = _dialogNodes.Max(nodeData => nodeData.DialogNodeData.Id) + 1;
        
        var nodeData = new DialogSO.DialogNodeData
        {
            Id = id,
            Actions = new List<DialogSO.DialogActionData>(),
            Transitions = new List<DialogSO.DialogTransitionData>(),
            Position = position
        };

        var node = CreateNode(nodeData);
        _dialogNodes.Add(node);
        AddElement(node);
        node.AnimateIn();
    }

    private DialogNode CreateNode(DialogSO.DialogNodeData nodeData)
    {
        var node = new DialogNode(nodeData);
        SetupNodeCommon(node);
        return node;
    }

    private void LinkNode(DialogNode node)
    {
        foreach (var transition in node.DialogNodeData.Transitions)
            AddTransition(node, transition);
    }
    
    private void SetupNodeCommon(DialogNode node)
    {
        node.capabilities |= Capabilities.Movable;
        node.capabilities |= Capabilities.Deletable;
        node.SetPosition(new Rect(node.DialogNodeData.Position.x, node.DialogNodeData.Position.y, 300, 200));

        if (node.DialogNodeData.Id == 0)
            SetupNodeInitial(node);
        else
            SetupNode(node);
    }
    
    private void SetupNodeInitial(DialogNode node)
    {
        var port = node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        port.portName = "Start";
        node.title = "START";
        node.name = "InputPort";
        node.outputContainer.Add(port);
    }

    private void SetupNode(DialogNode node)
    {
        var port = node.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        port.portName = "InputPort";
        port.name = "InputPort";
        node.inputContainer.Add(port);
        
        foreach (var action in node.DialogNodeData.Actions)
            AddAction(node, action);

        var addActionButton = new Button
        {
            name = "AddReplica",
            text = "Add Action",
        };
        addActionButton.clicked += () => AddActionHandler(node);
        
        var addTransitionButton = new Button
        {
            name = "AddTransition",
            text = "Add Transition",
        };
        addTransitionButton.clicked += () => AddTransitionHandler(node);
        
        node.mainContainer.Add(addTransitionButton);
        node.mainContainer.Add(addActionButton);
    }

    private Port AddOutputNode(DialogNode node, string portName)
    {
        var generatedChoicePort =
            node.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            
        generatedChoicePort.portName = portName;
        generatedChoicePort.name = portName;
        node.outputContainer.Add(generatedChoicePort);

        return generatedChoicePort;
    }

    private void AddTransitionHandler(DialogNode node)
    {
        var index = 0;

        if (node.DialogNodeData.Transitions.Any())
            index = node.DialogNodeData.Transitions.Count;

        var transitionData = new DialogSO.DialogTransitionData{TransitionName = index.ToString()};
        node.DialogNodeData.Transitions.Add(transitionData);
        AddTransition(node, transitionData);
    }

    private void AddActionHandler(DialogNode node)
    {
        var index = 0;
        
        if (node.DialogNodeData.Actions.Any())
            index = node.DialogNodeData.Actions.Count;

        var actionData = new DialogSO.DialogActionData{Replica = index.ToString()};
        node.DialogNodeData.Actions.Add(actionData);
        AddAction(node, actionData);
    }

    private void AddAction(DialogNode node, DialogSO.DialogActionData actionData)
    {
        var textField = new TextField
        {
            name = "ReplicaTextField",
            multiline = true,
            tripleClickSelectsLine = true,
            value = actionData.Replica
        };
        textField.RegisterValueChangedCallback(next =>
        {
            actionData.Replica = next.newValue;
        });

        var deleteButton = new Button();
        deleteButton.clicked += () =>
        {
            node.mainContainer.Remove(textField);
            node.DialogNodeData.Actions.Remove(actionData);
        };
        deleteButton.text = "x";
        textField.Add(deleteButton);
        node.mainContainer.Add(textField);
    }

    private void AddTransition(DialogNode node, DialogSO.DialogTransitionData transitionData)
    {
        var isInitial = node.DialogNodeData.Id == 0;
        var outPort = node.Q<Port>();
        
        if (!isInitial)
            outPort = AddOutputNode(node, transitionData.TransitionName);
        
        var transitionIndex = node.DialogNodeData.Transitions.Count - 1;

        if (!isInitial)
        {
            var textField = new TextField
            {
                name = $"Transition#{transitionIndex}",
                value = transitionData.TransitionName,
            };
            textField.RegisterValueChangedCallback(next =>
            {
                transitionData.TransitionName = next.newValue;
                outPort.portName = next.newValue;
            });

            var deleteButton = new Button
            {
                text = "x"
            };
            
            var isAdvCheckbox = new Toggle("IsAdv");
            isAdvCheckbox.value = transitionData.IsAdv;
            isAdvCheckbox.RegisterValueChangedCallback(next =>
            {
                transitionData.IsAdv = next.newValue;
            }); 

            deleteButton.clicked += () =>
            {
                node.DialogNodeData.Transitions.Remove(transitionData);
                node.outputContainer.Remove(deleteButton);
                node.outputContainer.Remove(textField);
                node.outputContainer.Remove(outPort);
                node.outputContainer.Remove(isAdvCheckbox);
                RemoveConnections(node, outPort);
            };

            node.outputContainer.Add(deleteButton);
            node.outputContainer.Add(textField);
            node.outputContainer.Add(isAdvCheckbox);
        }

        if (transitionData.NodeTranslationId != 0)
        {
            var targetNode = _dialogNodes.FirstOrDefault(n => n.DialogNodeData.Id.Equals(transitionData.NodeTranslationId));

            if (targetNode == null)
                transitionData.NodeTranslationId = 0;
            else
                LinkEdge(outPort, targetNode.Q<Port>("InputPort"));
        }
        
        node.RefreshPorts();
        node.RefreshExpandedState();
    }

    private void RemoveConnections(Node node, Port socket)
    {
        var targetEdges = edges.Where(edge => edge.output == socket);

        foreach (var edge in targetEdges)
        {
            edge.input.Disconnect(edge);
            RemoveElement(edge);
        }
        
        node.RefreshPorts();
        node.RefreshExpandedState();
    }
    
    private void LinkEdge(Port output, Port input)
    {
        var tempEdge = new Edge {
            output = output,
            input = input
        };
        
        tempEdge.input.Connect(tempEdge);
        tempEdge.output.Connect(tempEdge);
        Add(tempEdge);
    }
}
