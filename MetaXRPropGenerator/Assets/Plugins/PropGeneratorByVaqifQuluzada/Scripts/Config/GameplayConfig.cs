using UnityEngine;

namespace VaqifQuluzada.Config
{
    public class GameplayConfig
    {
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

