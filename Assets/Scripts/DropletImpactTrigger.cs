using UnityEngine;

public class DropletImpactTrigger : MonoBehaviour
{
    // If your wafer is a solid collider
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name.Contains("Droplet") || collision.gameObject.name.Contains("PR"))
        {
            Destroy(collision.gameObject);
        }
    }

    // Backup: If you change the Wafer collider to "Is Trigger = True"
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name.Contains("Droplet") || other.gameObject.name.Contains("PR"))
        {
            Destroy(other.gameObject);
        }
    }
}