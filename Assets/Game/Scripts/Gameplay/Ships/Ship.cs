using System.Collections;
using UnityEngine;
public class Ship : MonoBehaviour
{
    [SerializeField] private MeshRenderer[] shipMeshRenderers;
    [SerializeField] private ParticleSystem waterJet;

    public ShipData Data { get; private set; }
    public float CurrentT { get; set; }
    public int TargetKnot { get; set; }
    public bool IsDocked { get; set; }
    public bool IsFull {  get; set; }

    private ShipController controller;
    private int passengerCount = 0;
    private int boardedCount = 0;
    private float currentEmissionRate = 0f;
    private float targetEmissionRate = 30f;
    private ParticleSystem.EmissionModule emissionModule;

    private void Awake()
    {
        emissionModule = waterJet.emission;
    }

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
        Color unityColor = ColorUtility.GetColorFromType(color, controller.ColorShift);
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
        StartJet();
        controller.ProcessShipQueue();
    }

    public void StartJet()
    {
        if (!waterJet.isPlaying)
            waterJet.Play();

        StopAllCoroutines();
        StartCoroutine(TransitionJet(targetEmissionRate, 0.25f));
    }

    public void StopJet()
    {
        StopAllCoroutines();
        StartCoroutine(StopJetCoroutine());
    }

    private IEnumerator StopJetCoroutine()
    {
        yield return TransitionJet(0f, 0.1f);
    }

    private IEnumerator TransitionJet(float targetRate, float transitionTime)
    {
        float startRate = currentEmissionRate;
        float elapsedTime = 0f;
        while (elapsedTime < transitionTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionTime;
            t = t * t * (3f - 2f * t);
            currentEmissionRate = Mathf.Lerp(startRate, targetRate, t);
            emissionModule.rateOverTime = currentEmissionRate;
            yield return null;
        }
        currentEmissionRate = targetRate;
        emissionModule.rateOverTime = currentEmissionRate;
    }
}