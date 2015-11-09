using System;
using System.Threading;
using UnityEngine;
public class TextureScale
{
	private static Color[] texColors;
	private static Color[] newColors;
	private static int w;
	private static float ratioX;
	private static float ratioY;
	private static int w2;
	private static int finishCount;
	private static Mutex mutex;
	public static void Point(Texture2D tex, int newWidth, int newHeight)
	{
		TextureScale.ThreadedScale(tex, newWidth, newHeight);
	}
	private static void ThreadedScale(Texture2D tex, int newWidth, int newHeight)
	{
		TextureScale.texColors = tex.GetPixels();
		TextureScale.newColors = new Color[newWidth * newHeight];
		TextureScale.ratioX = (float)tex.width / (float)newWidth;
		TextureScale.ratioY = (float)tex.height / (float)newHeight;
		TextureScale.w = tex.width;
		TextureScale.w2 = newWidth;
		TextureScale.finishCount = 0;
		if (TextureScale.mutex == null)
		{
			TextureScale.mutex = new Mutex(false);
		}
		TextureScale.PointScale(0, newHeight);
		tex.Resize(newWidth, newHeight);
		tex.SetPixels(TextureScale.newColors);
		tex.Apply();
	}
	public static void PointScale(int start, int end)
	{
		for (int i = start; i < end; i++)
		{
			int num = (int)(TextureScale.ratioY * (float)i) * TextureScale.w;
			int num2 = i * TextureScale.w2;
			for (int j = 0; j < TextureScale.w2; j++)
			{
				TextureScale.newColors[num2 + j] = TextureScale.texColors[(int)((float)num + TextureScale.ratioX * (float)j)];
			}
		}
		TextureScale.mutex.WaitOne();
		TextureScale.finishCount++;
		TextureScale.mutex.ReleaseMutex();
	}
	private static Color ColorLerpUnclamped(Color c1, Color c2, float value)
	{
		return new Color(c1.r + (c2.r - c1.r) * value, c1.g + (c2.g - c1.g) * value, c1.b + (c2.b - c1.b) * value, c1.a + (c2.a - c1.a) * value);
	}
}
