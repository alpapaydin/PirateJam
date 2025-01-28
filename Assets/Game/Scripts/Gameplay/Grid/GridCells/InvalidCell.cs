using UnityEngine;

public class InvalidCell : CellBase
{
    private void Start()
    {
        if (spawner is EditorGridController)
            ShowDebugMesh();
    }
    private void ShowDebugMesh()
    {
        Transform cubeTransform = transform.Find("Cube");
        if (cubeTransform != null)
        {
            cubeTransform.gameObject.SetActive(true);
        }
    }
}
