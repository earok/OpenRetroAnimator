using RetroAnimator.Entities;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

namespace Spriter
{

	public class SpriterRoot
	{
		public Entity[] entity { get; set; }
		public Folder[] folder { get; set; }
		public string generator { get; set; }
		public string generator_version { get; set; }
		public int pixel_mode { get; set; }
		public string scon_version { get; set; }

		internal Project ToProject()
		{
			var project = new Project();

			//Step one, build a path dictionary
			var pathDict = new Dictionary<string, string>();
			var textureDict = new Dictionary<string, Texture2D>();

			foreach (var fold in folder)
			{
				foreach (var file in fold.file)
				{
					var id = Path.GetFileNameWithoutExtension(file.name);
					pathDict[id] = file.name;

					var texture = new Texture2D(2, 2);
					var bytes = System.IO.File.ReadAllBytes(SaveData.ProjectPath + file.name);
					texture.LoadImage(bytes);
					texture.wrapMode = TextureWrapMode.Clamp;
					texture.filterMode = FilterMode.Point;

					textureDict[id] = texture;
				}
			}

			foreach (var ent in entity)
			{
				foreach (var ani in ent.animation)
				{
					//For the rest of the timeline, work out the durations
					var times = new HashSet<int>();
					foreach (var tim in ani.timeline)
					{
						foreach (var keyFrame in tim.key)
						{
							times.Add(keyFrame.time);
						}
					}

					var animation = new RetroAnimator.Entities.Animation();

					var offset = 0;
					var timeList = times.OrderBy(p => p).ToList();

					for (var t = 0; t < timeList.Count; t++)
					{
						var timeOffset = timeList[t];

						var frameName = ani.name + "_" + offset.ToString("d2");
						var frame = new RetroAnimator.Entities.Frame();

						var mainLine = ani.mainline.key[t];

						foreach (var obj in mainLine.object_ref)
						{
							var tim = ani.timeline[int.Parse(obj.timeline)];
							var keyFrame = tim.key.LastOrDefault(p => p.time <= timeOffset);

							if (tim.object_type == "box")
							{
								var w = ent.obj_info[tim.obj].w;
								var h = ent.obj_info[tim.obj].h;

								var box = new ColRect()
								{
									X = (int)(keyFrame._object.x - (keyFrame._object.pivot_x * w)),
									Y = (int)(-keyFrame._object.y - ((1 - keyFrame._object.pivot_y) * h)),
									Width = (int)w,
									Height = (int)h,
								};
								frame.ColRects.Add(tim.name, box);
								continue;
							}

							if (keyFrame._object.folder.HasValue == false)
							{
								continue;
							}

							var file = folder[keyFrame._object.folder.Value].file[keyFrame._object.file.Value];

							var pivotX = file.pivot_x;
							var xFlip = false;
							var x = keyFrame._object.x;

							if (keyFrame._object.scale_x < 0)
							{
								xFlip = true;
								pivotX *= -1;
								x -= file.width;
							}

							var sprite = new RetroAnimator.Entities.Sprite()
							{
								HandleX = (int)(x - (pivotX * file.width)),
								HandleY = (int)(-keyFrame._object.y - ((1 - file.pivot_y) * file.height)),
								XFlip = xFlip,
								FramePath = file.name,
							};

							frame.Sprites.Add(sprite);


						}


						project.Frames[frameName] = frame;

						//Calculate the duration
						int duration = 0;
						if (t < timeList.Count - 1)
						{
							duration = timeList[t + 1] - timeList[t];
						}
						else
						{
							duration = ani.length - timeList[t];
						}

						var animFrame = new RetroAnimator.Entities.AnimationFrameData
						{
							FrameID = frameName,
							Duration = duration / 20
						};
						animation.FrameData.Add(animFrame);

						offset++;
					}

					project.Animations[ani.name] = animation;


					/*
						var texture = textureDict[name];
						var XFlip = obj.abs_scale_x < 0;
						var FramePath = pathDict[name];

						int HandleX = 0;
						int HandleY = 0;
						bool CreateSprite = false;

						foreach (var tim)
						{
							foreach (var keyFrame in tim.key.Where(p => p.time <= timeOffset))
							{
								HandleX = keyFrame._object.x;
								HandleY = keyFrame._object.y * -1;
								CreateSprite = true;
							}
						}

						if (CreateSprite)
						{
							frame.Sprites.Add(new RetroAnimator.Entities.Sprite()
							{
								HandleX = (int)(HandleX - (obj.abs_pivot_x * texture.width)),
								HandleY = (int)(HandleY - ((1 - obj.abs_pivot_y) * texture.height)),
								XFlip = XFlip,
								FramePath = FramePath,
							});
						}

					}
					*/


				}
			}

			return project;
		}
	}

