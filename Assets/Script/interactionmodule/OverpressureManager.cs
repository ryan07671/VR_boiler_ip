using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class OverpressureManager : MonoBehaviour
{
    [Header("3D Models & Effects")]
    public Transform pressureArrow;
    public ParticleSystem steamParticles;

    [Header("UI Elements")]
    public GameObject canvasObj;
    public TextMeshProUGUI statusText;
    public Button btnRelease;
    public Button btnNext;

    [Header("Timings & Animation")]
    public float arrowRotationAngle = -100f;
    public float pressureRiseTime = 6f;
    public float pressureReleaseTime = 5f;

    [Header("Audio")]
    public AudioSource alarmAudioSource;
    public AudioClip alarmClip;
    public AudioSource voiceAudioSource;
    public AudioClip instructionVoiceover;

    // State
    private Quaternion originalArrowRot;
    private bool hasInitialized = false;
    private bool hasAdvancedPhase = false; // THE SAFETY LOCK

    private string textRising = "Trigger the pressure relief valve to release the pressure.";
    private string textReleasing = "The pressure is being released...";

    void Awake()
    {
        if (pressureArrow != null)
            originalArrowRot = pressureArrow.localRotation;
    }

    void OnEnable()
    {
        if (!hasInitialized)
        {
            InitializePhase();
            hasInitialized = true;
        }
    }

    void OnDisable()
    {
        hasInitialized = false;
        StopAllCoroutines();
    }

    void InitializePhase()
    {
        hasAdvancedPhase = false; // Reset lock

        canvasObj.SetActive(false);
        statusText.text = "";
        btnRelease.gameObject.SetActive(false);
        btnNext.gameObject.SetActive(false);

        if (steamParticles != null)
        {
            steamParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            steamParticles.gameObject.SetActive(false);
        }

        if (pressureArrow != null)
            pressureArrow.localRotation = originalArrowRot;

        BindXRButton(btnRelease, OnReleaseClicked);
        BindXRButton(btnNext, OnNextClicked);

        StartCoroutine(RisePressureRoutine());
    }

    private void BindXRButton(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        var interactable = btn.GetComponent<XRSimpleInteractable>();
        if (interactable != null)
        {
            interactable.selectEntered.RemoveAllListeners();
            interactable.selectEntered.AddListener(_ => action());
        }
        else
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }
    }

    IEnumerator RisePressureRoutine()
    {
        float timer = 0f;
        bool alarmTriggered = false;

        Quaternion startRot = originalArrowRot;
        Quaternion targetRot = startRot * Quaternion.Euler(arrowRotationAngle, 0, 0);

        while (timer < pressureRiseTime)
        {
            timer += Time.deltaTime;
            float t = timer / pressureRiseTime;

            if (pressureArrow != null)
                pressureArrow.localRotation = Quaternion.Lerp(startRot, targetRot, t);

            if (t > 0.8f && !alarmTriggered)
            {
                alarmTriggered = true;
                if (alarmAudioSource != null && alarmClip != null)
                {
                    alarmAudioSource.loop = true;
                    alarmAudioSource.clip = alarmClip;
                    alarmAudioSource.Play();
                }
            }

            yield return null;
        }

        if (pressureArrow != null)
            pressureArrow.localRotation = targetRot;

        canvasObj.SetActive(true);
        statusText.text = textRising;

        if (voiceAudioSource != null && instructionVoiceover != null)
        {
            voiceAudioSource.PlayOneShot(instructionVoiceover);
            yield return new WaitWhile(() => voiceAudioSource.isPlaying);
        }
        else
        {
            yield return new WaitForSeconds(4f);
        }

        btnRelease.gameObject.SetActive(true);
    }

    void OnReleaseClicked()
    {
        btnRelease.gameObject.SetActive(false);
        statusText.text = textReleasing;

        if (alarmAudioSource != null) alarmAudioSource.Stop();

        StartCoroutine(ReleasePressureRoutine());
    }

    IEnumerator ReleasePressureRoutine()
    {
        if (steamParticles != null)
        {
            steamParticles.gameObject.SetActive(true);
            steamParticles.Play();
        }

        float timer = 0f;
        Quaternion currentRot = pressureArrow != null ? pressureArrow.localRotation : Quaternion.identity;

        while (timer < pressureReleaseTime)
        {
            timer += Time.deltaTime;
            float t = timer / pressureReleaseTime;

            if (pressureArrow != null)
                pressureArrow.localRotation = Quaternion.Lerp(currentRot, originalArrowRot, t);

            yield return null;
        }

        if (pressureArrow != null)
            pressureArrow.localRotation = originalArrowRot;

        if (steamParticles != null) steamParticles.Stop();

        statusText.text = "";
        btnNext.gameObject.SetActive(true);
    }

    void OnNextClicked()
    {
        // PREVENT DOUBLE FIRE
        if (hasAdvancedPhase) return;
        hasAdvancedPhase = true;

        btnNext.gameObject.SetActive(false);

        if (PhaseManager.Instance != null)
            PhaseManager.Instance.GoToNextPhase();
    }
}