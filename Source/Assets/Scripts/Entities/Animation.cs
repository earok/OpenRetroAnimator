using System.Collections.Generic;

namespace RetroAnimator.Entities
{
	public class Animation
	{
		public List<AnimationFrameData> FrameData = new List<AnimationFrameData>();
		public Dictionary<string, string> Properties = new Dictionary<string, string>();
	}
}
