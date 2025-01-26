using UnityEngine;
public class Ship : MonoBehaviour
{
    [SerializeField] private MeshRenderer[] shipMeshRenderers;

    public ShipData Data { get; private set; }
    public float CurrentT { get; set; }
    public int TargetKnot { get; set; }
    public bool IsDocked { get; set; }
    public bool IsFull {  get; set; }

    private ShipController controller;
    private int passengerCount = 0;
    private int boardedCount = 0;

    public void Initialize(ShipData data, ShipController ctrl)
    {
        Data = data;
        controller = ctrl;
        CurrentT = 0f;
        IsDocked = false;
        PaintShip(data.color);
    }
    private void PaintShip(PassengerColor color)
    {
        Color unityColor = ColorUtility.GetColorFromType(color);
        foreach (var renderer in shipMeshRenderers)
        {
            if (renderer != null)
            {
                renderer.materials[0].color = unityColor;
            }
        }
    }

    public void PassengerAssigned()
    {
        passengerCount++;
        if (passengerCount >= Data.capacity)
            IsFull = true;
    }

    public void PassengerBoarded()
    {
        boardedCount++;
        UpdateVisuals(boardedCount);

        string result = Mathf.Clamp(boardedCount, 1, 3).ToString();
        SoundController.Instance.PlaySound("bloop" + result);

        if (boardedCount >= Data.capacity)
            DepartShip();
    }

    private void UpdateVisuals(int count) { }
    private void DepartShip()
    {
        controller.ProcessShipQueue();
    }
}