using RetroAnimator.Entities;
using RetroAnimator.Undo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class ClipboardColRectClass
{
	public string Name;
	public RetroAnimator.Entities.ColRect ColRect;
}

public class FrameManager : Singleton<FrameManager>
{

	[Header("Part Panel")]
	public PartTemplate PartTemplate;
	public VerticalLayoutGroup PartContainer;
	public RectTransform PartPanelRT;

	[Header("Frame Panel")]
	public FrameTemplate FrameTemplate;
	public VerticalLayoutGroup FrameContainer;
	public Scrollbar FrameScrollBar;
	public RectTransform FramePanelRT;

	[Header("Sprite List Panel")]
	public SpriteItemTemplate SpriteItemTemplate;
	public VerticalLayoutGroup SpriteListContainer;
	public RectTransform SpriteListPanelRT;
	public RectTransform EmptySprite;

	[Header("ColRect List Panel")]
	public ColRectItemTemplate colRectItemTemplate;
	public VerticalLayoutGroup ColRectListContainer;
	public RectTransform ColRectPanelRT;

	[Header("Animation")]
	public VerticalLayoutGroup AnimationContainer;
	public AnimationTemplate AnimationTemplate;
	public RectTransform AnimationPanelRT;
	public Scrollbar AnimationsScrollBar;

	[Header("Anim Frames")]
	public HorizontalLayoutGroup AnimationFrameContainer;
	public AnimationFrameTemplate AnimationFrameTemplate;
	public RectTransform AnimationFramePanelRT;
	public Scrollbar AnimationFrameScrollbar;
	public Transform EmptyFrame;

	private SortedList<string, FrameTemplate> frameList = new SortedList<string, FrameTemplate>();
	private SortedList<string, AnimationTemplate> animationTemplateList = new SortedList<string, AnimationTemplate>();
	internal string DraggedPart;
	private SpriteTemplate newPart;

	private ClipboardColRectClass ClipboardColRect = null;
	private List<RetroAnimator.Entities.Sprite> Clipboard = new List<RetroAnimator.Entities.Sprite>();

	public enum CycleModes
	{
		Frame,
		Animation
	}
	public CycleModes CycleMode;

	public enum DownKeys { NA, Up, Down, Left, Right };
	float downKeyTime;

	public enum PlayModes { None, Once, Loop };
	public PlayModes PlayMode
	{
		get
		{
			return _playMode;
		}
		set
		{
			switch (value)
			{
				case PlayModes.None:
					SelectedAnimationFrame = SelectedAnimationFrame;
					break;

				default:
					DrawAnimationFrame = 0;
					break;
			}
			_playMode = value;
		}
	}
	PlayModes _playMode;

	private List<SpriteItemTemplate> spriteItemList = new List<SpriteItemTemplate>(); //Just a regular list as items are index by int

	internal void RectangleSelection(RectTransform rect)
	{
		if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKeyDown(KeyCode.RightControl))
		{
			ClearSelectedSprites();
		}

