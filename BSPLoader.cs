using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
public class BSPLoader : MonoBehaviour
{
	public struct dheader_t
	{
		public int version;
		public BSPLoader.dlump_t[] lumps;
	}
	public struct dlump_t
	{
		public int fileofs;
		public int filelen;
	}
	public struct dmodel_t
	{
		public Vector3 mins;
		public Vector3 maxs;
		public Vector3 origin;
		public int[] headnode;
		public int visleafs;
		public int firstface;
		public int numfaces;
	}
	[Serializable]
	public struct dtexinfo_t
	{
		public Vector3 vec3s;
		public float offs;
		public Vector3 vec3t;
		public float offt;
		public int miptex;
		public int flags;
	}
	[Serializable]
	public struct dmiptex_t
	{
		public string name;
		public int width;
		public int height;
		public int[] offsets;
		public int ID;
	}
	public struct dmiptexlump_t
	{
		public int tex_count;
		public int[] tex_offsets;
	}
	public struct dface_t
	{
		public int firstEdge;
		public short numedges;
		public short texinfo;
		public byte[] styles;
		public int lightofs;
	}
	private struct face
	{
		public int index;
		public Vector3[] points;
		public Vector2[] uv;
		public Vector2[] uv2;
		public int[] triangles;
		public int lightMapW;
		public int lightMapH;
	}
	public bool Lmaps;
	private BinaryReader BR;
	public WadReader[] WadFiles;
	public BSPLoader.dheader_t Header;
	private string entitiesLump;
	public BSPLoader.dmiptex_t[] texturesLump;
	private List<Vector3> vertexesLump = new List<Vector3>();
	public BSPLoader.dtexinfo_t[] texinfoLump;
	public List<BSPLoader.dface_t> facesLump = new List<BSPLoader.dface_t>();
	private byte[] lightingLump;
	public List<int[]> edgesLump = new List<int[]>();
	private int[] surfedgesLump;
	public List<BSPLoader.dmodel_t> modelsLump = new List<BSPLoader.dmodel_t>();
	private int[] mipStructOffsets;
	public GameObject mapObject;
	private List<GameObject> Models = new List<GameObject>();
	public List<string> entities = new List<string>();
	public List<Texture2D> Textures = new List<Texture2D>();
	public string tempLog;
	public void Clear()
	{
		GameManager.Ins.MapEntities.Clear();
		if (this.WadFiles != null)
		{
			WadReader[] wadFiles = this.WadFiles;
			for (int i = 0; i < wadFiles.Length; i++)
			{
				WadReader wadReader = wadFiles[i];
				if (wadReader.BR != null)
				{
					wadReader.BR.BaseStream.Dispose();
				}
			}
		}
		this.WadFiles = null;
		this.Header = default(BSPLoader.dheader_t);
		this.entitiesLump = null;
		this.texturesLump = null;
		this.vertexesLump.Clear();
		this.texinfoLump = null;
		this.facesLump.Clear();
		this.lightingLump = null;
		this.edgesLump.Clear();
		this.surfedgesLump = null;
		this.modelsLump.Clear();
		this.mipStructOffsets = null;
		UnityEngine.Object.Destroy(this.mapObject);
		this.Models.Clear();
		this.entities.Clear();
		this.Textures.Clear();
		this.tempLog = null;
		Resources.UnloadUnusedAssets();
	}
	public void Export()
	{
		Directory.CreateDirectory("I:\\HLP/maps/" + GameManager.Ins.mapName + "/");
		File.WriteAllText("I:\\HLP/maps/" + GameManager.Ins.mapName + "/Entityes.ent", this.entitiesLump);
	}
	public void Load()
	{
		this.Clear();
		if (!File.Exists(string.Concat(new string[]
		{
			GameManager.Ins.patch,
			GameManager.Ins.mod,
			"maps/",
			GameManager.Ins.mapName,
			".bsp"
		})))
		{
			Debug.Log(string.Concat(new string[]
			{
				GameManager.Ins.patch,
				GameManager.Ins.mod,
				"maps/",
				GameManager.Ins.mapName,
				".bsp"
			}));
			this.tempLog += "<color=#FF0000>Map file not found</color> \n";
			return;
		}
		this.BR = new BinaryReader(File.Open(string.Concat(new string[]
		{
			GameManager.Ins.patch,
			GameManager.Ins.mod,
			"maps/",
			GameManager.Ins.mapName,
			".bsp"
		}), FileMode.Open));
		this.Header = this.ReadHeader();
		if (this.Header.version != 30)
		{
			this.tempLog += "<color=#FF0000>Invalid BSP version</color> \n";
			return;
		}
		this.tempLog += "====Start read file====\n";
		this.ReadEntities();
		this.ParseEntities(this.entitiesLump);
		this.ReadTextures();
		this.ReadVertexes();
		this.ReadTexinfo();
		this.ReadFaces();
		this.ReadLighting();
		this.ReadEdges();
		this.ReadSurfedges();
		this.ReadModels();
		for (int i = 0; i < this.entities.Count; i++)
		{
			this.LoadEntity(i);
		}
		this.tempLog += "======Finish======\n";
		Debug.Log(this.tempLog);
		this.BR.BaseStream.Dispose();
	}
	private void WorldSpawn(List<string> data)
	{
		this.mapObject = new GameObject(GameManager.Ins.mapName);
		this.tempLog += "====Start loading textures====\n";
		this.LoadTextures(data);
		this.tempLog += "====Start Generate map====\n";
		for (int i = 0; i < this.modelsLump.Count; i++)
		{
			this.GenerateModel(i);
		}
	}
	private void LoadWadFiles(string[] wads)
	{
		int num = wads.Length;
		this.WadFiles = new WadReader[num];
		for (int i = 0; i < num; i++)
		{
			this.WadFiles[i] = new WadReader();
			this.WadFiles[i].Open(wads[i]);
		}
	}
	private void LoadTextures(List<string> data)
	{
		string[] array = data[data.FindIndex((string n) => n == "wad") + 1].Split(new char[]
		{
			';'
		});
		if (array[0] != string.Empty)
		{
			this.LoadWadFiles(array);
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < this.texturesLump.Length; i++)
		{
			if (this.texturesLump[i].offsets[0] == 0)
			{
				num++;
				Texture2D item = this.LoadTextureFromWad(this.texturesLump[i].name);
				this.Textures.Add(item);
			}
			else
			{
				num2++;
				if (this.texturesLump[i].width < 4096)
				{
					this.Textures.Add(this.CreateTexture(this.texturesLump[i], this.mipStructOffsets[i]));
				}
				else
				{
					this.tempLog = this.tempLog + "<color=#FF0000>Texture width error: </color>" + this.texturesLump[i].name + "\n";
					this.Textures.Add(new Texture2D(16, 16));
				}
			}
		}
		if (this.WadFiles != null)
		{
			for (int j = 0; j < this.WadFiles.Length; j++)
			{
				this.tempLog += this.WadFiles[j].log;
			}
		}
		this.tempLog = this.tempLog + num + " Wad Textures \n";
		this.tempLog = this.tempLog + num2 + " BSP Textures \n";
	}
	private Texture2D LoadTextureFromWad(string name)
	{
		for (int i = 0; i < this.WadFiles.Length; i++)
		{
			if (this.WadFiles[i].DirEntries != null)
			{
				Texture2D texture2D = this.WadFiles[i].LoadTexture(name);
				if (texture2D != null)
				{
					return texture2D;
				}
			}
		}
		this.tempLog = this.tempLog + "<color=#FF0000>Cannot find texture: </color>" + name + " \n";
		return null;
	}
	public Texture2D CreateTexture(BSPLoader.dmiptex_t tex, int structOffset)
	{
		int num = this.Header.lumps[2].fileofs + structOffset;
		Texture2D texture2D = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, true);
		int num2 = tex.offsets[3] + tex.width / 8 * (tex.height / 8) + 2;
		Color32[] array = new Color32[256];
		this.BR.BaseStream.Seek((long)(num2 + num), SeekOrigin.Begin);
		for (int i = 0; i < 256; i++)
		{
			array[i] = new Color32(this.BR.ReadByte(), this.BR.ReadByte(), this.BR.ReadByte(), 255);
			if (array[i] == Color.blue)
			{
				array[i] = Color.clear;
			}
		}
		texture2D.name = tex.name;
		Color32[] array2 = new Color32[tex.width * tex.height];
		this.BR.BaseStream.Seek((long)(num + tex.offsets[0]), SeekOrigin.Begin);
		for (int j = 0; j < tex.width * tex.height; j++)
		{
			int num3 = (int)this.BR.ReadByte();
			array2[j] = array[num3];
		}
		texture2D.SetPixels32(array2);
		texture2D.filterMode = FilterMode.Bilinear;
		texture2D.Apply();
		return texture2D;
	}
	public void ParseEntities(string input)
	{
		string pattern = "{[^}]*}";
		foreach (Match match in Regex.Matches(input, pattern, RegexOptions.IgnoreCase))
		{
			this.entities.Add(match.Value);
		}
		string text = this.tempLog;
		this.tempLog = string.Concat(new object[]
		{
			text,
			"Load: ",
			this.entities.Count,
			" Entityes \n"
		});
	}
	private void LoadEntity(int id)
	{
		List<string> list = new List<string>();
		string pattern = "\"[^\"]*\"";
		foreach (Match match in Regex.Matches(this.entities[id], pattern, RegexOptions.IgnoreCase))
		{
			list.Add(match.Value.Trim(new char[]
			{
				'"'
			}));
		}
		int num = list.FindIndex((string n) => n == "classname");
		if (list[num + 1] == "worldspawn")
		{
			this.WorldSpawn(list);
			return;
		}
		if (list[0] == "model")
		{
			GameObject gameObject = GameObject.Find(list[list.FindIndex((string n) => n == "model") + 1]);
			BaseEntity baseEntity = gameObject.AddComponent<BaseEntity>();
			baseEntity.ModelId = int.Parse(gameObject.name.Replace("*", string.Empty));
			baseEntity.classname = list[list.FindIndex((string n) => n == "classname") + 1];
			baseEntity.targetname = list[list.FindIndex((string n) => n == "targetname") + 1];
			baseEntity.target = list[list.FindIndex((string n) => n == "target") + 1];
			baseEntity.Params = list;
			baseEntity.EntityId = id;
			baseEntity.Spawn();
			GameManager.Ins.MapEntities.Add(baseEntity);
		}
		else
		{
			GameObject gameObject2 = new GameObject();
			if (list.Contains("targetname"))
			{
				gameObject2.name = list[list.FindIndex((string n) => n == "targetname") + 1];
			}
			else
			{
				gameObject2.name = list[num + 1];
			}
			gameObject2.transform.parent = this.mapObject.transform;
			BaseEntity baseEntity2 = gameObject2.AddComponent<BaseEntity>();
			baseEntity2.classname = list[list.FindIndex((string n) => n == "classname") + 1];
			baseEntity2.targetname = list[list.FindIndex((string n) => n == "targetname") + 1];
			baseEntity2.target = list[list.FindIndex((string n) => n == "target") + 1];
			baseEntity2.Params = list;
			baseEntity2.EntityId = id;
			baseEntity2.Spawn();
			GameManager.Ins.MapEntities.Add(baseEntity2);
			Vector3 zero = Vector3.zero;
		}
	}
	private void GenerateModel(int index)
	{
		Dictionary<int, List<int>> dictionary = new Dictionary<int, List<int>>();
		GameObject gameObject = new GameObject("*" + index);
		gameObject.transform.parent = this.mapObject.transform;
		this.Models.Add(gameObject);
		Vector3 origin = this.modelsLump[index].origin;
		int firstface = this.modelsLump[index].firstface;
		int numfaces = this.modelsLump[index].numfaces;
		gameObject.transform.position = origin;
		for (int i = firstface; i < firstface + numfaces; i++)
		{
			if (!dictionary.ContainsKey(this.texturesLump[this.texinfoLump[(int)this.facesLump[i].texinfo].miptex].ID))
			{
				dictionary.Add(this.texturesLump[this.texinfoLump[(int)this.facesLump[i].texinfo].miptex].ID, new List<int>());
			}
			dictionary[this.texturesLump[this.texinfoLump[(int)this.facesLump[i].texinfo].miptex].ID].Add(i);
		}
		for (int j = 0; j < this.texturesLump.Length; j++)
		{
			if (dictionary.ContainsKey(j))
			{
				for (int k = 0; k < dictionary[j].Count; k += 1024)
				{
					List<Vector3> list = new List<Vector3>();
					List<Vector2> list2 = new List<Vector2>();
					List<int> list3 = new List<int>();
					List<BSPLoader.face> list4 = new List<BSPLoader.face>();
					for (int l = k; l < Mathf.Clamp(k + 1024, 0, dictionary[j].Count); l++)
					{
						BSPLoader.face item = this.GenerateFace(dictionary[j][l]);
						item.index = dictionary[j][l];
						int count = list.Count;
						for (int m = 0; m < item.triangles.Length; m++)
						{
							list3.Add(item.triangles[m] + count);
						}
						list.AddRange(item.points);
						list2.AddRange(item.uv);
						list4.Add(item);
					}
					GameObject gameObject2;
					if (dictionary.Count > 1)
					{
						gameObject2 = new GameObject(this.texturesLump[j].name);
					}
					else
					{
						gameObject2 = gameObject;
					}
					gameObject2.AddComponent<MeshRenderer>();
					MeshFilter meshFilter = gameObject2.AddComponent<MeshFilter>();
					meshFilter.mesh = new Mesh();
					meshFilter.mesh.name = "BSPModel " + index;
					meshFilter.mesh.vertices = list.ToArray();
					meshFilter.mesh.triangles = list3.ToArray();
					meshFilter.mesh.uv = list2.ToArray();
					Material material;
					if (gameObject2.name == "sky")
					{
						material = new Material(GameManager.Ins.Sky);
						material.color = Color.clear;
					}
					else if (this.Textures[j].name.Contains("aaatrigger"))
					{
						material = new Material(GameManager.Ins.Difuse);
						gameObject2.renderer.enabled = false;
					}
					else if (this.Lmaps)
					{
						Texture2D texture2D;
						Vector2[] uv;
						this.CreateLightmap(list4, out texture2D, out uv);
						texture2D.Compress(false);
						material = new Material(GameManager.Ins.Lmap);
						if (texture2D != null)
						{
							material.SetTexture("_LightMap", texture2D);
						}
						else
						{
							string text = this.tempLog;
							this.tempLog = string.Concat(new object[]
							{
								text,
								"<color=#FF0000>Error with:</color> ",
								index,
								" lightmap \n"
							});
						}
						meshFilter.mesh.uv2 = uv;
					}
					else
					{
						material = new Material(GameManager.Ins.Difuse);
					}
					if (this.Textures[j] != null)
					{
						material.mainTexture = this.Textures[j];
					}
					else
					{
						this.tempLog = this.tempLog + "<color=#FF0000>Error with:</color> " + this.texturesLump[j].name + " Texture \n";
					}
					meshFilter.mesh.RecalculateNormals();
					gameObject2.transform.parent = gameObject.transform;
					if (index == 0)
					{
						gameObject2.AddComponent<MeshCollider>();
					}
					gameObject2.renderer.material = material;
					meshFilter.mesh.Optimize();
				}
			}
		}
	}
	private void GenerateFaceObject(int index)
	{
		List<Vector3> list = new List<Vector3>();
		List<Vector2> list2 = new List<Vector2>();
		List<Vector2> list3 = new List<Vector2>();
		List<int> list4 = new List<int>();
		BSPLoader.face f = this.GenerateFace(index);
		f.index = index;
		list4.AddRange(f.triangles);
		list.AddRange(f.points);
		list2.AddRange(f.uv);
		list3.AddRange(f.uv2);
		GameObject gameObject = new GameObject("Face: " + index);
		gameObject.AddComponent<MeshRenderer>();
		MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
		meshFilter.mesh = new Mesh();
		meshFilter.mesh.name = "BSPFace " + index;
		meshFilter.mesh.vertices = list.ToArray();
		meshFilter.mesh.triangles = list4.ToArray();
		meshFilter.mesh.uv = list2.ToArray();
		Material material;
		if (this.Textures[this.texinfoLump[(int)this.facesLump[index].texinfo].miptex].name == "aaatrigger")
		{
			material = new Material(Shader.Find("Mobile/Diffuse"));
		}
		else
		{
			Texture2D texture2D = this.CreateLightmapTex(f);
			texture2D.Compress(false);
			material = new Material(Shader.Find("Mobile/Legacy/Lightmap/Lightmap Only"));
			material.SetTexture("_LightMap", texture2D);
			meshFilter.mesh.uv2 = list3.ToArray();
		}
		material.mainTexture = this.Textures[this.texinfoLump[(int)this.facesLump[index].texinfo].miptex];
		meshFilter.mesh.RecalculateNormals();
		gameObject.transform.parent = this.mapObject.transform;
		if (index == 0)
		{
			gameObject.AddComponent<MeshCollider>();
		}
		gameObject.renderer.material = material;
		meshFilter.mesh.Optimize();
	}
	private BSPLoader.face GenerateFace(int index)
	{
		List<Vector3> list = new List<Vector3>();
		List<Vector2> list2 = new List<Vector2>();
		List<Vector2> list3 = new List<Vector2>();
		int firstEdge = this.facesLump[index].firstEdge;
		int numedges = (int)this.facesLump[index].numedges;
		for (int i = firstEdge; i < firstEdge + numedges; i++)
		{
			list.Add((this.surfedgesLump[i] <= 0) ? this.vertexesLump[this.edgesLump[Mathf.Abs(this.surfedgesLump[i])][1]] : this.vertexesLump[this.edgesLump[Mathf.Abs(this.surfedgesLump[i])][0]]);
		}
		List<int> list4 = new List<int>();
		for (int j = 1; j < list.Count - 1; j++)
		{
			list4.Add(0);
			list4.Add(j);
			list4.Add(j + 1);
		}
		float num = (float)this.texturesLump[this.texinfoLump[(int)this.facesLump[index].texinfo].miptex].width * GameManager.Ins.worldScale;
		float num2 = (float)this.texturesLump[this.texinfoLump[(int)this.facesLump[index].texinfo].miptex].height * GameManager.Ins.worldScale;
		for (int k = 0; k < list.Count; k++)
		{
			float num3 = Vector3.Dot(list[k] * GameManager.Ins.worldScale, this.texinfoLump[(int)this.facesLump[index].texinfo].vec3s) + this.texinfoLump[(int)this.facesLump[index].texinfo].offs * GameManager.Ins.worldScale;
			float num4 = Vector3.Dot(list[k] * GameManager.Ins.worldScale, this.texinfoLump[(int)this.facesLump[index].texinfo].vec3t) + this.texinfoLump[(int)this.facesLump[index].texinfo].offt * GameManager.Ins.worldScale;
			list2.Add(new Vector2(num3 / num, num4 / num2));
		}
		List<float> list5 = new List<float>();
		List<float> list6 = new List<float>();
		for (int l = 0; l < list.Count; l++)
		{
			float item = Vector3.Dot(this.texinfoLump[(int)this.facesLump[index].texinfo].vec3s, list[l]) + this.texinfoLump[(int)this.facesLump[index].texinfo].offs;
			float item2 = Vector3.Dot(this.texinfoLump[(int)this.facesLump[index].texinfo].vec3t, list[l]) + this.texinfoLump[(int)this.facesLump[index].texinfo].offt;
			list5.Add(item);
			list6.Add(item2);
		}
		float num5 = list5.Min();
		float num6 = list6.Min();
		float num7 = list5.Max();
		float num8 = list6.Max();
		float num9 = (float)Math.Floor((double)(list5.Min() / 16f));
		float num10 = (float)Math.Floor((double)(list6.Min() / 16f));
		float num11 = (float)Math.Ceiling((double)(list5.Max() / 16f));
		float num12 = (float)Math.Ceiling((double)(list6.Max() / 16f));
		int num13 = (int)(num11 - num9) + 1;
		int num14 = (int)(num12 - num10) + 1;
		float num15 = (num5 + num7) / 2f;
		float num16 = (num6 + num8) / 2f;
		float num17 = (float)num13 / 2f;
		float num18 = (float)num14 / 2f;
		for (int m = 0; m < list.Count; m++)
		{
			float num19 = Vector3.Dot(this.texinfoLump[(int)this.facesLump[index].texinfo].vec3s, list[m]) + this.texinfoLump[(int)this.facesLump[index].texinfo].offs;
			float num20 = Vector3.Dot(this.texinfoLump[(int)this.facesLump[index].texinfo].vec3t, list[m]) + this.texinfoLump[(int)this.facesLump[index].texinfo].offt;
			float num21 = num17 + (num19 - num15) / 16f;
			float num22 = num18 + (num20 - num16) / 16f;
			float x = num21 / (float)num13;
			float y = num22 / (float)num14;
			list3.Add(new Vector2(x, y));
		}
		for (int n = 0; n < list.Count; n++)
		{
			List<Vector3> list7;
			List<Vector3> expr_562 = list7 = list;
			int index2;
			int expr_567 = index2 = n;
			Vector3 a = list7[index2];
			expr_562[expr_567] = a * GameManager.Ins.worldScale;
		}
		return new BSPLoader.face
		{
			points = list.ToArray(),
			triangles = list4.ToArray(),
			uv = list2.ToArray(),
			uv2 = list3.ToArray(),
			lightMapW = num13,
			lightMapH = num14
		};
	}
	private void CreateLightmap(List<BSPLoader.face> inpFaces, out Texture2D lightmap, out Vector2[] lightmapUV)
	{
		int num = Mathf.Clamp(inpFaces.Count, 0, 32);
		int num2 = Mathf.CeilToInt((float)inpFaces.Count / 32f);
		int width = num * 16;
		int height = num2 * 16;
		float num3 = 1f / (float)num;
		float num4 = 1f / (float)num2;
		List<Vector2> list = new List<Vector2>();
		lightmap = new Texture2D(width, height, TextureFormat.RGB24, false);
		for (int i = 0; i < inpFaces.Count; i++)
		{
			Texture2D texture2D = new Texture2D(inpFaces[i].lightMapW, inpFaces[i].lightMapH, TextureFormat.RGB24, false);
			int lightofs = this.facesLump[inpFaces[i].index].lightofs;
			if (lightofs >= 0)
			{
				for (int j = 0; j < inpFaces[i].lightMapH; j++)
				{
					for (int k = 0; k < inpFaces[i].lightMapW; k++)
					{
						byte r = this.lightingLump[lightofs + (inpFaces[i].lightMapW * j + k) * 3];
						byte g = this.lightingLump[lightofs + (inpFaces[i].lightMapW * j + k) * 3 + 1];
						byte b = this.lightingLump[lightofs + (inpFaces[i].lightMapW * j + k) * 3 + 2];
						texture2D.SetPixel(k, j, new Color32(r, g, b, 255));
					}
				}
				texture2D.Apply();
			}
			TextureScale.Point(texture2D, 16, 16);
			for (int l = 0; l < 16; l++)
			{
				for (int m = 0; m < 16; m++)
				{
					lightmap.SetPixel(i % 32 * 16 + m, (int)((float)i / 32f) * 16 + l, texture2D.GetPixel(m, l));
				}
			}
			lightmap.Apply();
			UnityEngine.Object.DestroyImmediate(texture2D);
			for (int n = 0; n < inpFaces[i].uv2.Length; n++)
			{
				list.Add(new Vector2(Math.Abs(inpFaces[i].uv2[n].x - (float)((int)inpFaces[i].uv2[n].x)) * num3 + (float)(i % 32) * num3, Math.Abs(inpFaces[i].uv2[n].y - (float)((int)inpFaces[i].uv2[n].y)) * num4 + (float)((int)((float)i / 32f)) * num4));
			}
		}
		lightmapUV = list.ToArray();
	}
	private Texture2D CreateLightmapTex(BSPLoader.face f)
	{
		Texture2D texture2D = new Texture2D(f.lightMapW, f.lightMapH, TextureFormat.RGBA32, false);
		Color32[] array = new Color32[f.lightMapW * f.lightMapH];
		int lightofs = this.facesLump[f.index].lightofs;
		int num = 0;
		for (int i = 0; i < f.lightMapW * f.lightMapH; i++)
		{
			byte r = this.lightingLump[lightofs + num++];
			byte g = this.lightingLump[lightofs + num++];
			byte b = this.lightingLump[lightofs + num++];
			array[i] = new Color32(r, g, b, 255);
		}
		texture2D.SetPixels32(array);
		texture2D.Apply();
		return texture2D;
	}
	public BSPLoader.dheader_t ReadHeader()
	{
		this.BR.BaseStream.Seek(0L, SeekOrigin.Begin);
		BSPLoader.dheader_t result = default(BSPLoader.dheader_t);
		result.version = this.BR.ReadInt32();
		result.lumps = new BSPLoader.dlump_t[15];
		for (int i = 0; i < 15; i++)
		{
			result.lumps[i] = default(BSPLoader.dlump_t);
			result.lumps[i].fileofs = this.BR.ReadInt32();
			result.lumps[i].filelen = this.BR.ReadInt32();
		}
		string text = this.tempLog;
		this.tempLog = string.Concat(new object[]
		{
			text,
			"BSP version: ",
			result.version,
			"\n"
		});
		return result;
	}
	private void ReadEntities()
	{
		this.BR.BaseStream.Seek((long)this.Header.lumps[0].fileofs, SeekOrigin.Begin);
		this.entitiesLump = new string(this.BR.ReadChars(this.Header.lumps[0].filelen));
	}
	private void ReadTextures()
	{
		this.BR.BaseStream.Seek((long)this.Header.lumps[2].fileofs, SeekOrigin.Begin);
		int num = this.BR.ReadInt32();
		this.mipStructOffsets = new int[num];
		this.texturesLump = new BSPLoader.dmiptex_t[num];
		string text = this.tempLog;
		this.tempLog = string.Concat(new object[]
		{
			text,
			"Load: ",
			num,
			" Textures \n"
		});
		for (int i = 0; i < num; i++)
		{
			this.mipStructOffsets[i] = this.BR.ReadInt32();
		}
		for (int j = 0; j < this.mipStructOffsets.Length; j++)
		{
			BSPLoader.dmiptex_t dmiptex_t = default(BSPLoader.dmiptex_t);
			this.BR.BaseStream.Seek((long)(this.Header.lumps[2].fileofs + this.mipStructOffsets[j]), SeekOrigin.Begin);
			dmiptex_t.name = new string(this.BR.ReadChars(16));
			dmiptex_t.width = this.BR.ReadInt32();
			dmiptex_t.height = this.BR.ReadInt32();
			dmiptex_t.offsets = new int[]
			{
				this.BR.ReadInt32(),
				this.BR.ReadInt32(),
				this.BR.ReadInt32(),
				this.BR.ReadInt32()
			};
			dmiptex_t.ID = j;
			this.texturesLump[j] = dmiptex_t;
		}
	}
	private void ReadVertexes()
	{
		this.BR.BaseStream.Seek((long)this.Header.lumps[3].fileofs, SeekOrigin.Begin);
		int num = this.Header.lumps[3].filelen / 12;
		string text = this.tempLog;
		this.tempLog = string.Concat(new object[]
		{
			text,
			"Load: ",
			num,
			" Vertexes \n"
		});
		for (int i = 0; i < num; i++)
		{
			this.vertexesLump.Add(this.FlipVector(new Vector3(this.BR.ReadSingle(), this.BR.ReadSingle(), this.BR.ReadSingle())));
		}
	}
	private void ReadTexinfo()
	{
		this.BR.BaseStream.Seek((long)this.Header.lumps[6].fileofs, SeekOrigin.Begin);
		int num = this.Header.lumps[6].filelen / 40;
		this.texinfoLump = new BSPLoader.dtexinfo_t[num];
		string text = this.tempLog;
		this.tempLog = string.Concat(new object[]
		{
			text,
			"Load: ",
			num,
			" TexInfos \n"
		});
		for (int i = 0; i < num; i++)
		{
			BSPLoader.dtexinfo_t dtexinfo_t = default(BSPLoader.dtexinfo_t);
			dtexinfo_t.vec3s = this.FlipVector(new Vector3(this.BR.ReadSingle(), this.BR.ReadSingle(), this.BR.ReadSingle()));
			dtexinfo_t.offs = this.BR.ReadSingle();
			dtexinfo_t.vec3t = this.FlipVector(new Vector3(this.BR.ReadSingle(), this.BR.ReadSingle(), this.BR.ReadSingle()));
			dtexinfo_t.offt = this.BR.ReadSingle();
			dtexinfo_t.miptex = this.BR.ReadInt32();
			dtexinfo_t.flags = this.BR.ReadInt32();
			this.texinfoLump[i] = dtexinfo_t;
		}
	}
	private void ReadFaces()
	{
		this.BR.BaseStream.Seek((long)this.Header.lumps[7].fileofs, SeekOrigin.Begin);
		int num = this.Header.lumps[7].filelen / 20;
		for (int i = 0; i < num; i++)
		{
			BSPLoader.dface_t item = default(BSPLoader.dface_t);
			this.BR.BaseStream.Seek(4L, SeekOrigin.Current);
			item.firstEdge = this.BR.ReadInt32();
			item.numedges = this.BR.ReadInt16();
			item.texinfo = this.BR.ReadInt16();
			item.styles = this.BR.ReadBytes(4);
			item.lightofs = this.BR.ReadInt32();
			this.facesLump.Add(item);
		}
		string text = this.tempLog;
		this.tempLog = string.Concat(new object[]
		{
			text,
			"Load: ",
			num,
			" Faces \n"
		});
	}
	private void ReadEdges()
	{
		this.BR.BaseStream.Seek((long)this.Header.lumps[12].fileofs, SeekOrigin.Begin);
		int num = this.Header.lumps[12].filelen / 4;
		string text = this.tempLog;
		this.tempLog = string.Concat(new object[]
		{
			text,
			"Load :",
			num,
			" Edges \n"
		});
		for (int i = 0; i < num; i++)
		{
			this.edgesLump.Add(new int[]
			{
				(int)this.BR.ReadInt16(),
				(int)this.BR.ReadInt16()
			});
		}
	}
	private void ReadSurfedges()
	{
		this.BR.BaseStream.Seek((long)this.Header.lumps[13].fileofs, SeekOrigin.Begin);
		int num = this.Header.lumps[13].filelen / 4;
		this.surfedgesLump = new int[num];
		string text = this.tempLog;
		this.tempLog = string.Concat(new object[]
		{
			text,
			"Load :",
			num,
			" SurfEdges \n"
		});
		for (int i = 0; i < num; i++)
		{
			this.surfedgesLump[i] = this.BR.ReadInt32();
		}
	}
	private void ReadLighting()
	{
		this.BR.BaseStream.Seek((long)this.Header.lumps[8].fileofs, SeekOrigin.Begin);
		int filelen = this.Header.lumps[8].filelen;
		string text = this.tempLog;
		this.tempLog = string.Concat(new object[]
		{
			text,
			"Lighmaps: ",
			filelen,
			"\n"
		});
		this.lightingLump = new byte[filelen];
		this.lightingLump = this.BR.ReadBytes(filelen);
	}
	private void ReadModels()
	{
		this.BR.BaseStream.Seek((long)this.Header.lumps[14].fileofs, SeekOrigin.Begin);
		int num = this.Header.lumps[14].filelen / 64;
		for (int i = 0; i < num; i++)
		{
			BSPLoader.dmodel_t item = default(BSPLoader.dmodel_t);
			item.mins = this.FlipVector(new Vector3(this.BR.ReadSingle(), this.BR.ReadSingle(), this.BR.ReadSingle()));
			item.maxs = new Vector3(this.BR.ReadSingle(), this.BR.ReadSingle(), this.BR.ReadSingle());
			item.origin = new Vector3(this.BR.ReadSingle(), this.BR.ReadSingle(), this.BR.ReadSingle());
			item.headnode = new int[]
			{
				this.BR.ReadInt32(),
				this.BR.ReadInt32(),
				this.BR.ReadInt32(),
				this.BR.ReadInt32()
			};
			item.visleafs = this.BR.ReadInt32();
			item.firstface = this.BR.ReadInt32();
			item.numfaces = this.BR.ReadInt32();
			this.modelsLump.Add(item);
		}
		string text = this.tempLog;
		this.tempLog = string.Concat(new object[]
		{
			text,
			"Load :",
			num,
			" Models \n"
		});
	}
	public Vector3 FlipVector(Vector3 inp)
	{
		return new Vector3(-inp.x, inp.z, -inp.y);
	}
}
