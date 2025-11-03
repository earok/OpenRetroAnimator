using RetroAnimator;
using RetroAnimator.Entities;
using RetroAnimator.Undo;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColRectTemplate : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
	public RectTransform RectTransform;
	public RawImage Body;

	internal string Frame;
	internal string ID;

	internal ColRect Data
	{
		get
		{
			return Manager.Instance.Project.Frames[Frame].ColRects[ID];
		}
	}

	private Vector2 totalDelta;
	private Vector3 dragStart;
	private bool IsBeingDragged;

	public void Update()
	{
		var isSelected = FrameManager.Instance.SelectedColRect == ID;

		if (DrawPanelManager.Instance.previewColRects || isSelected)
		{
			Body.color = Data.GetColor32();
			Body.gameObject.SetActive(true);
		}
		else
		{
			Body.gameObject.SetActive(false);
		}


		if (isSelected)
		{
			transform.SetAsLastSibling();
		}

		if (IsBeingDragged == false)
		{
			transform.localPosition = new Vector3(Data.X, -Data.Y, 0);
		}
	}

	private static int ZoomLevel
	{
		get
		{
			return DrawPanelManager.Instance.zoomLevel;
		}
	}

	internal void Initialise(string frame, string id)
	{
		Frame = frame;
		ID = id;
		transform.localPosition = new Vector3(Data.X, -Data.Y);
		RectTransform.sizeDelta = new Vector2(Data.Width, Data.Height);
		Update();
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		Undo.Instance.PushUndo();
		IsBeingDragged = true;
		totalDelta = Vector2.zero;
		dragStart = transform.localPosition;
		FrameManager.Instance.SelectedColRect = ID;
	}

	public void OnDrag(PointerEventData eventData)
	{
		totalDelta += eventData.delta;
		transform.localPosition = dragStart +
			new Vector3(
				Mathf.RoundToInt(totalDelta.x / ZoomLevel) * (DrawPanelManager.Instance.mirrored ? -1 : 1),
				Mathf.RoundToInt(totalDelta.y / ZoomLevel), 0);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		IsBeingDragged = false;
		PositionToData();
	}

	internal void PositionToData()
	{
		Data.X = Mathf.RoundToInt(transform.localPosition.x);
		Data.Y = -Mathf.RoundToInt(transform.localPosition.y);
		Data.Width = Mathf.RoundToInt(RectTransform.sizeDelta.x);
		Data.Height = Mathf.RoundToInt(RectTransform.sizeDelta.y);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		FrameManager.Instance.SelectedColRect = ID;
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			ContextMenuManager.Instance.SetColRectContextMenu(Frame, ID);
		}
	}
}
