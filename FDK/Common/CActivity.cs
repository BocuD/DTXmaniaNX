using System.Diagnostics;

namespace FDK;

public class CActivity
{
	// プロパティ

	public bool bActivated { get; set; }
	public List<CActivity> listChildActivities;

	/// <summary>
	/// <para>初めて OnUpdateAndDraw() を呼び出す場合に true を示す。（OnActivate() 内で true にセットされる。）</para>
	/// <para>このフラグは、OnActivate() では行えないタイミングのシビアな初期化を OnUpdateAndDraw() で行うために準備されている。利用は必須ではない。</para>
	/// <para>OnUpdateAndDraw() 側では、必要な初期化を追えたら false をセットすること。</para>
	/// </summary>
	protected bool bJustStartedUpdate = true;

	
	// コンストラクタ

	public CActivity()
	{
		bActivated = false;
		listChildActivities = new List<CActivity>();
	}


	// ライフサイクルメソッド

	#region [ 子クラスで必要なもののみ override すること。]
	//-----------------

	public virtual void OnActivate()
	{
		// すでに活性化してるなら何もしない。
		if( bActivated )
			return;

		bActivated = true;		// このフラグは、以下の処理をする前にセットする。

		// 自身のリソースを作成する。
		OnManagedCreateResources();

		// すべての子 Activity を活性化する。
		foreach( CActivity activity in listChildActivities )
			activity.OnActivate();

		// その他の初期化
		bJustStartedUpdate = true;
	}
	public virtual void OnDeactivate()
	{
		// 活性化してないなら何もしない。
		if (!bActivated)
			return;

		try
		{
			// 自身のリソースを解放する。
			OnManagedReleaseResources();
		}
		catch (Exception e)
		{
			Trace.TraceError($"An exception was thrown during CActivity.OnDeactivate()! {e} {e.StackTrace}");
		}
		
		// すべての 子Activity を非活性化する。
		foreach (CActivity activity in listChildActivities)
		{
			try
			{
				activity.OnDeactivate();
			}
			catch (Exception e)
			{
				Trace.TraceError(
					$"An exception was thrown during CActivity.OnDeactivate() while deactivating child" +
					$"{(activity != null ? activity.GetType().Name : "null activity")}! {e} {e.StackTrace}");
			}
		}

		bActivated = false;	// このフラグは、以上のメソッドを呼び出した後にセットする。
	}

	/// <summary>
	/// <para>Managed リソースの作成を行う。</para>
	/// <para>Direct3D デバイスが作成された直後に呼び出されるので、自分が活性化している時に限り、
	/// Managed リソースを作成（または再構築）すること。</para>
	/// <para>いつどのタイミングで呼び出されるか（いつDirect3Dが再作成されるか）分からないので、
	/// いつ何時呼び出されても問題無いようにコーディングしておくこと。</para>
	/// </summary>
	public virtual void OnManagedCreateResources()
	{
		// 活性化してないなら何もしない。
		if (!bActivated)
			return;

		// すべての 子Activity の Managed リソースを作成する。
		foreach( CActivity activity in listChildActivities )
			activity.OnManagedCreateResources();
	}

	/// <summary>
	/// <para>Managed リソースの解放を行う。</para>
	/// <para>Direct3D デバイスの解放直前に呼び出される。
	/// （Unmanaged リソースとは異なり、Direct3D デバイスのリセット時には呼び出されない。）</para>
	/// <para>いつどのタイミングで呼び出されるか（いつDirect3Dが解放されるか）分からないので、
	/// いつ何時呼び出されても問題無いようにコーディングしておくこと。</para>
	/// </summary>
	public virtual void OnManagedReleaseResources()
	{
		// 活性化してないなら何もしない。
		if (!bActivated)
			return;

		// すべての 子Activity の Managed リソースを解放する。
		foreach( CActivity activity in listChildActivities )
			activity.OnManagedReleaseResources();
	}

	/// <summary>
	/// <para>Update and draw. (These are not separated, only one method is implemented).</para>
	/// <para>This method is called after BeginScene(), so it doesn't matter which drawing method is used.</para>
	/// </summary>
	/// <returns>Any integer. Be consistent with the caller.</returns>
	public virtual int OnUpdateAndDraw()
	{
		// 活性化してないなら何もしない。
		if (!bActivated)
			return 0;


		/* ここで進行と描画を行う。*/


		// 戻り値とその意味は子クラスで自由に決めていい。
		return 0;
	}
	
	#endregion
}