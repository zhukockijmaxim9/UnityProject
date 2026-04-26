using UnityEngine;
using System.Collections;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private AnimationCurve opacityCurve;
    [SerializeField] private AnimationCurve scaleCurve;

    private TextMeshPro textMesh;
    private float timer;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public void Setup(int damageAmount, Color textColor)
    {
        if (textMesh == null) textMesh = GetComponent<TextMeshPro>();
        
        textMesh.text = damageAmount.ToString();
        textMesh.color = textColor;
        timer = 0;
        
        // Немного случайного смещения, чтобы цифры не слипались
        transform.position += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.2f, 0.2f), 0);
        
        StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        Vector3 startScale = transform.localScale;
        Color startColor = textMesh.color;

        while (timer < lifetime)
        {
            timer += Time.deltaTime;
            float t = timer / lifetime;

            // Всплываем вверх
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;

            // Прозрачность и масштаб
            if (opacityCurve != null && opacityCurve.length > 0)
                startColor.a = opacityCurve.Evaluate(t);
            else
                startColor.a = 1f - t;

            if (scaleCurve != null && scaleCurve.length > 0)
                transform.localScale = startScale * scaleCurve.Evaluate(t);

            textMesh.color = startColor;
            yield return null;
        }

        // Возвращаем в пул (или удаляем, если не используешь пул для UI)
        Destroy(gameObject);
    }
}
