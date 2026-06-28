using System;
using UnityEngine;
using Image = UnityEngine.UI.Image;

namespace Code.Gameplay
{
    [CreateAssetMenu(fileName = "New Character Scriptable", menuName = "Character Scriptable")]
    public class CharacterScriptable : ScriptableObject
    {
        public string characterName;
        
        // Animation
        public RuntimeAnimatorController runtimeAnimator;
        public Sprite armWithGun;
        public Sprite armWithBomb;
        public Sprite armWithShield;
        
        // Screens UI
        public Sprite postGameImage;
        
        // Gameplay UI
        public Sprite characterIcon;
        public Sprite portraitOutline;
        public Sprite portraitNameTag;
        public Sprite portraitFrame;
        public Sprite arrow;

        public Skins[] skinsArray; // 0 is default
        
        // Skins
        [Serializable]
        public struct Skins
        {
            public string skinName;
            
            // In game animations
            public RuntimeAnimatorController runtimeAnimator;
            public Sprite armWithGun;
            public Sprite armWithBomb;
            public Sprite armWithShield;
            
            // Lobby splash art
            public Sprite lobbySplashImage;
        }
    }
}