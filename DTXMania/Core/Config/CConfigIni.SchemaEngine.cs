#nullable disable

using System.Linq.Expressions;
using System.Text;
using FDK;

namespace DTXMania.Core;

// The engine behind the declarative Config.ini schema (CConfigIni.Schema.cs): expression-based field
// binding, the typed item builders, bespoke value conversions, and the read/write dispatch that maps
// INI lines to schema items.
internal partial class CConfigIni
{
	#region [ field binding ]

	// Turns a single "c => c.someField" expression into a get/set pair, so that the common case —
	// a value that simply reads from and writes to a field — needs only ONE lambda instead of two.
	// The setter is derived by rebuilding the same access as an assignment, which works for plain
	// fields, nested value-type members (c.bReverse.Drums), properties, and array elements
	// (c.strCardName[0]). Values needing real logic use Custom()/ReadOnly() with explicit lambdas.
	private static (Func<CConfigIni, T> get, Action<CConfigIni, T> set) Bind<T>(Expression<Func<CConfigIni, T>> field)
	{
		Func<CConfigIni, T> get = field.Compile();

		ParameterExpression cParam = field.Parameters[0];
		ParameterExpression vParam = Expression.Parameter(typeof(T), "v");

		Expression target = field.Body;
		// "c.array[i]" compiles to a read-only ArrayIndex node; turn it into a writable ArrayAccess.
		if (target is BinaryExpression { NodeType: ExpressionType.ArrayIndex } arrayIndex)
		{
			target = Expression.ArrayAccess(arrayIndex.Left, arrayIndex.Right);
		}

		Action<CConfigIni, T> set = Expression
			.Lambda<Action<CConfigIni, T>>(Expression.Assign(target, vParam), cParam, vParam)
			.Compile();

		return (get, set);
	}

	#endregion

	#region [ item builders ]

	// Short, expressive factories for the value shapes that appear in Config.ini. Each pairs the
	// exact parse call the legacy code used with the matching render, so behaviour is identical by
	// construction. The direct-field variants take just "c => c.field"; the setter is derived.

	/// <summary>A free-form string value.</summary>
	private static ConfigItem Str(string key, Expression<Func<CConfigIni, string>> field)
	{
		var (get, set) = Bind(field);
		return new(key, (c, v) => set(c, v), c => get(c));
	}

	/// <summary>A boolean rendered as 1/0 and parsed with <see cref="CConversion.bONorOFF"/>.</summary>
	private static ConfigItem Bool(string key, Expression<Func<CConfigIni, bool>> field)
	{
		var (get, set) = Bind(field);
		return new(key,
			(c, v) => { if (v.Length > 0) set(c, CConversion.bONorOFF(v[0])); },
			c => get(c) ? "1" : "0");
	}

	/// <summary>An integer clamped to [<paramref name="min"/>, <paramref name="max"/>], keeping the
	/// current value when the text is unparseable or out of range (<see cref="CConversion.nGetNumberIfInRange"/>).</summary>
	private static ConfigItem Int(string key, int min, int max, Expression<Func<CConfigIni, int>> field)
	{
		var (get, set) = Bind(field);
		return new ConfigItem(key,
			(c, v) => set(c, CConversion.nGetNumberIfInRange(v, min, max, get(c))),
			c => get(c).ToString());
	}

	/// <summary>An integer rounded into [<paramref name="min"/>, <paramref name="max"/>]
	/// (<see cref="CConversion.nRoundToRange"/>).</summary>
	private static ConfigItem IntRound(string key, int min, int max, Expression<Func<CConfigIni, int>> field)
	{
		var (get, set) = Bind(field);
		return new(key,
			(c, v) => set(c, CConversion.nRoundToRange(v, min, max, get(c))),
			c => get(c).ToString());
	}

	/// <summary>An unbounded integer, keeping the current value on parse failure
	/// (<see cref="CConversion.nStringToInt"/>).</summary>
	private static ConfigItem IntRaw(string key, Expression<Func<CConfigIni, int>> field)
	{
		var (get, set) = Bind(field);
		return new(key,
			(c, v) => set(c, CConversion.nStringToInt(v, get(c))),
			c => get(c).ToString());
	}

