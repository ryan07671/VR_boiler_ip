using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WaterTreatmentSequence : MonoBehaviour
{
    [Header("Canvas")]
    public Canvas waterTreatmentCanvas;

    [Header("Paragraph Text Objects (same canvas)")]
    public GameObject para1;
    public GameObject para2;
    public GameObject para3;

    [Header("Buttons")]
    public Button treatFeedButton;
    public Button nextButton;

    [Header("Voice Overs")]
    public AudioClip voice1;
    public AudioClip voice2;
    public AudioClip voice3;

    [Header("Display Times (seconds)")]
    public float time1 = 5f;
    public float time2 = 5f;
    public float time3 = 5f;

    [Header("Water Treatment Animation")]
    public Animator waterTreatmentAnimator;

    [Header("Audio Source (Shared)")]
    [SerializeField] private AudioSource voiceAudioSource;

    private bool animationEventTriggered = false;
    private bool hasAdvancedPhase = false;

    void Start()
    {
        // Safety
        if (voiceAudioSource == null)
        {
            Debug.LogError("WaterTreatmentSequence: VoiceAudioSource NOT assigned!");
            return;
        }

        para1.SetActive(false);
        para2.SetActive(false);
        para3.SetActive(false);
        treatFeedButton.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);

        if (waterTreatmentAnimator != null)
            waterTreatmentAnimator.enabled = false;

        SetupButtons();
        StartCoroutine(PlayWaterTreatmentSequence());
    }

    void Update()
    {
        if (waterTreatmentAnimator != null &&
            waterTreatmentAnimator.enabled &&
            !animationEventTriggered)
        {
            AnimatorStateInfo state =
                waterTreatmentAnimator.GetCurrentAnimatorStateInfo(0);

            if (state.normalizedTime >= 0.99f)
            {
                animationEventTriggered = true;
                OnWaterTreatmentAnimationFinished();
            }
        }
    }

    void SetupButtons()
    {
        var treatInteractable =
            treatFeedButton.GetComponent<XRSimpleInteractable>();
        treatInteractable.selectEntered.RemoveAllListeners();
        treatInteractable.selectEntered.AddListener(_ => OnTreatFeedClicked());

        var nextInteractable =
            nextButton.GetComponent<XRSimpleInteractable>();
        nextInteractable.selectEntered.RemoveAllListeners();
        nextInteractable.selectEntered.AddListener(_ => OnNextButtonPressed());
    }

    IEnumerator PlayWaterTreatmentSequence()
    {
        // PARA 1
        para1.SetActive(true);
        yield return PlayVoiceOrWait(voice1, time1);
        para1.SetActive(false);

        // PARA 2
        para2.SetActive(true);
        yield return PlayVoiceOrWait(voice2, time2);
        para2.SetActive(false);

        // PARA 3
        para3.SetActive(true);
        yield return PlayVoiceOrWait(voice3, time3);

        treatFeedButton.gameObject.SetActive(true);
    }

    IEnumerator PlayVoiceOrWait(AudioClip clip, float fallbackTime)
    {
        if (clip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.PlayOneShot(clip);
            yield return new WaitWhile(() => voiceAudioSource.isPlaying);
        }
        else
        {
            yield return new WaitForSeconds(fallbackTime);
        }
    }

    public void OnTreatFeedClicked()
    {
        animationEventTriggered = false;
        treatFeedButton.gameObject.SetActive(false);

        if (waterTreatmentAnimator != null)
        {
            waterTreatmentAnimator.enabled = true;
            waterTreatmentAnimator.Play(0);
        }
    }

    void OnWaterTreatmentAnimationFinished()
    {
        nextButton.gameObject.SetActive(true);
    }

    public void OnNextButtonPressed()
    {
        if (hasAdvancedPhase) return;
        hasAdvancedPhase = true;

        if (voiceAudioSource.isPlaying)
            voiceAudioSource.Stop();

        if (PhaseManager.Instance != null)
            PhaseManager.Instance.GoToNextPhase();
    }
}
