using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using TMPro;
using UnityEngine;

public class BombCountGauge : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _bombCountText;
    
    private void Update() => UpdateText();

    private void UpdateText()
    {
        if (PlayerController.Singleton == null) return;

        if (!PlayerController.Singleton.Dependencies.TryGetFeature(out Bomb bomb))
        {
            _bombCountText.text = string.Empty;
            return;
        }
        
        _bombCountText.text = $"#{bomb.BombCount}";
    }
}
