using UnityEngine;

public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private GameObject barBackground;
    [SerializeField] private GameObject barForeground;
    [SerializeField] private Vector3 offset = new Vector3(0, 1.2f, 0);

    private EnemyAI bossAI;
    private int maxHealth;
    private float initialScaleX;

    void Start()
    {
        bossAI = GetComponentInParent<EnemyAI>();
        if (bossAI != null)
        {
            // Если это НЕ босс, нам не нужна шкала здоровья - удаляем её
            if (bossAI.startingArchetype != EnemyAI.EnemyArchetype.Boss)
            {
                Destroy(gameObject);
                return;
            }

            maxHealth = bossAI.currentHealth;
            if (barForeground != null)
            {
                initialScaleX = barForeground.transform.localScale.x;
            }
        }
    }

    void Update()
    {
        if (bossAI == null || barForeground == null) return;

        // Поворачиваем шкалу всегда к камере (чтобы не крутилась вместе с боссом)
        transform.rotation = Quaternion.identity;

        // Теперь читаем АКТУАЛЬНОЕ здоровье
        float healthPercent = (float)bossAI.currentHealth / maxHealth;
        healthPercent = Mathf.Clamp01(healthPercent);
        
        // Сжимаем полоску
        Vector3 newScale = barForeground.transform.localScale;
        newScale.x = initialScaleX * healthPercent;
        barForeground.transform.localScale = newScale;

        // Если босс умер - удаляем шкалу (или она сама удалится с ним)
    }
}
