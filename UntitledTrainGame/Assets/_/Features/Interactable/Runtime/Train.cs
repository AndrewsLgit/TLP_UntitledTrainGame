using System;
using Foundation.Runtime;
using SharedData.Runtime;
using UnityEngine;

namespace Interactable.Runtime
{
    public class Train : FMono, IInteractable
    {
        #region Variables
        
        #region Private
        // Private Variables
        
        private float _timeToInteract;
        
        // Private Variables
        #endregion
        
        #region Public
        // Public Variables
        
        public float TimeToInteract => _timeToInteract;
        
        // Public Variables
        #endregion
        
        #endregion
        
        #region Unity API

        private void Start()
        {
            throw new NotImplementedException();
        }

        #endregion
        
        #region Main Methods
        public void Interact()
        {
            throw new System.NotImplementedException();
        }
        #endregion
    }
}