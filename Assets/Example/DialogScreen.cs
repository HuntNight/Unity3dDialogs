using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogScreen : MonoBehaviour
{
    [SerializeField] private RectTransform _leftCharPosition;
    [SerializeField] private RectTransform _rightCharPosition;
    [SerializeField] private RectTransform _centerCharPosition;
    [SerializeField] private RectTransform _variantsGrid;
    [SerializeField] private DialogReplica _replica;
    [SerializeField] private DialogVariant _dialogVariantOriginal;
    [SerializeField] private ParticleSystem _particleSystem;
    [SerializeField] private GameObject _enableOnFinish;
    [SerializeField] private Button _restartButton;

    #region Assets
    [SerializeField] private DialogSO[] _dialogSOs;
    [SerializeField] private DialogActor[] _actorOriginals;
    #endregion
    
    private List<DialogActor> _actors = new List<DialogActor>();
    private DialogActor _actor;
    private RectTransform _targetDialogPosition;

    private void Awake()
    {
        _restartButton.onClick.AddListener(GoIntro);
        GoIntro();
    }
    
    private void GoIntro()
    {
        _enableOnFinish.SetActive(false);
        
        IntroTask().ContinueWith(result =>
        {
            if (!result.IsFaulted)
                return;
            
            Debug.LogException(result.Exception);
        });
    }

    private async Task IntroTask()
    {
        var dialogModels = _dialogSOs.Select(doalogSO => new DialogModel(doalogSO.Dialog)).ToArray();
        
        foreach (var model in dialogModels)
            await Appear(model);
        
        _enableOnFinish.gameObject.SetActive(true);
    }
    
    private async Task Appear(DialogModel input)
    {
        _variantsGrid.gameObject.SetActive(false);
        ClearContainers();
        await ProcessModel(input);
        _particleSystem.Stop();
    }

    private async Task ProcessModel(DialogModel model)
    {
        var steps = model.StartDialog();

        while (steps != null)
        {
            List<IDialogStep> nextSteps = null;
            
            foreach (var step in steps)
            {
                var localNextSteps = await ProcessStep(model, step);

                if (localNextSteps == null) continue;
                
                if (nextSteps == null)
                    nextSteps = localNextSteps;
                else
                    nextSteps.AddRange(localNextSteps);
            }
            steps = nextSteps;
        }
    }

    private async Task<List<IDialogStep>> ProcessStep(DialogModel model, IDialogStep step)
    {
        if (step is Replica replica)
            return await ProcessStep(model, replica);
        if (step is Reply reply)
            return await ProcessStep(model, reply);
        if (step is ReplicaReaction reaction)
            return await ProcessStep(model, reaction);
        if (step is ReplicaSetActors setActors)
            return await ProcessStep(model, setActors);
        if (step is ReplicaSetSpeaker speaker)
            return await ProcessStep(model, speaker);

        throw new ArgumentException($"Unsupported step: {step.GetType()}");
    }

    private void Update()
    {
        if (Time.frameCount % 5 == 0)
            ActualizeReplicaPosition();
    }

    private Task<List<IDialogStep>> ProcessStep(DialogModel model, ReplicaSetSpeaker speaker)
    {
        _actor = _actors[speaker.SpeakerIndex];
        ActualizeReplicaPosition();

        return Task.FromResult<List<IDialogStep>>(null);
    }

    private void ActualizeReplicaPosition()
    {
        if (_actor == null)
            return;
        
        if (_actors.Count <= 1)
        {
            _targetDialogPosition = _centerCharPosition;
        }
        else
        {
            var actorIndex = _actors.IndexOf(_actor);
            
            if (actorIndex == 0)
                _targetDialogPosition = _leftCharPosition;
            else
                _targetDialogPosition = _rightCharPosition;
        }
        
        var replicaRT = _replica.transform as RectTransform;
        replicaRT!.position =
            _targetDialogPosition!.position + new Vector3(_actor.ReplicaPivot.x, _actor.ReplicaPivot.y, 0f);
    }

    private async Task<List<IDialogStep>> ProcessStep(DialogModel model, Replica replica)
    {
        var actorName = string.Empty;

        if (_actor != null)
            actorName = _actor.ActorName;

        _replica.gameObject.SetActive(true);
        await _replica.SetText(replica.Text, actorName);

        return null;
    }

    private async Task<List<IDialogStep>> ProcessStep(DialogModel model, Reply reply)
    {
        var selectedVariant = await BuildVariants(reply.ReplyVariants);
        var steps = model.SelectVariant(selectedVariant);

        return steps;
    }

    private Task<List<IDialogStep>> ProcessStep(DialogModel model, ReplicaReaction reaction)
    {
        if (reaction.ReactionType == EReactionType.PlaySomeFX1)
            _particleSystem.Play();

        if (_actor == null)
            return Task.FromResult<List<IDialogStep>>(null);

        return Task.FromResult<List<IDialogStep>>(null);
    }
    
    private Task<List<IDialogStep>> ProcessStep(DialogModel model, ReplicaSetActors setActors)
    {
        ClearContainers();
        _actors.Clear();
        
        if (setActors.ActorIds.Length == 0)
            return Task.FromResult<List<IDialogStep>>(null);

        if (setActors.ActorIds.Length > 2)
            return Task.FromResult<List<IDialogStep>>(null);

        if (setActors.ActorIds.Length == 1)
        {
            SpawnActor(setActors.ActorIds.First(), _centerCharPosition);
            return Task.FromResult<List<IDialogStep>>(null);
        }
        
        SpawnActor(setActors.ActorIds[0], _leftCharPosition);
        SpawnActor(setActors.ActorIds[1], _rightCharPosition);

        return Task.FromResult<List<IDialogStep>>(null);
    }

    private void SpawnActor(int index, RectTransform container)
    {
        var instanced = Instantiate(_actorOriginals[index], container, false);
        _actor = instanced.GetComponentInChildren<DialogActor>();
        _actors.Add(_actor);
    }

    private void ClearContainers()
    {
        foreach (Transform subTransform in _leftCharPosition)
            Destroy(subTransform.gameObject);
        
        foreach (Transform subTransform in _rightCharPosition)
            Destroy(subTransform.gameObject);
        
        foreach (Transform subTransform in _centerCharPosition)
            Destroy(subTransform.gameObject);
    }

    private async Task<int> BuildVariants(List<ReplyVariantData> variants)
    {
        _variantsGrid.gameObject.SetActive(true);

        foreach (var variant in _variantsGrid.GetComponentsInChildren<DialogVariant>())
            Destroy(variant.gameObject);
        
        var cs = new TaskCompletionSource<int>();
        
        for (var i = 0; i < variants.Count; i++)
        {
            var closureI = i;
            var variantData = variants[i];
            var variant = Instantiate(_dialogVariantOriginal, _variantsGrid.transform, false);
            variant.Build(variantData.Replica, variantData.IsAdv, () => cs.SetResult(closureI));
        }

        var res = await cs.Task;
        _replica.gameObject.SetActive(false);
        _variantsGrid.gameObject.SetActive(false);
        return res;
    }
}



public struct DialogResult
{
    
}