using UnityEngine;

public class SCV_AchievementManager : MonoBehaviour
{
    [SerializeField] private SCV_ProgressionManager progressionManager;
    [SerializeField] private GameObject quartermasterAchievement;
    [SerializeField] private float achievementDisplayDuration = 5f;

    private bool achievementUnlocked = false;

    private void Start()
    {
        if (quartermasterAchievement != null)
            quartermasterAchievement.SetActive(false);
    }

    private void Update()
    {
        if (achievementUnlocked)
            return;

        if (progressionManager == null)
            return;

        if (progressionManager.WorkshopComplete)
        {
            achievementUnlocked = true;

            Invoke(
                nameof(ShowQuartermasterAchievement),
                7f
            );
        }
    }

    private void ShowQuartermasterAchievement()
    {
        if (quartermasterAchievement != null)
        {
            quartermasterAchievement.SetActive(true);

            CancelInvoke(nameof(HideAchievement));

            Invoke(
                nameof(HideAchievement),
                achievementDisplayDuration
            );
        }

        Debug.Log(
            "Achievement unlocked: OUTPOST QUARTERMASTER"
        );
    }
    private void HideAchievement()
    {
        if (quartermasterAchievement != null)
            quartermasterAchievement.SetActive(false);
    }
}