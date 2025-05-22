using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class CharacterAssignator : NetworkBehaviour
{
    private NetworkVariable<FixedString32Bytes> selectedCharacter = new NetworkVariable<FixedString32Bytes>();

    [SerializeField] private GameObject ceciModel;
    [SerializeField] private GameObject gladysModel;

    public override void OnNetworkSpawn()
    {
        selectedCharacter.OnValueChanged += (_, newValue) =>
        {
            Debug.Log($"[CharacterAssignator] OnValueChanged triggered: {newValue}");
            EnableModel(newValue);
        };
    }

    public void SetCharacter(string character)
    {
        if (IsServer)
        {
            selectedCharacter.Value = new FixedString32Bytes(character);
            Debug.Log($"[CharacterAssignator] SetCharacter called with: {character}");

            EnableModel(selectedCharacter.Value); // Manually apply visuals
        }
    }

    private void EnableModel(FixedString32Bytes character)
    {
        string c = character.ToString();
        Debug.Log($"[CharacterAssignator] Enabling model for character: {c}");

        ceciModel.SetActive(c == "Ceci");
        gladysModel.SetActive(c == "Gladys");
    }
}