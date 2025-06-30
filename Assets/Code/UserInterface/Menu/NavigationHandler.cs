using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Code.UserInterface.Menu
{
    public class NavigationHandler : MonoBehaviour
    {
        [SerializeField] private float _delay;
        private GameObject _cachedObject;
        [SerializeField] Button[] _menuButtons;

        public void SetNavigationWithDelay(GameObject target)
        {
            _cachedObject = target;
            Invoke(nameof(SetNavigation), _delay);
        }

        public void SetNavigation()
        {
            EventSystem.current.SetSelectedGameObject(_cachedObject);
            _cachedObject = null;
        }

        public void DisableAllMenuButtons()
        {
            foreach (var button in _menuButtons)
            {
                button.gameObject.SetActive(false);
            }
        }

        public void EnableAllMenuButtons()
        {
            foreach (var button in _menuButtons)
            {
                button.gameObject.SetActive(true);
            }
        }
    }
}