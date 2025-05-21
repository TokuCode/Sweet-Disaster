using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class GameManager : MonoBehaviour
{
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] GameObject playerPrefab;

    private void Start()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        var players = NetworkManager.Singleton.ConnectedClientsList;
        int i = 0;

        foreach (var client in players)
        {
            Vector3 spawnPos = spawnPoints[i % spawnPoints.Length].position;

            var playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

            var networkObj = playerInstance.GetComponent<NetworkObject>();
            networkObj.SpawnAsPlayerObject(client.ClientId);

            if (SessionManager.Instance.clientIdToPlayerId.TryGetValue(client.ClientId, out var playerId))
            {
                var sessionPlayer = SessionManager.Instance.ActiveSession.Players
                    .FirstOrDefault(p => p.Id == playerId);

                if (sessionPlayer != null && sessionPlayer.Properties.TryGetValue(SessionManager.Instance.playerCharacterPropertyKey, out var charProp))
                {
                    Debug.Log($"[GameManager] Assigning character {charProp.Value} to player {client.ClientId}");
                    playerInstance.GetComponent<CharacterAssignator>().SetCharacter(charProp.Value);
                }
                else
                {
                    Debug.LogWarning($"[GameManager] No character property found for player {client.ClientId}");
                }
            }
            else
            {
                Debug.LogWarning($"[GameManager] No playerId mapping found for ClientId {client.ClientId}");
            }

            i++;
        }
    }
}