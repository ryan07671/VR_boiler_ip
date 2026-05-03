using System.Collections.Generic;
using UnityEngine;

public class PhaseManager : MonoBehaviour
{
    public static PhaseManager Instance;

    [Header("Training Phases")]
    [SerializeField] private List<GameObject> phases = new List<GameObject>();

    private int currentPhaseIndex = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {

        for (int i = 0; i < phases.Count; i++)
        {
            if (phases[i] != null)
            {
                phases[i].SetActive(i == 0);
            }
        }

        currentPhaseIndex = 0;
        Debug.Log($"PhaseManager: Started with phase 0 - {(phases[0] != null ? phases[0].name : "NULL")}");
    }

    public void GoToNextPhase()
    {
        Debug.Log($"PhaseManager: GoToNextPhase called. Current index: {currentPhaseIndex}");

        if (currentPhaseIndex < phases.Count && phases[currentPhaseIndex] != null)
        {
            phases[currentPhaseIndex].SetActive(false);
            Debug.Log($"PhaseManager: Deactivated phase {currentPhaseIndex} - {phases[currentPhaseIndex].name}");
        }

        currentPhaseIndex++;

        if (currentPhaseIndex < phases.Count && phases[currentPhaseIndex] != null)
        {
            phases[currentPhaseIndex].SetActive(true);
            Debug.Log($"PhaseManager: Activated phase {currentPhaseIndex} - {phases[currentPhaseIndex].name}");
        }
        else
        {
            Debug.Log("PhaseManager: No more phases - Training complete!");
        }
    }

    public void GoToPhase(int phaseIndex)
    {
        if (phaseIndex < 0 || phaseIndex >= phases.Count)
        {
            Debug.LogError($"PhaseManager: Invalid phase index {phaseIndex}");
            return;
        }

        if (currentPhaseIndex < phases.Count && phases[currentPhaseIndex] != null)
        {
            phases[currentPhaseIndex].SetActive(false);
        }

        currentPhaseIndex = phaseIndex;
        if (phases[currentPhaseIndex] != null)
        {
            phases[currentPhaseIndex].SetActive(true);
            Debug.Log($"PhaseManager: Jumped to phase {currentPhaseIndex} - {phases[currentPhaseIndex].name}");
        }
    }

    public int GetCurrentPhaseIndex()
    {
        return currentPhaseIndex;
    }

    public int GetTotalPhases()
    {
        return phases.Count;
    }
}