using UnityEngine;

namespace Alens.OpenLab
{
    public class ButtonFront : MonoBehaviour
    {
        #region Private Fields

        protected PressableButton button;

        #endregion

        #region Public Fields

        public bool debugButton = false;

        #endregion

        #region MonoBehaviour Callbacks

        protected virtual void Awake()
        {
            button = GetComponentInParent<PressableButton>();
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.name == "Collider")
            {
                if (button.isPressed)
                {
                    button.ReleaseButton();
                }
            }
        }

        #endregion
    }
}