using Newtonsoft.Json;
using RetroAnimator.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using UnityEngine;
using UnityEngine.Playables;

public static class Functions
{
	public static Dictionary<string, Texture2D> NormalTextures = new Dictionary<string, Texture2D>();
	public static Dictionary<string, Texture2D> WhiteTextures = new Dictionary<string, Texture2D>();
	public static Dictionary<string, UnityEngine.Sprite> NormalSprites = new Dictionary<string, UnityEngine.Sprite>();
	public static Dictionary<string, UnityEngine.Sprite> WhiteSprites = new Dictionary<string, UnityEngine.Sprite>();


	public static bool Overlaps(this RectTransform a, RectTransform b)
	{
		return a.WorldRect().Overlaps(b.WorldRect());
	}
	public static bool Overlaps(this RectTransform a, RectTransform b, bool allowInverse)
	{
		return a.WorldRect().Overlaps(b.WorldRect(), allowInverse);
	}

	static Vector3[] corners = new Vector3[4];

	public static Rect WorldRect(this RectTransform rectTransform)
	{
		rectTransform.GetWorldCorners(corners);
		var minX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
		var maxX = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
		var minY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
		var maxY = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
		return new Rect(minX, minY, maxX - minX, maxY - minY);

		/*
		Vector2 sizeDelta = rectTransform.sizeDelta;
		float rectTransformWidth = sizeDelta.x * rectTransform.lossyScale.x;
		float rectTransformHeight = sizeDelta.y * rectTransform.lossyScale.y;

		Vector3 position = rectTransform.position;
		return new Rect(position.x - rectTransformWidth / 2f, position.y - rectTransformHeight / 2f, rectTransformWidth, rectTransformHeight);*/
	}


	internal static string GetRelativePath(string fromPath, string toPath)
	{
		var fromUri = new Uri(fromPath);
		var toUri = new Uri(toPath);
		var relativeUri = fromUri.MakeRelativeUri(toUri);
		var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
		return relativePath.FixPath();
	}

	internal static string FixPath(this string path)
	{
		return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
	}

	internal static bool ContainsPoint(this RectTransform t, Vector2 point)
	{
		var localPos = t.InverseTransformPoint(point);
		return t.rect.Contains(localPos);
	}

	internal static Texture2D GetTexture(string framePath, bool isWhite)
	{
		var targetDictionary = isWhite ? WhiteTextures : NormalTextures;

		if (framePath == null)
		{
			throw new Exception("Frame path cannot be null");
		}

		if (targetDictionary.TryGetValue(framePath, out Texture2D returnValue))
		{
			return returnValue;
		}

		if (!File.Exists((SaveData.ProjectPath + framePath).FixPath()))
		{
			return Manager.Instance.MissingPart;
		}

		var texture = new Texture2D(2, 2);
		var bytes = File.ReadAllBytes((SaveData.ProjectPath + framePath).FixPath());
		texture.LoadImage(bytes);
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = FilterMode.Point;

		if (isWhite)
		{
			var pixels = texture.GetPixels();
			for (var i = 0; i < pixels.Length; i++)
			{
				var color = pixels[i];
				color.r = color.g = color.b = 255;
				pixels[i] = color;
			}
			texture.SetPixels(pixels);
			texture.Apply(false);
		}

		targetDictionary[framePath] = texture;
		return texture;
	}

	internal static UnityEngine.Sprite GetSprite(string framePath, bool isWhite)
	{
		var targetDictionary = isWhite ? WhiteSprites : NormalSprites;
		if (targetDictionary.TryGetValue(framePath, out UnityEngine.Sprite returnValue))
		{
			return returnValue;
		}

		var Texture = GetTexture(framePath, isWhite);
		var sprite = UnityEngine.Sprite.Create(Texture, new Rect(0, 0, Texture.width, Texture.height), new Vector2(0.5f, 0.5f));
		targetDictionary[framePath] = sprite;
		return sprite;
	}

	//Use newtonsoft to create a complete clone of an object
	public static T JSONClone<T>(this T Source)
	{
		return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(Source));
	}


	public static Texture2D GetOutlineTexture(this Texture2D texture)
	{
		var result = new Texture2D(texture.width + 2, texture.height + 2)
		{
			filterMode = FilterMode.Point
		};

		var pixels = texture.GetPixels();
		var outPixels = new Color32[(texture.width + 2) * (texture.height + 2)];

		for (var x = 0; x < texture.width; x++)
		{
			for (var y = 0; y < texture.height; y++)
			{
				if (pixels[x + y * texture.width].a > 0)
				{
					for (var x2 = 0; x2 < 3; x2++)
					{
						for (var y2 = 0; y2 < 3; y2++)
						{
							outPixels[x + x2 + ((y + y2) * (texture.width + 2))] = new Color32(255, 255, 255, 128);
						}
					}
				}
			}
		}

		result.SetPixels32(outPixels);
		result.Apply();
		return result;
	}

	public static int GetWidth(this RetroAnimator.Entities.Sprite sprite)
	{
		return Functions.GetTexture(sprite.FramePath, false).width;
	}

	public static Color32 GetColor32(this ColRect colRect)
	{
		return new Color32(colRect.ColorR, colRect.ColorG, colRect.ColorB, 128);
	}

	public static void SetColor32(this ColRect colRect, Color32 value)
	{
		colRect.ColorR = value.r;
		colRect.ColorG = value.g;
		colRect.ColorB = value.b;
	}


}
