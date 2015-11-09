using System;
using System.Collections.Generic;
using UnityEngine;
public class BaseEntity : MonoBehaviour
{
	public List<Parametr> Parametrs = new List<Parametr>();
	public int ModelId;
	public int EntityId;
	public List<string> Params;
	public string classname;
	public string targetname;
	public string target;
	private void OnDrawGizmos()
	{
		Gizmos.DrawCube(base.transform.position, Vector3.one / 5f);
	}
	public void Spawn()
	{
		Vector3 zero = Vector3.zero;
		if (this.Params.Contains("origin"))
		{
			string[] array = this.Params[this.Params.FindIndex((string n) => n == "origin") + 1].Split(new char[]
			{
				' '
			});
			zero = new Vector3(-float.Parse(array[0]) * GameManager.Ins.worldScale, float.Parse(array[2]) * GameManager.Ins.worldScale, -float.Parse(array[1]) * GameManager.Ins.worldScale);
			base.transform.position = zero;
		}
		if (this.classname == "func_wall")
		{
			if (base.transform.childCount == 0)
			{
				base.renderer.material.shader = GameManager.Ins.LmapAlpha;
				base.gameObject.AddComponent<BoxCollider>();
			}
			else
			{
				MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					MeshRenderer meshRenderer = componentsInChildren[i];
					meshRenderer.gameObject.AddComponent<MeshCollider>();
					meshRenderer.material.shader = GameManager.Ins.LmapAlpha;
				}
			}
		}
		if (this.classname == "func_illusionary" && base.renderer)
		{
			base.renderer.material.shader = GameManager.Ins.LmapAlpha;
		}
		if (this.classname == "trigger_autosave")
		{
			if (base.renderer)
			{
				base.renderer.enabled = false;
			}
			else
			{
				MeshRenderer[] componentsInChildren2 = base.GetComponentsInChildren<MeshRenderer>();
				for (int j = 0; j < componentsInChildren2.Length; j++)
				{
					MeshRenderer meshRenderer2 = componentsInChildren2[j];
					meshRenderer2.enabled = false;
				}
			}
		}
	}
	public void Do()
	{
	}
}
