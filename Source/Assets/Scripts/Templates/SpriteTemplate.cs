using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RetroAnimator.Entities;
using System.Linq;
using RetroAnimator.Undo;

public class SpriteTemplate : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
	public Image Image;
	public RawImage SelectedRawImage;
	public RectTransform RectTransform;

	public string Frame;
	public int ID;

	public RetroAnimator.Entities.Sprite Data
	{
		get
		{
			if (!Manager.Instance.Project.Frames.ContainsKey(Frame)) return null;
			var frame = Manager.Instance.Project.Frames[Frame];
			if (ID < 0 || ID >= frame.Sprites.Count)
			{
				return null;
			}
			return frame.Sprites[ID];
		}
	}

	private bool IsDraggable;
	internal bool OverrideScale;
	private Vector2 totalDelta;
	private Vector3 dragStart;
	private bool HasGeneratedSelectedVersion;
	private string lastSpritePath;
	private bool _cancelClick;

	private Texture2D Texture
	{
		get
		{
			return Image.sprite.texture;
		}
	}

	private static int ZoomLevel
	{
		get
		{
			return DrawPanelManager.Instance.zoomLevel;
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

	internal void Initialize(string frame, int index, bool isDraggable)
	{
		ID = index;
		Frame = frame;
		IsDraggable = isDraggable;
		Update();
	}

	public void Update()
	{
		//Sanity check
		if (Data == null)
		{
			return;
		}

		//Replaced sprite path
		if (lastSpritePath != Data.FramePath)
		{
			//Import the texture
			Image.sprite = Functions.GetSprite(Data.FramePath, Data.RenderMode == RetroAnimator.Entities.Sprite.RenderModes.White);
			Image.raycastTarget = IsDraggable;
			Image.alphaHitTestMinimumThreshold = 1;
			RectTransform.sizeDelta = new Vector2(Texture.width, Texture.height);
			transform.localPosition = new Vector3(Data.HandleX, -Data.HandleY);
			HasGeneratedSelectedVersion = false;
			lastSpritePath = Data.FramePath;
		}

		if (OverrideScale) return;

		var flipped = Data.XFlip;
		if (Data.NoFlip && transform.parent.localScale.x < 0)
		{
			flipped = !flipped;
		}

		transform.localScale = new Vector3(flipped ? -1 : 1, 1, 1);
		RectTransform.pivot = new Vector2(flipped ? 1 : 0, 1);

		transform.localPosition = new Vector3(Data.HandleX, -Data.HandleY, 0);

		SelectedRawImage.gameObject.SetActive(false);
		if (IsDraggable && IsSelected)
		{
			if (!HasGeneratedSelectedVersion)
			{
				SelectedRawImage.texture = Texture.GetOutlineTexture();
				HasGeneratedSelectedVersion = true;
			}
			SelectedRawImage.gameObject.SetActive(true);
			SelectedRawImage.color = SaveData.HighlightColor.Value;
		}
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (!IsDraggable) return;

		if (!FrameManager.Instance.SelectedSprites.Contains(ID))
		{
			FrameManager.Instance.SelectSprite(ID);
		}

		Undo.Instance.PushUndo();
		totalDelta = Vector2.zero;
		dragStart = transform.localPosition;
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (!IsDraggable) return;

		_cancelClick = true;
		var delta = eventData.delta;
		delta /= DrawPanelManager.Instance.zoomLevel;

		if (DrawPanelManager.Instance.mirrored)
		{
			delta.x *= -1;
		}

		delta.y *= -1;
		totalDelta += delta;

		if (totalDelta.x > 1)
		{
			FrameManager.Instance.MoveSpritePosition(Mathf.FloorToInt(totalDelta.x), 0, FrameManager.SmallMovementModes.off);
			totalDelta.x -= Mathf.FloorToInt(totalDelta.x);
		}
		if (totalDelta.x < -1)
		{
			FrameManager.Instance.MoveSpritePosition(Mathf.CeilToInt(totalDelta.x), 0, FrameManager.SmallMovementModes.off);
			totalDelta.x -= Mathf.CeilToInt(totalDelta.x);
		}

		if (totalDelta.y > 1)
		{
			FrameManager.Instance.MoveSpritePosition(0, Mathf.FloorToInt(totalDelta.y), FrameManager.SmallMovementModes.off);
			totalDelta.y -= Mathf.FloorToInt(totalDelta.y);
		}
		if (totalDelta.y < -1)
		{
			FrameManager.Instance.MoveSpritePosition(0, Mathf.CeilToInt(totalDelta.y), FrameManager.SmallMovementModes.off);
			totalDelta.y -= Mathf.CeilToInt(totalDelta.y);
		}

	}


	public void OnEndDrag(PointerEventData eventData)
	{
		if (!IsDraggable) return;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (!IsDraggable) return;

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
