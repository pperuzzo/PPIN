using HandPosing.Interaction;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using UniRx.Async;
using UnityEngine;
using UnityEngine.Networking;

public enum StateType
{
	Default = 0,
	Search = 1,
	Interact = 2,
	Path = 3,
	Edit = 4,
	Draw = 5
}

public class GameManager : Singleton<GameManager>
{
    #region Private Fields

    public SQLiteAsyncConnection connection;
	public List<ProteinInfo> proteinsInfo = new List<ProteinInfo>();

	#endregion

	#region Public Fields

	public StateType currentState = StateType.Default;

	public HandInteraction leftHandInteraction;
	public HandInteraction rightHandInteraction;

	public Draw leftDraw;
	public Draw rightDraw;

	public ProteinNetwork proteinNetwork;
	public GameObject menu;
	public GameObject keyboard;
	public GameObject infoUI;
	public GameObject drawMenu;
	public GameObject editMenu;
	public PathUI pathUI;
	public GameObject contextUI;

	#endregion

	#region MonoBehaviour Callbacks

	private async void Awake()
    {
        Connect();
		await GetProteinInfo();
	}

	#endregion

	#region Private Methods

	private void Connect()
	{
		string path = GetDatabasePath("main.db");
		connection = new SQLiteAsyncConnection(path);
	}

	private string GetDatabasePath(string name)
	{
		string filePath = string.Format("{0}/{1}", Application.persistentDataPath, name);
		bool fileExists = File.Exists(filePath);

		switch (Application.platform)
		{
			default:
				{
					// alternatively implement an assumed fallback
					throw new NotSupportedException();
				}

			case RuntimePlatform.WindowsEditor:
			case RuntimePlatform.OSXEditor:
			case RuntimePlatform.LinuxEditor:
				{
					return string.Format("Assets/StreamingAssets/{0}", name);
				}

			case RuntimePlatform.Android:
				{
					if (fileExists)
					{
						return filePath;
					}

					// this is the path to your StreamingAssets in android
					string path = string.Format("jar:file://{0}!/assets/{1}", Application.dataPath, name);
					var req = UnityWebRequest.Get(path).SendWebRequest();

					// NOTE: may want to add some checks to this
					while (!req.isDone) { }

					File.WriteAllBytes(filePath, req.webRequest.downloadHandler.data);
					break;
				}

			case RuntimePlatform.IPhonePlayer:
				{
					if (fileExists)
					{
						return filePath;
					}

					// this is the path to your StreamingAssets in iOS
					string path = string.Format("/{0}Raw/{1}", Application.dataPath, name);
					File.Copy(path, filePath);
					break;
				}
		}

		return filePath;
	}

	private async UniTask GetProteinInfo()
	{
		var query = GameManager.Instance.connection.Table<ProteinInfo>();
		proteinsInfo = await query.ToListAsync();
	}

    #endregion

    #region Public Methods

	public void ToggleHandsInteraction(bool v)
    {
		leftHandInteraction.enabled = v;
		rightHandInteraction.enabled = v;
	}

	public void ToggleGrabbable(bool v)
    {
		if (v)
        {
			proteinNetwork.gameObject.AddComponent<Grabbable>();
			return;
		}

		if (proteinNetwork.gameObject.TryGetComponent(out Grabbable g))
			Destroy(g);
	}

	public void ToggleDraw(bool v)
    {
        if (!v)
        {
			leftDraw.ClearDraws();
			rightDraw.ClearDraws();
			leftDraw.enabled = false;
			rightDraw.enabled = false;
		}
        else
        {
			leftDraw.enabled = true;
			rightDraw.enabled = true;
		}
		drawMenu.SetActive(v);
	}

	public void ToggleEdit(bool v)
    {
		editMenu.SetActive(v);
	}

	public void TogglePath(bool v)
    {
        if (v)
			proteinNetwork.ResetPath();

		pathUI.gameObject.SetActive(v);
    }

	public void SetNewState(int state)
    {
		StateType newState = (StateType)state;

		currentState = newState;

		switch (newState)
        {
            case StateType.Default:
				ToggleHandsInteraction(false);
				ToggleGrabbable(false);
				ToggleDraw(false);
				ToggleEdit(false);
				TogglePath(false);
				break;
            case StateType.Search:
				ToggleHandsInteraction(false);
				ToggleGrabbable(false);
				proteinNetwork.gameObject.SetActive(false);
				contextUI.SetActive(false);
				menu.SetActive(false);
				infoUI.SetActive(false);
				keyboard.SetActive(true);
				ToggleDraw(false);
				ToggleEdit(false);
				TogglePath(false);
				break;
            case StateType.Interact:
				ToggleHandsInteraction(true);
				ToggleGrabbable(false);
				ToggleDraw(false);
				ToggleEdit(false);
				TogglePath(false);
				break;
            case StateType.Path:
				ToggleHandsInteraction(false);
				ToggleGrabbable(false);
				infoUI.SetActive(false);
				ToggleDraw(false);
				ToggleEdit(false);
				TogglePath(true);
				break;
            case StateType.Edit:
				ToggleHandsInteraction(false);
				ToggleGrabbable(true);
				ToggleDraw(false);
				ToggleEdit(true);
				TogglePath(false);
				break;
			case StateType.Draw:
				ToggleHandsInteraction(false);
				ToggleGrabbable(false);
				ToggleDraw(true);
				ToggleEdit(false);
				TogglePath(false);
				break;
			default:
                break;
        }
    }

    #endregion
}

[Table("ProteinInfo")]
public class ProteinInfo
{
	[PrimaryKey]
	[Column("Id")]
	public string Id { get; set; }
	[Column("Name")]
	public string Name { get; set; }
	[Column("Size")]
	public string Size { get; set; }
	[Column("Annotation")]
	public string Annotation { get; set; }
}

[Table("ProteinLink")]
public class ProteinLink
{
	[PrimaryKey, AutoIncrement]
	[Column("Id")]
	public int Id { get; set; }
	[Column("Protein1")]
	public string Protein1 { get; set; }
	[Column("Protein2")]
	public string Protein2 { get; set; }
	[Column("Score")]
	public int Score { get; set; }
}
