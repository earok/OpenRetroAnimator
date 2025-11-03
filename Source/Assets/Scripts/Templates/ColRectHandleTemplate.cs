using RetroAnimator.Undo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ColRectHandleTemplate : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	public ColRectTemplate ColRectTemplate;
	private Vector2 totalDelta;
	private Vector2 dragStart;

	private static int ZoomLevel
	{
		get
		{
			return DrawPanelManager.Instance.zoomLevel;
		}
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		Undo.Instance.PushUndo();
		totalDelta = Vector2.zero;
		dragStart = ColRectTemplate.RectTransform.sizeDelta;
	}

	public void OnDrag(PointerEventData eventData)
	{
		totalDelta += eventData.delta;

		ColRectTemplate.RectTransform.sizeDelta = new Vector2(
			Mathf.Max(dragStart.x + Mathf.RoundToInt(totalDelta.x / ZoomLevel) * (DrawPanelManager.Instance.mirrored ? -1 : 1), 1),
			Mathf.Max(dragStart.y - Mathf.RoundToInt(totalDelta.y / ZoomLevel), 1)
			);

	}

	public void OnEndDrag(PointerEventData eventData)
	{
		ColRectTemplate.PositionToData();
	}
}
