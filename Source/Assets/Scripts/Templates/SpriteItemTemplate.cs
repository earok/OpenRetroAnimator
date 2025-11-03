using RetroAnimator.Undo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpriteItemTemplate : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
	public const string Attr_White = "W";
	public const string Attr_NoFlip = "✗";
	public const string Attr_Flipped = "←";

	public RawImage RawImage;
	public Image Background;
	public TextMeshProUGUI Text;
	public TextMeshProUGUI AttributeText;

	public string Frame;
	public int ID;
	private bool _cancelClick;

	public List<SpriteItemTemplate> SelectedSprites
	{
		get
		{
			var result = new List<SpriteItemTemplate>();
			foreach (var item in FrameManager.Instance.SpriteListContainer.GetComponentsInChildren<SpriteItemTemplate>())
			{
				if (FrameManager.Instance.SelectedSprites.Contains(item.ID))
				{
					result.Add(item);
				}
			}

			return result.OrderBy(p => p.ID).ToList();
		}
	}

	public List<SpriteItemTemplate> DraggedSprites;

	public RetroAnimator.Entities.Sprite Data
	{
		get
		{
			return Manager.Instance.Project.Frames[Frame].Sprites[ID];
		}
	}

	public bool IsSelected
	{
		get
		{
			return
				FrameManager.Instance.SelectedFrame == Frame
				&& FrameManager.Instance.SelectedSprites.Contains(ID);
		}
	}

	public void Update()
	{
		Background.color = IsSelected ? Manager.Instance.SelectedColor : Manager.Instance.NotSelectedColor;
		Text.color = Color.white;
		AttributeText.text = string.Empty;
		if (Data.NoFlip) AttributeText.text += Attr_NoFlip;
		if (Data.RenderMode == RetroAnimator.Entities.Sprite.RenderModes.White) AttributeText.text += Attr_White;
		if (Data.XFlip) AttributeText.text += Attr_Flipped;
	}

	int PositionOffset
	{
		get
		{
			var local1 = FrameManager.Instance.SpriteListContainer.transform.InverseTransformPoint(DraggedSprites.Last().transform.position);
			local1.y += 16;
			var local2 = FrameManager.Instance.SpriteListContainer.transform.InverseTransformPoint(FrameManager.Instance.SpriteListContainer.transform.position);

			var delta = local2.y - local1.y;
			return Mathf.Clamp((int)(delta / 32), 0, FrameManager.Instance.SpriteListContainer.transform.childCount - 1);

			/*
			foreach (Transform child in FrameManager.Instance.SpriteListContainer.transform)
			{
				if (local1.y + heightOffset > local2.y)
				{
					return i;
				}
				i++;
			}
			return FrameManager.Instance.SpriteListContainer.transform.childCount - 1;*/
		}
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (!FrameManager.Instance.SelectedSprites.Contains(ID))
		{
			FrameManager.Instance.SelectSprite(ID);
		}
		DraggedSprites = SelectedSprites;

		Undo.Instance.PushUndo();
		FrameManager.Instance.EmptySprite.parent = FrameManager.Instance.SpriteListContainer.transform;
		var sizeDelta = FrameManager.Instance.EmptySprite.sizeDelta;
		sizeDelta.y = GetComponent<RectTransform>().sizeDelta.y * DraggedSprites.Count;
		FrameManager.Instance.EmptySprite.sizeDelta = sizeDelta;

		//		GetComponentInParent<VerticalLayoutGroup>().enabled = false;

		foreach (var item in DraggedSprites)
		{
			item.transform.parent = FrameManager.Instance.transform;
			item.transform.SetAsLastSibling();
		}

	}

	public void OnDrag(PointerEventData eventData)
	{
		_cancelClick = true;
		foreach (var item in DraggedSprites)
		{
			item.transform.localPosition += new Vector3(0, eventData.delta.y, 0);
		}

		FrameManager.Instance.EmptySprite.SetSiblingIndex(PositionOffset);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		//		GetComponentInParent<VerticalLayoutGroup>().enabled = true;

		var selectedIDs = new List<int>();
		//sprites.Reverse();

		var spriteDatas = new List<RetroAnimator.Entities.Sprite>();
		foreach (var sprite in DraggedSprites)
		{
			spriteDatas.Add(sprite.Data);
		}

		//Special handling for first sprite
		var selectedFrame = Manager.Instance.Project.Frames[FrameManager.Instance.SelectedFrame];
		var offset = PositionOffset;

		//Clean up
		FrameManager.Instance.EmptySprite.parent = null;
		foreach (var sprite in DraggedSprites)
		{
			Destroy(sprite.gameObject);
		}

		//Remove all of the selected sprites from the frame data
		foreach (var data in spriteDatas)
		{
			selectedFrame.Sprites.Remove(data);
		}

		//Flip the draw order
		offset = selectedFrame.Sprites.Count - offset;

		//Flip draw order
		//offset = selectedFrame.Sprites.Count - 1 - offset;
		//if (offset + spriteDatas.Count > selectedFrame.Sprites.Count)
		//{
		//offset = selectedFrame.Sprites.Count - spriteDatas.Count;
		//}


		selectedFrame.Sprites.Insert(offset, spriteDatas[0]);
		selectedIDs.Add(offset);

		//For the remainder of the sprites
		for (var i = 1; i < spriteDatas.Count; i++)
		{
			offset++;
			selectedFrame.Sprites.Insert(offset, spriteDatas[i]);
			selectedIDs.Add(offset);
		}

		FrameManager.Instance.ClearSelectedSprites();
		FrameManager.Instance.RefreshSpriteList();
		foreach (var selectedID in selectedIDs)
		{
			FrameManager.Instance.SelectedSprites.Add(selectedID);
		}
	}

	internal void Initialize(string frame, int index)
	{
		Frame = frame;
		ID = index;
		RawImage.texture = Functions.GetTexture(Data.FramePath, false);
		Text.text = Path.GetFileNameWithoutExtension(Data.FramePath);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (_cancelClick)
		{
			_cancelClick = false;
			return;
		}

		if (eventData.button == PointerEventData.InputButton.Right)
		{
			if (!IsSelected)
			{
				FrameManager.Instance.SelectSprite(ID);
			}
			ContextMenuManager.Instance.SetSpriteContextMenu(Frame, FrameManager.Instance.SelectedSprites.ToList());
		}
		else
		{
			FrameManager.Instance.SelectSprite(ID, true);
		}
	}

}
