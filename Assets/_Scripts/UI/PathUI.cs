using UnityEngine;
using TMPro;
using OculusSampleFramework;

public class PathUI : MonoBehaviour
{
    #region Private Fields

    #endregion

    #region Public Fields

    public TextMeshProUGUI midProtein;
    public TextMeshProUGUI finalProtein;
    public ProteinNetwork proteinNetwork;
    public ButtonController controller;

    #endregion

    #region MonoBehaviour Callbacks

    private void Awake()
    {
        controller.InteractableStateChanged.AddListener(DisplayPath);
    }

    #endregion

    #region Public Methods

    public void ChangeMidProtein(string proteinName)
    {
        midProtein.text = proteinName;
    }

    public void ChangeFinalProtein(string proteinName)
    {
        finalProtein.text = proteinName;
    }

    public void DisplayPath(InteractableStateArgs obj) 
    {
        if (string.IsNullOrEmpty(midProtein.text) || string.IsNullOrEmpty(finalProtein.text))
            return;

        bool inActionState = obj.NewInteractableState == InteractableState.ActionState;
        if (inActionState)
        {
            proteinNetwork.GetShortestPath();
        }
    }

    #endregion
}
