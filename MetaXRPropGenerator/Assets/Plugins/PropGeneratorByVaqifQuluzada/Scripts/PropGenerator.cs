#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.HandGrab.Visuals;
using Unity.VisualScripting;
using UnityEditor;
#endif
using UnityEngine;

namespace VaqifQuluzada.Helpers
{
    public class PropGenerator : MonoBehaviour
    {
#if UNITY_EDITOR
        [Header("****Prop Generation****")]

        [SerializeField] private ColliderType colliderType = ColliderType.NONE;

        [SerializeField] private float objectDistance = 0.1f;

        [SerializeField] private bool isGenerateSockets = false;
        [SerializeField] private bool isGeneratePrefabs = false;
        [SerializeField] private bool isGenerateSocketHoverMeshes = false;
        /// <summary>
        /// This variable is used to set prefab and socket into Vector3 pos
        /// </summary>
        [SerializeField] private bool isVectorZeroPos = false;

        [SerializeField] private GrabbablePrefabCrateType prefabCrateType = GrabbablePrefabCrateType.CREATEGRABBABLEFROMMODEL;

        [ShowIf(nameof(prefabCrateType), GrabbablePrefabCrateType.CREATEGRABBABLEASVARIANT)]
        [SerializeField] private GameObject baseGrabbablePrefabAsset;

        [SerializeField] private SocketPrefabCrateType socketPrefabCrateType = SocketPrefabCrateType.CREATESOCKETFROMMODEL;

        [ShowIf(nameof(socketPrefabCrateType), SocketPrefabCrateType.CREATESOCKETASVARIANT)]
        [SerializeField] private GameObject baseSocketPrefabAsset;

        [EnableIf(nameof(isGenerateSocketHoverMeshes))]
        [SerializeField] private Material socketHoverMeshMat;

        [SerializeField] private string grabbablePrefabsFolderPath = "Assets/App/Prefabs/Grabbables";
        [SerializeField] private string grabbableSocketsFolderPath = "Assets/App/Prefabs/Grabbables/Sockets";

        [SerializeField] private List<GameObject> propVisualsList = new List<GameObject>();
        [SerializeField] private List<GameObject> propPrefabInstancesList = new List<GameObject>();

        [SerializeField] private List<GameObject> propSocketPrefabInstancesList = new List<GameObject>();
        [SerializeField] private List<GameObject> propSocketPrefabsList = new List<GameObject>();

        [Header("****************************************")]

        [Header("***Hand Grab Interactables Configuration***")]

        [SerializeField] private bool isDisableGhostHands = false;

        [SerializeField] private List<GameObject> propPrefabsList = new List<GameObject>();

        [Header("****************************************")]

        [Header("***PropSocket Pair Interactables Configuration***")]

        [SerializeField] private string propSocketPairPrefabsFolder = "Assets/App/Prefabs/Grabbables/PropSocketPair";


        [SerializeField] private float snapInteractorResetTime = 0;

        
        [SerializeField] private bool isCreatePropSocketPairPrefab = false;

        [SerializeField] private List<GameObject> propSocketPairsList = new List<GameObject>();

        [ContextMenu(nameof(GenerateProps))]
        [Button]
        private void GenerateProps()
        {
            propPrefabInstancesList.Clear();
            propSocketPrefabInstancesList.Clear();
            propSocketPairsList.Clear();

            switch (prefabCrateType)
            {
                case GrabbablePrefabCrateType.NONE:
                    break;
                case GrabbablePrefabCrateType.CREATEGRABBABLEFROMMODEL:
                    GenerateFromModels();
                    break;
                case GrabbablePrefabCrateType.CREATEGRABBABLEASVARIANT:
                    GenerateGrabbableAsPrefabVariant();
                    break;
                default:
                    break;
            }
        }

