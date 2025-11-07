using System;
using Code.Networking.Session;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UserInterface
{
    public class GamemodeToggle : MonoBehaviour
    {
        [SerializeField] private GameObject bindings;
        [SerializeField] private GameObject tutorial;

        private void OnEnable()
        {
            if (SessionManager.Instance.IsPracticeMode)
            {
                if (PlayerPrefs.GetInt("TutorialPlayed") == 1)
                {
                    tutorial.SetActive(false);
                    bindings.SetActive(true);
                }
                else
                {
                    tutorial.SetActive(true);
                    bindings.SetActive(false);
                }
            }
            else
            {
                tutorial.SetActive(false);
                bindings.SetActive(false);
                FindFirstObjectByType<PlayerIdSender>().enabled = false;
            }
        }
    }
}