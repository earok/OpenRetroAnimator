using RetroAnimator.Entities;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DrawPanelManager : Singleton<DrawPanelManager>
{
	[Header("Game Objects")]
	public RectTransform DrawPanel;
	public Transform ScaleRoot;
	public TextMeshProUGUI ZoomLevelText;
	public SpriteTemplate SpriteTemplate;
	public Transform DrawArea;
	public ColRectTemplate ColRectTemplate;
	public Toggle PlayToggle;
	public RawImage BackgroundImage;
	public TextMeshProUGUI InfoText;

	internal int zoomLevel = 4;
	private float scrollDelta;
	internal bool mirrored;
	internal bool previewColRects;

	internal static void PreviewFrame(string frame, Transform target, bool isDraggable = true)
	{
		foreach (Transform child in target)
		{
			Destroy(child.gameObject);
		}

		if (frame != null && Manager.Instance.Project.Frames.ContainsKey(frame))
		{
			var frameData = Manager.Instance.Project.Frames[frame];

			int index = 0;
			foreach (var sprite in frameData.Sprites)
			{
				if (string.IsNullOrEmpty(sprite.FramePath)) continue;

				var newDrawTemplate = Instantiate(Instance.SpriteTemplate);
				newDrawTemplate.transform.SetParent(target);
				newDrawTemplate.Initialize(frame, index, isDraggable);
				newDrawTemplate.gameObject.SetActive(true);
				index++;
			}

			//Colrects
			if (isDraggable)
			{
				foreach (var colRectKey in frameData.ColRects.Keys)
				{
					var colRect = Instantiate(DrawPanelManager.Instance.ColRectTemplate);
					colRect.transform.SetParent(DrawPanelManager.Instance.DrawArea);
					colRect.transform.localScale = Vector3.one;
					colRect.transform.localPosition = Vector3.zero;
					colRect.Initialise(frame, colRectKey);
					colRect.gameObject.SetActive(true);
				}
			}
		}
	}

	public void OnEnable()
	{
		RedrawBackgroundImage();
	}

	internal void RedrawBackgroundImage()
	{
		BackgroundImage.color = Color.white;

		var texture = new Texture2D(1024, 1024)
		{
			filterMode = FilterMode.Point
		};

		var pixels = new Color32[1024 * 1024];
		for (var x = 0; x < 1024; x++)
		{
			var xoffset = x - 512;
			for (var y = 0; y < 1024; y++)
			{
				var yoffset = y - 511;

				var color = SaveData.BGColor.Value;
				if (SaveData.OriginLinesEnabled.Value && (xoffset == 0 || yoffset == 0))
				{
					color = SaveData.OriginColor.Value;
				}
				else if (SaveData.GridLinesEnabled.Value && SaveData.GridSize.Value > 0 && (xoffset % SaveData.GridSize.Value == 0 || yoffset % SaveData.GridSize.Value == 0))
				{
					color = SaveData.GridColor.Value;
				}

				pixels[x + y * 1024] = color;
			}
		}

		texture.SetPixels32(pixels);
		texture.Apply();
		BackgroundImage.texture = texture;
	}

	public void SetMirrored(bool value)
	{
		mirrored = value;
		DrawArea.localScale = mirrored ? new Vector3(-1, 1, 1) : Vector3.one;
	}

	public void SetPreviewColRects(bool value)
	{
		previewColRects = value;
	}

	public void SetPlay(bool value)
	{
		FrameManager.Instance.PlayMode = value ? FrameManager.PlayModes.Loop : FrameManager.PlayModes.None;
	}

	public void PlayOnce()
	{
		FrameManager.Instance.PlayMode = FrameManager.PlayModes.Once;
	}


	public void Update()
	{
		if (Input.GetKeyUp(KeyCode.Space))
		{
			PlayToggle.isOn = !PlayToggle.isOn;
		}

		//Mouse is over draw panel
		if (DrawPanel.ContainsPoint(Input.mousePosition) && OverlayManager.Instance.gameObject.activeInHierarchy == false)
		{
			//Scroll wheel
			scrollDelta += Input.mouseScrollDelta.y;
			if (scrollDelta > 1)
			{
				scrollDelta -= 1;
				zoomLevel += 1;
			}
			if (scrollDelta < -1)
			{
				scrollDelta += 1;
				zoomLevel -= 1;
			}
		}

		zoomLevel = Mathf.Clamp(zoomLevel, 1, 16);
		ScaleRoot.localScale = new Vector3(zoomLevel, zoomLevel, 1);
		ZoomLevelText.text = zoomLevel + "x";
	}

}
