using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DrawPanelBG : Singleton<MonoBehaviour>, IPointerClickHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
{
	public RectTransform DragRect;
	public Image DragRectImage;
	public ScrollRect ScrollRect;
	private bool _cancelClick;


	private Vector2 _currentDragPosition;
	private Vector2 _startDragPosition;

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Right)
		{
			ExecuteEvents.Execute(ScrollRect.gameObject, eventData, ExecuteEvents.beginDragHandler);
			return;
		}

		_startDragPosition = _currentDragPosition = eventData.position;
		DragRect.gameObject.SetActive(true);
		RefreshDragRect();
	}

	private void RefreshDragRect()
	{
		var minX = Mathf.Min(_startDragPosition.x, _currentDragPosition.x);
		var maxX = Mathf.Max(_startDragPosition.x, _currentDragPosition.x);
		var minY = Mathf.Min(_startDragPosition.y, _currentDragPosition.y);
		var maxY = Mathf.Max(_startDragPosition.y, _currentDragPosition.y);

		var color = SaveData.DragRectColor.Value;
		color.a = 128;

		DragRectImage.color = color;
		DragRect.position = new Vector3(minX, maxY, 0);
		DragRect.sizeDelta = new Vector2(maxX - minX, maxY - minY) / DrawPanelManager.Instance.zoomLevel;
	}

	public void OnDrag(PointerEventData eventData)
	{
		_cancelClick = true;
		if (eventData.button != PointerEventData.InputButton.Right)
		{
			ExecuteEvents.Execute(ScrollRect.gameObject, eventData, ExecuteEvents.dragHandler);
			return;
		}

		_currentDragPosition = eventData.position;
		RefreshDragRect();
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Right)
		{
			ExecuteEvents.Execute(ScrollRect.gameObject, eventData, ExecuteEvents.endDragHandler);
			return;
		}

		_currentDragPosition = eventData.position;

		FrameManager.Instance.RectangleSelection(DragRect);

		DragRect.gameObject.SetActive(false);
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
			ContextMenuManager.Instance.SetBackgroundContextMenu();
		}
		else
		{
			FrameManager.Instance.UnSelect();
		}
	}
}