	/// <summary>An enum stored as its integer value and clamped to [<paramref name="min"/>, <paramref name="max"/>].</summary>
	private static ConfigItem Enum<T>(string key, int min, int max, Expression<Func<CConfigIni, T>> field)
		where T : struct, Enum
	{
		var (get, set) = Bind(field);
		return new(key,
			(c, v) => set(c, (T)(object)CConversion.nGetNumberIfInRange(v, min, max, Convert.ToInt32(get(c)))),
			c => Convert.ToInt32(get(c)).ToString());
	}

	/// <summary>A key-assignment list, parsed/rendered via the shared key-code encoding.</summary>
	private static ConfigItem KeyItem(string key, Expression<Func<CConfigIni, CKeyAssign.STKEYASSIGN[]>> field)
	{
		Func<CConfigIni, CKeyAssign.STKEYASSIGN[]> get = field.Compile();
		return new(key,
			(c, v) => c.tReadAndSetSkey(v, get(c)),
			c => strEncodeKeyAssignments(get(c)));
	}

	/// <summary>Full control over both directions, for values with bespoke parse/render logic.</summary>
	private static ConfigItem Custom(string key, Action<CConfigIni, string> read, Func<CConfigIni, string> write)
		=> new(key, read, write);

	/// <summary>A key that is still accepted when parsing (for backwards compatibility) but never written.</summary>
	private static ConfigItem ReadOnly(string key, Action<CConfigIni, string> read)
		=> new(key, read);

	/// <summary>A key that may occur multiple times (e.g. [GUID]'s JoystickID entries): <paramref name="read"/>
	/// is invoked once per line and <paramref name="writeMany"/> yields one value per output line.</summary>
	private static ConfigItem Repeated(string key, Action<CConfigIni, string> read, Func<CConfigIni, IEnumerable<string>> writeMany)
		=> new(key, read, write: null, writeMany: writeMany);

	private static ConfigGroup G(string[] comments, params ConfigItem[] items) => new(comments, items);
	private static ConfigGroup G(string comment, params ConfigItem[] items) => new([comment], items);
	private static ConfigGroup G(params ConfigItem[] items) => new([], items);

	#endregion

	#region [ bespoke value conversions ]

	// Applies a legacy (un-prefixed) [HitRange] value to every category at once. The value only takes
	// effect when it parses within range
	private static void tApplyLegacyHitRange(string v, ref int drum, ref int drumPedal, ref int guitar, ref int bass)
	{
		int n = CConversion.nGetNumberIfInRange(v, 0, 0x3e7, -1);
		if (n >= 0)
		{
			drum = drumPedal = guitar = bass = n;
		}
	}

	// Encodes a key-assignment list into its Config.ini string form (e.g. "K044,M042,J16").
	// #24166 2011.1.15 yyagi: IDs use a 36-numeral system so device numbers > 9 are supported (e.g. J1023 -> JA23).
	private static string strEncodeKeyAssignments(CKeyAssign.STKEYASSIGN[] assign)
	{
		StringBuilder sb = new(32);
		bool first = true;
		for (int i = 0; i < 0x10; i++)
		{
			if (assign[i].InputDevice == EInputDevice.Unknown)
			{
				continue;
			}
			if (!first)
			{
				sb.Append(',');
			}
			first = false;
			switch (assign[i].InputDevice)
			{
				case EInputDevice.Keyboard: sb.Append('K'); break;
				case EInputDevice.MIDI入力: sb.Append('M'); break;
				case EInputDevice.Joypad: sb.Append('J'); break;
				case EInputDevice.Mouse: sb.Append('N'); break;
			}
			sb.Append("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".Substring(assign[i].ID, 1));
			sb.Append(assign[i].Code);
		}
		return sb.ToString();
	}

	// Builds the item groups for an [AutoPlay]-style section. Both [AutoPlay] and [AutoPlayCustom]
	// share the same keys, differing only in which STAUTOPLAY field they target, so the field is
	// passed in once as an expression and each pad key is bound relative to it.
	private static ConfigGroup[] tAutoPlayGroups(Expression<Func<CConfigIni, STAUTOPLAY>> ap)
	{
		//key = INI name, field = STAUTOPLAY member it maps to
		ConfigItem Pad(string key, string field)
		{
			Expression body = Expression.Field(ap.Body, field);
			var lambda = Expression.Lambda<Func<CConfigIni, bool>>(body, ap.Parameters);
			return Bool(key, lambda);
		}

		return
		[
			G("自動演奏(0:OFF, 1:ON)"),
			G(["Drums"],
				Pad("LC", "LC"), Pad("HH", "HH"), Pad("SD", "SD"), Pad("BD", "BD"), Pad("HT", "HT"),
				Pad("LT", "LT"), Pad("FT", "FT"), Pad("CY", "CY"), Pad("RD", "RD"), Pad("LP", "LP"), Pad("LBD", "LBD")),
			G(["Guitar"],
				Pad("GuitarR", "GtR"), Pad("GuitarG", "GtG"), Pad("GuitarB", "GtB"), Pad("GuitarY", "GtY"),
				Pad("GuitarP", "GtP"), Pad("GuitarPick", "GtPick"), Pad("GuitarWailing", "GtW")),
			G(["Bass"],
				Pad("BassR", "BsR"), Pad("BassG", "BsG"), Pad("BassB", "BsB"), Pad("BassY", "BsY"),
				Pad("BassP", "BsP"), Pad("BassPick", "BsPick"), Pad("BassWailing", "BsW"))
		];
	}


