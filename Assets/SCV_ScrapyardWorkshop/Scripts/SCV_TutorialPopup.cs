using System.Collections;
using UnityEngine;

public class SCV_TutorialPopup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerHead;
    [SerializeField] private GameObject popupCanvas;

    [Header("Trigger")]
    [SerializeField] private float triggerDistance = 2.5f;

    [Header("Timing")]
    [SerializeField] private float displayDuration = 4f;

    bool integrated;
    private bool hasShown = false;
    private bool isShowing = false;

    private void Start()
    {
        integrated = DEF_IntegratedXRPresentation.IsIntegratedScene(gameObject.scene);
        if (popupCanvas != null)
            popupCanvas.SetActive(false);
    }

    private void Update()
    {
        // The integrated corner objective provides current-step instructions; proximity alone must not reveal later mechanics.
        if (integrated || hasShown || isShowing)
            return;

        if (playerHead == null)
            return;

        float distance = Vector3.Distance(
            playerHead.position,
            transform.position
        );

        if (distance <= triggerDistance)
        {
            StartCoroutine(ShowTutorial());
        }
    }

    private IEnumerator ShowTutorial()
    {
        isShowing = true;
        hasShown = true;

        Debug.Log(
            "Tutorial popup triggered: " +
            gameObject.name
        );

        if (popupCanvas != null)
            popupCanvas.SetActive(true);

        yield return new WaitForSeconds(displayDuration);

        if (popupCanvas != null)
            popupCanvas.SetActive(false);

        isShowing = false;
    }
}