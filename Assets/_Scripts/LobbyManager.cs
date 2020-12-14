using System.Collections;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    #region Private Fields

    private bool isAnimating = false;

    #endregion

    #region Public Fields

    public Particles particleSys;
    public CanvasGroup backCanvas;
    public CanvasGroup frontButton;
    public CanvasGroup backButton;
    public GameObject interactableToolsSDKDriver;
    public GameObject protein;
    public Transform button;
    public GameObject keyboard;

    #endregion

    #region Private Methods

    private IEnumerator LerpVertices()
    {
        bool iter = true;
        while (iter)
        {
            bool done = true;
            for (int i = 0; i < particleSys.particles.Length; i++)
            {
                if ((Mathf.Abs(particleSys.particles[i].transform.position.x - button.position.x) >= 0.01f) ||
                    (Mathf.Abs(particleSys.particles[i].transform.position.y - button.position.y) >= 0.01f) ||
                    (Mathf.Abs(particleSys.particles[i].transform.position.z - button.position.z) >= 0.01f))
                {
                    particleSys.particles[i].transform.position = new Vector3(
                        Mathf.Lerp(particleSys.particles[i].transform.position.x, button.position.x, Time.deltaTime * 3f),
                        Mathf.Lerp(particleSys.particles[i].transform.position.y, button.position.y, Time.deltaTime * 3f),
                        Mathf.Lerp(particleSys.particles[i].transform.position.z, button.position.z, Time.deltaTime * 3f)
                        );
                    done = false;
                }
            }
            if (done)
                iter = false;
            yield return null;
        }
        for (int i = 0; i < particleSys.particles.Length; i++)
            particleSys.particles[i].SetActive(false);

        StartCoroutine(Hide());
        yield break;
    }

    private IEnumerator Hide()
    {
        bool done = false;
        while (!done)
        {
            done = true;
            if (Mathf.Abs(backCanvas.alpha - 0) >= 0.005f)
            {
                frontButton.alpha -= 0.025f;
                backButton.alpha -= 0.025f;
                backCanvas.alpha -= 0.025f;

                done = false;
            }

            yield return null;
        }

        backButton.gameObject.SetActive(false);
        backCanvas.gameObject.SetActive(false);
        frontButton.gameObject.SetActive(false);
        frontButton.transform.parent.gameObject.SetActive(false);
        interactableToolsSDKDriver.SetActive(true);
        keyboard.SetActive(true);
        GameManager.Instance.ToggleHandsInteraction(false);
        yield break;
    }

    #endregion

    #region Public Methods

    public void StartExperience()
    {
        if (isAnimating)
            return;
        isAnimating = true;
        particleSys.enabled = false;
        protein.SetActive(false);

        for (int i = 0; i < particleSys.allLines.Count; i++)
            particleSys.allLines[i].gameObject.SetActive(false);

        StartCoroutine(LerpVertices());
    }

    #endregion
}
