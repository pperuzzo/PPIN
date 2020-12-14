using UnityEngine;
using System;
using UniRx.Async;
using SQLite;

public class DBConnection : MonoBehaviour
{
	public SQLiteAsyncConnection connection;
	string[] lines;

	private async void Start()
    {
		connection = GameManager.Instance.connection;
        //lines = System.IO.File.ReadAllLines(@"C:\Users\pierl\Desktop\protein-link.txt");
        //Debug.Log(lines.Length);
        //await FillProteinLinks();
    }

	public async UniTask FillProteinLinks()
    {
		//int c = 0;
        for (int i = 8229715; i < lines.Length; i++)
        {
            string sep = " ";
            string[] splitcontent = lines[i].Split(sep.ToCharArray());

			var pi = new ProteinLink()
			{
				Protein1 = splitcontent[0],
				Protein2 = splitcontent[1],
				Score = Int32.Parse(splitcontent[2])
			};


			await connection.InsertAsync(pi);
			//if (c == 999)
			//{
			//	Debug.Log(i);
			//	c = 0;
			//}
   //         else
   //         {
			//	c++;
   //         }
		}
    }
}