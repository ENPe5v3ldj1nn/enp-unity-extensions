using UnityEditor;
using UnityEngine.UI;

namespace ENP.UnityExtensions.Editor
{
    [InitializeOnLoad]
    internal static class ImageRaycastTargetDisabler
    {
        static ImageRaycastTargetDisabler()
        {
            ObjectFactory.componentWasAdded += OnComponentWasAdded;
        }

        private static void OnComponentWasAdded(UnityEngine.Component component)
        {
            if (component is Image image)
            {
                image.raycastTarget = false;
            }
        }
    }
}
