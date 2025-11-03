using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PartTemplate : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPartImage
{
	public RawImage RawImage;
	public TextMeshProUGUI Text;
	public string FilePath;
	public Image Background;

	public bool IsSelected
	{
		get
		{
			return FrameManager.Instance.DraggedPart == FilePath;
		}
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		FrameManager.Instance.OnDragPart(this);
	}

	public void OnDrag(PointerEventData eventData)
	{
	}

	public void OnEndDrag(PointerEventData eventData)
	{
	}


	public void Update()
	{
		Background.color = IsSelected ? Manager.Instance.SelectedColor : Manager.Instance.NotSelectedColor;
	}

	internal void Initialize(string file)
	{
		FilePath = file;
		RefreshPartImage();
		gameObject.SetActive(true);
	}

	public void RefreshPartImage()
	{
		//Import the texture
		var texture = new Texture2D(2, 2);
		var bytes = File.ReadAllBytes(SaveData.ProjectPath + FilePath);
		texture.LoadImage(bytes);
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = FilterMode.Point;
		RawImage.texture = texture;
		Text.text = Path.GetFileNameWithoutExtension(FilePath);
	}
}
