using TMPro;
using UnityEngine;

public class DebugMatrix : MonoBehaviour
{
    // Debugging 
    public TextMeshProUGUI[] TitleCols = new TextMeshProUGUI[10];
    public TextMeshProUGUI[] TitleRows = new TextMeshProUGUI[10];
    public Transform[] rows = new Transform[10];

    public void DrawMatrix(Vertex[] headers, float[,] data)
    {
        for (int i = 0; i < TitleCols.Length; i++)
        {
            TitleCols[i].text = headers[i].name;
            TitleRows[i].text = headers[i].name;
        }

        for (int i = 0; i < 10; i++)
        {
            TextMeshProUGUI[] cols = rows[i].GetComponentsInChildren<TextMeshProUGUI>();
            for (int j = 0; j < 10; j++)
            {
                cols[j].text = data[i,j].ToString();
            }
        }

    }
}
