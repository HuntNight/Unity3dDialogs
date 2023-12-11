using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FinishScreen : MonoBehaviour
{
    [SerializeField] private Button _tg;
    [SerializeField] private Button _yt;

    private void Start()
    {
        _tg.onClick.AddListener(() => Application.OpenURL("https://t.me/kolobov_gamedev"));
        _yt.onClick.AddListener(() => Application.OpenURL("https://www.youtube.com/@nikita.kolobov"));
    }
}