        private void GenerateFromModels()
        {
            DeletePropPrefabsInstances();
            for (int i = 0; i < propVisualsList.Count; i++)
            {
                //Grabbable generation part
                GameObject prop = propVisualsList[i];

                GameObject propDuplicate;

                //If we use duplicated gameobject
                if (PrefabUtility.GetPrefabAssetType(prop) == PrefabAssetType.NotAPrefab)
                {
                    propDuplicate = Instantiate(prop);
                }
                //if we use model prefab from asset folder
                else
                {
                    propDuplicate = PrefabUtility.InstantiatePrefab(prop) as GameObject;
                }

                if (isVectorZeroPos)
                {
                    propDuplicate.transform.position = Vector3.zero;
                    propDuplicate.transform.rotation = Quaternion.Euler(Vector3.zero);
                }

                propDuplicate.name = propDuplicate.name.Replace("(Clone)", "");

                GameObject grabbablePrefabParent = new GameObject($"Prefab - {propDuplicate.name}Grabbable");
                Rigidbody grabbableRb = grabbablePrefabParent.AddComponent<Rigidbody>();
                Grabbable grabbable = grabbablePrefabParent.AddComponent<Grabbable>();
                GrabFreeTransformer grabFreeTransformer = grabbablePrefabParent.AddComponent<GrabFreeTransformer>();
                grabbable.InjectOptionalRigidbody(grabbableRb);
                grabbable.InjectOptionalOneGrabTransformer(grabFreeTransformer);
                grabbablePrefabParent.transform.position = propDuplicate.transform.position;

                GameObject visualParent = new GameObject("Visuals");

                visualParent.transform.localPosition = Vector3.zero;
                visualParent.transform.localRotation = Quaternion.Euler(Vector3.zero);
                visualParent.transform.position = propDuplicate.transform.position;
                propDuplicate.transform.parent = visualParent.transform;

                visualParent.transform.parent = grabbablePrefabParent.transform;

                grabbablePrefabParent.transform.parent = this.transform;

                propPrefabInstancesList.Add(grabbablePrefabParent);

                AddColliders(grabbablePrefabParent, propDuplicate);

                //Generating snap interactor
                //Snap interactor generation part
                GameObject snapInteractorGameObject = new GameObject($"SnapInteractor");
                SnapInteractor snapInteractor = snapInteractorGameObject.AddComponent<SnapInteractor>();
                TagSetFilter tagSetFilter = snapInteractor.AddComponent<TagSetFilter>();
                string[] requiredTags = new string[] { propDuplicate.name };
                tagSetFilter.InjectOptionalRequireTags(requiredTags);
                List<IGameObjectFilter> tagSetFilterList = new List<IGameObjectFilter> { tagSetFilter };
                snapInteractor.InjectOptionalInteractableFilters(tagSetFilterList);
                snapInteractorGameObject.transform.parent = grabbablePrefabParent.transform;
                snapInteractor.transform.localPosition = Vector3.zero;
                snapInteractor.InjectRigidbody(grabbableRb);

                if (isGenerateSockets)
                {
                    switch (socketPrefabCrateType)
                    {
                        case SocketPrefabCrateType.NONE:
                            break;
                        case SocketPrefabCrateType.CREATESOCKETFROMMODEL:
                            GenerateSocketsFromModel(propDuplicate, requiredTags.ToList());
                            break;
                        case SocketPrefabCrateType.CREATESOCKETASVARIANT:
                            GenerateSocketAsPrefabVariant(propDuplicate, requiredTags.ToList());
                            break;
                        default:
                            break;
                    }
                }

                //Saving as prefab
                if (isGeneratePrefabs)
                {
                    GameObject propPrefab = GeneratePrefabAndSave(grabbablePrefabParent, grabbablePrefabsFolderPath);
                    propPrefabsList.Add(propPrefab);
                }
            }
        }

