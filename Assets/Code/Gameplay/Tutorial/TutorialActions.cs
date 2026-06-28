using UnityEngine;
using Code.Helpers.Singleton;
using System;

namespace Code.Gameplay.Tutorial
{
    public class TutorialActions : Singleton<TutorialActions>
    {
        public int currentIndex;
        public bool waitForTrigger;
        
        private bool playerHasJumped;
        private bool playerHasCrouched;
        private bool playerHasShotABot;
        private bool playerHasShotABomb;
        private bool playerHasBlockedAShot;
        private bool playerHasDoneAShieldBash;
        private bool playerHasBrokenOutOfStun;
    
        // Events
        public event Action OnPlayerJumpedOrCrouched;
        public event Action<bool> OnPlayerShotABot;
        public event Action<bool> OnPlayerShotABomb;
        public event Action<bool> OnPlayerBlockedAShot;
        public event Action<bool> OnPlayerDidAShieldBash;
        public event Action<bool> OnPlayerBrokenOutOfStun;
        
        // Properties with event invocation
    
        public bool PlayerHasJumped
        {
            get => playerHasJumped;
            set
            {
                if (playerHasJumped == value) return;
                playerHasJumped = value;
                if (value)
                    OnPlayerJumpedOrCrouched?.Invoke();
            }
        }
    
        public bool PlayerHasCrouched
        {
            get => playerHasCrouched;
            set
            {
                if (playerHasCrouched == value) return;
                playerHasCrouched = value;
                if (value) 
                    OnPlayerJumpedOrCrouched?.Invoke();
            }
        }
    
        public bool PlayerHasShotABot
        {
            get => playerHasShotABot;
            set
            {
                if (playerHasShotABot == value) return;
                playerHasShotABot = value;
                OnPlayerShotABot?.Invoke(value);
            }
        }
    
        public bool PlayerHasShotABomb
        {
            get => playerHasShotABomb;
            set
            {
                if (playerHasShotABomb == value) return;
                playerHasShotABomb = value;
                OnPlayerShotABomb?.Invoke(value);
            }
        }
    
        public bool PlayerHasBlockedAShot
        {
            get => playerHasBlockedAShot;
            set
            {
                if (playerHasBlockedAShot == value) return;
                playerHasBlockedAShot = value;
                OnPlayerBlockedAShot?.Invoke(value);
            }
        }
    
        public bool PlayerHasDoneAShieldBash
        {
            get => playerHasDoneAShieldBash;
            set
            {
                if (playerHasDoneAShieldBash == value) return;
                playerHasDoneAShieldBash = value;
                OnPlayerDidAShieldBash?.Invoke(value);
            }
        }
    
        public bool PlayerHasBrokenOutOfStun
        {
            get => playerHasBrokenOutOfStun;
            set
            {
                if (playerHasBrokenOutOfStun == value) return;
                playerHasBrokenOutOfStun = value;
                OnPlayerBrokenOutOfStun?.Invoke(value);
            }
        }
    }
}