using Code.Helpers.UI;
using UnityEngine;

namespace Code.Networking.Session
{
    public class ExitPracticeMode : MonoBehaviour
    {
        private bool _hasPressedEscape;
        
        private void Update()
        {
            if (!SessionManager.Instance.IsPracticeMode) return;
            
            if (_hasPressedEscape) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Time.timeScale = 1;
                _hasPressedEscape = true;
                SessionManager.Instance.LeaveSession();
                UIUtilities.Instance.LoadScene("MainMenu");
            }
        }
    }
}