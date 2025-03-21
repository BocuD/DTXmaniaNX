﻿using System.Diagnostics;
using FDK;
using SharpDX.Direct3D9;

namespace DTXMania.Core;

/// <summary>
/// 描画フレーム毎にGPUをフラッシュして、描画遅延を防ぐ。
/// DirectX9の、Occlusion Queryを用いる。(Flush属性付きでGetDataする)
/// Device Lost対策のため、QueueをCActivitiyのManagedリソースとして扱う。
/// OnUpdateAndDraw()を呼び出すことで、GPUをフラッシュする。
/// </summary>
internal class CActFlushGPU : CActivity
{
	// CActivity 実装

	public override void OnManagedCreateResources()
	{
		if ( !bNotActivated )
		{
			try			// #xxxxx 2012.12.31 yyagi: to prepare flush, first of all, I create q queue to the GPU.
			{
				IDirect3DQuery9 = new Query( CDTXMania.app.Device, QueryType.Occlusion );
			}
			catch ( Exception e )
			{
				Trace.TraceError( e.Message );
			}
			base.OnManagedCreateResources();
		}
	}
	public override void  OnManagedReleaseResources()
	{
		IDirect3DQuery9.Dispose();
		IDirect3DQuery9 = null;
		base.OnManagedReleaseResources();
	}
	public override int OnUpdateAndDraw()
	{
		if ( !bNotActivated )
		{
			IDirect3DQuery9.Issue( Issue.End );
			IDirect3DQuery9.GetData<int>( out _, true );	// flush GPU queue
		}
		return 0;
	}

	// Other

	#region [ private ]
	//-----------------
	private Query IDirect3DQuery9;
	//-----------------
	#endregion
}