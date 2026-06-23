using UnityEngine;
using UnityEngine.UI;

public class TutorialInputBlocker : Image
{
    public RectTransform AllowedTargetRect { get; set; }

    public override bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        if (AllowedTargetRect != null)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(AllowedTargetRect, sp, eventCamera))
            {
                // Return false to let the raycast pass through the blocker and hit the target below
                return false;
            }
        }
        
        // Otherwise, return true to make the blocker catch (block) the raycast
        return true;
    }
}
