using UnityEngine;
using System.Collections;

public class SimpleExplosionEffect : MonoBehaviour
{
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float targetScale = 4f;
    [SerializeField] private Color startColor = new Color(1, 0.5f, 0, 1); // Оранжевый
    [SerializeField] private Color endColor = new Color(1, 0, 0, 0);   // Красный прозрачный

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        transform.localScale = Vector3.zero;
    }

    private void Start()
    {
        StartCoroutine(ExplodeRoutine());
    }

    private IEnumerator ExplodeRoutine()
    {
        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            // Расширяем круг
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * targetScale, t);
            
            // Меняем цвет и прозрачность
            if (sr != null)
            {
                sr.color = Color.Lerp(startColor, endColor, t);
            }

            yield return null;
        }

        // Удаляем эффект после завершения
        Destroy(gameObject);
    }
}
