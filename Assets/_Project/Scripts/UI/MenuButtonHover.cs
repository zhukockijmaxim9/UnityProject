using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float hoverScale = 1.035f;
    [SerializeField] private float pressScale = 0.985f;

    private Vector3 baseScale;
    private bool isHovered;
    private bool isPressed;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        ApplyScale();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        ApplyScale();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        ApplyScale();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        ApplyScale();
    }

    private void ApplyScale()
    {
        if (isPressed)
        {
            transform.localScale = baseScale * pressScale;
            return;
        }

        transform.localScale = isHovered ? baseScale * hoverScale : baseScale;
    }
}
