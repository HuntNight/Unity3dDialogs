using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class DialogReplica : MonoBehaviour
{
    [SerializeField] private TMP_Text[] _label;

    public async Task SetText(string text, string actorName)
    {
        foreach (var label in _label)
            label.text = $"<b>{actorName} - </b> {text}";

        while (!Input.GetMouseButtonDown(0))
            await Task.Yield();
        
        while (!Input.GetMouseButtonUp(0))
            await Task.Yield();
        
        await Task.Yield();
    }
}
