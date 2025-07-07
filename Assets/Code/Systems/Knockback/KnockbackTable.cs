using UnityEngine;
using Code.Helpers.Singleton;

namespace Code.Systems.Knockback
{
    public class KnockbackTable : Singleton<KnockbackTable>
    {
        [Header("Knockback Table")]
        [SerializeField] private KnockbackRow[] _table;
    
        public float GetKnockbackForce(int level)
        {
            float lastForce = _table[0].force;
            for(int i = 0; i < _table.Length; i++)
            {
                if (level <= _table[i].level)
                    return Mathf.Lerp(lastForce, _table[i].force, (float)(level - _table[i - 1].level) / (_table[i].level - _table[i - 1].level));
                lastForce = _table[i].force;
            }
            return lastForce;
        }
    }
}
