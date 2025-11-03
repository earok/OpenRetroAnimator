using System.Collections.Generic;

namespace RetroAnimator.Entities
{
	public class Sprite
	{
		public enum RenderModes
		{
			Normal = 0,
			White = 1,
		}

		public string FramePath;
		public int HandleX;
		public int HandleY;
		public bool XFlip;

		public bool NoFlip;
		public RenderModes RenderMode;
		public Dictionary<string, string> Properties = new Dictionary<string, string>();
	}
}
