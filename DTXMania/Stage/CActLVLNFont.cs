﻿using System.Runtime.InteropServices;
using System.Drawing;
using DTXMania.Core;
using FDK;

namespace DTXMania;

public class CActLVLNFont : CActivity
{
	// コンストラクタ

	const int numWidth = 15;
	const int numHeight = 19;
	public CActLVLNFont()
	{
		string numChars = "0123456789?-";
		st数字 = new ST数字[12, 4];

		for (int j = 0; j < 4; j++)
		{
			for (int i = 0; i < 12; i++)
			{
				st数字[i, j].ch = numChars[i];
				st数字[i, j].rc = new Rectangle(
					(i % 4) * numWidth + (j % 2) * 64,
					(i / 4) * numHeight + (j / 2) * 64,
					numWidth,
					numHeight
				);
			}
		}
	}


	// メソッド
	public void tDrawString(int x, int y, string str)
	{
		tDrawString(x, y, str, EFontColor.White, EFontAlign.Right);
	}
	public void tDrawString(int x, int y, string str, EFontColor efc, EFontAlign efa)
	{
		if (!bNotActivated && !string.IsNullOrEmpty(str))
		{
			if (tx数値 != null)
			{
				bool bRightAlign = (efa == EFontAlign.Right);

				if (bRightAlign)							// 右詰なら文字列反転して右から描画
				{
					char[] chars = str.ToCharArray();
					Array.Reverse(chars);
					str = new string(chars);
				}

				foreach (char ch in str)
				{
					int p = (ch == '-' ? 11 : ch - '0');
					ST数字 s = st数字[p, (int)efc];
					int sw = s.rc.Width;
					int delta = bRightAlign ? 0 : -sw;
					tx数値.tDraw2D(CDTXMania.app.Device, x + delta, y, s.rc);
					x += bRightAlign ? -sw : sw;
				}
			}
		}
	}


	// CActivity 実装

	public override void OnManagedCreateResources()
	{
		if (!bNotActivated)
		{
			tx数値 = CDTXMania.tGenerateTexture(CSkin.Path(@"Graphics\ScreenSelect level numbers.png"));
			base.OnManagedCreateResources();
		}
	}
	public override void OnManagedReleaseResources()
	{
		if (!bNotActivated)
		{
			if ( tx数値 != null )
			{
				tx数値.Dispose();
				tx数値 = null;
			}
			base.OnManagedReleaseResources();
		}
	}


	// Other

	#region [ private ]
	//-----------------
	[StructLayout(LayoutKind.Sequential)]
	private struct ST数字
	{
		public char ch;
		public Rectangle rc;
	}

	public enum EFontColor
	{
		Red = 0,
		Yellow = 1,
		Orange = 2,
		White = 3
	}
	public enum EFontAlign
	{
		Left,
		Right
	}
	private ST数字[,] st数字;
	private CTexture tx数値;
	//-----------------
	#endregion
}