using System.Collections;
using UnityEngine;

public class SimpleExplosionEffect : MonoBehaviour
{
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float targetScale = 4f;
    [SerializeField] private Color startColor = new Color(1f, 0.5f, 0f, 1f);
    [SerializeField] private Color endColor = new Color(1f, 0f, 0f, 0f);

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        transform.localScale = Vector3.zero;
    }

    private void Start()
    {
        StartCoroutine(ExplodeRoutine());
    }

    private IEnumerator ExplodeRoutine()
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;

            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * targetScale, progress);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(startColor, endColor, progress);
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
