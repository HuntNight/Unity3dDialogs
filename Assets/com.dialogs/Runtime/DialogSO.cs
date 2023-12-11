using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogSO")] public class DialogSO : ScriptableObject
{
    public void Validate()
    {
        if (Dialog.nodes == null)
            Dialog.nodes = new List<DialogNodeData>();

        var grouped = Dialog.nodes.GroupBy(node => node.Id);
        foreach (var group in grouped)
        {
            if (group.Count() <= 1)
                continue;

            foreach (var nodeData in group)
                nodeData.Id = Dialog.nodes.Max(node => node.Id) + 1;
            
            Debug.Log("Fixed double ids");
        }

        if (Dialog.nodes.Any(node => node.Id == 0))
            return;

        Dialog.nodes.Add(new DialogNodeData
        {
            Id = 0, Actions = new List<DialogActionData>(), Transitions = new List<DialogTransitionData>(), Position = Vector2.zero,
        });
    }

    [Serializable] public class DialogData
    {
        public List<DialogNodeData> nodes;
    }
    [Serializable] public class DialogNodeData
    {
        public int Id; 
        public Vector2 Position;
        public List<DialogActionData> Actions;
        public List<DialogTransitionData> Transitions;
    }

    [Serializable] public class DialogActionData
    {
        public string Replica;
    }
    [Serializable] public class DialogTransitionData
    {
        public string TransitionName;
        public int NodeTranslationId;
        public bool IsAdv;
    }
    
    public DialogData Dialog;
}