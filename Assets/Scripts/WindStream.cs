using UnityEngine;

public class WindStream : MonoBehaviour
{
    public Vector3 windDirection = Vector3.right;
    public float windStrength = 5f;

    private ParticleSystem ps;
    private ParticleSystem.VelocityOverLifetimeModule vel;

    void Start()
    {
        ps = GetComponentInChildren<ParticleSystem>();

        if (ps != null)
        {
            ps.Play();

            vel = ps.velocityOverLifetime;
            vel.enabled = true;

            vel.x = windDirection.x * 2f;
            vel.y = windDirection.y * 2f;
            vel.z = windDirection.z * 2f;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;

        if (rb != null)
        {
            rb.AddForce(
                windDirection.normalized * windStrength,
                ForceMode.Acceleration
            );
        }
    }
}