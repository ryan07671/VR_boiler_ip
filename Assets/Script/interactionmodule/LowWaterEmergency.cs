using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

public class LowWaterEmergency : MonoBehaviour
{
    [Header("Canvas (SAME CANVAS ALWAYS ON)")]
    public GameObject canvas;

    [Header("Canvas Parts (Texts)")]
    public GameObject part1;
    public GameObject part2;
    public GameObject part3;
    public GameObject part4;

    [Header("Buttons")]
    public Button startButton;
    public Button nextButton;

    [Header("Timings")]
    public float part1Time = 4f;
    public float part2Time = 4f;
    public float part3Time = 4f;

    [Header("Emergency Timings")]
    public float waterSprayDuration = 8f;
    public float whooshSoundDelay = 2f;
    public float steamDuration = 10f;

    [Header("Scene Objects")]
    public GameObject wallToDisable;
    public GameObject[] objectsToDeactivate;

    [Header("Water System")]
    public ParticleSystem waterParticles;
    public Animator waterLevelAnimator;

    [Header("Fire & Smoke")]
    public ParticleSystem fireParticles;
    public ParticleSystem smokeParticles;
    public ParticleSystem chimneySmoke;
    public Animator fireExtinguishAnimator;

    [Header("Audio Clips")]
    public AudioClip fireLoopSound;
    public AudioClip extinguishWhooshSound;

    [Header("Voiceovers")]
    public AudioClip part1Voiceover;
    public AudioClip part2Voiceover;
    public AudioClip part3Voiceover;
    public AudioClip part4Voiceover;

    [Header("Shared Audio Sources")]
    [SerializeField] private AudioSource fireAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioSource voiceAudioSource;

    private bool hasInitialized = false;
    private bool hasAdvancedPhase = false;

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
        hasAdvancedPhase = false;
        if (canvas != null)
            canvas.SetActive(true);

        if (part1 != null) part1.SetActive(false);
        if (part2 != null) part2.SetActive(false);
        if (part3 != null) part3.SetActive(false);
        if (part4 != null) part4.SetActive(false);

        if (startButton != null) startButton.gameObject.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        if (fireParticles != null)
        {
            fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            fireParticles.Clear();
            fireParticles.Play();
        }

        if (smokeParticles != null) smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (waterParticles != null) waterParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (chimneySmoke != null)
            chimneySmoke.Play();

        if (waterLevelAnimator != null) waterLevelAnimator.enabled = false;
        if (fireExtinguishAnimator != null) fireExtinguishAnimator.enabled = false;

        DeactivateObjects();
        SetupXRButtons();

        StartCoroutine(CanvasSequence());
    }

    void DeactivateObjects()
    {
        if (objectsToDeactivate == null) return;
        foreach (var obj in objectsToDeactivate)
            if (obj != null) obj.SetActive(false);
    }

    IEnumerator CanvasSequence()
    {
        yield return ShowPart(part1, part1Voiceover, part1Time);
        yield return ShowPart(part2, part2Voiceover, part2Time);
        yield return ShowPart(part3, part3Voiceover, part3Time);

        if (part4 != null) part4.SetActive(true);
        PlayVoice(part4Voiceover);
        if (startButton != null) startButton.gameObject.SetActive(true);
    }

    IEnumerator ShowPart(GameObject part, AudioClip voice, float fallbackTime)
    {
        if (part != null) part.SetActive(true);

        if (voice != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.PlayOneShot(voice);
            // Wait until the audio clip physically stops playing
            yield return new WaitWhile(() => voiceAudioSource.isPlaying);
        }
        else
        {
            yield return new WaitForSeconds(fallbackTime);
        }

        if (part != null) part.SetActive(false);
    }

    public void OnStartEmergencyClicked()
    {
        if (startButton != null) startButton.gameObject.SetActive(false);
        if (part4 != null) part4.SetActive(false);
        StartCoroutine(EmergencySequence());
    }

    IEnumerator EmergencySequence()
    {
        if (wallToDisable != null) wallToDisable.SetActive(false);
        if (waterParticles != null) waterParticles.Play();

        yield return new WaitForSeconds(whooshSoundDelay);

        if (fireAudioSource != null) fireAudioSource.Stop();
        if (sfxAudioSource != null && extinguishWhooshSound != null)
            sfxAudioSource.PlayOneShot(extinguishWhooshSound);

        if (fireExtinguishAnimator != null)
        {
            fireExtinguishAnimator.enabled = true;
            fireExtinguishAnimator.gameObject.SetActive(true);
            fireExtinguishAnimator.Play("fireextinguish", 0, 0f);
            Debug.Log("Playing Fire Extinguish Animation Now");
        }

        if (waterLevelAnimator != null)
            waterLevelAnimator.enabled = true;

        if (smokeParticles != null) smokeParticles.Play();

        yield return new WaitForSeconds(waterSprayDuration - whooshSoundDelay);
        if (waterParticles != null) waterParticles.Stop();

        yield return new WaitForSeconds(steamDuration - waterSprayDuration);
        if (smokeParticles != null) smokeParticles.Stop();

        if (nextButton != null) nextButton.gameObject.SetActive(true);
    }

    public void OnNextClicked()
    {
        if (hasAdvancedPhase) return;
            hasAdvancedPhase = true;

        if (voiceAudioSource != null)
            voiceAudioSource.Stop();

        if (sfxAudioSource != null)
            sfxAudioSource.Stop();

        if (PhaseManager.Instance != null)
            PhaseManager.Instance.GoToNextPhase();
    }

    void PlayVoice(AudioClip clip)
    {
        if (clip == null || voiceAudioSource == null) return;
        voiceAudioSource.Stop();
        voiceAudioSource.PlayOneShot(clip);
    }

    void SetupXRButtons()
    {
        BindXR(startButton, OnStartEmergencyClicked);
        BindXR(nextButton, OnNextClicked);
    }

    void BindXR(Button btn, System.Action callback)
    {
        if (btn == null) return;
        var interactable = btn.GetComponent<XRSimpleInteractable>();
        if (interactable != null)
        {
            interactable.selectEntered.RemoveAllListeners();
            interactable.selectEntered.AddListener(_ => callback());
        }
    }
}