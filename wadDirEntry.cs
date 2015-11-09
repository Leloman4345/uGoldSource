using System;
[Serializable]
public struct wadDirEntry
{
	public int nFilePos;
	public int nDiskSize;
	public int nSize;
	public byte nType;
	public bool bCompression;
	public short nDummy;
	public string szName;
	public wadDirEntry(int Pos, int DSize, int Size, byte Type, bool Compess, short Dummy)
	{
		this.nFilePos = Pos;
		this.nDiskSize = DSize;
		this.nSize = Size;
		this.nType = Type;
		this.bCompression = Compess;
		this.nDummy = Dummy;
		this.szName = string.Empty;
	}
}
