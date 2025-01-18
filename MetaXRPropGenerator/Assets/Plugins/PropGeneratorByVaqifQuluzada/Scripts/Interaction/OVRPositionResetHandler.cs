using DesignPatterns.Utilities;
using Oculus.Interaction.HandGrab;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using VaqifQuluzada.Config;

namespace VaqifQuluzada.Handlers
{
    public class OVRPositionResetHandler : MonoBehaviour
    {
        [Header("Properties")]
        [SerializeField] private LayerMask groundLayer;

        [SerializeField] private bool isParentDetached = true;

        [Header("Links")]
        [SerializeField] private Rigidbody grabbableRb;
        [SerializeField] private Transform resetPosTransform;
        [SerializeField] private List<HandGrabInteractable> grabbablesList = new List<HandGrabInteractable>();
        [SerializeField] private UnityEvent OnTouchedGroundEvent;

        #region Unity Methods

        private void Start()
        {
            if (isParentDetached)
            {
                if (resetPosTransform != null)
                {
                    resetPosTransform.parent = GameplayConfig.ReturnDetachedElementParents();
                }
            }

            SetupInteractable();
        }

        private void SetupInteractable()
        {

            if (grabbablesList.Count == 0)
            {
                grabbablesList.AddRange(GetComponentsInChildren<HandGrabInteractable>().ToList());
            }

            if (grabbableRb == null)
            {
                grabbableRb = GetComponent<Rigidbody>();
            }

        }

        private void OnCollisionEnter(Collision collision)
        {
            if (grabbablesList.Count > 0)
            {
                foreach (var handGrabbable in grabbablesList)
                {
                    if (handGrabbable.SelectingInteractors.Count > 0)
                    {
                        return;
                    }
                }

            }

            if (groundLayer.ContainsLayer(collision.gameObject))
            {
                ResetPosition();
            }
        }

        public void ResetPosition()
        {
            if (grabbableRb != null)
            {
                grabbableRb.linearVelocity = Vector3.zero;
            }

            if (resetPosTransform != null)
            {
                transform.position = resetPosTransform.position;
                transform.rotation = resetPosTransform.rotation;
            }

            OnTouchedGroundEvent.Invoke();
        }



        #endregion


    }
}
