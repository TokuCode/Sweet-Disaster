using Code.Helpers.UI;
using UnityEngine;

namespace Code.Networking.Session
{
    public class ExitPracticeMode : MonoBehaviour
    {
        private bool _isLeaving;
        
        private async void Update()
        {
            if (_isLeaving) return;
            if (SessionManager.Instance == null) return;
            if (!SessionManager.Instance.IsPracticeMode) return;
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            _isLeaving = true;

            try
            {
                await SessionManager.Instance.LeaveSessionAsync();

                if (UIUtilities.Instance != null)
                    UIUtilities.Instance.LoadScene("MainMenu");
            }
            catch (System.Exception e)
            {
#if UNITY_EDITOR
                Debug.LogException(e);
#endif
                _isLeaving = false;
            }
        }
    }
}