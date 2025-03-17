﻿namespace FDK
{
	/// <summary>
	/// <para>タイマの抽象クラス。</para>
	/// <para>このクラスを継承し、override したクラスを作成することで、任意のクロックを持つタイマを作成できる。</para>
	/// </summary>
	public abstract class CTimerBase : IDisposable
	{
		public const long nUnused = -1;

		// この２つを override する。
		public abstract long nSystemTimeMs
		{
			get;
		}
		public abstract void Dispose();

		#region [ DTXMania用に、語尾にmsのつかない宣言を追加 ]
		public long nシステム時刻
		{
			get { return nSystemTimeMs; }
		}
		public long nCurrentTime  // n現在時刻
		{
			get { return n現在時刻ms; }
			set { n現在時刻ms = value; }
		}
		public long n前回リセットした時のシステム時刻
		{
			get { return n前回リセットした時のシステム時刻ms; }
		}
		#endregion

		public long n現在時刻ms
		{
			get
			{
				if (this.n停止数 > 0)
					return (this.n一時停止システム時刻ms - this.n前回リセットした時のシステム時刻ms);

				return (this.n更新システム時刻ms - this.n前回リセットした時のシステム時刻ms);
			}
			set
			{
				if (this.n停止数 > 0)
					this.n前回リセットした時のシステム時刻ms = this.n一時停止システム時刻ms - value;
				else
					this.n前回リセットした時のシステム時刻ms = this.n更新システム時刻ms - value;
			}
		}
		public long nリアルタイム現在時刻ms
		{
			get
			{
				if (this.n停止数 > 0)
					return (this.n一時停止システム時刻ms - this.n前回リセットした時のシステム時刻ms);

				return (this.nSystemTimeMs - this.n前回リセットした時のシステム時刻ms);
			}
		}
		public long n前回リセットした時のシステム時刻ms
		{
			get;
			protected set;
		}

		public bool b停止していない
		{
			get
			{
				return (this.n停止数 == 0);
			}
		}
		public void tReset()  // tリセット
		{
			this.tUpdate();
			this.n前回リセットした時のシステム時刻ms = this.n更新システム時刻ms;
			this.n一時停止システム時刻ms = this.n更新システム時刻ms;
			this.n停止数 = 0;
		}
		public void tPause()  // t一時停止
		{
			if (this.n停止数 == 0)
				this.n一時停止システム時刻ms = this.n更新システム時刻ms;

			this.n停止数++;
		}
		public void tUpdate()  // t更新
		{
			this.n更新システム時刻ms = this.nSystemTimeMs;
		}
		public void tResume()  // t再開
		{
			if (this.n停止数 > 0)
			{
				this.n停止数--;
				if (this.n停止数 == 0)
				{
					this.tUpdate();
					this.n前回リセットした時のシステム時刻ms += this.n更新システム時刻ms - this.n一時停止システム時刻ms;
				}
			}
		}

		#region [ protected ]
		//-----------------
		protected long n一時停止システム時刻ms = 0;
		protected long n更新システム時刻ms = 0;
		protected int n停止数 = 0;
		//-----------------
		#endregion
	}
}
