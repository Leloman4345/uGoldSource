using System;
using System.IO;
using UnityEngine;
[Serializable]
public class WadReader
{
	public wadHeader Header;
	public wadDirEntry[] DirEntries;
	public BinaryReader BR;
	public string log;
	public string names;
	public void Open(string name)
	{
		if (name != string.Empty)
		{
			if (!File.Exists(GameManager.Ins.patch + GameManager.Ins.mod + Path.GetFileName(name)))
			{
				this.log = this.log + "No wad " + Path.GetFileName(name) + " \n";
				return;
			}
			this.log = this.log + "Load Wad: " + Path.GetFileName(name) + " \n";
			this.BR = new BinaryReader(File.Open(GameManager.Ins.patch + GameManager.Ins.mod + Path.GetFileName(name), FileMode.Open));
			this.names = this.names + name + "\n";
			this.LoadDirectory();
		}
	}
	public Texture2D LoadTexture(string name)
	{
		Texture2D result = new Texture2D(16, 16);
		bspmiptex texture = this.GetTexture(name);
		if (texture.szName == null || texture.nHeight > 10000)
		{
			return null;
		}
		this.CreateMipTexture(texture, out result);
		return result;
	}
	private void LoadDirectory()
	{
		this.BR.BaseStream.Seek(0L, SeekOrigin.Begin);
		this.Header.magic = new string(this.BR.ReadChars(4));
		this.Header.nDir = this.BR.ReadInt32();
		this.Header.nDirOffset = this.BR.ReadInt32();
		if (this.Header.magic != "WAD3" && this.Header.magic != "WAD2")
		{
			this.log += "<color=#FF0000>No valid WAD</color> \n";
			return;
		}
		this.DirEntries = new wadDirEntry[this.Header.nDir];
		this.BR.BaseStream.Seek((long)this.Header.nDirOffset, SeekOrigin.Begin);
		for (int i = 0; i < this.Header.nDir; i++)
		{
			this.BR.BaseStream.Seek((long)(this.Header.nDirOffset + i * 32), SeekOrigin.Begin);
			this.DirEntries[i] = new wadDirEntry(this.BR.ReadInt32(), this.BR.ReadInt32(), this.BR.ReadInt32(), this.BR.ReadByte(), this.BR.ReadBoolean(), this.BR.ReadInt16());
			this.BR.BaseStream.Seek((long)this.DirEntries[i].nFilePos, SeekOrigin.Begin);
			this.DirEntries[i].szName = new string(this.BR.ReadChars(16)).ToLower();
			this.names = this.names + this.DirEntries[i].szName + "\n";
		}
	}
	private int FindTexture(string name)
	{
		return Array.FindIndex<wadDirEntry>(this.DirEntries, (wadDirEntry n) => n.szName == name);
	}
	private bspmiptex GetTexture(string name)
	{
		int num = this.FindTexture(name);
		if (num < 0)
		{
			return default(bspmiptex);
		}
		if (this.DirEntries[num].bCompression)
		{
			this.log = this.log + "<color=#FF0000>Cannot read compressed texture</color>" + name + " \n";
			return default(bspmiptex);
		}
		return this.ReadTexture(this.DirEntries[num]);
	}
	private void CreateMipTexture(bspmiptex RawTex, out Texture2D MipTex)
	{
		int filePos = RawTex.FilePos;
		MipTex = new Texture2D(RawTex.nWidth, RawTex.nHeight, TextureFormat.RGBA32, true);
		int num = RawTex.nOffsets[3] + RawTex.nWidth / 8 * (RawTex.nHeight / 8) + 2;
		Color32[] array = new Color32[256];
		this.BR.BaseStream.Seek((long)(num + filePos), SeekOrigin.Begin);
		for (int i = 0; i < 256; i++)
		{
			array[i] = new Color32(this.BR.ReadByte(), this.BR.ReadByte(), this.BR.ReadByte(), 255);
			if (array[i] == Color.blue)
			{
				array[i] = Color.clear;
			}
		}
		MipTex.name = RawTex.szName;
		Color32[] array2 = new Color32[RawTex.nWidth * RawTex.nHeight];
		this.BR.BaseStream.Seek((long)(filePos + RawTex.nOffsets[0]), SeekOrigin.Begin);
		for (int j = 0; j < RawTex.nWidth * RawTex.nHeight; j++)
		{
			int num2 = (int)this.BR.ReadByte();
			array2[j] = array[num2];
		}
		MipTex.SetPixels32(array2);
		MipTex.filterMode = FilterMode.Bilinear;
		MipTex.Apply();
	}
	public bspmiptex ReadTexture(wadDirEntry dir)
	{
		bspmiptex result = default(bspmiptex);
		this.BR.BaseStream.Seek((long)dir.nFilePos, SeekOrigin.Begin);
		result.szName = new string(this.BR.ReadChars(16));
		result.nWidth = (int)this.BR.ReadUInt32();
		result.nHeight = (int)this.BR.ReadUInt32();
		result.nOffsets = new int[]
		{
			(int)this.BR.ReadUInt32(),
			(int)this.BR.ReadUInt32(),
			(int)this.BR.ReadUInt32(),
			(int)this.BR.ReadUInt32()
		};
		result.FilePos = dir.nFilePos;
		return result;
	}
}
