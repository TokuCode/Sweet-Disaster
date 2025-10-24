using System;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UserInterface
{
    public class GamemodeToggle : MonoBehaviour
    {
        [SerializeField] private Button onlineModeButton;

        private void OnEnable()
        {
            if (PlayerPrefs.GetInt("TutorialPlayed") == 1)
                onlineModeButton.interactable = true;
            else
                onlineModeButton.interactable = false;
        }
    }
}