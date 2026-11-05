using UnityEngine;

namespace Edlore.GamePages
{
    public abstract class BasePage : MonoBehaviour
    {
        [SerializeField]
        private GameObject panelRoot = null;

        protected GameObject GetPanelRoot()
        {
            return panelRoot;
        }

        private void TogglePanelRootStatus(bool status)
        {
            panelRoot.SetActive(status);
        }

        public void HidePage()
        {
            TogglePanelRootStatus(false);
        }

        public void ShowPage()
        {
            TogglePanelRootStatus(true);
        }

        public bool IsRootActive()
        {
            return panelRoot.activeSelf;
        }
    }
}
