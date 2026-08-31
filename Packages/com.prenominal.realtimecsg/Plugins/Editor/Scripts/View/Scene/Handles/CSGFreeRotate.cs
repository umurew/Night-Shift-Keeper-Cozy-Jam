using UnityEditor;
using UnityEngine;

namespace RealtimeCSG.Helpers
{
	public class CSGFreeRotate
	{
		private static Vector2 s_CurrentMousePosition;
		internal const float kPickDistance = 5.0f;

		public static Quaternion Do(Camera camera, int id, Quaternion rotation, Vector3 position, float size, bool snapping, CSGHandles.InitFunction initFunction, CSGHandles.InitFunction shutdownFunction)
        {
            var worldPosition   = Handles.matrix.MultiplyPoint(position);
			var origMatrix      = Handles.matrix;

			var e = Event.current;
			switch (e.GetTypeForControl(id))
			{
				case EventType.Layout:
				{
					Handles.matrix = Matrix4x4.identity;
					HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(worldPosition, size) + kPickDistance);
					Handles.matrix = origMatrix;
					break;
				}
				case EventType.MouseDown:
				{
					if (CSGHandles.disabled)
						break;
					if (((HandleUtility.nearestControl == id && e.button == 0) || 
					 	 (GUIUtility.keyboardControl == id && e.button == 2)) && GUIUtility.hotControl == 0)
					{
						if (initFunction != null)
							initFunction();
						GUIUtility.hotControl = GUIUtility.keyboardControl = id; // Grab mouse focus
						//Tools.LockHandlePosition();
						s_CurrentMousePosition = e.mousePosition;
						e.Use();
						EditorGUIUtility.SetWantsMouseJumping(1);
					}
					break;
				}
				case EventType.MouseDrag:
				{
					if (GUIUtility.hotControl == id)
					{
						s_CurrentMousePosition += e.delta;
						var rotDir = camera.transform.TransformDirection(new Vector3(-e.delta.y, -e.delta.x, 0));
						rotation = Quaternion.AngleAxis(e.delta.magnitude, rotDir.normalized) * rotation;

						GUI.changed = true;
						e.Use();
					}
					break;
				}
				case EventType.MouseUp:
				{
					if (GUIUtility.hotControl == id && (e.button == 0 || e.button == 2))
					{
						//Tools.UnlockHandlePosition();
						GUIUtility.hotControl = 0;
						e.Use();
						if (shutdownFunction != null)
							shutdownFunction();
						EditorGUIUtility.SetWantsMouseJumping(0);
					}
					break;
				}
				case EventType.KeyDown:
				{
					if (e.keyCode == KeyCode.Escape && GUIUtility.hotControl == id)
					{
						// We do not use the event nor clear hotcontrol to ensure auto revert value kicks in from native side
						//Tools.UnlockHandlePosition();
						EditorGUIUtility.SetWantsMouseJumping(0);
					}
					break;
				}
				case EventType.Repaint:
				{
					var originalColor = Color.white;
					if (id == GUIUtility.keyboardControl)
						Handles.color = Handles.selectedColor;
					else
					if (CSGHandles.disabled)
						Handles.color = Color.Lerp(originalColor, Handles.secondaryColor, 0.75f);

					// We only want the position to be affected by the Handles.matrix.
					Handles.matrix = Matrix4x4.identity;
					Handles.DrawWireDisc(worldPosition, camera.transform.forward, size);
					Handles.matrix = origMatrix;

					Handles.color = originalColor;
					break;
				}
			}
			return rotation;
		}
	}
}