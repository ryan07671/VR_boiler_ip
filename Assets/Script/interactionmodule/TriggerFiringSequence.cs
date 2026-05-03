using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class TriggerFiringSequence : MonoBehaviour
{
    [Header("UI Containers")]
    public GameObject step1Text;
    public GameObject step2Text;
    public GameObject step3Text;

    [Header("Buttons")]
    public Button openGrateButton;
    public Button addFuelButton;
    public Button closeGrateButton;
    public Button triggerFiringButton;
    public Button nextButton;

    [Header("Grate Animation")]
    public Animator grateAnimator;

    [Header("Fuel & Fire")]
    public GameObject fuelObject;
    public ParticleSystem fireParticles;

    [Header("Sounds")]
    public AudioClip grateOpenSFX;
    public AudioClip grateCloseSFX;
    public AudioClip fireLoopSFX;

    [Header("Voiceovers")]
    public AudioClip step1Voiceover;
    public AudioClip step2Voiceover;
    public AudioClip step3Voiceover;

    [Header("Shared Audio Sources")]
    [SerializeField] private AudioSource fireAudioSource;   
    [SerializeField] private AudioSource sfxAudioSource;    
    [SerializeField] private AudioSource voiceAudioSource;  
    private bool grateOpened;
    private bool fuelAdded;
    private bool grateClosed;
    private bool fireStarted;

    void Start()
    {
        
        if (fireAudioSource == null || sfxAudioSource == null || voiceAudioSource == null)
        {
            Debug.LogError("TriggerFiringSequence: AudioSources NOT assigned!");
            return;
        }

        if (grateAnimator != null)
            grateAnimator.enabled = false;

        if (fuelObject != null)
            fuelObject.SetActive(false);

        
        if (fireParticles != null)
        {
            fireParticles.gameObject.SetActive(true);
            fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            fireParticles.Clear();
        }

        ResetUI();
        SetupXRButtons();

        step1Text.SetActive(true);
        openGrateButton.gameObject.SetActive(true);

        PlayVoiceover(step1Voiceover);
    }

    void ResetUI()
    {
        step1Text.SetActive(false);
        step2Text.SetActive(false);
        step3Text.SetActive(false);

        openGrateButton.gameObject.SetActive(false);
        addFuelButton.gameObject.SetActive(false);
        closeGrateButton.gameObject.SetActive(false);
        triggerFiringButton.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);
    }


    void OnOpenGrate()
    {
        if (grateOpened) return;
        grateOpened = true;

        if (grateAnimator != null)
        {
            grateAnimator.enabled = true;
            grateAnimator.Play("GrateOpen", 0, 0f);
            grateAnimator.SetTrigger("grateopen");
        }

        sfxAudioSource.PlayOneShot(grateOpenSFX);

        step1Text.SetActive(false);
        openGrateButton.gameObject.SetActive(false);

        step2Text.SetActive(true);
        addFuelButton.gameObject.SetActive(true);

        PlayVoiceover(step2Voiceover);
    }

    void OnAddFuel()
    {
        if (fuelAdded || !grateOpened) return;
        fuelAdded = true;

        if (fuelObject != null)
            fuelObject.SetActive(true);

        step2Text.SetActive(false);
        addFuelButton.gameObject.SetActive(false);

        step3Text.SetActive(true);
        closeGrateButton.gameObject.SetActive(true);

        PlayVoiceover(step3Voiceover);
    }

    void OnCloseGrate()
    {
        if (grateClosed || !fuelAdded) return;
        grateClosed = true;

        if (grateAnimator != null)
        {
            grateAnimator.enabled = true;
            grateAnimator.ResetTrigger("grateopen");
            grateAnimator.SetTrigger("grateclose");
        }

        sfxAudioSource.PlayOneShot(grateCloseSFX);

        closeGrateButton.gameObject.SetActive(false);
        triggerFiringButton.gameObject.SetActive(true);
    }

    void OnTriggerFiring()
    {
        if (fireStarted || !grateClosed) return;
        fireStarted = true;

        if (fireParticles != null)
        {
            fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            fireParticles.Clear();
            fireParticles.Play();
        }

        fireAudioSource.Stop();
        fireAudioSource.clip = fireLoopSFX;
        fireAudioSource.loop = true;
        fireAudioSource.Play();

        step3Text.SetActive(false);
        triggerFiringButton.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(true);
    }

    void OnNext()
    {
        fireAudioSource.Stop();
        voiceAudioSource.Stop();

        if (PhaseManager.Instance != null)
            PhaseManager.Instance.GoToNextPhase();
        else
            Debug.LogError("TriggerFiringSequence: PhaseManager not found");
    }



    void PlayVoiceover(AudioClip clip)
    {
        if (clip == null) return;

        voiceAudioSource.Stop();
        voiceAudioSource.PlayOneShot(clip);
    }

    void SetupXRButtons()
    {
        BindXR(openGrateButton, OnOpenGrate);
        BindXR(addFuelButton, OnAddFuel);
        BindXR(closeGrateButton, OnCloseGrate);
        BindXR(triggerFiringButton, OnTriggerFiring);
        BindXR(nextButton, OnNext);
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
