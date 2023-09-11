#if UNITY_EDITOR
using LevelEditing.WaveFunctionCollapsing;
using UnityEditor;
using UnityEngine;

namespace CustomEditorScripts
{
    [CustomEditor(typeof(WaveFunctionCollapseStart))]
    public class WaveFunctionCollapseStartEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            WaveFunctionCollapseStart wfc = (WaveFunctionCollapseStart)target;

            if (GUILayout.Button("Run"))
                wfc.Run();
        }
    }
}
#endif