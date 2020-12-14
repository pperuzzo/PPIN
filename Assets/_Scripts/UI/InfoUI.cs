using UnityEngine;
using TMPro;
using OculusSampleFramework;

public class InfoUI : MonoBehaviour
{
    #region Private Fields

    private ButtonController controller;

    #endregion

    #region Public Fields

    public TextMeshProUGUI proteinName;
    public TextMeshProUGUI proteinSize;
    public TextMeshProUGUI proteinAnnotation;
    public Vertex currentProtein;
    public ProteinNetwork proteinNetwork;
    public GameObject pathUI;

    #endregion

    #region MonoBehaviour Callbacks

    public void Awake()
    {
        controller = transform.GetComponentInChildren<ButtonController>();
    }

    public void OnEnable()
    {
        controller.InteractableStateChanged.AddListener(Recenter);
    }

    public void OnDisable()
    {
        controller.InteractableStateChanged.RemoveListener(Recenter);
    }

    #endregion

    #region Private Methods

    public void Recenter(InteractableStateArgs obj)
    {
        bool inActionState = obj.NewInteractableState == InteractableState.ActionState;
        if (inActionState)
        {
            if (currentProtein == null)
                return;
            proteinNetwork.Recenter(currentProtein.id);
            currentProtein = null;
            gameObject.SetActive(false);
        }
    }

    #endregion

    #region Public Methods

    public void UpdateUI(Vertex v)
    {
        if (isActiveAndEnabled && currentProtein == v)
        {
            currentProtein = null;
            gameObject.SetActive(false);
            return;
        }
        else if (!isActiveAndEnabled)
        {
            if (pathUI.activeInHierarchy)
                pathUI.SetActive(false);
            gameObject.SetActive(true);
        }

        currentProtein = v;
        proteinName.text = currentProtein.name;
        proteinSize.text = currentProtein.size;
        proteinAnnotation.text = currentProtein.annotation;
    }

    #endregion
}