        private void GenerateGrabbableAsPrefabVariant()
        {
            PrefabAssetType basePrefabType = PrefabUtility.GetPrefabAssetType(baseGrabbablePrefabAsset);

            if (basePrefabType == PrefabAssetType.NotAPrefab)
            {
                Debug.LogError($"{baseGrabbablePrefabAsset.name} isn't prefab");
                return;
            }

            DeletePropPrefabsInstances();

            for (int i = 0; i < propVisualsList.Count; i++)
            {
                GameObject grabbablePrefabVariantObject = (GameObject)PrefabUtility.InstantiatePrefab(baseGrabbablePrefabAsset, transform);
                GameObject visualsChildObject = FindOrCreateObjectByName(grabbablePrefabVariantObject, "Visuals");

                GameObject prop = propVisualsList[i];

                GameObject propDuplicate;

                //If we use duplicated gameobject
                if (PrefabUtility.GetPrefabAssetType(prop) == PrefabAssetType.NotAPrefab || PrefabUtility.GetPrefabAssetType(prop) == PrefabAssetType.Model)
                {
                    propDuplicate = Instantiate(prop);
                }
                //if we use model prefab from asset folder
                else
                {
                    propDuplicate = PrefabUtility.InstantiatePrefab(prop) as GameObject;

                }


                propDuplicate.name = propDuplicate.name.Replace("(Clone)", "");

                propDuplicate.transform.parent = visualsChildObject.transform;

                propDuplicate.transform.localPosition = Vector3.zero;

                grabbablePrefabVariantObject.name = $"Prefab - {propDuplicate.name}Grabbable Variant";

                grabbablePrefabVariantObject.transform.position = prop.transform.position;

                TagSetFilter tagSetFilter = grabbablePrefabVariantObject.GetComponentInChildren<TagSetFilter>();

                string[] requiredTags = new string[] { propDuplicate.name };

                if (tagSetFilter != null)
                {
                    tagSetFilter.InjectOptionalRequireTags(requiredTags);
                }
                AddColliders(grabbablePrefabVariantObject, propDuplicate);

                if (isGenerateSockets)
                {
                    switch (socketPrefabCrateType)
                    {
                        case SocketPrefabCrateType.NONE:
                            break;
                        case SocketPrefabCrateType.CREATESOCKETFROMMODEL:
                            GenerateSocketsFromModel(propDuplicate, requiredTags.ToList());
                            break;
                        case SocketPrefabCrateType.CREATESOCKETASVARIANT:
                            GenerateSocketAsPrefabVariant(propDuplicate, requiredTags.ToList());
                            break;
                        default:
                            break;
                    }
                }

                propPrefabInstancesList.Add(grabbablePrefabVariantObject);

                if (isGeneratePrefabs)
                {
                    GameObject propPrefab = GeneratePrefabAndSave(grabbablePrefabVariantObject, grabbablePrefabsFolderPath);
                    propPrefabsList.Add(propPrefab);
                }
            }
        }

        private void GenerateSocketsFromModel(GameObject propDuplicate, List<string> requiredTagsList)
        {
            //Socket and snap interactor generation part
            if (isGenerateSockets)
            {
                //Socket generation part
                GameObject grabbableSocketParent = new GameObject($"Prefab - {propDuplicate.name}GrabbableSocket");
                grabbableSocketParent.transform.position = propDuplicate.transform.position;
                Rigidbody grabbableSocketRb = grabbableSocketParent.AddComponent<Rigidbody>();
                grabbableSocketRb.constraints = RigidbodyConstraints.FreezeAll;
                grabbableSocketRb.isKinematic = true;
                grabbableSocketParent.transform.parent = this.transform;

                AddSnapInteractableAndTagSet(grabbableSocketParent, requiredTagsList);

                AddCollidersToSockets(grabbableSocketParent, propDuplicate, true);

                if (isGenerateSocketHoverMeshes)
                {
                    AddSocketHoverMeshesToSockets(grabbableSocketParent, propDuplicate);
                }

                propSocketPrefabInstancesList.Add(grabbableSocketParent);

                //That commented code was used for making prop-socket pair
                //snapInteractor.InjectOptionalTimeOutInteractable(socketSnapInteractable);
                //snapInteractor.InjectPointableElement(grabbable);

                grabbableSocketParent.transform.position = propDuplicate.transform.position;

                if (isGeneratePrefabs)
                {
                    GameObject propSocketGameObject = GeneratePrefabAndSave(grabbableSocketParent, grabbableSocketsFolderPath);
                    propSocketPrefabsList.Add(propSocketGameObject);
                }
            }
        }

