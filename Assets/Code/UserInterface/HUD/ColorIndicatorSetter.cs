using UnityEngine;
using Code.Gameplay.Character;

namespace Code.UserInterface.HUD
{
    public class ColorIndicatorSetter : PlayerHUDBase
    {
        [SerializeField] private SpriteRenderer colorIndicator;

        protected override void TryCachePlayer()
        {
            base.TryCachePlayer();

            if (!Assigned) return;

            colorIndicator.color = PlayerVisibility.Instance.GetPlayerColor(Player);
        }
    }
}