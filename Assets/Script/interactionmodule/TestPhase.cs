using UnityEngine;

public class TestPhase : MonoBehaviour
{
    [Header("Canvas to Activate")]
    [SerializeField] private GameObject testCanvas;

    void OnEnable()
    {
        // Called automatically when PhaseManager activates this phase
        if (testCanvas != null)
        {
            testCanvas.SetActive(true);
            Debug.Log("TestPhase: Canvas activated");
        }
        else
        {
            Debug.LogWarning("TestPhase: No canvas assigned!");
        }
    }

    void OnDisable()
    {
        // Cleanup when phase ends
        if (testCanvas != null)
        {
            testCanvas.SetActive(false);
        }
    }
}
