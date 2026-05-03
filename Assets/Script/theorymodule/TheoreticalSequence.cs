using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class BoilerTrainingCorrectSequence : MonoBehaviour
{
    [System.Serializable]
    public class TextWithVoice
    {
        public GameObject textObject;
        public AudioClip voiceOver;
    }

    [System.Serializable]
    public class ComponentStep
    {
        public GameObject canvas;
        public AudioClip voiceOver;
        [Tooltip("Drag the 3D Model part here (e.g., the Furnace Mesh)")]
        public GameObject modelPart;
    }

    [System.Serializable]
    public class TimedAnimation
    {
        public Animator animator;
        public string stateName;
        public float duration;
    }

    [Header("Audio")]
    public AudioSource audioSource;

    public GameObject introCanvas;
    public TextWithVoice[] introTexts;
    public ComponentStep[] components;

    public ParticleSystem[] particleSystems;
    public TimedAnimation[] animations;
    public AudioClip workingPrincipleVoiceOver;

    public GameObject nextCanvas;
    public GameObject nextText;
    public AudioClip nextVoiceOver;
    public Button nextButton;

    [Header("Navigation Wisp")]
    [Tooltip("Drag your Wisp Prefab here")]
    public GameObject wispPrefab;
    public float travelDuration = 2.0f;
    private GameObject activeWisp;
    [Header("Highlight Settings")]
    public Color highlightColor = Color.yellow;
    public float highlightIntensity = 1.0f;

    // --- STATE VARIABLES ---
    private bool isListeningForSkip = false;
    private bool skipTriggered = false;

    // Input Latches
    private bool wasAPressedLastFrame = false;
    private bool wasBPressedLastFrame = false;
    private bool isPaused = false;

    private List<InputDevice> inputDevices = new List<InputDevice>();

    void Start()
    {
        ResetAll();
        StartCoroutine(MainSequence());
    }

    void Update()
    {
        if (isListeningForSkip)
        {
            // Keyboard Debug
            if (Input.GetKeyDown(KeyCode.Space)) { TriggerSkip(); return; }
            if (Input.GetKeyDown(KeyCode.P)) { TogglePause(); return; }

            // VR Input Check
            CheckVRInput();
        }
    }

    void CheckVRInput()
    {
        inputDevices.Clear();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, inputDevices);

        foreach (var device in inputDevices)
        {
            if (device.isValid)
            {
                // 1. Check 'A' Button (Primary) for SKIP
                bool isAPressed = false;
                if (device.TryGetFeatureValue(CommonUsages.primaryButton, out isAPressed))
                {
                    if (isAPressed && !wasAPressedLastFrame)
                    {
                        wasAPressedLastFrame = true;
                        TriggerSkip();
                    }
                    else if (!isAPressed) wasAPressedLastFrame = false;
                }

                // 2. Check 'B' Button (Secondary) for PAUSE
                bool isBPressed = false;
                if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out isBPressed))
                {
                    if (isBPressed && !wasBPressedLastFrame)
                    {
                        wasBPressedLastFrame = true;
                        TogglePause();
                    }
                    else if (!isBPressed) wasBPressedLastFrame = false;
                }
            }
        }
    }

    void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            if (audioSource != null) audioSource.Pause();
            Debug.Log("Sequence PAUSED");
        }
        else
        {
            if (audioSource != null) audioSource.UnPause();
            Debug.Log("Sequence RESUMED");
        }
    }

    void ResetAll()
    {
        if (introCanvas != null) introCanvas.SetActive(false);

        foreach (var t in introTexts)
        {
            if (t != null && t.textObject != null) t.textObject.SetActive(false);
        }

        foreach (var c in components)
        {
            if (c != null && c.canvas != null) c.canvas.SetActive(false);
            if (c != null) SetHighlight(c.modelPart, false);
        }

        foreach (var ps in particleSystems)
        {
            if (ps != null) ps.Stop();
        }

        foreach (var a in animations)
        {
            if (a != null && a.animator != null) a.animator.enabled = false;
        }

        if (nextCanvas != null) nextCanvas.SetActive(false);
        if (nextText != null) nextText.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (activeWisp != null) Destroy(activeWisp);

        isListeningForSkip = false;
        isPaused = false;
    }

    IEnumerator MainSequence()
    {
        yield return PlayIntro();
        yield return PlayComponents();
        yield return PlayWorkingPrinciple();
        yield return PlayNextCanvas();
    }

    IEnumerator PlayIntro()
    {
        if (introCanvas != null) introCanvas.SetActive(true);
        foreach (var step in introTexts)
        {
            if (step != null && step.textObject != null) step.textObject.SetActive(true);

            if (step != null && step.voiceOver != null)
            {
                PlayVoice(step.voiceOver);
                yield return StartCoroutine(WaitForAudioOrSkip(step.voiceOver.length));
            }
            else
            {
                // Fallback if audio is missing
                yield return StartCoroutine(WaitForAudioOrSkip(3f));
            }

            if (step != null && step.textObject != null) step.textObject.SetActive(false);
        }
        if (introCanvas != null) introCanvas.SetActive(false);
    }

    IEnumerator PlayComponents()
    {
        Vector3 lastPosition = introCanvas != null ? introCanvas.transform.position : Vector3.zero;

        if (wispPrefab != null)
        {
            activeWisp = Instantiate(wispPrefab, lastPosition, Quaternion.identity);
            activeWisp.SetActive(false);
        }
        foreach (var step in components)
        {
            if (step == null) continue;

            Vector3 targetPosition = step.canvas != null ? step.canvas.transform.position : lastPosition;

            if (activeWisp != null && step.canvas != null)
            {
                isListeningForSkip = false;
                activeWisp.transform.position = lastPosition;
                activeWisp.SetActive(true);
                TrailRenderer tr = activeWisp.GetComponent<TrailRenderer>();
                if (tr != null) { tr.Clear(); tr.time = 100.0f; }
                yield return StartCoroutine(MoveWispSmoothly(lastPosition, targetPosition));
                activeWisp.SetActive(false);
            }

            if (step.canvas != null) step.canvas.SetActive(true);
            SetHighlight(step.modelPart, true);

            if (step.voiceOver != null)
            {
                PlayVoice(step.voiceOver);
                yield return StartCoroutine(WaitForAudioOrSkip(step.voiceOver.length));
            }
            else
            {
                // Fallback timer if audio is missing
                yield return StartCoroutine(WaitForAudioOrSkip(4f));
            }

            if (step.canvas != null) step.canvas.SetActive(false);
            SetHighlight(step.modelPart, false);
            if (audioSource != null) audioSource.Stop();

            lastPosition = targetPosition;
        }
        if (activeWisp != null) Destroy(activeWisp);
    }

    IEnumerator WaitForAudioOrSkip(float duration)
    {
        skipTriggered = false;
        float timer = 0;

        isListeningForSkip = true;

        while (timer < duration && !skipTriggered)
        {
            if (isPaused)
            {
                yield return null;
                continue;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        isListeningForSkip = false;
    }

    void TriggerSkip()
    {
        if (isPaused) TogglePause();
        skipTriggered = true;
    }

    IEnumerator MoveWispSmoothly(Vector3 startPos, Vector3 endPos)
    {
        float elapsedTime = 0;
        while (elapsedTime < travelDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / travelDuration;
            float smoothT = Mathf.SmoothStep(0.0f, 1.0f, t);
            if (activeWisp != null) activeWisp.transform.position = Vector3.Lerp(startPos, endPos, smoothT);
            yield return null;
        }
        if (activeWisp != null) activeWisp.transform.position = endPos;
    }

    IEnumerator PlayWorkingPrinciple()
    {
        if (workingPrincipleVoiceOver != null) PlayVoice(workingPrincipleVoiceOver);

        foreach (var ps in particleSystems)
        {
            if (ps != null) ps.Play();
        }

        foreach (var anim in animations)
        {
            if (anim != null && anim.animator != null)
            {
                anim.animator.enabled = true;
                if (!string.IsNullOrEmpty(anim.stateName)) anim.animator.Play(anim.stateName);
                yield return new WaitForSeconds(anim.duration);
            }
        }
    }

    IEnumerator PlayNextCanvas()
    {
        if (nextCanvas != null) nextCanvas.SetActive(true);
        if (nextText != null) nextText.SetActive(true);

        if (nextVoiceOver != null)
        {
            PlayVoice(nextVoiceOver);
            yield return new WaitForSeconds(nextVoiceOver.length);
        }
        else
        {
            yield return new WaitForSeconds(3f); // Fallback for missing audio
        }

        if (nextButton != null) nextButton.gameObject.SetActive(true);
    }

    void PlayVoice(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        isPaused = false;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    void SetHighlight(GameObject target, bool active)
    {
        if (target == null) return;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            foreach (var mat in rend.materials)
            {
                if (active)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", highlightColor * highlightIntensity);
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.black);
                }
            }
        }
    }
}