	public class Entity
	{
		public Animation[] animation { get; set; }
		public Character_Map[] character_map { get; set; }
		public int id { get; set; }
		public string name { get; set; }
		public Obj_Info[] obj_info { get; set; }
	}

	public class Animation
	{
		public int id { get; set; }
		public int interval { get; set; }
		public int length { get; set; }
		public Mainline mainline { get; set; }
		public string name { get; set; }
		public Timeline[] timeline { get; set; }
		public string looping { get; set; }
		public Eventline[] eventline { get; set; }
	}

	public class Mainline
	{
		public Key[] key { get; set; }
	}

	public class Key
	{
		public object[] bone_ref { get; set; }
		public string curve_type { get; set; }
		public int id { get; set; }
		public Object_Ref[] object_ref { get; set; }
		public int time { get; set; }
	}

	public class Object_Ref
	{
		public float abs_a { get; set; }
		public float abs_angle { get; set; }
		public float abs_pivot_x { get; set; }
		public float abs_pivot_y { get; set; }
		public float abs_scale_x { get; set; }
		public float abs_scale_y { get; set; }
		public float abs_x { get; set; }
		public float abs_y { get; set; }
		public int file { get; set; }
		public int folder { get; set; }
		public int id { get; set; }
		public int key { get; set; }
		public string name { get; set; }
		public string timeline { get; set; }
		public string z_index { get; set; }
	}

	public class Timeline
	{
		public int id { get; set; }
		public Key1[] key { get; set; }
		public string name { get; set; }
		public int obj { get; set; }
		public string object_type { get; set; }
	}

	public class Key1
	{
		public int id { get; set; }

		[JsonProperty("object")]
		public Object _object { get; set; }
		public int spin { get; set; }
		public int time { get; set; }
	}

	public class Object
	{
		public float angle { get; set; }
		public int? file { get; set; }
		public int? folder { get; set; }
		public int x { get; set; }
		public int y { get; set; }
		public float pivot_x { get; set; }
		public float pivot_y { get; set; }
		public float scale_x { get; set; }
		public float scale_y { get; set; }
	}

	public class Eventline
	{
		public int id { get; set; }
		public Key2[] key { get; set; }
		public string name { get; set; }
	}

	public class Key2
	{
		public int id { get; set; }
		public int time { get; set; }
	}

	public class Character_Map
	{
		public int id { get; set; }
		public Map[] map { get; set; }
		public string name { get; set; }
	}

	public class Map
	{
		public int file { get; set; }
		public int folder { get; set; }
		public int target_file { get; set; }
		public int target_folder { get; set; }
	}

	public class Obj_Info
	{
		public float h { get; set; }
		public string name { get; set; }
		public int pivot_x { get; set; }
		public int pivot_y { get; set; }
		public string realname { get; set; }
		public string type { get; set; }
		public float w { get; set; }
		public Frame[] frames { get; set; }
	}

	public class Frame
	{
		public int file { get; set; }
		public int folder { get; set; }
	}

	public class Folder
	{
		public File[] file { get; set; }
		public int id { get; set; }
		public string name { get; set; }
	}

	public class File
	{
		public int height { get; set; }
		public int id { get; set; }
		public string name { get; set; }
		public float pivot_x { get; set; }
		public float pivot_y { get; set; }
		public int width { get; set; }
	}

}