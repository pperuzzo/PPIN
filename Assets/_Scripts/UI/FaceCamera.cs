using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    #region Private Fields

    Transform cameraTransform;

    #endregion

    #region MonoBehaviour Callbacks

    void Awake()
    {
        cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward, cameraTransform.rotation * Vector3.up);
    }

    #endregion
}
