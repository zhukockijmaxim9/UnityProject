using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float hitShakeForce = 0.5f;
    [SerializeField] private float shotShakeForce = 0.1f;

    private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    /// <summary>
    /// Trigger a screen shake with a specified force.
    /// </summary>
    /// <param name="force">Multiplier for the impulse force (default is 1.0).</param>
    public void Shake(float force = 1f)
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulseWithForce(force);
        }
    }

    public void ShakeOnHit()
    {
        Shake(hitShakeForce);
    }

    public void ShakeOnShot()
    {
        Shake(shotShakeForce);
    }
}
