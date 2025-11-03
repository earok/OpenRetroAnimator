using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	public static T Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = FindObjectOfType<T>();
				//AGGRESSIVE find
				if (_instance == null)
				{
					var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
					foreach (var root in rootObjects)
					{
						_instance = root.GetComponentInChildren<T>(true);
						if (_instance)
						{
							return _instance;
						}
					}
				}
			}
			return _instance;
		}
	}
	static T _instance;

	//Shortcuts
	public AudioSource AudioSource
	{
		get
		{
			if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
			return _audioSource;
		}
	}
	AudioSource _audioSource;
}
