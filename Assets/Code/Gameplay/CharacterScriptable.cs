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
    }
}