using System.Collections.Generic;
using UnityEngine;
using static OVRHand;

public class Draw : MonoBehaviour
{
    #region Private Fields

    private List<LineRenderer> lines = new List<LineRenderer>();
    private bool wasPinch= false;
    private OVRHand hand;
    private OVRSkeleton skeleton;
    private LineRenderer currentLine;
    private Transform indexTranf;
    private bool isInit = false;

    #endregion

    #region Public Fields

    public LineRenderer linePrefab;
    public Transform lineContainer;

    #endregion

    #region MonoBehaviour Callbacks

    private void Awake()
    {
        hand = GetComponent<OVRHand>();
        skeleton = GetComponent<OVRSkeleton>();
    }

    private void Update()
    {
        if (!isInit)
        {
            if (skeleton.Bones.Count == 0)
                return;
            for (int i = 0; i < skeleton.Bones.Count; i++)
            {
                if (skeleton.Bones[i].Id == OVRSkeleton.BoneId.Hand_Index3)
                {
                    indexTranf = skeleton.Bones[i].Transform;
                    break;
                }
            }
            if (indexTranf != null)
            {
                isInit = true;
            }
            else
                return;
        }


        bool isIndexFingerPinching = hand.GetFingerIsPinching(HandFinger.Index);

        if (!wasPinch && isIndexFingerPinching)
        {
            currentLine = Instantiate(linePrefab, lineContainer);
            currentLine.positionCount++;
            currentLine.useWorldSpace = false;
            currentLine.SetPosition(currentLine.positionCount - 1, indexTranf.position);
            lines.Add(currentLine);
            wasPinch = true;
        }
        else if (wasPinch && isIndexFingerPinching)
        {
            currentLine.positionCount++;
            currentLine.SetPosition(currentLine.positionCount - 1, indexTranf.position);
        }
        else if (wasPinch && !isIndexFingerPinching)
        {
            wasPinch = false;
        }


    }

    #endregion

    #region Public Methods

    public void ClearDraws()
    {
        foreach (LineRenderer item in lines)
            Destroy(item);
    }

    #endregion
}
