using System;
using System.Collections.Generic;
using UnityEngine;
public class GameManager : MonoBehaviour
{
	public static GameManager Ins;
	public BSPLoader BSP;
	public bool debug;
	public string patch;
	public string mod = "valve/";
	public string mapName = "c0a0";
	public string[] Entityes;
	public float worldScale = 0.03f;
	public GameObject pl;
	public List<BaseEntity> MapEntities;
	public Dictionary<string, AudioClip> Sounds = new Dictionary<string, AudioClip>();
	public Shader Difuse;
	public Shader Lmap;
	public Shader LmapAlpha;
	public Shader Sky;
	private bool show = true;
	private Vector2 scrollPos;
	private void Awake()
	{
		GameManager.Ins = this;
		this.pl = GameObject.FindWithTag("Player");
		this.BSP = base.GetComponent<BSPLoader>();
		if (!this.debug)
		{
			this.patch = Application.persistentDataPath;
		}
	}
	private void Update()
	{
		GameManager.Ins = this;
	}
	public void PlayerSpawn()
	{
		if (this.pl != null)
		{
			BaseEntity baseEntity = this.MapEntities.Find((BaseEntity n) => n.classname == "info_player_start");
			if (baseEntity.Params.Contains("angle"))
			{
				this.pl.transform.rotation = Quaternion.AngleAxis((float)(int.Parse(baseEntity.Params[baseEntity.Params.FindIndex((string n) => n == "angle") + 1]) - 90), Vector3.up);
			}
			this.pl.transform.position = baseEntity.transform.position + Vector3.up / 2f;
			this.pl.GetComponentInChildren<Camera>().enabled = true;
		}
		else
		{
			Debug.LogWarning("No Player");
		}
	}
	private void OnGUI()
	{
		this.show = GUI.Toggle(new Rect((float)(Screen.width - 110), (float)(Screen.height - 30), 110f, 20f), this.show, "Show interface");
		if (this.show)
		{
			this.patch = GUI.TextField(new Rect(10f, 10f, 260f, 30f), this.patch);
			this.mapName = GUI.TextField(new Rect(280f, 10f, (float)(Screen.width - 400), 30f), this.mapName);
			if (GUI.Button(new Rect((float)(Screen.width - 110), 10f, 100f, 30f), "Load"))
			{
				this.BSP.Load();
			}
			if (GUI.Button(new Rect((float)(Screen.width - 110), 50f, 100f, 30f), "Clear"))
			{
				this.BSP.Clear();
			}
			if (GUI.Button(new Rect((float)(Screen.width - 110), 90f, 100f, 30f), "Spawn"))
			{
				this.PlayerSpawn();
			}
			this.scrollPos = GUI.BeginScrollView(new Rect(10f, 50f, 210f, 260f), this.scrollPos, new Rect(0f, 0f, 195f, 800f));
			GUI.Box(new Rect(0f, 0f, 200f, 800f), this.BSP.tempLog);
			GUI.EndScrollView();
		}
	}
}
