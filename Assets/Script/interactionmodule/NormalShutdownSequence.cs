using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NormalShutdownSequence : MonoBehaviour
{
    [Header("Phase Manager")]
    public PhaseManager phaseManager;

    [Header("Main UI Panel")]
    [Tooltip("Drag the main Canvas or Panel here so the script ensures it is turned ON")]
    public GameObject mainBackgroundPanel;

    [Header("Shutdown Canvases (Size = 6)")]
    public GameObject[] shutdownCanvases;


    [Header("Phase 1 – Reduce Boiler Load")]
    public Button fireReductionButton;
    public Transform fireObject;
    public GameObject cubeToDeactivate;
    public float fireScaleDownDuration = 10f;
    public AudioSource fireAudioSource;
    private Vector3 originalFireScale;

    
    [Header("Phase 2 – Isolate Boiler")]
    public Button coalIsolationButton;
    public GameObject[] coalChildren;
    public Transform fireTransform;
    public float coalVanishInterval = 1f;
    public float fireTransformDuration = 5f;

    
    [Header("Phase 3 – Natural Cooling")]
    public Animator pgAnimator1;
    public string pgAnim1TriggerName = "PlayCooling";

   
    [Header("Phase 4 – Venting at 25 psi")]
    public Button ventButton;
    public AudioSource ventAudioSource;
    public AudioClip ventSound;

   
    [Header("Phase 6 – Final Venting")]
    public Animator pgAnimator2;
    public string pgAnim2TriggerName = "PlayFinalVent";
    public Button nextPhaseButton;

    [Header("Voiceovers (Size = 6)")]
    public AudioSource voiceAudioSource;
    public AudioClip[] voiceClips;

   
    private int currentPhase = 0;
    private bool phaseComplete;
    private bool coalDone;
    private bool fireDone;
    private bool hasInitialized = false;
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
        if (fireObject != null) originalFireScale = fireObject.localScale;

        if (mainBackgroundPanel != null)
            mainBackgroundPanel.SetActive(true);

        foreach (var canvas in shutdownCanvases)
        {
            if (canvas != null) canvas.SetActive(false);
        }

        if (nextPhaseButton != null) nextPhaseButton.gameObject.SetActive(false);

        SetupXRButtons();
        StartCoroutine(ShutdownFlow());
    }


    void SetupXRButtons()
    {
        BindXR(fireReductionButton, OnFireReduction);
        BindXR(coalIsolationButton, OnCoalIsolation);
        BindXR(ventButton, OnVent);
        BindXR(nextPhaseButton, OnNextPhase);
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
        else
        {
            // Fallback for PC testing
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(new UnityEngine.Events.UnityAction(callback));
        }
    }

    IEnumerator ShutdownFlow()
    {
        yield return Phase1_ReduceLoad();
        yield return Phase2_IsolateBoiler();
        yield return Phase3_NaturalCooling();
        yield return Phase4_Venting();
        yield return Phase5_WaterLevelMaintenance();
        yield return Phase6_FinalVenting();
    }

    
    IEnumerator Phase1_ReduceLoad()
    {
        currentPhase = 1;
        if (shutdownCanvases.Length > 0 && shutdownCanvases[0] != null)
            shutdownCanvases[0].SetActive(true);
        PlayVoice(0);

        phaseComplete = false;
        yield return new WaitUntil(() => phaseComplete);

        if (shutdownCanvases.Length > 0 && shutdownCanvases[0] != null)
            shutdownCanvases[0].SetActive(false);
    }

    void OnFireReduction()
    {
        if (currentPhase != 1) return;

        fireReductionButton.gameObject.SetActive(false);

        if (cubeToDeactivate != null)
            cubeToDeactivate.SetActive(false);

        StartCoroutine(ScaleDownFire());
        
    }

    IEnumerator ScaleDownFire()
    {
        float t = 0f;

        while (t < fireScaleDownDuration)
        {
            t += Time.deltaTime;
            float k = t / fireScaleDownDuration;

            fireObject.localScale =
                Vector3.Lerp(originalFireScale, originalFireScale * 0.1f, k);

            if (fireAudioSource != null)
                fireAudioSource.volume = Mathf.Lerp(1f, 0f, k);

            yield return null;
        }

        if (fireAudioSource != null)
        {
            fireAudioSource.Stop();
            fireAudioSource.volume = 1f;
        }

        phaseComplete = true;
    }

    IEnumerator Phase2_IsolateBoiler()
    {
        currentPhase = 2;
        shutdownCanvases[1].SetActive(true);
        PlayVoice(1);

        phaseComplete = false;
        coalDone = false;
        fireDone = false;

        yield return new WaitUntil(() => phaseComplete);
        shutdownCanvases[1].SetActive(false);
    }

    void OnCoalIsolation()
    {
        if (currentPhase != 2) return;

        coalIsolationButton.gameObject.SetActive(false);

        StartCoroutine(VanishCoal());
        StartCoroutine(FireToZero());
    }

    IEnumerator VanishCoal()
    {
        foreach (var coal in coalChildren)
        {
            coal.SetActive(false);
            yield return new WaitForSeconds(coalVanishInterval);
        }

        coalDone = true;
        TryCompletePhase2();
    }

    IEnumerator FireToZero()
    {
        float t = 0f;
        Vector3 start = fireTransform != null ? fireTransform.localScale : Vector3.zero;

        while (t < fireTransformDuration)
        {
            t += Time.deltaTime;
            if (fireTransform != null)
                fireTransform.localScale = Vector3.Lerp(start, Vector3.zero, t / fireTransformDuration);
            yield return null;
        }

        fireDone = true;
        TryCompletePhase2();
    }

    void TryCompletePhase2()
    {
        if (coalDone && fireDone)
            phaseComplete = true;
    }


    IEnumerator Phase3_NaturalCooling()
    {
        currentPhase = 3;
        if (shutdownCanvases.Length > 2 && shutdownCanvases[2] != null)
            shutdownCanvases[2].SetActive(true);
        PlayVoice(2);

        if (pgAnimator1 != null)
            pgAnimator1.SetTrigger(pgAnim1TriggerName);

        float waitTime = 2f;
        if (voiceClips != null && voiceClips.Length > 2 && voiceClips[2] != null)
        {
            waitTime += voiceClips[2].length;
        }
        yield return new WaitForSeconds(voiceClips[2].length + 2f);
        if (shutdownCanvases.Length > 2 && shutdownCanvases[2] != null)
            shutdownCanvases[2].SetActive(false);
    }


    IEnumerator Phase4_Venting()
    {
        currentPhase = 4;
        if (shutdownCanvases.Length > 3 && shutdownCanvases[3] != null)
            shutdownCanvases[3].SetActive(true);
        PlayVoice(3);

        phaseComplete = false;
        yield return new WaitUntil(() => phaseComplete);

        if (shutdownCanvases.Length > 3 && shutdownCanvases[3] != null)
            shutdownCanvases[3].SetActive(false);
    }

    void OnVent()
    {
        if (currentPhase != 4) return;

        ventButton.gameObject.SetActive(false);

        if (ventAudioSource != null && ventSound != null)
            ventAudioSource.PlayOneShot(ventSound);

        phaseComplete = true;
    }


    IEnumerator Phase5_WaterLevelMaintenance()
    {
        currentPhase = 5;
        if (shutdownCanvases.Length > 4 && shutdownCanvases[4] != null)
            shutdownCanvases[4].SetActive(true);
        PlayVoice(4);

        float waitTime = 2f;
        if (voiceClips != null && voiceClips.Length > 4 && voiceClips[4] != null)
        {
            waitTime = voiceClips[4].length;
        }
        yield return new WaitForSeconds(voiceClips[4].length);
        if (shutdownCanvases.Length > 4 && shutdownCanvases[4] != null)
            shutdownCanvases[4].SetActive(false);
    }

    IEnumerator Phase6_FinalVenting()
    {
        currentPhase = 6;
        if (shutdownCanvases.Length > 5 && shutdownCanvases[5] != null)
            shutdownCanvases[5].SetActive(true);
        PlayVoice(5);

        if (pgAnimator2 != null)
            pgAnimator2.SetTrigger(pgAnim2TriggerName);

        if (nextPhaseButton != null) nextPhaseButton.gameObject.SetActive(true);

        phaseComplete = false;
        yield return new WaitUntil(() => phaseComplete);

        if (shutdownCanvases.Length > 5 && shutdownCanvases[5] != null)
            shutdownCanvases[5].SetActive(false);
        if (PhaseManager.Instance != null)
        {
            PhaseManager.Instance.GoToNextPhase();
        }
    }

    void OnNextPhase()
    {
        if (currentPhase != 6) return;

        if (nextPhaseButton != null) nextPhaseButton.gameObject.SetActive(false);
        phaseComplete = true;
    }

    void PlayVoice(int index)
    {
        if (voiceClips == null || index >= voiceClips.Length || voiceClips[index] == null)
        {
            Debug.LogWarning("Missing voice clip at index " + index);
            return;
        }

        if (voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.PlayOneShot(voiceClips[index]);
        }
    }
}
