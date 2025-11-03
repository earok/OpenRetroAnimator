using System.Collections.Generic;

namespace RetroAnimator.Entities
{
	public class ColRect
	{
		public int Value;
		public int X;
		public int Y;
		public int Width;
		public int Height;

		public byte ColorR;
		public byte ColorG;
		public byte ColorB = 255;

		public Dictionary<string, string> Properties = new Dictionary<string, string>();
	}
}
