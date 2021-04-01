using GTA;
using SharpDX.XInput;
using System;

namespace Benjamin94
{
	internal class PlayerSettings
	{
		private static ScriptSettings settings;

		private const string PlayerKey = "Player";

		static PlayerSettings()
		{
			PlayerSettings.settings = ScriptSettings.Load(string.Concat("scripts//", TwoPlayerMod.ScriptName, ".ini"));
		}

		public PlayerSettings()
		{
		}

		public static TEnum GetEnumValue<TEnum>(UserIndex player, string key, string defaultValue)
		{
			TEnum tEnum;
			try
			{
				string value = PlayerSettings.GetValue(player, key, defaultValue);
				tEnum = (TEnum)Enum.Parse(typeof(TEnum), value);
			}
			catch (Exception exception)
			{
				tEnum = (TEnum)Enum.Parse(typeof(TEnum), defaultValue);
			}
			return tEnum;
		}

		public static string GetValue(UserIndex player, string key, string defaultValue)
		{
			string str;
			try
			{
				string value = PlayerSettings.settings.GetValue(string.Concat("Player", player), key, defaultValue);
				if (string.IsNullOrEmpty(value))
				{
					value = defaultValue;
				}
				str = value;
				return str;
			}
			catch (Exception exception)
			{
			}
			str = defaultValue;
			return str;
		}

		public static void SetValue(UserIndex player, string key, string value)
		{
			PlayerSettings.settings.SetValue(string.Concat("Player", player), key, value);
			PlayerSettings.settings.Save();
			PlayerSettings.settings = ScriptSettings.Load(string.Concat("scripts//", TwoPlayerMod.ScriptName, ".ini"));
		}
	}
}