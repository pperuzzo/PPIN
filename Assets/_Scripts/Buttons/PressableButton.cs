using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Alens.OpenLab
{
    public class PressableButton : MonoBehaviour
    {
        #region Enum

        public enum PressedState
        {
            None = 0,
            FullPressed = 1,
            FullReleased = 2,
        }

        #endregion

        #region Events

        public delegate void PressableButtonHandler(bool v, PressableButton sender);
        public event PressableButtonHandler ButtonEvent;
        public event PressableButtonHandler FullToggleEvent;

        #endregion

        #region Private Fields

        WaitForSeconds waitForSeconds = new WaitForSeconds(1f);

        #endregion

        #region Protected Fields

        protected AudioSource audioSourceClick;
        protected PressedState pressedState = PressedState.FullReleased;
        protected bool wasPressed = false;

        #endregion

        #region Protected Internal Fields

        protected internal BoxCollider frontCollider;
        protected internal Rigidbody buttonRigidbody;
        protected internal Rigidbody mainRigidbody;
        protected internal Vector3 initialLocalPosition = Vector3.zero;

        #endregion

        #region Public Fields

        [SerializeField]
        public ButtonEvents OnClickEvent;
        [SerializeField]
        public ButtonEvents OnStayEvent;

        public float min;
        public bool keepPressed = false;
        public bool isPressed = false;
        public float buttonThreshold;
        public bool shouldCheckFullToggle = false;
        public bool shouldForceRelase = false;
        public bool debugMode;
        public bool shouldReset;

        #endregion

        #region MonoBehaviour Callbacks

        protected virtual void Awake()
        {
            audioSourceClick = GetComponent<AudioSource>();
            Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
            mainRigidbody = rbs[0];
            buttonRigidbody = rbs[1];
            initialLocalPosition = buttonRigidbody.transform.localPosition;
            frontCollider = GetComponentsInChildren<BoxCollider>(true)[1];
        }

        protected virtual void LateUpdate()
        {
            UpdateButtonConstraints();
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (isPressed)
                return;
            if (other.name == "FrontButton")
            {
                if (keepPressed)
                {
                    isPressed = !isPressed;
                    OnButtonEvent(true);
                }
                audioSourceClick.Play();
                OnClickEvent?.Invoke();
                if (shouldForceRelase)
                {
                    OnButtonEvent(true);
                    StartCoroutine(ForceRelease());
                }
            }
        }

        protected virtual void OnTriggerStay(Collider other)
        {
            if (other.name == "FrontButton")
                OnStayEvent?.Invoke();
        }

        protected virtual void OnDisable()
        {
            if (isPressed || shouldReset)
            {
                isPressed = false;
                frontCollider.isTrigger = false;
                buttonRigidbody.transform.localPosition = new Vector3
                (
                    initialLocalPosition.x,
                    initialLocalPosition.y,
                    initialLocalPosition.z
                );
            }
        }

        #endregion

        #region Private Methods

        protected virtual void UpdateButtonConstraints()
        {
            if (isPressed)
            {
                buttonRigidbody.transform.localPosition = new Vector3
                (
                    initialLocalPosition.x,
                    initialLocalPosition.y,
                    min
                );
            }
            else
            {
                buttonRigidbody.transform.localPosition = new Vector3
                (
                    initialLocalPosition.x,
                    initialLocalPosition.y,
                    Mathf.Clamp(buttonRigidbody.transform.localPosition.z, min, initialLocalPosition.z)
                );

                if (shouldCheckFullToggle)
                {
                    if (wasPressed && (pressedState == PressedState.FullPressed || pressedState == PressedState.None) && Mathf.Abs(buttonRigidbody.transform.localPosition.z - initialLocalPosition.z) < buttonThreshold)
                    {
                        wasPressed = false;
                        pressedState = PressedState.FullReleased;
                        FullToggleEvent?.Invoke(false, this);
                    }
                    else if (!wasPressed && (pressedState == PressedState.FullReleased || pressedState == PressedState.None) && Mathf.Abs(buttonRigidbody.transform.localPosition.z - min) < buttonThreshold)
                    {
                        wasPressed = true;
                        pressedState = PressedState.FullPressed;
                        FullToggleEvent?.Invoke(true, this);
                    }
                    else
                    {
                        pressedState = PressedState.None;
                    }
                }
            }
        }

        #endregion

        #region Public Fields

        public void ForcePress()
        {
            isPressed = true;
        }

        public void ReleaseButton()
        {
            if (isPressed == false)
                return;
            isPressed = false;
        }

        public IEnumerator ForceRelease()
        {
            frontCollider.isTrigger = true;
            yield return waitForSeconds;
            frontCollider.isTrigger = false;
            yield break;
        }

        #endregion

        #region On Events

        public void OnButtonEvent(bool v)
        {
            ButtonEvent?.Invoke(v, this);
        }

        #endregion
    }

    [Serializable]
    public class ButtonEvents : UnityEvent { }
}
