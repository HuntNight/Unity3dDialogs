using System;
using System.Collections.Generic;
using System.Linq;

public enum EReactionType
{
    PlaySomeFX1,
}
    
public interface IDialogStep {}

public class Replica : IDialogStep
{
    public string Text;
}

public class Reply : IDialogStep
{
    public List<ReplyVariantData> ReplyVariants;
}

public class ReplyVariantData
{
    public string Replica;
    public bool IsAdv;
}

public class ReplicaReaction : IDialogStep
{
    public EReactionType ReactionType;
}

public class ReplicaSetActors : IDialogStep
{
    public int[] ActorIds;
}

public class ReplicaSetSpeaker : IDialogStep
{
    public int SpeakerIndex;
}

public class DialogModel
{
    private DialogSO.DialogData _dialogData;
    private DialogSO.DialogNodeData _currentNode;
    private Action _onFinish;

    public DialogModel(DialogSO.DialogData dialogData, Action onFinish = null)
    {
        _dialogData = dialogData;
        _onFinish = onFinish;
    }

    public List<IDialogStep> StartDialog()
    {
        var startNode = _dialogData.nodes.First(node => node.Id.Equals(0));
        _currentNode = _dialogData.nodes.First(node => node.Id.Equals(startNode.Transitions.First().NodeTranslationId));

        return GetDialogStep(_currentNode);
    }

    public List<IDialogStep> SelectVariant(int variantIndex)
    {
        var choice = _currentNode.Transitions[variantIndex];
        var nextNode = _dialogData.nodes.FirstOrDefault(node => node.Id.Equals(choice.NodeTranslationId));

        if (nextNode == null)
            return null;

        _currentNode = nextNode;
        return GetDialogStep(_currentNode);
    }

    private void FinishDialog()
    {
        _onFinish?.Invoke();
    }

    private List<IDialogStep> GetDialogStep(DialogSO.DialogNodeData nodeData)
    {
        if (nodeData == null)
            return null;

        if (nodeData.Actions.Count == 0)
            return null;

        var actions = nodeData.Actions.Select(action => action.Replica).ToList();
        var replyVariants = nodeData.Transitions.Select(transition => new ReplyVariantData{Replica = transition.TransitionName, IsAdv = transition.IsAdv}).ToList();
        var result = new List<IDialogStep>();

        foreach (var action in actions)
        {
            if (action.StartsWith("action."))
                result.Add(new ReplicaReaction{ReactionType = Enum.Parse<EReactionType>(action.Substring(7))});
            else if (action.StartsWith("actors."))
                result.Add(new ReplicaSetActors{ActorIds = action.Substring(7).Split(",").Select(int.Parse).ToArray()});
            else if (action.StartsWith("speaker."))
                result.Add(new ReplicaSetSpeaker{SpeakerIndex = int.Parse(action.Substring(8))});
            else
                result.Add(new Replica{Text = action});
        }
        
        if (replyVariants.Any())
            result.Add(new Reply{ReplyVariants = replyVariants});
        else
            FinishDialog();

        return result;
    }
}
