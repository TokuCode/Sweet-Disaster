using System;
using UnityEngine;
using Code.Gameplay.Character;

namespace Code.UserInterface.HUD
{
    public class ColorIndicatorSetter : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer colorIndicator;
        [SerializeField] private PlayerController player;

        private void Awake()
        {
            player.OnPost += OnPost;
        }

        private void OnPost(PlayerPublicInfo publicInfo)
        {
            colorIndicator.color = publicInfo.playerColor;
        }
    }
}