        private void GenerateSocketAsPrefabVariant(GameObject propDuplicate, List<string> requiredTags)
        {
            PrefabAssetType baseSocketPrefabType = PrefabUtility.GetPrefabAssetType(baseSocketPrefabAsset);

            if (baseSocketPrefabType == PrefabAssetType.NotAPrefab)
            {
                Debug.LogError($"{baseSocketPrefabAsset.name} isn't prefab");
                return;
            }

            GameObject socketPrefabVariantObject = (GameObject)PrefabUtility.InstantiatePrefab(baseSocketPrefabAsset, transform);
            socketPrefabVariantObject.GetComponent<TagSet>().AddTag(propDuplicate.name);
            socketPrefabVariantObject.name = $"Prefab - {propDuplicate.name}Socket Variant";

            socketPrefabVariantObject.transform.position = propDuplicate.transform.position;

            GameObject visualsChildObject = FindOrCreateObjectByName(socketPrefabVariantObject, "Visuals");

            AddCollidersToSockets(socketPrefabVariantObject, propDuplicate, true);

            if (isGenerateSocketHoverMeshes)
            {
                AddSocketHoverMeshesToSockets(socketPrefabVariantObject, propDuplicate);
            }

            AddSnapInteractableAndTagSet(socketPrefabVariantObject, requiredTags);

            propSocketPrefabInstancesList.Add(socketPrefabVariantObject);

            if (isGeneratePrefabs)
            {
                GameObject propSocketGameObject = GeneratePrefabAndSave(socketPrefabVariantObject, grabbableSocketsFolderPath);
                propSocketPrefabsList.Add(propSocketGameObject);
            }
        }


        private static void AddSnapInteractableAndTagSet(GameObject grabbableSocketParent, List<string> requiredTagsList)
        {
            SnapInteractable socketSnapInteractable = grabbableSocketParent.GetComponent<SnapInteractable>();

            if (socketSnapInteractable == null)
            {
                socketSnapInteractable = grabbableSocketParent.AddComponent<SnapInteractable>();
            }

            TagSet tagSet = grabbableSocketParent.GetComponent<TagSet>();

            if (tagSet == null)
            {
                tagSet = socketSnapInteractable.AddComponent<TagSet>();
            }

            tagSet.InjectOptionalTags(requiredTagsList);
        }

        private void AddCollidersToSockets(GameObject objectParent, GameObject propDuplicate, bool isTrigger = false)
        {
            AddColliders(objectParent, propDuplicate);

            GameObject collidersParent = FindOrCreateObjectByName(objectParent, "Colliders");

            List<Collider> collidersList = collidersParent.GetComponentsInChildren<Collider>().ToList();

            foreach (Collider collider in collidersList)
            {
                collider.isTrigger = isTrigger;
            }
        }

        private void AddSocketHoverMeshesToSockets(GameObject grabbableSocketParent, GameObject propDuplicate)
        {
            GameObject visualsParent = FindOrCreateObjectByName(grabbableSocketParent, "Visuals");
            visualsParent.transform.parent = grabbableSocketParent.transform;
            visualsParent.transform.localPosition = Vector3.zero;

            GameObject propCollidersClone = Instantiate(propDuplicate, visualsParent.transform);
            propCollidersClone.transform.localPosition = Vector3.zero;

            List<Collider> collidersList = propCollidersClone.GetComponentsInChildren<Collider>().ToList();

            foreach (Collider collider in collidersList)
            {
                DestroyImmediate(collider);
            }

            List<MeshRenderer> meshRenderers = propCollidersClone.GetComponentsInChildren<MeshRenderer>().ToList();

            if (socketHoverMeshMat == null)
            {
                Debug.LogError("Socket hover mesh mat is null");
                return;
            }

            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                List<Material> hoverMeshMatList = meshRenderer.sharedMaterials.ToList();

                Debug.Log(hoverMeshMatList.Count);

                for (int i = 0; i < hoverMeshMatList.Count; i++)
                {
                    hoverMeshMatList[i] = socketHoverMeshMat;
                }

                meshRenderer.sharedMaterials = hoverMeshMatList.ToArray();
            }

        }

