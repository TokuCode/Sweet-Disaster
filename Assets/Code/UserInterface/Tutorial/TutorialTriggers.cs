using System;
using Code.Gameplay.Tutorial;
using Code.Networking.Session;
using UnityEngine;

namespace Code.UserInterface.Tutorial
{
    public class TutorialTriggers : MonoBehaviour
    {
        public int indexNumber;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!SessionManager.Instance.IsPracticeMode) return;
            
            if (other.gameObject.CompareTag("P1") && TutorialDialogues.Instance.GetIndex() == indexNumber && 
                TutorialActions.Instance.waitForTrigger)
            {
                TutorialDialogues.Instance.IncreaseIndex(1);
                TutorialDialogues.Instance.TriggerContinue();
            }
        }
    }
}