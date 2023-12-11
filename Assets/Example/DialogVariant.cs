using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogVariant : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;
    [SerializeField] private Button _button;
    
    public void Build(string text, bool isAdv, Action onClick)
    {
        _label.text = text + (isAdv ? "(ADV)" : string.Empty);
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => onClick?.Invoke());
    }
}
