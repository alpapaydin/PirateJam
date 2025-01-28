using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellBase : MonoBehaviour
{
    protected GridController spawner;
    private TapController tapController;
    private GameObject selectedCue;
    private bool isInEditor;
    private Vector2Int cellPos;

    private void Awake()
    {
        tapController = GetComponent<TapController>();
        if (tapController == null)
        {
            tapController = gameObject.AddComponent<TapController>();
        }
        Transform selectedCueTrans = transform.Find("Cylinder");
        selectedCue = selectedCueTrans.gameObject;
        tapController.OnTapped.AddListener(HandleTap);
    }
    private void OnDestroy()
    {
        if (tapController != null)
        {
            tapController.OnTapped.RemoveListener(HandleTap);
        }
    }

    public void InitializeCell(Vector2Int pos, GridController controller)
    {
        spawner = controller;
        cellPos = pos;
        if (controller is EditorGridController)
            isInEditor = true;
    }

    private void HandleTap()
    {
        if (!isInEditor)
            return;
        EditorGridController editorGrid = spawner as EditorGridController;
        if (!editorGrid.IsInEditMode)
            return;
        editorGrid.OnTileClicked(cellPos);
    }

    public void CellSelected()
    {
        if (!isInEditor) return;
        selectedCue.SetActive(true);
    }

    public void CellDeselected()
    {
        if (!isInEditor) return;
        selectedCue.SetActive(false);
    }

}
