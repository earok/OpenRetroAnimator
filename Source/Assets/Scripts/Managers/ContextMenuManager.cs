using System.Collections.Generic;
using UnityEngine;
using RetroAnimator.Undo;

public class ContextMenuManager : MonoBehaviour
{
	public RectTransform AnimationFrameTemplateContextMenu;
	public RectTransform FrameTemplateContextMenu;
	public RectTransform AnimationContextMenu;
	public RectTransform SpriteContextMenu;
	public RectTransform ColRectContextMenu;
	public RectTransform BackgroundContextMenu;

	private string CurrentAnimation;
	private string CurrentFrame;
	private int CurrentAnimationIndex;
	private string CurrentColRect;
	private List<int> CurrentSprites;

	private static Vector3[] Corners = new Vector3[4];

	public static ContextMenuManager Instance
	{
		get
		{
			return Manager.Instance.ContextMenuManager;
		}
	}

	internal void SetBackgroundContextMenu()
	{
		SetMenu(BackgroundContextMenu);
	}

	public void Cmd_SetColor(int offset)
	{
		var SaveColor = SaveData.BGColor;
		switch (offset)
		{
			case 1:
				SaveColor = SaveData.OriginColor;
				break;

			case 2:
				SaveColor = SaveData.GridColor;
				break;

			case 3:
				SaveColor = SaveData.HighlightColor;
				break;

			case 4:
				SaveColor = SaveData.DragRectColor;
				break;
		}

		gameObject.SetActive(false);
		OverlayManager.Instance.AskColorQuestion(SaveColor.Value, (NewColor) =>
		{
			SaveColor.Value = NewColor;
			DrawPanelManager.Instance.RedrawBackgroundImage();
		});
	}

	public void Cmd_ToggleOriginLines()
	{
		gameObject.SetActive(false);
		SaveData.OriginLinesEnabled.Value = !SaveData.OriginLinesEnabled.Value;
		DrawPanelManager.Instance.RedrawBackgroundImage();
	}

	public void Cmd_ToggleGridLines()
	{
		gameObject.SetActive(false);
		SaveData.GridLinesEnabled.Value = !SaveData.GridLinesEnabled.Value;
		DrawPanelManager.Instance.RedrawBackgroundImage();
	}

	public void Cmd_SetGridSize()
	{
		gameObject.SetActive(false);
		OverlayManager.Instance.AskTextQuestion("Grid size", "Set grid size", SaveData.GridSize.Value.ToString(), (NewSize) =>
		  {
			  if (int.TryParse(NewSize, out int outValue))
			  {
				  SaveData.GridSize.Value = outValue;
			  }
			  DrawPanelManager.Instance.RedrawBackgroundImage();
		  });
	}

	internal void SetColRectContextMenu(string frame, string colRect)
	{
		CurrentFrame = frame;
		CurrentColRect = colRect;
		SetMenu(ColRectContextMenu);
	}

	internal void SetAnimationFrameContextMenu(string animation, int index)
	{
		CurrentAnimation = animation;
		CurrentAnimationIndex = index;
		SetMenu(AnimationFrameTemplateContextMenu);
	}

	internal void SetAnimationContextMenu(string frame)
	{
		CurrentAnimation = frame;
		SetMenu(AnimationContextMenu);
	}

	internal void SetSpriteContextMenu(string frame, List<int> sprite)
	{
		CurrentFrame = frame;
		CurrentSprites = sprite;
		SetMenu(SpriteContextMenu);
	}


	internal void SetFrameContextMenu(string frame)
	{
		CurrentFrame = frame;
		SetMenu(FrameTemplateContextMenu);
	}

