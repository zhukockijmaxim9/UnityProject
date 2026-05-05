using UnityEngine;
using TMPro;

public class UpgradeButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private int upgradeTypeIndex;

    public void Setup(string title, string description, int typeIndex)
    {
        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;
        upgradeTypeIndex = typeIndex;
    }

    public void OnClick()
    {
        // Вызываем метод в менеджере по индексу типа
        UpgradeManager.Instance.ApplyUpgrade((int)upgradeTypeIndex);
    }
}
