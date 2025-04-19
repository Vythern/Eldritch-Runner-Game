using UnityEngine;
using UnityEditor;

public class SnapToGrid : EditorWindow
{
    private float gridSize = 5f;

    [MenuItem("Tools/Snap Selected To Grid %g")]
    public static void ShowWindow()
    {
        SnapSelectedObjectsToGrid(5f);
    }

    static void SnapSelectedObjectsToGrid(float gridSize)
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            Undo.RecordObject(obj.transform, "Snap To Grid");
            Vector3 pos = obj.transform.position;
            pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
            pos.y = Mathf.Round(pos.y / gridSize) * gridSize;
            pos.z = Mathf.Round(pos.z / gridSize) * gridSize;
            obj.transform.position = pos;
        }
    }
}