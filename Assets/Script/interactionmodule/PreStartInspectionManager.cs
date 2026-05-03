using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PreStartInspectionManager : MonoBehaviour
{
    [System.Serializable]
    public class InstructionStep
    {
        public GameObject textChild;
        [Tooltip("Supports MP3 and M4A formats")]
        public AudioClip voiceOver;
        public float displayDuration = 3f;
    }

    [System.Serializable]
    public class ChecklistItem
    {
        public string equipmentName;
        public GameObject arrow3D;
        public Button checkButton;
    }

    [Header("Single Canvas")]
    [SerializeField] private Canvas instructionCanvas;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource voiceAudioSource; 
    [SerializeField] private AudioSource sfxAudioSource;   

    [Header("Instruction Steps")]
    [SerializeField] private List<InstructionStep> instructionSteps;

    [Header("Last Text Child (Checklist)")]
    [SerializeField] private GameObject checklistTextChild;
    [Tooltip("Supports MP3 and M4A formats")]
    [SerializeField] private AudioClip checklistVoiceOver;

    [Header("Checklist Items")]
    [SerializeField] private List<ChecklistItem> checklistItems;
    [SerializeField] private Button nextButton;

    [Header("Audio Feedback")]
    [Tooltip("Supports MP3 and M4A formats")]
    [SerializeField] private AudioClip correctSound;
    [Tooltip("Supports MP3 and M4A formats")]
    [SerializeField] private AudioClip incorrectSound;

    private int currentChecklistIndex = 0;

    void Start()
    {
        InitializeInspection();
    }

    void InitializeInspection()
    {
     
        foreach (var step in instructionSteps)
        {
            if (step.textChild != null)
                step.textChild.SetActive(false);
        }

        if (checklistTextChild != null)
            checklistTextChild.SetActive(false);

        foreach (var item in checklistItems)
        {
            if (item.arrow3D != null)
                item.arrow3D.SetActive(false);

            if (item.checkButton != null)
            {
                var interactable = item.checkButton.GetComponent<XRSimpleInteractable>();
                if (interactable != null)
                {
                    ChecklistItem capturedItem = item;
                    interactable.selectEntered.AddListener(_ => OnCheckButtonClicked(capturedItem));
                }
            }
        }

        if (nextButton != null)
        {
            var nextInteractable = nextButton.GetComponent<XRSimpleInteractable>();
            if (nextInteractable != null)
            {
                nextInteractable.selectEntered.AddListener(_ => OnNextButtonClicked());
            }
            nextButton.gameObject.SetActive(false);
        }

        StartCoroutine(PlayInstructionSequence());
    }

    IEnumerator PlayInstructionSequence()
    {
        for (int i = 0; i < instructionSteps.Count; i++)
        {
            var step = instructionSteps[i];

            if (step.textChild != null)
                step.textChild.SetActive(true);

            if (step.voiceOver != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.PlayOneShot(step.voiceOver);
                yield return new WaitWhile(() => voiceAudioSource.isPlaying);
            }
            else
            {
                yield return new WaitForSeconds(step.displayDuration);
            }

            if (step.textChild != null)
                step.textChild.SetActive(false);
        }

        StartChecklistPhase();
    }

    void StartChecklistPhase()
    {
        if (checklistTextChild != null)
            checklistTextChild.SetActive(true);

        if (checklistVoiceOver != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.PlayOneShot(checklistVoiceOver);
        }

        if (checklistItems.Count > 0 && checklistItems[0].arrow3D != null)
            checklistItems[0].arrow3D.SetActive(true);
    }

    void OnCheckButtonClicked(ChecklistItem clickedItem)
    {
        if (clickedItem == checklistItems[currentChecklistIndex])
        {
            
            if (correctSound != null && sfxAudioSource != null)
                sfxAudioSource.PlayOneShot(correctSound);

            clickedItem.checkButton.gameObject.SetActive(false);

            if (clickedItem.arrow3D != null)
                clickedItem.arrow3D.SetActive(false);

            currentChecklistIndex++;

            if (currentChecklistIndex < checklistItems.Count)
            {
                if (checklistItems[currentChecklistIndex].arrow3D != null)
                    checklistItems[currentChecklistIndex].arrow3D.SetActive(true);
            }
            else
            {
                if (nextButton != null)
                    nextButton.gameObject.SetActive(true);
            }
        }
        else
        {
            
            if (incorrectSound != null && sfxAudioSource != null)
                sfxAudioSource.PlayOneShot(incorrectSound);
        }
    }

    void OnNextButtonClicked()
    {
        if (voiceAudioSource != null)
            voiceAudioSource.Stop();

        if (PhaseManager.Instance != null)
            PhaseManager.Instance.GoToNextPhase();
    }
}