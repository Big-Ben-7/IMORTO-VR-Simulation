using UnityEngine;
using UnityEditor;
public class ReplaceNames : EditorWindow
{
    private GameObject parentObject;
    private string search = "";
    private string replace = "";

    [MenuItem("Tools/Replace Child Names")]
    public static void ShowWindow() => GetWindow<ReplaceNames>("Name Replacer");

    void OnGUI()
    {
        parentObject = (GameObject)EditorGUILayout.ObjectField("Parent", parentObject, typeof(GameObject), true);
        search = EditorGUILayout.TextField("Find", search);
        replace = EditorGUILayout.TextField("Replace With", replace);

        if (GUILayout.Button("Replace") && parentObject != null)
        {
            Undo.RecordObjects(parentObject.GetComponentsInChildren<Transform>(), "Replace Names");
            foreach (var t in parentObject.GetComponentsInChildren<Transform>())
                t.name = t.name.Replace(search, replace);
        }
    }
}