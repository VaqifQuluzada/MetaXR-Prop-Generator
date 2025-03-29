using UnityEngine;

namespace VaqifQuluzada.Config
{
    public static class PropGeneratorConfig
    {
        public static string VisualsParentName = "Visuals";

        public static string CollidersParentName = "Colliders";

        public static Transform detachedElementsParent;

        public static Transform ReturnDetachedElementParents()
        {
            if (detachedElementsParent == null)
            {
                GameObject detachedElementsParentGameObject = GameObject.Find("---DetachedElementsParent---");

                if (detachedElementsParentGameObject == null)
                {
                    detachedElementsParent = new GameObject("---DetachedElementsParent---").transform;
                }
            }

            return detachedElementsParent.transform;
        }

    }
}

