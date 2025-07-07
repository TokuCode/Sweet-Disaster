using System;
using System.Collections.Generic;
using Code.Gameplay.Character.Features;
using Code.Helpers.MergeSort;
using Code.Systems.MatchTime;
using UnityEngine;

namespace Code.Gameplay.Character
{
    public class PlayerMatchResolver : MonoBehaviour
    {
        public static PlayerMatchResolver Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple PlayerMatchResolver found!");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Start()
        {
            MatchTime.Instance.OnEndMatch += ResolveMatch;
        }

        private void ResolveMatch()
        {
            List<PlayerWinCondition> conditions = new();
            foreach (var player in PlayerVisibility.Instance.Players)
            {
                PlayerWinCondition condition = GetPlayerWinCondition(player.player);
                if(condition.alreadyDefeated) continue;
                conditions.Add(condition);
            }

            if (conditions.Count <= 1) return;
            
            MergeSortUtil<PlayerWinCondition>.MergeSort(conditions);

            for (int i = 0; i < conditions.Count - 1; i++)
                ReportLoss(conditions[i]);
        }

        private void ReportLoss(PlayerWinCondition condition)
        {
            condition.player.Dependencies.TryGetFeature(out LoseReporterPadded reporter);
            reporter.ReportDefeat();
        }

        private PlayerWinCondition GetPlayerWinCondition(PlayerController player)
        {
            bool alreadyDefeated = player.defeated.Value;

            if (alreadyDefeated)
                return new PlayerWinCondition { alreadyDefeated = alreadyDefeated };

            int stocks = player.Dependencies.TryGetFeature(out LoseReporterPadded reporter) ? reporter.StockCount : -1;
            float health = player.Dependencies.TryGetFeature(out Health healthFeature) ? healthFeature.HealthAmount : Mathf.Infinity;

            return new PlayerWinCondition
            {
                player = player,
                alreadyDefeated = alreadyDefeated,
                stocks = stocks,
                health = health
            };
        }
    }

    public struct PlayerWinCondition : IComparable<PlayerWinCondition>
    {
        public PlayerController player;
        public bool alreadyDefeated;
        public int stocks;
        public float health;
        
        public int CompareTo(PlayerWinCondition other)
        {
            if (other.alreadyDefeated && alreadyDefeated) return 0;
            if (other.alreadyDefeated && !alreadyDefeated) return 1;
            if (!other.alreadyDefeated && alreadyDefeated) return -1;
            
            if(other.stocks > stocks) return -1;
            if(other.stocks < stocks) return 1;
            
            if(other.health > health) return 1;
            if(other.health < health) return -1;
            return 0;
        }
        
        public static bool operator <(PlayerWinCondition a, PlayerWinCondition b) => a.CompareTo(b) < 0;
        public static bool operator >(PlayerWinCondition a, PlayerWinCondition b) => a.CompareTo(b) > 0;
        public static bool operator <=(PlayerWinCondition a, PlayerWinCondition b) => a.CompareTo(b) <= 0;
        public static bool operator >=(PlayerWinCondition a, PlayerWinCondition b) => a.CompareTo(b) >= 0;
    }
}