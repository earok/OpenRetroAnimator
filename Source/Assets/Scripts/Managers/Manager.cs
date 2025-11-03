using Newtonsoft.Json;
using RetroAnimator.Entities;
using RetroAnimator.Undo;
using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Manager : Singleton<Manager>
{
	public OverlayManager OverlayManager;
	public ContextMenuManager ContextMenuManager;
	public GameObject ProjectBase;
	public StateSwitcher State_Frames;
	public Project Project = new Project();

	public Texture2D MissingPart;

	public Color BackgroundColor;
	public Color SelectedColor;
	public Color NotSelectedColor;

	public List<Color32> SpriterColors;

	public GameObject ErrorPanel;
	public TextMeshProUGUI ErrorPanelText;
	public HashSet<string> RecordedException = new HashSet<string>();

	internal bool ProjectIsLoaded;

	public List<ExtensionFilter> extensions = new List<ExtensionFilter>
		{
			new ExtensionFilter("JSON file", new string[] { "json" }),
			new ExtensionFilter("Spriter SCON (import)", new string[] { "scon" }),
			new ExtensionFilter("Spriter beta file (import legacy)", new string[] { "sprtr" })
		};

	public void Awake()
	{
		ProjectBase.gameObject.SetActive(false);
		Application.logMessageReceived += Application_logMessageReceived;
	}

	private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
	{
		if ((type == LogType.Exception || type == LogType.Error)
			&& !RecordedException.Contains(stackTrace)
			&& ErrorPanel.gameObject.activeSelf == false)
		{
			ErrorPanel.gameObject.SetActive(true);
			ErrorPanelText.text = stackTrace;
			RecordedException.Add(stackTrace);
		}
	}

	public static Project LoadProject(Manager manager, string path)
	{
		var extension = Path.GetExtension(path).ToLower();

		Project project;

		switch (extension)
		{
			case ".json":
				project = JsonConvert.DeserializeObject<Project>(File.ReadAllText(path));
				break;

			case ".scon":
				project = JsonConvert.DeserializeObject<Spriter.SpriterRoot>(File.ReadAllText(path)).ToProject();
				break;

			case ".sprtr":
				project = new Project();
				var lines = File.ReadAllLines(path);

				//First pass, frames only
				var index = 0;
				while (index < lines.Length)
				{
					if (lines[index].StartsWith("[frame"))
					{
						//Parse this frame
						var currentFrame = new Frame();
						var frameName = lines[index].Substring(6);
						frameName = frameName.Substring(0, frameName.Length - 1).Trim();

						index++;
						while (index < lines.Length && lines[index].Contains("="))
						{
							var splitLine = lines[index].Split('=');
							var value = "";
							if (splitLine.Length > 1)
							{
								value = splitLine[1].Trim();
							}

							if (splitLine[0].StartsWith("colrect "))
							{
								//Extract the property
								var colRectProperty = splitLine[0].Split(' ').Last().Trim().ToLower();

								//Extract the name
								var name = splitLine[0].Substring(8, splitLine[0].Length - 8 - colRectProperty.Length).Trim().ToLower();
								if (!currentFrame.ColRects.ContainsKey(name))
								{
									currentFrame.ColRects[name] = new ColRect();
								}
								var colRect = currentFrame.ColRects[name];
								var intValue = int.Parse(splitLine.Last());

								switch (colRectProperty)
								{
									case "x":
										colRect.X = intValue;
										break;
									case "y":
										colRect.Y = intValue;
										break;
									case "width":
										colRect.Width = intValue;
										break;
									case "height":
										colRect.Height = intValue;
										break;
									case "value":
										colRect.Value = intValue;
										break;
								}

							}
							else if (splitLine[0].StartsWith("colrect:"))
							{
								var name = splitLine[0].Substring(8).Trim();

								int.TryParse(splitLine[1], out int colorIndex);
								var color = manager.SpriterColors[colorIndex];

								if (!currentFrame.ColRects.ContainsKey(name))
								{
									currentFrame.ColRects[name] = new ColRect();
								}
								var colRect = currentFrame.ColRects[name];
								colRect.ColorR = color.r;
								colRect.ColorG = color.g;
								colRect.ColorB = color.b;
							}
							else if (splitLine[0].StartsWith("sprite "))
							{
								var spriteHeader = splitLine[0].Substring(7);
								var spriteNumberString = "";

								var i = 0;
								while (Char.IsDigit(spriteHeader[i]))
								{
									spriteNumberString += spriteHeader[i];
									i++;
								}

								if (spriteHeader[i] != ' ')
								{
									index += 1;
									continue; //Invalid line. Should handle with try catch etc
								}

								var spriteNumber = int.Parse(spriteNumberString);

								//Populate sprites list
								while (currentFrame.Sprites.Count <= spriteNumber)
								{
									currentFrame.Sprites.Add(new RetroAnimator.Entities.Sprite());
								}
								var currentSprite = currentFrame.Sprites[spriteNumber];

								switch (spriteHeader.Substring(spriteNumberString.Length).Trim())
								{
									case "x":
										currentSprite.HandleX = int.Parse(value);
										break;

									case "y":
										currentSprite.HandleY = int.Parse(value);
										break;

									case "image":
										currentSprite.FramePath = value;
										break;

									case "x flip":
										currentSprite.XFlip = int.Parse(value) != 0;
										break;

									case "ink":
										switch (value.Trim().ToLower())
										{
											case "add":
												currentSprite.RenderMode = RetroAnimator.Entities.Sprite.RenderModes.White;
												break;
										}
										break;
								}
							}
							index++;
						}

						project.Frames.Add(frameName, currentFrame);
					}
					else
					{
						index++;
					}
				}

				//Second pass, connect frames to animations
				index = 0;
				while (index < lines.Length)
				{
					if (lines[index].StartsWith("[") && lines[index].Contains("/anim "))
					{
						var split = lines[index].Split(' ');
						var animName = split[split.Length - 1].Substring(0, split[split.Length - 1].Length - 1).Trim().ToLower();
						var currentAnimation = new RetroAnimator.Entities.Animation();

						index++;
						while (index < lines.Length && lines[index].Contains("="))
						{
							if (lines[index].StartsWith("frame "))
							{
								int frameID = int.Parse(lines[index].Replace(" duration", "").Replace("frame ", "").Split('=')[0]);

								while (currentAnimation.FrameData.Count <= frameID)
								{
									currentAnimation.FrameData.Add(new AnimationFrameData());
								}

								if (lines[index].Contains("duration"))
								{
									currentAnimation.FrameData[frameID].Duration = int.Parse(lines[index].Split('=')[1]);
								}
								else
								{
									currentAnimation.FrameData[frameID].FrameID = lines[index].Split('=')[1];
								}
							}
							index++;
						}

						project.Animations.Add(animName, currentAnimation);
					}
					else
					{
						index++;
					}
				}
				break;

			default:
				throw new Exception("Unsupported file type - " + extension);
		}

		return project;
	}

	public void Command_ImportSingleAnimation()
	{
		if (ProjectIsLoaded == false)
		{
			OverlayManager.AskConfirmQuestion("Load the project before importing an animation", null);
			return;
		}

		var path = GetProjectPath();
		if (path == null)
		{
			return;
		}
		var importDir = Path.GetDirectoryName(path) + "\\";

		var importProject = LoadProject(this, path);
		var availableAnimations = new SortedSet<string>();

		foreach (var anim in importProject.Animations.Keys)
		{
			//We already have this animation
			if (Project.Animations.ContainsKey(anim))
			{
				continue;
			}
			availableAnimations.Add(anim);
		}

		if (availableAnimations.Count == 0)
		{
			OverlayManager.AskConfirmQuestion("This project already has all of the animations", null);
			return;
		}

		OverlayManager.AskMultichoiceQuestion("Which animations should be imported?", availableAnimations, (selected) =>
		{
			foreach (var anim in selected)
			{
				//Add the animation
				Project.Animations[anim] = importProject.Animations[anim].JSONClone();

				//Add all of the frames - assuming we don't have them already
				foreach (var frame in importProject.Animations[anim].FrameData)
				{
					//Make sure we have all of the parts!
					var thisFrame = importProject.Frames[frame.FrameID].JSONClone();
					foreach (var sprite in thisFrame.Sprites)
					{
						if (string.IsNullOrEmpty(sprite.FramePath))
						{
							continue;
						}

						var source = importDir + sprite.FramePath;
						var destination = SaveData.ProjectPath + sprite.FramePath;

						//Make sure that it actually exists in the source
						if (File.Exists(source) == false)
						{
							continue;
						}

						//Make sure it DOESN'T exist in the destination
						if (File.Exists(destination))
						{
							continue;
						}

						//Create any missing folders
						var sourcePath = Path.GetDirectoryName(sprite.FramePath);
						if (sourcePath.Length > 1)
						{
							var destPath = SaveData.ProjectPath + sourcePath;
							if (Directory.Exists(destPath) == false)
							{
								Directory.CreateDirectory(destPath);
							}
						}

						File.Copy(source, destination);
					}

					if (Project.Frames.ContainsKey(frame.FrameID))
					{
						continue;
					}

					Project.Frames[frame.FrameID] = thisFrame;
				}
			}

			ProjectLoaded();
		});

	}

	public void Command_LoadProject()
	{
		var path = GetProjectPath();
		if (path == null)
		{
			return;
		}

		Project = LoadProject(this, path);
		SaveData.ProjectPath = Path.GetDirectoryName(path);
		SaveData.ProjectName = Path.GetFileNameWithoutExtension(path);
		ProjectLoaded();
	}

	private string GetProjectPath()
	{
		var path = StandaloneFileBrowser.OpenFilePanel("Open RetroAnimator file", SaveData.ProjectPath, extensions.ToArray(), false);
		if (path.Length != 1 || string.IsNullOrWhiteSpace(path[0])) return null;
		return path[0];

	}

	public void Command_NewProject()
	{
		StandaloneFileBrowser.SaveFilePanelAsync("Create JSON file", SaveData.ProjectPath, SaveData.ProjectName, "json", (path) =>
		{
			if (string.IsNullOrWhiteSpace(path)) return;
			Project = new Project();
			SaveData.ProjectPath = Path.GetDirectoryName(path);
			SaveData.ProjectName = Path.GetFileNameWithoutExtension(path);
			File.WriteAllText(path, JsonConvert.SerializeObject(Project, Formatting.Indented));
			ProjectLoaded();
		});

	}

	public void Command_ExportProject()
	{
		StandaloneFileBrowser.OpenFolderPanelAsync("Select export folder for frames", SaveData.ProjectName, false, (paths) =>
		{
			foreach (var path in paths)
			{
				if (string.IsNullOrWhiteSpace(path)) continue;

				foreach (var keypair in Project.Frames)
				{

					var id = keypair.Key;
					var frame = keypair.Value;

					var image_MinX = int.MaxValue;
					var image_MinY = int.MaxValue;
					var image_MaxX = int.MinValue;
					var image_MaxY = int.MinValue;
					foreach (var sprite in frame.Sprites)
					{
						if (string.IsNullOrWhiteSpace(sprite.FramePath) || !File.Exists((SaveData.ProjectPath + sprite.FramePath).FixPath()))
						{
							continue;
						}
						var texture = Functions.GetTexture(sprite.FramePath, false);

						image_MinX = Mathf.Min(sprite.HandleX, image_MinX);
						image_MaxX = Mathf.Max(sprite.HandleX + texture.width, image_MaxX);

						image_MinY = Mathf.Min(sprite.HandleY, image_MinY);
						image_MaxY = Mathf.Max(sprite.HandleY + texture.height, image_MaxY);
					}

					//Clear all pixels
					var outTexture = new Texture2D(image_MaxX - image_MinX, image_MaxY - image_MinY);
					for (var x = 0; x < outTexture.width; x++)
					{
						for (var y = 0; y < outTexture.height; y++)
						{
							outTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
						}
					}

					//Final pass - actually draw the sprite in place
					foreach (var sprite in frame.Sprites)
					{
						if (string.IsNullOrWhiteSpace(sprite.FramePath) || !File.Exists((SaveData.ProjectPath + sprite.FramePath).FixPath()))
						{
							continue;
						}
						var texture = Functions.GetTexture(sprite.FramePath, false);

						for (var y = 0; y < texture.height; y++)
						{
							for (var x = 0; x < texture.width; x++)
							{
								var targetX = sprite.XFlip ? texture.width - 1 - x : x;
								var color = texture.GetPixel(targetX, texture.height - 1 - y);
								if (color.a == 0)
								{
									continue;
								}

								if (sprite.RenderMode == RetroAnimator.Entities.Sprite.RenderModes.White)
								{
									color = Color.white;
								}

								var relativeX = x + (sprite.HandleX - image_MinX);
								var relativeY = y + (sprite.HandleY - image_MinY);

								if (relativeX < 0)
								{
									throw new Exception("Frame out of bounds on left side");
								}
								if (relativeY < 0)
								{
									throw new Exception("Frame out of bounds on top side");
								}
								if (relativeX >= outTexture.width)
								{
									throw new Exception("Frame out of bounds on right side");
								}
								if (relativeY >= outTexture.height)
								{
									throw new Exception("Frame out of bounds on bottom side");
								}

								outTexture.SetPixel(relativeX, outTexture.height - 1 - relativeY, color);
							}
						}
					}

					outTexture.Apply();
					var bytes = outTexture.EncodeToPNG();
					File.WriteAllBytes((path + "\\" + id + ".png").FixPath(), bytes);
				}
			}
		});

	}

	public void Command_SaveProject()
	{
		StandaloneFileBrowser.SaveFilePanelAsync("Save JSON file", SaveData.ProjectPath, SaveData.ProjectName, "json", (path) =>
		 {
			 if (string.IsNullOrWhiteSpace(path)) return;

			 //Sanity check
			 foreach (var frame in Project.Frames.Values)
			 {
				 frame.Sprites.RemoveAll(p => string.IsNullOrEmpty(p.FramePath));
			 }

			 File.WriteAllText(path, JsonConvert.SerializeObject(Project, Formatting.Indented));
			 SaveData.ProjectPath = Path.GetDirectoryName(path);
			 SaveData.ProjectName = Path.GetFileNameWithoutExtension(path);
		 });

	}

	private void ProjectLoaded()
	{
		ProjectIsLoaded = true;
		Undo.Instance.Reset();
		ProjectBase.gameObject.SetActive(true);
		State_Frames.ReEnable();
		CheckPartsMissing();
	}

	private void OnApplicationFocus(bool isFocused)
	{
		if (!isFocused) return;

		Functions.NormalTextures.Clear();
		Functions.WhiteTextures.Clear();
		Functions.NormalSprites.Clear();
		Functions.WhiteSprites.Clear();

		if (!FrameManager.Instance) return;
		FrameManager.Instance.ForceRefresh();
	}

	internal void CheckPartsMissing()
	{
		//Check that all parts exist
		var missingParts = new List<RetroAnimator.Entities.Sprite>();
		foreach (var frame in Project.Frames.Values)
		{
			foreach (var sprite in frame.Sprites)
			{
				if (File.Exists(SaveData.ProjectPath + sprite.FramePath) == false)
				{
					missingParts.Add(sprite);
				}
			}
		}

		if (missingParts.Count > 0)
		{
			OverlayManager.Instance.AskConfirmQuestion("Parts are missing or moved in this project. Attempt automatic fix?", () =>
			{
				var pngs = Directory.GetFiles(SaveData.ProjectPath, "*.png", SearchOption.AllDirectories);
				var dict = new Dictionary<string, string>();
				foreach (var png in pngs)
				{
					dict[Path.GetFileNameWithoutExtension(png).ToLower()] = Functions.GetRelativePath(SaveData.ProjectPath, png);
				}

				foreach (var sprite in missingParts)
				{
					var key = Path.GetFileNameWithoutExtension(SaveData.ProjectPath + sprite.FramePath).ToLower();
					if (dict.ContainsKey(key))
					{
						sprite.FramePath = dict[key];
					}
				}


			});
		}
	}


}