		foreach (var st in DrawPanelManager.Instance.DrawArea.GetComponentsInChildren<SpriteTemplate>())
		{
			if (rect.Overlaps(st.RectTransform, true))
			{
				SelectedSprites.Add(st.ID);
			}
		}
	}

	private SortedList<string, ColRectItemTemplate> colRectItemList = new SortedList<string, ColRectItemTemplate>();

	private List<AnimationFrameTemplate> animationFrameTemplateList = new List<AnimationFrameTemplate>();
	float selectedFrameTime = 0;
	private SmallMovementModes SmallMovementMode;

	public enum SmallMovementModes { off, spritesmall, spritelarge, colrectsmall, colrectlarge };

	public void UnSelect()
	{
		ClearSelectedSprites();
		SelectedColRect = null;
	}

	internal void RenameAnimation(string oldName, string newName, bool clone)
	{
		Undo.Instance.PushUndo();

		newName = newName.Trim().ToLower();
		if (string.IsNullOrEmpty(newName) || Manager.Instance.Project.Animations.ContainsKey(newName))
		{
			//Animation with name already exists. Do nothing
			return;
		}

		SelectedAnimation = null;
		var data = Manager.Instance.Project.Animations[oldName];

		if (clone == false)
		{
			Manager.Instance.Project.Animations.Remove(oldName);
		}

		Manager.Instance.Project.Animations[newName] = data.JSONClone();
		RefreshFrameAndAnimationList();
		SelectAnimationAndScroll(newName);
	}

	internal void AddFrameToAnimation(string frameID)
	{
		if (string.IsNullOrEmpty(SelectedAnimation)) return;
		Undo.Instance.PushUndo();
		Manager.Instance.Project.Animations[SelectedAnimation].FrameData.Add(new AnimationFrameData() { FrameID = frameID, Duration = 5 });
		ForceRefresh(SelectedFrame, SelectedColRect, SelectedAnimation, SelectedAnimationFrame, SelectedSprites.ToList());
		SelectedAnimationFrame = Manager.Instance.Project.Animations[SelectedAnimation].FrameData.Count - 1;
		Canvas.ForceUpdateCanvases();
		AnimationFrameScrollbar.value = 1;
	}

	public string SelectedFrame
	{
		get
		{
			return _selectedFrame;
		}
		set
		{
			//Sanity check
			if (value == null || Manager.Instance.Project.Frames.ContainsKey(value))
			{
				_selectedFrame = value;
			}

			SelectedColRect = null;
			RefreshSpriteList();
			RefreshColRectList();
		}
	}
	string _selectedFrame;

	public static void Scroll(ref float? scrollTime, int index, int total)
	{
		if (total == 0) return;
		scrollTime = index / ((float)total - 1);
	}

	private void SelectFrameAndScroll(string frameID)
	{
		SelectedFrame = frameID;

		//ALSO move the scrollbar appropriately
		if (SelectedFrame != null)
		{
			var index = Manager.Instance.Project.Frames.Keys.OrderByDescending(p => p).ToList().IndexOf(SelectedFrame);
			Scroll(ref _nextFramesScrollTime, index, Manager.Instance.Project.Frames.Count);
		}
	}

	private void SelectAnimationAndScroll(string animationID)
	{
		SelectedAnimation = animationID;

		//ALSO move the scrollbar appropriately
		if (animationID != null)
		{
			var index = Manager.Instance.Project.Animations.Keys.OrderByDescending(p => p).ToList().IndexOf(animationID);
			Scroll(ref _nextAnimationsScrollTime, index, Manager.Instance.Project.Animations.Count);
		}
	}


	public FrameTemplate SelectedFrameTemplate
	{
		get
		{
			if (_selectedFrame == null) return null;

			try
			{
				return frameList[_selectedFrame];
			}
			catch (Exception)
			{
				print("frame not found: " + _selectedFrame);
				return null;
			}
		}
	}

	public string SelectedAnimation
	{
		get
		{
			return _selectedAnimation;
		}
		set
		{
			//Sanity check
			if (value == null || Manager.Instance.Project.Animations.ContainsKey(value))
			{
				_selectedAnimation = value;
			}
			_selectedColRect = null;
			RefreshAnimationFrameList();
		}
	}
	private string _selectedAnimation;

	public AnimationTemplate SelectedAnimationTemplate
	{
		get
		{
			if (_selectedAnimation == null) return null;
			return animationTemplateList[_selectedAnimation];
		}

	}

	public int DrawAnimationFrame
	{
		get
		{
			return _drawAnimationFrame;
		}
		set
		{
			if (SelectedAnimation == null) return;
			_drawAnimationFrame = value;
			var data = Manager.Instance.Project.Animations[SelectedAnimation].FrameData[value];
			selectedFrameTime = Mathf.Min(selectedFrameTime + data.Duration, data.Duration);
			DrawPanelManager.PreviewFrame(data.FrameID, DrawPanelManager.Instance.DrawArea.transform);
		}
	}

	internal void ChangeAnimationSpeed(string currentAnimation, float value)
	{
		foreach (var frame in Manager.Instance.Project.Animations[currentAnimation].FrameData)
		{
			frame.Duration = Mathf.RoundToInt(value / AnimationFrameTemplate.ConversionRate);
		}
		SelectedAnimation = SelectedAnimation;
	}

	int _drawAnimationFrame;

	public int SelectedAnimationFrame
	{
		get
		{
			return _selectedAnimationFrame;
		}

		set
		{
			if (SelectedAnimationTemplate != null
				&& SelectedAnimationTemplate.Data.FrameData.Count > 0
				&& value > -1)
			{
				_selectedAnimationFrame = Mathf.Clamp(value, 0, SelectedAnimationTemplate.Data.FrameData.Count - 1);

				var data = SelectedAnimationFrameTemplate.Data;
				DrawPanelManager.PreviewFrame(data.FrameID, DrawPanelManager.Instance.DrawArea.transform);

				//Also select the frame from the frame list
				SelectFrameAndScroll(data.FrameID);
			}
			else
			{
				DrawPanelManager.PreviewFrame(SelectedFrame, DrawPanelManager.Instance.DrawArea.transform);
				_selectedAnimationFrame = -1;
			}
		}

	}


	int _selectedAnimationFrame = -1;

	public AnimationFrameTemplate SelectedAnimationFrameTemplate
	{
		get
		{
			if (_selectedAnimationFrame >= animationFrameTemplateList.Count) return null;
			return animationFrameTemplateList[_selectedAnimationFrame];
		}
	}

	private AnimationFrameTemplate RefreshAnimationFrameList(int selected = 0)
	{
		AnimationFrameTemplate lastItem = null;
		animationFrameTemplateList.Clear();
		foreach (Transform child in AnimationFrameContainer.transform)
		{
			Destroy(child.gameObject);
		}

		if (SelectedAnimation != null)
		{
			for (var index = 0; index < SelectedAnimationTemplate.Data.FrameData.Count; index++)
			{
				var frameItem = Instantiate(AnimationFrameTemplate);
				frameItem.Initialize(SelectedAnimation, index);
				frameItem.transform.SetParent(AnimationFrameContainer.transform);
				frameItem.gameObject.SetActive(true);
				animationFrameTemplateList.Add(frameItem);
				lastItem = frameItem;
			}

			SelectedAnimationFrame = selected;
		}
		else
		{
			SelectedAnimationFrame = 0;
		}
		return lastItem;
	}

	internal void DeleteColRectFromAllFrames(string currentColRect)
	{
		Undo.Instance.PushUndo();
		foreach (var frame in Manager.Instance.Project.Frames.Values)
		{
			if (frame.ColRects.ContainsKey(currentColRect))
			{
				frame.ColRects.Remove(currentColRect);
			}
		}
		RefreshFrameAndAnimationList();
	}

	internal void ApplyColrectToAllFrames(string currentFrame, string currentColRect)
	{
		Undo.Instance.PushUndo();
		var colRect = Manager.Instance.Project.Frames[currentFrame].ColRects[currentColRect];
		foreach (var frame in Manager.Instance.Project.Frames.Values)
		{
			frame.ColRects[currentColRect] = colRect.JSONClone();
		}
	}

	internal void ForceRefresh()
	{
		ForceRefresh(SelectedFrame, SelectedColRect, SelectedAnimation, SelectedAnimationFrame, SelectedSprites.ToList());
	}

	public HashSet<int> SelectedSprites = new HashSet<int>();
	int lastSelectedSprite = -1;

	public void ClearSelectedSprites()
	{
		lastSelectedSprite = -1;
		SelectedSprites.Clear();
	}

	public void SelectSprite(int index, bool allowUnselect = false)
	{
		SelectedColRect = null;

		//Are we doing CTRL to add sprites?
		if (Input.GetKey(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
		{
			if (allowUnselect && SelectedSprites.Contains(index))
			{
				SelectedSprites.Remove(index);
				return;
			}
		}
		else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
		{
			if (lastSelectedSprite > -1 && lastSelectedSprite != index)
			{
				//Fill in ALL gaps between last selected sprite and this selected sprite
				var dir = 1;
				if (index < lastSelectedSprite) dir = -1;

				for (var i = lastSelectedSprite; i != index; i += dir)
				{
					SelectedSprites.Add(i);
				}
			}
		}
		else
		{

			if (allowUnselect && SelectedSprites.Contains(index))
			{
				ClearSelectedSprites();
				return;
			}

			//Clearing all sprites
			ClearSelectedSprites();
		}

		lastSelectedSprite = index;
		SelectedSprites.Add(index);
	}

	public void UnselectSprite(int index)
	{
		SelectedSprites.Remove(index);
	}

	public string SelectedColRect
	{
		get
		{
			return _selectedColRect;
		}

		set
		{
			if (value != null)
			{
				ClearSelectedSprites();
			}
			_selectedColRect = value;
		}

	}
	string _selectedColRect;
	private static float? _nextFramesScrollTime;
	private static float? _nextAnimationsScrollTime;
	private AnimationFrameTemplate newAnimationFrame;
	private PointerEventData draggedAnimationFrameData;
	private const float DOWNKEYDELAY = 0.2f;

	public ColRectItemTemplate SelectedColRectItem
	{
		get
		{
			if (_selectedColRect == null || !colRectItemList.ContainsKey(_selectedColRect)) return null;
			return colRectItemList[_selectedColRect];
		}
	}

	public int SelectedAnimationIndex
	{
		get
		{
			if (SelectedAnimation == null || Manager.Instance.Project.Animations.Count == 0) return 0;
			return Manager.Instance.Project.Animations.Keys.OrderBy(p => p).ToList().IndexOf(SelectedAnimation);
		}
		set
		{
			if (Manager.Instance.Project.Animations.Count == 0) return;
			value = Mathf.Clamp(value, 0, Manager.Instance.Project.Animations.Count - 1);
			SelectedAnimation = Manager.Instance.Project.Animations.Keys.OrderBy(p => p).ToList()[value];
		}
	}

	public int SelectedFrameIndex
	{
		get
		{
			if (SelectedFrame == null || Manager.Instance.Project.Frames.Count == 0) return 0;
			return Manager.Instance.Project.Frames.Keys.OrderBy(p => p).ToList().IndexOf(SelectedFrame);
		}
		set
		{
			if (Manager.Instance.Project.Frames.Count == 0) return;
			value = Mathf.Clamp(value, 0, Manager.Instance.Project.Frames.Count - 1);
			SelectedFrame = Manager.Instance.Project.Frames.Keys.OrderBy(p => p).ToList()[value];
		}
	}

	internal void RefreshFrameAndAnimationList(string selectedAnimation = null)
	{
		//Delete objects first
		frameList.Clear();
		foreach (Transform child in FrameContainer.transform)
		{
			Destroy(child.gameObject);
		}
		//Animations
		SelectedAnimation = null;
		animationTemplateList.Clear();
		animationFrameTemplateList.Clear();
		foreach (Transform child in AnimationContainer.transform)
		{
			Destroy(child.gameObject);
		}
		foreach (Transform child in AnimationFrameContainer.transform)
		{
			Destroy(child.gameObject);
		}

		//Sprites and colrects
		SelectedColRect = null;
		colRectItemList.Clear();
		foreach (Transform child in ColRectListContainer.transform)
		{
			Destroy(child.gameObject);
		}

		ClearSelectedSprites();
		spriteItemList.Clear();
		foreach (Transform child in SpriteListContainer.transform)
		{
			Destroy(child.gameObject);
		}

		//Frames
		var oldFrame = SelectedFrame;
		SelectedFrame = null;
		foreach (var frame in Manager.Instance.Project.Frames.OrderBy(p => p.Key))
		{
			var newFrame = Instantiate(FrameTemplate);
			newFrame.Initialize(frame.Key);
			newFrame.transform.SetParent(FrameContainer.transform);
			newFrame.gameObject.SetActive(true);
			frameList[frame.Key] = newFrame;
		}

		foreach (var key in Manager.Instance.Project.Animations.Keys.OrderBy(p => p))
		{
			var animationItem = Instantiate(AnimationTemplate);
			animationItem.Initialize(key);
			animationItem.transform.SetParent(AnimationContainer.transform);
			animationItem.gameObject.SetActive(true);
			animationTemplateList.Add(key, animationItem);
		}

		SelectedAnimation = selectedAnimation;
		SelectedFrame = oldFrame;
	}

	public void RefreshColRectList()
	{
		colRectItemList.Clear();
		foreach (Transform child in ColRectListContainer.transform)
		{
			Destroy(child.gameObject);
		}

		if (SelectedFrame == null) return;

		foreach (var key in SelectedFrameTemplate.Data.ColRects.Keys)
		{
			var colRectItem = Instantiate(colRectItemTemplate);
			colRectItem.transform.SetParent(ColRectListContainer.transform);
			colRectItem.Initialize(SelectedFrame, key);
			colRectItem.gameObject.SetActive(true);
			colRectItemList.Add(key, colRectItem);
		}

		DrawPanelManager.PreviewFrame(SelectedFrame, DrawPanelManager.Instance.DrawArea.transform);
	}

	public void RefreshSpriteList(int? spriteIndex = null)
	{
		spriteItemList.Clear();
		var selectedSprites = new HashSet<int>(SelectedSprites);
		ClearSelectedSprites();

		foreach (var animationFrame in animationFrameTemplateList)
		{
			animationFrame.RefreshImage();
		}

		foreach (Transform child in SpriteListContainer.transform)
		{
			Destroy(child.gameObject);
		}
		DrawPanelManager.PreviewFrame(SelectedFrame, DrawPanelManager.Instance.DrawArea.transform);

		if (SelectedFrameTemplate != null)
		{
			var index = 0;

			//Clean up / sanity check
			SelectedFrameTemplate.Data.Sprites.RemoveAll(p => string.IsNullOrEmpty(p.FramePath));

			foreach (var sprite in SelectedFrameTemplate.Data.Sprites)
			{
				if (string.IsNullOrEmpty(sprite.FramePath))
				{
					throw new Exception("Missing sprite framepath");
				}

				var spriteItem = Instantiate(SpriteItemTemplate);
				spriteItem.Initialize(SelectedFrame, index);
				spriteItem.transform.SetParent(SpriteListContainer.transform);
				spriteItem.transform.SetAsFirstSibling();
				spriteItem.gameObject.SetActive(true);
				spriteItemList.Add(spriteItem);
				index++;
			}
		}

		if (spriteIndex != null)
		{
			SelectSprite(spriteIndex.Value);
		}
		else
		{
			SelectedSprites = selectedSprites;
		}
	}

	//Used for when we want to force refresh the display
	public void ForceRefresh(string selectedFrameID, string selectedColRectID, string selectedAnimation, int selectedAnimationFrame, List<int> selectedSprites)
	{
		DrawPanelManager.PreviewFrame(null, DrawPanelManager.Instance.DrawArea.transform);
		RefreshFrameAndAnimationList();
		RefreshSpriteList();

		SelectedAnimation = selectedAnimation;
		if (selectedAnimation != null)
		{
			SelectedAnimationFrame = selectedAnimationFrame;
		}
		SelectedFrame = selectedFrameID;
		SelectedColRect = selectedColRectID;
		SelectedSprites = new HashSet<int>(selectedSprites);

		Manager.Instance.CheckPartsMissing();


	}


	public void MoveSpritePosition(int x, int y, SmallMovementModes smallMovementMode)
	{
		if (smallMovementMode != SmallMovementModes.off && smallMovementMode != SmallMovementMode)
		{
			Undo.Instance.PushUndo();
			SmallMovementMode = smallMovementMode;
		}

		var frame = Manager.Instance.Project.Frames[SelectedFrame];
		for (var i = 0; i < frame.Sprites.Count; i++)
		{
			if (SelectedSprites.Contains(i) || SelectedSprites.Count == 0)
			{
				var sprite = frame.Sprites[i];
				sprite.HandleX += x;
				sprite.HandleY += y;
			}
		}

		downKeyTime = DOWNKEYDELAY;
	}

	public void MoveColRectPosition(int x, int y, SmallMovementModes smallMovementMode)
	{
		if (smallMovementMode != SmallMovementModes.off && smallMovementMode != SmallMovementMode)
		{
			Undo.Instance.PushUndo();
			SmallMovementMode = smallMovementMode;
		}

		SelectedColRectItem.Data.X += x;
		SelectedColRectItem.Data.Y += y;
		downKeyTime = DOWNKEYDELAY;
	}



	public void Update()
	{
		var newText = "";
		if (SelectedColRectItem != null)
		{
			var data = SelectedColRectItem.Data;
			newText = string.Format("X:{0} Y:{1} W:{2} H:{3}", data.X, data.Y, data.Width, data.Height);
		}
		else if (SelectedSprites.Count > 0)
		{
			var data = Manager.Instance.Project.Frames[SelectedFrame].Sprites[SelectedSprites.First()];
			newText = string.Format("X:{0} Y:{1}", data.HandleX, data.HandleY);
		}

		DrawPanelManager.Instance.InfoText.text = newText;

		//Dragging a new part?
		if (newPart)
		{
			newPart.transform.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);

			newPart.transform.localScale = new Vector3(
				DrawPanelManager.Instance.zoomLevel * (DrawPanelManager.Instance.mirrored ? -1 : 1),
				DrawPanelManager.Instance.zoomLevel,
				1);


			if (Input.GetMouseButton(0) == false)
			{
				if (DrawPanelManager.Instance.DrawPanel.ContainsPoint(newPart.transform.position))
				{
					newPart.transform.SetParent(DrawPanelManager.Instance.DrawArea.transform, true);
					newPart.transform.localScale = Vector3.one;

					var xpos = Mathf.RoundToInt(newPart.transform.localPosition.x);
					var ypos = Mathf.RoundToInt(newPart.transform.localPosition.y);

					newPart.transform.localPosition = new Vector3(xpos, ypos, 0);
					newPart.Data.HandleX = xpos;
					newPart.Data.HandleY = -ypos;
					RefreshSpriteList(SelectedFrameTemplate.Data.Sprites.Count - 1);
				}
				else
				{
					Undo.Instance.PopUndo();
					Destroy(newPart.gameObject);
				}
				newPart = null;
				DraggedPart = null;
			}
			return;
		}

		if (SelectedAnimation != null
			&& SelectedAnimationTemplate.Data.FrameData.Count > 0
			&& PlayMode != PlayModes.None)
		{
			selectedFrameTime -= Time.deltaTime * 50;
			if (selectedFrameTime <= 0)
			{
				var index = DrawAnimationFrame + 1;
				if (index >= animationFrameTemplateList.Count)
				{
					if (PlayMode == PlayModes.Once)
					{
						PlayMode = PlayModes.None;
					}
					else
					{
						DrawAnimationFrame = 0;
					}
				}
				else
				{
					DrawAnimationFrame = index;
				}

			}
		}


		//Only do input commands if overlay is closed
		if (Manager.Instance.OverlayManager.gameObject.activeSelf == false)
		{
			var controlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

			if (controlDown)
			{

				if (Input.GetKeyDown(KeyCode.Y)
					|| (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))))
				{
					SmallMovementMode = SmallMovementModes.off;
					var redoObject = Undo.Instance.PopRedo();
					if (redoObject != null)
					{
						ForceRefresh(redoObject.SelectedFrame, redoObject.SelectedColRect, redoObject.SelectedAnimation, redoObject.SelectedAnimationFrame, redoObject.SelectedSprites);
					}
				}
				else if (Input.GetKeyDown(KeyCode.Z))
				{
					SmallMovementMode = SmallMovementModes.off;
					var undoObject = Undo.Instance.PopUndo();
					if (undoObject != null)
					{
						ForceRefresh(undoObject.SelectedFrame, undoObject.SelectedColRect, undoObject.SelectedAnimation, undoObject.SelectedAnimationFrame, undoObject.SelectedSprites);
					}
				}
				else if (Input.GetKeyDown(KeyCode.X))
				{
					CopyToClipboard();
					Undo.Instance.PushUndo();

					if (ClipboardColRect != null)
					{
						DeleteColRect(SelectedFrame, SelectedColRect);
					}
					else
					{
						DeleteSprite(SelectedFrame, SelectedSprites.ToList());
					}
					SelectedFrame = SelectedFrame;
					SelectedSprites.Clear();
					SelectedColRect = null;
				}
				else if (Input.GetKeyDown(KeyCode.C))
				{
					CopyToClipboard();
				}
				else if (Input.GetKeyDown(KeyCode.V))
				{
					if (ClipboardColRect != null)
					{
						Undo.Instance.PushUndo();
						var frameColRects = Manager.Instance.Project.Frames[SelectedFrame].ColRects;
						var name = ClipboardColRect.Name;
						if (frameColRects.ContainsKey(name))
						{
							name += " CLONE";
						}
						frameColRects.Add(name, ClipboardColRect.ColRect.JSONClone());
						SelectedFrame = SelectedFrame;
						SelectedColRect = name;
					}
					else if (Clipboard.Count > 0)
					{
						Undo.Instance.PushUndo();
						var frameSprites = Manager.Instance.Project.Frames[SelectedFrame].Sprites;
						foreach (var sprite in Clipboard)
						{
							frameSprites.Add(sprite.JSONClone());
						}
						SelectedSprites.Clear();
						SelectedFrame = SelectedFrame;
						for (var i = frameSprites.Count - Clipboard.Count; i < frameSprites.Count; i++)
						{
							SelectedSprites.Add(i);
						}
					}
				}
			}
			else
			{
				if (Input.GetKeyDown(KeyCode.UpArrow))
				{
					switch (CycleMode)
					{
						case CycleModes.Frame:
							SelectedFrameIndex -= 1;
							break;
						case CycleModes.Animation:
							SelectedAnimationIndex -= 1;
							break;
					}
				}
				else if (Input.GetKeyDown(KeyCode.DownArrow))
				{
					switch (CycleMode)
					{
						case CycleModes.Frame:
							SelectedFrameIndex += 1;
							break;
						case CycleModes.Animation:
							SelectedAnimationIndex += 1;
							break;
					}
				}
				else if ((Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Alpha1)) && SelectedAnimationFrame > 0)
				{
					SelectedAnimationFrame -= 1;
				}
				else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Alpha2))
				{
					SelectedAnimationFrame += 1;
				}

				var moveAmount = 1;
				var colRectMovement = SmallMovementModes.colrectsmall;
				var spriteMovement = SmallMovementModes.spritesmall;
				if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				{
					moveAmount = 10;
					colRectMovement = SmallMovementModes.colrectlarge;
					spriteMovement = SmallMovementModes.spritelarge;
				}

				if ((Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)) == false)
				{
					downKeyTime = 0;
				}

				if (downKeyTime > 0)
				{
					downKeyTime -= Time.deltaTime;
				}
				else
				{
					if (SelectedColRectItem)
					{
						if (Input.GetKey(KeyCode.W))
						{
							MoveColRectPosition(0, -moveAmount, colRectMovement);
						}
						else if (Input.GetKey(KeyCode.S))
						{
							MoveColRectPosition(0, moveAmount, colRectMovement);
						}
						else if (Input.GetKey(KeyCode.A))
						{
							MoveColRectPosition(-moveAmount, 0, colRectMovement);
						}
						else if (Input.GetKey(KeyCode.D))
						{
							MoveColRectPosition(moveAmount, 0, colRectMovement);
						}
					}
					else
					{
						if (Input.GetKey(KeyCode.W))
						{
							MoveSpritePosition(0, -moveAmount, spriteMovement);
						}
						else if (Input.GetKey(KeyCode.S))
						{
							MoveSpritePosition(0, moveAmount, spriteMovement);
						}
						else if (Input.GetKey(KeyCode.A))
						{
							MoveSpritePosition(-moveAmount, 0, spriteMovement);
						}
						else if (Input.GetKey(KeyCode.D))
						{
							MoveSpritePosition(moveAmount, 0, spriteMovement);
						}
					}
				}

				//If we click somewhere, we're no longer making small adjustments
				if (Input.GetMouseButtonDown(0))
				{
					SmallMovementMode = SmallMovementModes.off;
				}

				//Delete
				if (Input.GetKeyDown(KeyCode.Delete) && SelectedFrame != null)
				{
					Undo.Instance.PushUndo();
					foreach (var index in SelectedSprites.OrderByDescending(p => p))
					{
						Manager.Instance.Project.Frames[SelectedFrame].Sprites.RemoveAt(index);
					}
					ClearSelectedSprites();
					SelectedFrame = SelectedFrame;
				}

				//XFlip
				if (Input.GetKeyDown(KeyCode.X))
				{
					SpriteToggleXFlip(SelectedFrame, SelectedSprites.ToList());
				}
				//White
				if (Input.GetKeyDown(KeyCode.C))
				{
					SpriteToggleWhite(SelectedFrame, SelectedSprites.ToList());
				}

			}
		}

		if (newAnimationFrame)
		{
			newAnimationFrame.transform.position = Input.mousePosition;
			if (Input.GetMouseButton(0) == false)
			{
				newAnimationFrame.Drop();
				newAnimationFrame = null;
			}
		}
	}

	private void CopyToClipboard()
	{
		Clipboard.Clear();
		ClipboardColRect = null;

		if (SelectedColRectItem != null)
		{
			ClipboardColRect = new ClipboardColRectClass()
			{
				Name = SelectedColRect,
				ColRect = Manager.Instance.Project.Frames[SelectedFrame].ColRects[SelectedColRect].JSONClone()
			};
		}
		else
		{
			foreach (var s in SelectedSprites)
			{
				var sprite = Manager.Instance.Project.Frames[SelectedFrame].Sprites[s];
				Clipboard.Add(sprite.JSONClone());
			}
		}


	}

	public void LateUpdate()
	{
		if (_nextFramesScrollTime.HasValue)
		{
			Canvas.ForceUpdateCanvases();
			FrameScrollBar.value = _nextFramesScrollTime.Value;
			_nextFramesScrollTime = null;
		}
		if (_nextAnimationsScrollTime.HasValue)
		{
			Canvas.ForceUpdateCanvases();
			AnimationsScrollBar.value = _nextAnimationsScrollTime.Value;
			_nextAnimationsScrollTime = null;
		}
	}

	public void OnEnable()
	{
		//FOrce disable if project is not laoded
		if (Manager.Instance.ProjectIsLoaded == false)
		{
			gameObject.SetActive(false);
			return;
		}

		DrawPanelManager.PreviewFrame(null, DrawPanelManager.Instance.DrawArea.transform);



		RefreshPartsList();
		RefreshFrameAndAnimationList();
	}

	private void RefreshPartsList()
	{
		//Clear the part container before reinitialising
		foreach (Transform child in PartContainer.transform)
		{
			Destroy(child.gameObject);
		}

		//Parts
		var files = Directory.GetFiles(SaveData.ProjectPath, "*.png", SearchOption.AllDirectories);
		foreach (var file in files)
		{
			var newPart = Instantiate(PartTemplate);
			newPart.Initialize(Functions.GetRelativePath(SaveData.ProjectPath, file));
			newPart.transform.SetParent(PartContainer.transform);
			newPart.gameObject.SetActive(true);
		}
	}

	public void OnApplicationPause(bool pause)
	{
		if (!pause)
		{
			RefreshPartsList();
		}
	}

	internal void InsertAnimationFrameAt(AnimationFrameData data, int index)
	{
		SelectedAnimationTemplate.Data.FrameData.Remove(data);
		SelectedAnimationTemplate.Data.FrameData.Insert(index, data);

		//Reset animation list
		SelectedAnimation = SelectedAnimation;
		SelectedAnimationFrame = index;
	}

	public void Command_NewFrame()
	{
		Manager.Instance.OverlayManager.AskTextQuestion("New frame", "Enter frame name", "", (name) =>
		 {
			 if (string.IsNullOrWhiteSpace(name)) return;

			 Undo.Instance.PushUndo();
			 name = name.Trim().ToLower(); //normalize
			 var originalName = name;
			 var frame = new Frame();
			 var offset = 2;

			 //Make sure name is unique
			 while (Manager.Instance.Project.Frames.ContainsKey(name))
			 {
				 name = originalName + " " + offset;
				 offset++;
			 }

			 Manager.Instance.Project.Frames[name] = frame;
			 RefreshFrameAndAnimationList();
			 SelectFrameAndScroll(name);
		 });
	}

	public void RenameFrame(string oldName, string newName)
	{
		Undo.Instance.PushUndo();

		newName = newName.Trim().ToLower();
		if (string.IsNullOrEmpty(newName) || Manager.Instance.Project.Frames.ContainsKey(newName))
		{
			//Frame with name already exists. Do nothing
			return;
		}

		//Rename every single frame in every single animation
		foreach (var anim in Manager.Instance.Project.Animations.Values)
		{
			foreach (var frame in anim.FrameData)
			{
				if (frame.FrameID == oldName)
				{
					frame.FrameID = newName;
				}
			}
		}

		var oldAnim = SelectedAnimation;
		var data = Manager.Instance.Project.Frames[oldName];
		Manager.Instance.Project.Frames.Remove(oldName);
		Manager.Instance.Project.Frames[newName] = data;
		SelectedFrame = null;
		RefreshFrameAndAnimationList();
		SelectedAnimation = oldAnim;
		SelectFrameAndScroll(newName);

	}


	public void DeleteFrame(string frame)
	{
		Undo.Instance.PushUndo();

		Manager.Instance.Project.Frames.Remove(frame);
		foreach (var animation in Manager.Instance.Project.Animations.Values)
		{
			animation.FrameData.RemoveAll(p => p.FrameID == frame);
		}

		ForceRefresh(SelectedFrame, SelectedColRect, SelectedAnimation, SelectedAnimationFrame, SelectedSprites.ToList());
	}

	public void CloneFrame(string oldName, string newName, string selectedAnim, int selectedAnimIndex)
	{
		Undo.Instance.PushUndo();

		var newData = Manager.Instance.Project.Frames[oldName].JSONClone();
		Manager.Instance.Project.Frames[newName] = newData;
		SelectedAnimation = null;
		RefreshFrameAndAnimationList();
		SelectFrameAndScroll(newName);

		//Add the frame to timeline?
		if (selectedAnim != null)
		{
			Manager.Instance.Project.Animations[selectedAnim].FrameData.Insert(selectedAnimIndex + 1, new AnimationFrameData() { Duration = 5, FrameID = newName });
			SelectedAnimation = selectedAnim;
			SelectedAnimationFrame = selectedAnimIndex + 1;
		}
	}

	public void SpriteToggleXFlip(string frameIndex, List<int> currentSprites)
	{
		if (currentSprites.Count == 0) return;

		Undo.Instance.PushUndo();

		var frame = Manager.Instance.Project.Frames[frameIndex];

		//First pass, set the xflip
		foreach (var index in currentSprites)
		{
			var sprite = frame.Sprites[index];
			sprite.XFlip = !sprite.XFlip;
			sprite.HandleX *= -1;
			sprite.HandleX -= sprite.GetWidth();
		}



	}

	/*
	public void SpriteToggleNoFlip(string frame, int index)
	{
		Undo.Instance.PushUndo();
		var sprite = Manager.Instance.Project.Frames[frame].Sprites[index];
		sprite.NoFlip = !sprite.NoFlip;
	}*/

	public void SpriteToggleWhite(string frame, List<int> currentSprites)
	{
		Undo.Instance.PushUndo();
		foreach (var index in currentSprites)
		{
			var sprite = Manager.Instance.Project.Frames[frame].Sprites[index];
			if (sprite.RenderMode == RetroAnimator.Entities.Sprite.RenderModes.Normal)
			{
				sprite.RenderMode = RetroAnimator.Entities.Sprite.RenderModes.White;
			}
			else
			{
				sprite.RenderMode = RetroAnimator.Entities.Sprite.RenderModes.Normal;
			}
		}
		RefreshSpriteList();
	}

	public void DeleteSprite(string currentFrame, List<int> currentSprites)
	{
		Undo.Instance.PushUndo();

		//Delete the sprites in reverse order
		currentSprites.Sort();
		for (var i = currentSprites.Count - 1; i >= 0; i--)
		{
			Manager.Instance.Project.Frames[currentFrame].Sprites.RemoveAt(currentSprites[i]);
			SelectedSprites.Remove(currentSprites[i]);
		}

		RefreshSpriteList();
	}


	public void Command_NewColRect()
	{
		if (SelectedFrame == null) return;

		Manager.Instance.OverlayManager.AskTextQuestion("New colrect", "Enter colrect name", "", (name) =>
		 {
			 if (string.IsNullOrWhiteSpace(name)) return;

			 Undo.Instance.PushUndo();
			 name = name.Trim().ToLower(); //normalize
			 var originalName = name;
			 var colrect = new ColRect()
			 {
				 X = 0,
				 Y = 0,
				 Width = 16,
				 Height = 16,
			 };

			 var offset = 2;

			 //Make sure name is unique
			 while (SelectedFrameTemplate.Data.ColRects.ContainsKey(name))
			 {
				 name = originalName + " " + offset;
				 offset++;
			 }

			 SelectedFrameTemplate.Data.ColRects[name] = colrect;
			 RefreshColRectList();
			 SelectedColRect = name;
			 SelectedAnimationFrame = SelectedAnimationFrame; //Force a refresh of the animation frame
		 });
	}

	public void RenameColRect(string frame, string oldname, string newname)
	{
		newname = newname.ToLower();
		if (Manager.Instance.Project.Frames[frame].ColRects.ContainsKey(newname))
		{
			//ColRect with name already exists. Do nothing
			return;
		}

		Undo.Instance.PushUndo();
		var data = Manager.Instance.Project.Frames[frame].ColRects[oldname];
		SelectedFrameTemplate.Data.ColRects.Remove(oldname);
		SelectedFrameTemplate.Data.ColRects[newname] = data;
		RefreshColRectList();
		SelectedColRect = newname;
	}

	public void ColRectSetValue(string frame, string colrect, int value)
	{
		Undo.Instance.PushUndo();
		Manager.Instance.Project.Frames[frame].ColRects[colrect].Value = value;
		RefreshColRectList();
	}

	public void DeleteColRect(string frame, string id)
	{
		Undo.Instance.PushUndo();
		Manager.Instance.Project.Frames[frame].ColRects.Remove(id);
		RefreshColRectList();
		SelectedAnimationFrame = SelectedAnimationFrame; //Force a refresh of the animation frame

	}

	public void Command_NewAnimation()
	{
		Manager.Instance.OverlayManager.AskTextQuestion("New animation", "New animation name", "", (animationName) =>
		 {
			 if (!string.IsNullOrEmpty(animationName))
			 {
				 animationName = animationName.ToLower().Trim();

				 if (!Manager.Instance.Project.Animations.ContainsKey(animationName))
				 {
					 Undo.Instance.PushUndo();
					 var data = new RetroAnimator.Entities.Animation();
					 Manager.Instance.Project.Animations[animationName] = data;
					 RefreshFrameAndAnimationList();
					 RefreshAnimationFrameList();
					 SelectAnimationAndScroll(animationName);
				 }

			 }
		 });
	}

	public void DeleteAnimation(string animation)
	{
		Undo.Instance.PushUndo();
		Manager.Instance.Project.Animations.Remove(animation);
		ForceRefresh(SelectedFrame, SelectedColRect, null, 0, new List<int>());
	}

	public void DeleteAnimationFrame(string animation, int frame)
	{
		Undo.Instance.PushUndo();
		Manager.Instance.Project.Animations[animation].FrameData.RemoveAt(frame);
		ForceRefresh(SelectedFrame, SelectedColRect, SelectedAnimation, -1, SelectedSprites.ToList());
	}

	internal void DuplicateAnimationFrame(string currentAnimation, int currentAnimationIndex)
	{
		Undo.Instance.PushUndo();
		var newFrame = Manager.Instance.Project.Animations[currentAnimation].FrameData[currentAnimationIndex].JSONClone();
		Manager.Instance.Project.Animations[currentAnimation].FrameData.Insert(currentAnimationIndex + 1, newFrame);
		ForceRefresh(SelectedFrame, SelectedColRect, SelectedAnimation, currentAnimationIndex + 1, SelectedSprites.ToList());
	}

	public void MaximiseTimeline(bool value)
	{
		if (value)
		{
			PartPanelRT.offsetMin = new Vector2(PartPanelRT.offsetMin.x, 150);
			FramePanelRT.offsetMin = new Vector2(FramePanelRT.offsetMin.x, 150);
			ColRectPanelRT.anchoredPosition = new Vector2(ColRectPanelRT.anchoredPosition.x, 150);
			SpriteListPanelRT.offsetMin = new Vector2(SpriteListPanelRT.offsetMin.x, 200 + 150);
			AnimationPanelRT.offsetMin = new Vector2(AnimationPanelRT.offsetMin.x, 150);

			AnimationFramePanelRT.offsetMin = new Vector2(0, AnimationFramePanelRT.offsetMin.y);
			AnimationFramePanelRT.offsetMax = new Vector2(0, AnimationFramePanelRT.offsetMax.y);
		}
		else
		{
			PartPanelRT.offsetMin = new Vector2(PartPanelRT.offsetMin.x, 0);
			FramePanelRT.offsetMin = new Vector2(FramePanelRT.offsetMin.x, 0);
			ColRectPanelRT.anchoredPosition = new Vector2(ColRectPanelRT.anchoredPosition.x, 0);
			SpriteListPanelRT.offsetMin = new Vector2(SpriteListPanelRT.offsetMin.x, 200);
			AnimationPanelRT.offsetMin = new Vector2(AnimationPanelRT.offsetMin.x, 0);

			AnimationFramePanelRT.offsetMin = new Vector2(170, AnimationFramePanelRT.offsetMin.y);
			AnimationFramePanelRT.offsetMax = new Vector2(-510, AnimationFramePanelRT.offsetMax.y);
		}
	}

	internal void OnDragPart(PartTemplate partTemplate)
	{
		if (newPart || SelectedFrame == null) return;
		Undo.Instance.PushUndo();

		DraggedPart = partTemplate.FilePath;
		var frame = Manager.Instance.Project.Frames[SelectedFrame];
		frame.Sprites.Add(new RetroAnimator.Entities.Sprite() { FramePath = partTemplate.FilePath });
		newPart = Instantiate(DrawPanelManager.Instance.SpriteTemplate);
		newPart.Initialize(SelectedFrame, frame.Sprites.Count - 1, true);

		newPart.OverrideScale = true;
		newPart.transform.SetParent(DrawPanelManager.Instance.transform);
		newPart.gameObject.SetActive(true);
	}

	internal void OnDragFrame(FrameTemplate frameTemplate, UnityEngine.EventSystems.PointerEventData eventData)
	{
		if (SelectedAnimation == null) return;
		Undo.Instance.PushUndo();

		var anim = Manager.Instance.Project.Animations[SelectedAnimation];
		anim.FrameData.Add(new AnimationFrameData() { FrameID = frameTemplate.ID, Duration = 5 });
		newAnimationFrame = RefreshAnimationFrameList(anim.FrameData.Count - 1);
		draggedAnimationFrameData = eventData;

		Canvas.ForceUpdateCanvases();
		newAnimationFrame.transform.parent = transform;
		newAnimationFrame.IsDragging = true;

	}

}
