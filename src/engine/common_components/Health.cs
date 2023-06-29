namespace Components
{
    using System.Collections.Generic;

    /// <summary>
    ///   Things that have a health and can be damaged
    /// </summary>
    public struct Health
    {
        public List<DamageEventNotice>? RecentDamageReceived;

        public float CurrentHealth;
        public float MaxHealth;

        public bool Invulnerable;
    }

    /// <summary>
    ///   Notice to an entity that it took damage. Used for example to play sounds or other feedback about taking
    ///   damage
    /// </summary>
    public class DamageEventNotice
    {
        public string DamageSource;
        public float Amount;

        public DamageEventNotice(string damageSource, float amount)
        {
            DamageSource = damageSource;
            Amount = amount;
        }
    }
}
