using System;
using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using TMPro;
using UnityEngine;

public class BombCountGauge : MonoBehaviour
{
    private Bomb _bomb;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _bombCountText;

    private void Update() => UpdateText();

    private void UpdateText()
    {
        if(PlayerController.Singleton == null) return;

        PlayerController.Singleton.Dependencies.TryGetFeature(out _bomb);
        
        if (_bomb == null)
        {
            _bombCountText.text = string.Empty;
            return;
        }
        
        _bombCountText.text = $"#{_bomb.BombCount}";
    }
}
