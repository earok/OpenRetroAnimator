using Newtonsoft.Json;
using RetroAnimator.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RetroAnimator.Undo

{

	public class UndoRecord
	{
		public static UndoRecord Generate()
		{
			return new UndoRecord()
			{
				Project = Manager.Instance.Project,
				SelectedFrame = FrameManager.Instance.SelectedFrame,
				SelectedAnimation = FrameManager.Instance.SelectedAnimation,
				SelectedColRect = FrameManager.Instance.SelectedColRect,
				SelectedAnimationFrame = FrameManager.Instance.SelectedAnimationFrame,
				SelectedSprites = FrameManager.Instance.SelectedSprites.ToList()
			};
		}

		public static UndoRecord Deserialize(string source)
		{
			return JsonConvert.DeserializeObject<UndoRecord>(source);
		}

		public Project Project;
		public string SelectedFrame;
		public string SelectedAnimation;
		public int? SelectedSprite;
		public string SelectedColRect;
		public int SelectedAnimationFrame;
		public List<int> SelectedSprites;
	}

	public class Undo : Singleton<Undo>
	{
		public int MaximumUndos;

		internal Stack<string> UndoStack = new Stack<string>();
		internal Stack<string> RedoStack = new Stack<string>();

		public void PushUndo() //Any change made
		{
			UndoStack.Push(JsonConvert.SerializeObject(UndoRecord.Generate()));
			RedoStack.Clear();
		}

		public UndoRecord PopUndo() //Ctrl+Z
		{
			if (UndoStack.Count == 0) return null;
			RedoStack.Push(JsonConvert.SerializeObject(UndoRecord.Generate()));
			var undoObject = UndoRecord.Deserialize(UndoStack.Pop());
			ApplyUndo(undoObject);
			return undoObject;
		}

		public UndoRecord PopRedo() //Ctrl+Y
		{
			if (RedoStack.Count == 0) return null;
			UndoStack.Push(JsonConvert.SerializeObject(UndoRecord.Generate()));
			var redoObject = UndoRecord.Deserialize(RedoStack.Pop());
			ApplyUndo(redoObject);
			return redoObject;
		}

		private static void ApplyUndo(UndoRecord undoObject)
		{
			Manager.Instance.Project = undoObject.Project;
		}

		internal void Reset()
		{
			UndoStack.Clear();
		}
	}

}
