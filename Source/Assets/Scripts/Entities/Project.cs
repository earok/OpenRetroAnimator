using System.Collections.Generic;

namespace RetroAnimator.Entities
{
	public class Project
	{
		public string ProjectType = "RetroAnimator";
		public Dictionary<string, Animation> Animations = new Dictionary<string, Animation>();
		public Dictionary<string, Frame> Frames = new Dictionary<string, Frame>();
		public Dictionary<string, string> Properties = new Dictionary<string, string>();
	}
}