        private GameObject FindOrCreateObjectByName(GameObject grabbablePrefabVariantObject, string objectName)
        {
            Transform visualsChildTransform = grabbablePrefabVariantObject.transform.Find(objectName);

            if (visualsChildTransform == null)
            {
                GameObject visualsChildObject = new GameObject(objectName);
                visualsChildObject.transform.parent = grabbablePrefabVariantObject.transform;
                visualsChildObject.transform.localPosition = Vector3.zero;

                return visualsChildObject;
            }
            else
            {
                return visualsChildTransform.gameObject;
            }
        }

        private void AddColliders(GameObject objectParent, GameObject prop)
        {
            GameObject collidersParent = FindOrCreateObjectByName(objectParent, "Colliders");

            collidersParent.transform.localPosition = Vector3.zero;
            collidersParent.transform.localRotation = Quaternion.Euler(Vector3.zero);


            GameObject propDuplicate = Instantiate(prop, collidersParent.transform);

            propDuplicate.transform.localPosition = Vector3.zero;

            List<MeshRenderer> meshRenderersList = propDuplicate.GetComponentsInChildren<MeshRenderer>().ToList();

            List<MeshFilter> meshFilters = propDuplicate.GetComponentsInChildren<MeshFilter>().ToList();

            foreach (MeshRenderer mesh in meshRenderersList)
            {
                switch (colliderType)
                {
                    case ColliderType.NONE:

                        break;
                    case ColliderType.BOX:
                        mesh.AddComponent<BoxCollider>();
                        break;
                    case ColliderType.MESH:
                        mesh.AddComponent<MeshCollider>();
                        break;
                    case ColliderType.MESH_CONVEX:
                        MeshCollider meshCollider = mesh.AddComponent<MeshCollider>();
                        meshCollider.convex = true;
                        break;
                    case ColliderType.SPHERE:
                        mesh.AddComponent<SphereCollider>();
                        break;
                    case ColliderType.CAPSULE:
                        mesh.AddComponent<CapsuleCollider>();
                        break;
                    default:
                        break;
                }
            }

            meshRenderersList.ForEach(meshRenderer => DestroyImmediate(meshRenderer));
            meshFilters.ForEach(meshFilter => DestroyImmediate(meshFilter));
        }

        [ContextMenu(nameof(DeletePropPrefabsInstances))]
        [Button]
        private void DeletePropPrefabsInstances()
        {
            foreach (GameObject prefab in propPrefabInstancesList)
            {
                if (prefab != null)
                {
                    DestroyImmediate(prefab.gameObject);
                }
            }
            propPrefabInstancesList.Clear();

            foreach (GameObject propSocket in propSocketPrefabInstancesList)
            {
                if (propSocket != null)
                {
                    DestroyImmediate(propSocket.gameObject);
                }
            }

            propSocketPrefabInstancesList.Clear();
        }