	//SkinPath is stored internally as an absolute path but persisted relative to System/
	private static string tWriteSkinPath(CConfigIni c)
	{
		Uri uriRoot = new(Path.Combine(CDTXMania.executableDirectory, "System" + Path.DirectorySeparatorChar));
		Uri uriPath = new(Path.Combine(c.strSystemSkinSubfolderFullName, "." + Path.DirectorySeparatorChar));
		string relPath = uriRoot.MakeRelativeUri(uriPath).ToString();     // 相対パスを取得
		relPath = System.Web.HttpUtility.UrlDecode(relPath);              // デコードする
		relPath = relPath.Replace('/', Path.DirectorySeparatorChar);      // 区切り文字が\ではなく/なので置換する
		return relPath;
	}

	private static void tReadSkinPath(CConfigIni c, string v)
	{
		string absSkinPath = v;
		if (!Path.IsPathRooted(v))
		{
			absSkinPath = Path.Combine(CDTXMania.executableDirectory, "System");
			absSkinPath = Path.Combine(absSkinPath, v);
			Uri u = new(absSkinPath);
			absSkinPath = u.AbsolutePath;                      // v内に相対パスがある場合に備える
			absSkinPath = System.Web.HttpUtility.UrlDecode(absSkinPath);  // デコードする
			absSkinPath = absSkinPath.Replace('/', Path.DirectorySeparatorChar); // 区切り文字が\ではなく/なので置換する
		}
		if (absSkinPath[absSkinPath.Length - 1] != Path.DirectorySeparatorChar)  // フォルダ名末尾に\を必ずつけて、CSkin側と表記を統一する
		{
			absSkinPath += Path.DirectorySeparatorChar;
		}
		c.strSystemSkinSubfolderFullName = absSkinPath;
	}

	#endregion

	#region [ schema-driven read/write ]

	/// <summary>
	/// Applies a single "key=value" line. Lines in unknown sections or with unknown keys are ignored,
	/// matching the reader's tolerance for missing/renamed/reordered entries.
	/// </summary>
	private void tReadSchemaField(string sectionName, string key, string value)
	{
		if (SchemaByKey.TryGetValue(sectionName, out Dictionary<string, ConfigItem> byKey)
		    && byKey.TryGetValue(key, out ConfigItem item))
		{
			item.Read(this, value);
		}
	}

	/// <summary>Writes one schema section (header, grouped comments and values) to <paramref name="sw"/>.</summary>
	private void tWriteSchemaSection(StreamWriter sw, string sectionName)
	{
		ConfigSection section = Array.Find(Schema, s => s.Name == sectionName);
		if (section == null)
		{
			return;
		}

		foreach (string line in section.HeaderLines)
		{
			sw.WriteLine(line);
		}

		sw.WriteLine("[{0}]", section.Name);
		sw.WriteLine();

		foreach (ConfigGroup group in section.Groups)
		{
			foreach (string comment in group.Comments)
			{
				sw.WriteLine("; {0}", comment);
			}
			if (group.DynamicComments != null)
			{
				foreach (string comment in group.DynamicComments())
				{
					sw.WriteLine("; {0}", comment);
				}
			}

			foreach (ConfigItem item in group.Items)
			{
				if (item.WriteMany != null)
				{
					foreach (string value in item.WriteMany(this))
					{
						sw.WriteLine("{0}={1}", item.Key, value);
					}
				}
				else if (item.Write != null)
				{
					sw.WriteLine("{0}={1}", item.Key, item.Write(this));
				}
			}

			sw.WriteLine();
		}
	}

	#endregion
}
