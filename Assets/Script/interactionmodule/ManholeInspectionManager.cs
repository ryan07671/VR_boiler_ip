using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ManholeInspectionManager : MonoBehaviour
{
    [Header("3D Models")]
    public Transform manholeCover;
    public Transform screw1;
    public Transform screw2;
    public GameObject scalesGroup;

    [Header("UI Canvases")]
    public GameObject canvasOutside;
    public GameObject canvasInside;

    [Header("Outside UI Elements")]
    public TextMeshProUGUI outsideInstructionsText;
    public Button btnInspect;
    public Button btnClose;

    [Header("Inside UI Elements")]
    public Button btnMaintenance;
    public Button btnSkip;

    [Header("Audio Feedback")]
    [Tooltip("The AudioSource that will play the sound (e.g., attach an AudioSource to this object)")]
    public AudioSource sfxAudioSource;
    [Tooltip("The buzzer sound to play when the user tries to skip")]
    public AudioClip warningBuzzSound;

    [Header("Voiceovers")]
    [Tooltip("The AudioSource dedicated to playing voiceovers")]
    public AudioSource voiceAudioSource;
    public AudioClip introVoiceover;
    public AudioClip decisionVoiceover;
    public AudioClip closingVoiceover;

    [Header("Animation Settings")]
    public Vector3 coverOpenOffset = new Vector3(0, 0, -1.5f);
    public Vector3 screwOpenOffset = new Vector3(0, 0, -0.5f);
    public float animationDuration = 2.0f;

    // State memory
    private Vector3 coverStartPos;
    private Vector3 screw1StartPos;
    private Vector3 screw2StartPos;
    private bool hasInitialized = false;

    // Text strings for the OUTSIDE canvas
    private string introText = "To inspect the steam drum, open the manhole and inspect.";
    private string closingText = "The boiler must be tightly shut for safe operation. Click 'Close Manhole' to do the same.";

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
        // Save original positions for closing later
        if (manholeCover != null) coverStartPos = manholeCover.localPosition;
        if (screw1 != null) screw1StartPos = screw1.localPosition;
        if (screw2 != null) screw2StartPos = screw2.localPosition;

        // Bind buttons securely for VR
        BindVRButton(btnInspect, OnInspectClicked);
        BindVRButton(btnMaintenance, OnMaintenanceClicked);
        BindVRButton(btnSkip, OnSkipClicked);
        BindVRButton(btnClose, OnCloseClicked);
        // Set initial state
        canvasOutside.SetActive(true);
        canvasInside.SetActive(false);

        outsideInstructionsText.text = introText;

        // Hide buttons initially, they will be shown after audio
        btnInspect.gameObject.SetActive(false);
        btnClose.gameObject.SetActive(false);

        // Play Intro Audio and show Inspect button when done
        StartCoroutine(PlayVoiceAndShowButtons(introVoiceover, btnInspect));
    }

    // Helper method to bind XR VR clicks, with a fallback to standard UI clicks
    private void BindVRButton(Button btn, UnityEngine.Events.UnityAction action)
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
            btn.onClick.AddListener(new UnityEngine.Events.UnityAction(action));
        }
    }

    // --- HELPER COROUTINE FOR VOICEOVERS ---
    IEnumerator PlayVoiceAndShowButtons(AudioClip clip, params Button[] buttonsToShow)
    {
        if (clip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.PlayOneShot(clip);
            // Wait while the audio is actively playing
            yield return new WaitWhile(() => voiceAudioSource.isPlaying);
        }
        else
        {
            // Fallback just in case audio is missing, so you don't get stuck
            yield return new WaitForSeconds(3f);
        }

        // Show the buttons after the wait is over
        foreach (var btn in buttonsToShow)
        {
            if (btn != null) btn.gameObject.SetActive(true);
        }
    }

    // --- BUTTON ACTIONS ---

    void OnInspectClicked()
    {
        if (voiceAudioSource != null) voiceAudioSource.Stop();
        canvasOutside.SetActive(false);
        StartCoroutine(OpenManholeRoutine());
    }

    void OnMaintenanceClicked()
    {
        if (scalesGroup != null)
        {
            scalesGroup.SetActive(false); // Make scales disappear
        }
        PrepareForClosing();
    }

    void OnSkipClicked()
    {
        if (sfxAudioSource != null && warningBuzzSound != null)
        {
            sfxAudioSource.PlayOneShot(warningBuzzSound);
        }
    }

    void PrepareForClosing()
    {
        if (voiceAudioSource != null) voiceAudioSource.Stop();
        canvasInside.SetActive(false); // Turn off the inside UI
        canvasOutside.SetActive(true); // Turn the outside UI back on

        // Swap the text and buttons on the Outside Canvas
        outsideInstructionsText.text = closingText;
        btnInspect.gameObject.SetActive(false);
        btnClose.gameObject.SetActive(true);
        StartCoroutine(PlayVoiceAndShowButtons(closingVoiceover, btnClose));
    }

    void OnCloseClicked()
    {
        if (voiceAudioSource != null) voiceAudioSource.Stop();
        canvasOutside.SetActive(false);
        StartCoroutine(CloseManholeRoutine());
    }

    // --- ANIMATIONS ---

    IEnumerator OpenManholeRoutine()
    {
        // 1. Unscrew
        float timer = 0;
        while (timer < animationDuration)
        {
            timer += Time.deltaTime;
            float t = timer / animationDuration;

            if (screw1 != null)
            {
                screw1.Rotate(0, 0, -360 * Time.deltaTime);
                screw1.localPosition = Vector3.Lerp(screw1StartPos, screw1StartPos + screwOpenOffset, t);
            }
            if (screw2 != null)
            {
                screw2.Rotate(0, 0, -360 * Time.deltaTime);
                screw2.localPosition = Vector3.Lerp(screw2StartPos, screw2StartPos + screwOpenOffset, t);
            }
            yield return null;
        }

        if (screw1 != null) screw1.gameObject.SetActive(false);
        if (screw2 != null) screw2.gameObject.SetActive(false);

        // 2. Open Cover
        timer = 0;
        while (timer < animationDuration)
        {
            timer += Time.deltaTime;
            float t = timer / animationDuration;
            if (manholeCover != null)
                manholeCover.localPosition = Vector3.Lerp(coverStartPos, coverStartPos + coverOpenOffset, t);
            yield return null;
        }

        // 3. Show inside UI
        canvasInside.SetActive(true);
        // Hide buttons initially
        btnMaintenance.gameObject.SetActive(false);
        btnSkip.gameObject.SetActive(false);

        // Play Decision Audio and show Maintenance & Skip buttons when done
        yield return StartCoroutine(PlayVoiceAndShowButtons(decisionVoiceover, btnMaintenance, btnSkip));
    }

    IEnumerator CloseManholeRoutine()
    {
        // 1. Close Cover
        float timer = 0;
        Vector3 currentCoverPos = manholeCover.localPosition;
        while (timer < animationDuration)
        {
            timer += Time.deltaTime;
            float t = timer / animationDuration;
            if (manholeCover != null)
                manholeCover.localPosition = Vector3.Lerp(currentCoverPos, coverStartPos, t);
            yield return null;
        }

        // 2. Screw back in
        if (screw1 != null) screw1.gameObject.SetActive(true);
        if (screw2 != null) screw2.gameObject.SetActive(true);

        timer = 0;
        Vector3 currentScrew1Pos = screw1 != null ? screw1.localPosition : Vector3.zero;
        Vector3 currentScrew2Pos = screw2 != null ? screw2.localPosition : Vector3.zero;

        while (timer < animationDuration)
        {
            timer += Time.deltaTime;
            float t = timer / animationDuration;

            if (screw1 != null)
            {
                screw1.Rotate(0, 0, 360 * Time.deltaTime);
                screw1.localPosition = Vector3.Lerp(currentScrew1Pos, screw1StartPos, t);
            }
            if (screw2 != null)
            {
                screw2.Rotate(0, 0, 360 * Time.deltaTime);
                screw2.localPosition = Vector3.Lerp(currentScrew2Pos, screw2StartPos, t);
            }
            yield return null;
        }

        // 3. Move to the next phase!
        if (PhaseManager.Instance != null)
        {
            PhaseManager.Instance.GoToNextPhase();
        }
    }
}