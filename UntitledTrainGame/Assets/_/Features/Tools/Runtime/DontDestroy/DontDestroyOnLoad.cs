using UnityEngine;

namespace Tools.Runtime
{
    public class DontDestroyOnLoad : MonoBehaviour
    {
        #region Publics

        #endregion


        #region Unity Api

        private void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
        }

        #endregion


        #region Main Methods

        #endregion


        #region Utils

        #endregion


        #region Private and Protected

        #endregion


    }
}