	public void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			//Did we click outside the corners?
			if (Input.mousePosition.x < Corners[0].x
				|| Input.mousePosition.x > Corners[3].x
				|| Input.mousePosition.y > Corners[1].y
				|| Input.mousePosition.y < Corners[0].y)
			{
				gameObject.SetActive(false);
			}
		}
	}


	private void SetMenu(RectTransform menu)
	{
		gameObject.SetActive(true);
		menu.gameObject.SetActive(true);
		menu.gameObject.transform.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
		Canvas.ForceUpdateCanvases();

		//Does this box stretch outside the corners?
		var adjustment = Vector3.zero;
		menu.GetWorldCorners(Corners);
		if (Corners[3].x > Screen.width)
		{
			adjustment.x -= menu.sizeDelta.x;
		}
		if (Corners[3].y < 0)
		{
			adjustment.y += menu.sizeDelta.y;
		}
		menu.gameObject.transform.position += adjustment;

		//Get the corners again for detecting clicks outside of
		menu.GetWorldCorners(Corners);
	}

	public void Cmd_DeleteAnimationFrame()
	{
		FrameManager.Instance.DeleteAnimationFrame(CurrentAnimation, CurrentAnimationIndex);
		gameObject.SetActive(false);
	}

	public void Cmd_DuplicateAnimationFrame()
	{
		FrameManager.Instance.DuplicateAnimationFrame(CurrentAnimation, CurrentAnimationIndex);
		gameObject.SetActive(false);
	}

	public void Cmd_RenameFrame()
	{
		gameObject.SetActive(false);
		OverlayManager.Instance.AskTextQuestion("Change Frame Name", "Name", CurrentFrame, (result) =>
		 {
			 FrameManager.Instance.RenameFrame(CurrentFrame, result);
		 });
	}

	public void Cmd_RenameAnimation()
	{
		gameObject.SetActive(false);
		OverlayManager.Instance.AskTextQuestion("Change Animation Name", "Name", CurrentAnimation, (result) =>
		{
			FrameManager.Instance.RenameAnimation(CurrentAnimation, result, false);
		});
	}


	public void Cmd_CloneAnimation()
	{
		var suggestedName = CurrentAnimation;
		if (suggestedName.Contains("_clone"))
		{
			suggestedName = suggestedName.Substring(0, suggestedName.LastIndexOf("_clone"));
		}

		var number = 1;
		while (Manager.Instance.Project.Animations.ContainsKey(suggestedName + "_clone" + number))
		{
			number++;
		}

		gameObject.SetActive(false);
		OverlayManager.Instance.AskTextQuestion("Clone Animation", "Name", suggestedName + "_clone" + number, (result) =>
		{
			FrameManager.Instance.RenameAnimation(CurrentAnimation, result, true);
		});
	}

	public void Cmd_DeleteFrame()
	{
		FrameManager.Instance.DeleteFrame(CurrentFrame);
		gameObject.SetActive(false);
	}

	public void Cmd_CloneAnimFrame()
	{
		var thisFrame = Manager.Instance.Project.Animations[CurrentAnimation].FrameData[CurrentAnimationIndex].FrameID;
		Cmd_CloneFrame(thisFrame, CurrentAnimation, CurrentAnimationIndex);
	}

	public void Cmd_CloneFrame()
	{
		Cmd_CloneFrame(CurrentFrame, null, -1);
	}

	public void Cmd_CloneFrame(string targetFrame, string selectedAnim, int selectedAnimFrame)
	{
		var suggestedName = targetFrame;
		if (suggestedName.Contains("_clone"))
		{
			suggestedName = suggestedName.Substring(0, suggestedName.LastIndexOf("_clone"));
		}

		var number = 1;
		while (Manager.Instance.Project.Frames.ContainsKey(suggestedName + "_clone" + number))
		{
			number++;
		}

		OverlayManager.Instance.AskTextQuestion("Clone Frame", "Name", suggestedName + "_clone" + number, (result) =>
		  {
			  FrameManager.Instance.CloneFrame(targetFrame, result, selectedAnim, selectedAnimFrame);
		  });
		gameObject.SetActive(false);
	}


	public void Cmd_DeleteAnimation()
	{
		FrameManager.Instance.DeleteAnimation(CurrentAnimation);
		gameObject.SetActive(false);
	}

	public void Cmd_ChangeAnimationSpeed()
	{
		OverlayManager.Instance.AskTextQuestion("Change animation speed", "Enter the new delay per frame", "0.1", (value) =>
		   {
			   float outputValue = 0;
			   if (float.TryParse(value, out outputValue))
			   {
				   FrameManager.Instance.ChangeAnimationSpeed(CurrentAnimation, outputValue);
			   }
		   });
		gameObject.SetActive(false);
	}

	public void Cmd_AddFrameToAnimation()
	{
		FrameManager.Instance.AddFrameToAnimation(CurrentFrame);
		gameObject.SetActive(false);
	}

	public void Cmd_ColRectSetColor()
	{
		gameObject.SetActive(false);
		var data = Manager.Instance.Project.Frames[CurrentFrame].ColRects[CurrentColRect];

		OverlayManager.Instance.AskColorQuestion(data.GetColor32(), (color) =>
		 {
			 Undo.Instance.PushUndo();
			 data.SetColor32(color);
		 });
	}

	public void Cmd_ColRectSetName()
	{
		gameObject.SetActive(false);
		OverlayManager.Instance.AskTextQuestion("Change ColRect Name", "Name", CurrentColRect, (result) =>
		{
			FrameManager.Instance.RenameColRect(CurrentFrame, CurrentColRect, result);
		});
	}

	public void Cmd_ColRectSetValue()
	{
		gameObject.SetActive(false);
		OverlayManager.Instance.AskTextQuestion("Change ColRect Value", "Value", Manager.Instance.Project.Frames[CurrentFrame].ColRects[CurrentColRect].Value.ToString(), (result) =>
		 {
			 int value = 0;
			 int.TryParse(result, out value);
			 FrameManager.Instance.ColRectSetValue(CurrentFrame, CurrentColRect, value);
		 });
	}

	public void Cmd_DeleteColRect()
	{
		FrameManager.Instance.DeleteColRect(CurrentFrame, CurrentColRect);
		gameObject.SetActive(false);
	}

	public void Cmd_DeleteSprite()
	{
		FrameManager.Instance.DeleteSprite(CurrentFrame, CurrentSprites);
		gameObject.SetActive(false);
	}

	public void Cmd_SetWhiteSprite()
	{
		FrameManager.Instance.SpriteToggleWhite(CurrentFrame, CurrentSprites);
		gameObject.SetActive(false);
	}

	public void Cmd_ApplyToAllFrames()
	{
		gameObject.SetActive(false);
		OverlayManager.Instance.AskConfirmQuestion("Are you sure you want to apply colrect " + CurrentColRect + " to all frames on all animations?", () =>
		 {
			 FrameManager.Instance.ApplyColrectToAllFrames(CurrentFrame, CurrentColRect);
		 });
	}

	public void Cmd_DeleteFromAllFrames()
	{
		gameObject.SetActive(false);
		OverlayManager.Instance.AskConfirmQuestion("Are you sure you want to delete colrect " + CurrentColRect + " from all frames on all animations?", () =>
		{
			FrameManager.Instance.DeleteColRectFromAllFrames(CurrentColRect);
		});
	}


	public void Cmd_SetXFlip()
	{
		FrameManager.Instance.SpriteToggleXFlip(CurrentFrame, CurrentSprites);
		gameObject.SetActive(false);
	}


}
