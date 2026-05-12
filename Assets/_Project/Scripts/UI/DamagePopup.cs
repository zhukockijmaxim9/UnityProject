using System.Collections;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float moveSpeed = 1.5f;

    private TextMeshPro textMesh;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public void Setup(int damageAmount, Color textColor)
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshPro>();
        }

        textMesh.text = damageAmount.ToString();
        textMesh.color = textColor;
        transform.position += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.2f, 0.2f), 0f);

        StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        float timer = 0f;
        Color startColor = textMesh.color;

        while (timer < lifetime)
        {
            timer += Time.deltaTime;
            float progress = timer / lifetime;

            transform.position += Vector3.up * moveSpeed * Time.deltaTime;

            Color currentColor = startColor;
            currentColor.a = 1f - progress;
            textMesh.color = currentColor;

            yield return null;
        }

        Destroy(gameObject);
    }
}
