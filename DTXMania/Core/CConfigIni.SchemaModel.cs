#nullable disable

namespace DTXMania.Core;

// Data model for the declarative Config.ini schema: the small types describing a section, a group
// of related keys, and a single key. The schema itself lives in CConfigIni.Schema.cs; the builders
// and read/write engine live in CConfigIni.SchemaEngine.cs.
internal partial class CConfigIni
{
	#region [ schema model ]

	/// <summary>
	/// A single INI key. Knows how to parse its value from a string and how to render it back.
	/// <para>
	/// A null <see cref="Write"/> makes the item read-only: still accepted when parsing (for backwards
	/// compatibility) but never written. A non-null <see cref="WriteMany"/> lets one key appear on
	/// multiple lines (e.g. [GUID]'s repeated JoystickID entries); it takes precedence over
	/// <see cref="Write"/> and yields one value per output line. <see cref="Read"/> is still invoked
	/// once per occurrence, so it can accumulate.
	/// </para>
	/// </summary>
	private sealed class ConfigItem
	{
		public readonly string Key;
		public readonly Action<CConfigIni, string> Read;
		public readonly Func<CConfigIni, string> Write;
		public readonly Func<CConfigIni, IEnumerable<string>> WriteMany;

		public ConfigItem(string key, Action<CConfigIni, string> read,
			Func<CConfigIni, string> write = null, Func<CConfigIni, IEnumerable<string>> writeMany = null)
		{
			Key = key;
			Read = read;
			Write = write;
			WriteMany = writeMany;
		}
	}

	/// <summary>
	/// A run of related items that share a leading comment block. Groups are purely a
	/// write-time/readability convenience; on read the items are flattened into a lookup by key.
	/// </summary>
	private sealed class ConfigGroup
	{
		public readonly string[] Comments;
		public readonly ConfigItem[] Items;

		/// <summary>Extra comment lines computed at write time (e.g. the list of available ASIO devices).</summary>
		public readonly Func<IEnumerable<string>> DynamicComments;

		public ConfigGroup(string[] comments, ConfigItem[] items, Func<IEnumerable<string>> dynamicComments = null)
		{
			Comments = comments ?? [];
			Items = items;
			DynamicComments = dynamicComments;
		}
	}

	/// <summary>A named INI section made up of ordered groups of items.</summary>
	private sealed class ConfigSection
	{
		public readonly string Name;
		public readonly ConfigGroup[] Groups;

		/// <summary>Raw lines written verbatim before the section header (e.g. the key-assign legend).</summary>
		public readonly string[] HeaderLines;

		public ConfigSection(string name, ConfigGroup[] groups, string[] headerLines = null)
		{
			Name = name;
			Groups = groups;
			HeaderLines = headerLines ?? [];
		}
	}

	#endregion
}
