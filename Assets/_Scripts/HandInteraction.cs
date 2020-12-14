using UnityEngine;

public class HandInteraction : MonoBehaviour
{
    #region Private Fields

    private GameObject hand
    {
        get => gameObject;
    }

    #endregion

    #region Public Field

    [Tooltip("Radius used for detecting particles collision")]
    public float radius = 0.2f;
    [Tooltip("Power to use ")]
    public float power = 3;
    [Tooltip("Layer where to detect particles collision")]
    public LayerMask mask;

    #endregion

    #region MonoBehaviour Callbacks

    private void Update()
    {
        Vector3 handPos = hand.transform.position;
        // Get the particles that are in the range of the hand radius
        Collider[] hitColliders = Physics.OverlapSphere(handPos, radius, mask);
        for (int i = 0; i < hitColliders.Length; i++)
        {
            // Get the rigidbody on the particle to apply the repulsion force
            Rigidbody rb = hitColliders[i].GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddExplosionForce(power, handPos, radius, 0f);
            }
        }
    }

    #endregion
}