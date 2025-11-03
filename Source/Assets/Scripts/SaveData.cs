
using System;
using System.Runtime.Remoting.Messaging;
using UnityEngine;

public static class SaveData
{
	public static string ProjectPath
	{
		get => PlayerPrefs.GetString("ProjectPath", ".");
		set
		{
			//Always set trailing slash
			if (!value.EndsWith("/"))
			{
				value += "/";
			}

			PlayerPrefs.SetString("ProjectPath", value.FixPath());
			PlayerPrefs.Save();
		}
	}

	public static string ProjectName
	{
		get => PlayerPrefs.GetString("ProjectName", ".");
		set
		{
			PlayerPrefs.SetString("ProjectName", value);
			PlayerPrefs.Save();
		}
	}

	public static SaveData<Color32> BGColor = new SaveData<Color32>("BGColor", 1, new Color32(187, 187, 187, 255));
	public static SaveData<Color32> GridColor = new SaveData<Color32>("GridColor", 2, new Color32(141, 141, 141, 255));
	public static SaveData<Color32> OriginColor = new SaveData<Color32>("OriginColor", 1, new Color32(255, 0, 0, 255));
	public static SaveData<Color32> HighlightColor = new SaveData<Color32>("HighlightColor", 1, new Color32(0, 0, 255, 255));
	public static SaveData<Color32> DragRectColor = new SaveData<Color32>("DragRectColor", 1, new Color32(128, 128, 128, 255));

	public static SaveData<int> GridSize = new SaveData<int>("GridSize", 1, 16);

	public static SaveData<bool> OriginLinesEnabled = new SaveData<bool>("OriginLinesEnabled", 1, true);
	public static SaveData<bool> GridLinesEnabled = new SaveData<bool>("GridLinesEnabled", 1, true);


}

public class SaveDataBox<T>
{
	public T Value;

	public SaveDataBox(T value)
	{
		Value = value;
	}
}

public class SaveData<T>
{
	public string Key;

	bool _hasBeenRestored;
	T _value;

	public SaveData(string key, int version, T defaultValue)
	{
		Key = key + "_" + version;
		_value = defaultValue;
	}

	public T Value
	{
		get
		{
			if (_hasBeenRestored == false)
			{
				if (PlayerPrefs.HasKey(Key))
				{
					_value = JsonUtility.FromJson<SaveDataBox<T>>(PlayerPrefs.GetString(Key)).Value;
				}
				_hasBeenRestored = true;
			}
			return _value;
		}
		set
		{
			_value = value;
			PlayerPrefs.SetString(Key, JsonUtility.ToJson(new SaveDataBox<T>(value)));
			PlayerPrefs.Save();
		}
	}
}
