using System.Collections.Generic;

namespace RetroAnimator.Entities
{
	public class AnimationFrameData
	{
		public string FrameID;
		public int Duration;
		public Dictionary<string, string> Properties = new Dictionary<string, string>();
	}
}
