using System.Collections.Generic;

namespace RetroAnimator.Entities
{
	public class Frame
	{
		public List<Sprite> Sprites = new List<Sprite>();
		public Dictionary<string, ColRect> ColRects = new Dictionary<string, ColRect>();
		public Dictionary<string, string> Properties = new Dictionary<string, string>();
	}
}
