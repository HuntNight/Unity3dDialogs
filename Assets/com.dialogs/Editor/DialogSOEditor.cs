using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DialogSO))]
public class DialogSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var targetDialogSo = (DialogSO) target;
        
        if (GUILayout.Button("Open"))
        {
            targetDialogSo.Validate();
            DialogEditorWindow.Open(targetDialogSo);
        }
    }
}
