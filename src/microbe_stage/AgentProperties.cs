using System;
using System.Collections.Generic;
using Components;
using Newtonsoft.Json;

/// <summary>
///   Properties of an agent. Mainly used currently to block friendly fire
/// </summary>
public class AgentProperties
{
    public AgentProperties(Species species, Compound compound)
    {
        Species = species;
        Compound = compound;
    }

    public Species Species { get; set; }
    public string AgentType { get; set; } = "oxytoxy";
    public Compound Compound { get; set; }

    [JsonIgnore]
    public LocalizedString Name =>
        new("AGENT_NAME", new LocalizedString(Compound.GetUntranslatedName()));

    public void DealDamage(ref Health health, float toxinAmount)
    {
        if (health.Invulnerable)
        {
            // Consume this damage event if the target is not taking damage
            return;
        }

        var damage = Constants.OXYTOXY_DAMAGE * toxinAmount;

        // This should result in at least reasonable health even if thread race conditions hit here
        health.CurrentHealth = Math.Max(0, health.CurrentHealth - damage);

        var damageEvent = new DamageEventNotice(AgentType, damage);
        var damageList = health.RecentDamageReceived;

        if (damageList == null)
        {
            // Create new damage list, don't really care if due to data race some info is lost here so we don't
            // immediately set the list here and lock it
            damageList = new List<DamageEventNotice> { damageEvent };

            health.RecentDamageReceived = damageList;
        }
        else
        {
            lock (damageList)
            {
                damageList.Add(damageEvent);
            }
        }
    }

    public override string ToString()
    {
        return Name.ToString();
    }
}