        private GameObject GeneratePrefabAndSave(GameObject prefabGeneratedObject, string savePath)
        {

            string pathWithoutAssetsFolder = savePath.Replace("Assets/", "");

            List<string> pathFolders = pathWithoutAssetsFolder.Split('/').ToList();

            foreach (var pathitem in pathFolders)
            {
                Debug.Log(pathitem);
            }

            string folderPathTemp = "Assets";

            //If path doesn't exist we need to generate it.
            foreach (string pathFolder in pathFolders)
            {
                //if following folder doesn't exist
                if (!AssetDatabase.IsValidFolder($"{folderPathTemp}/{pathFolder}"))
                {
                    AssetDatabase.CreateFolder(folderPathTemp, pathFolder);
                }
                folderPathTemp = $"{folderPathTemp}/{pathFolder}";
            }

            folderPathTemp = $"{folderPathTemp}/{prefabGeneratedObject.name}.prefab";

            GameObject createdPrefab = null;

            createdPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(prefabGeneratedObject, folderPathTemp, InteractionMode.AutomatedAction);


            return createdPrefab;
        }

        [ContextMenu(nameof(ConfigureHandGrabInteractables))]
        [Button]
        private void ConfigureHandGrabInteractables()
        {
            foreach (GameObject propPrefab in propPrefabsList)
            {
                Grabbable propGrabbable = propPrefab.GetComponentInChildren<Grabbable>();
                List<HandGrabPose> handGrabPoses = propPrefab.GetComponentsInChildren<HandGrabPose>().ToList();
                List<HandGhost> handGhosts = propPrefab.GetComponentsInChildren<HandGhost>().ToList();

                foreach (HandGrabPose handGrabPose in handGrabPoses)
                {
                    handGrabPose.InjectRelativeTo(propGrabbable.transform);
                }

                if (isDisableGhostHands)
                {
                    foreach (HandGhost handGhost in handGhosts)
                    {
                        handGhost.gameObject.SetActive(false);
                    }
                }

                if (PrefabUtility.GetPrefabAssetType(propGrabbable) == PrefabAssetType.Regular)
                {
                    PrefabUtility.ApplyPrefabInstance(propGrabbable.gameObject, InteractionMode.AutomatedAction);
                }

            }
        }

        

        [ContextMenu(nameof(CreatePropSocketPairs))]
        [Button]
        private void CreatePropSocketPairs()
        {
            for (int i = 0; i < propPrefabInstancesList.Count; i++)
            {
                GameObject prop = propPrefabInstancesList[i];

                Grabbable propGrabbable = prop.GetComponent<Grabbable>();

                Debug.Log(propGrabbable);

                SnapInteractor snapInteractor = propGrabbable.GetComponentInChildren<SnapInteractor>();

                InteractorUnityEventWrapper snapInteractorEventWrapper = snapInteractor.AddComponent<InteractorUnityEventWrapper>();

                snapInteractorEventWrapper.InjectInteractorView(snapInteractor);


                if (snapInteractor != null)
                {
                    if (i < propSocketPrefabInstancesList.Count)
                    {
                        SnapInteractable snapInteractable = propSocketPrefabInstancesList[i].GetComponent<SnapInteractable>();

                        snapInteractor.InjectOptionalTimeOutInteractable(snapInteractable);
                        snapInteractor.InjectOptionaTimeOut(snapInteractorResetTime);

                        GameObject propSocketPair = new GameObject($"{prop.name} PropSocketPair");

                        propSocketPair.transform.position = snapInteractable.transform.position;

                        snapInteractable.transform.parent = propSocketPair.transform;
                        prop.transform.parent = propSocketPair.transform;

                        if (isCreatePropSocketPairPrefab)
                        {
                            GameObject propSocketPairPrefab = GeneratePrefabAndSave(propSocketPair, propSocketPairPrefabsFolder);
                            propSocketPairsList.Add(propSocketPairPrefab);
                        }
                    }
                }
            }
        }

        enum ColliderType
        {
            NONE,
            BOX,
            MESH,
            MESH_CONVEX,
            SPHERE,
            CAPSULE
        }

        enum GrabbablePrefabCrateType
        {
            NONE,
            CREATEGRABBABLEFROMMODEL,
            CREATEGRABBABLEASVARIANT
        }

        enum SocketPrefabCrateType
        {
            NONE,
            CREATESOCKETFROMMODEL,
            CREATESOCKETASVARIANT
        }
#endif
    }
}
