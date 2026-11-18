using System;
using System.Collections.Generic;
using System.Linq;
using AceTaffy;
using LOR_DiceSystem;

// Reception-only passives are kept separate from the obtainable librarian page.
public class PassiveAbility_1883201 : PassiveAbilityBase
{
    public override int SpeedDiceNumAdder()
    {
        // One native base die plus five starts the reception with six dice.
        // Preserve Speed III's existing emotion-3 bonus for this guest.
        return owner != null && owner.emotionDetail != null &&
            owner.emotionDetail.EmotionLevel >= 3 ? 6 : 5;
    }
}

// Separate guest identities reuse the established smoke/self-harm mechanics.
public class PassiveAbility_1883211 : PassiveAbility_1883101 { }
public class PassiveAbility_1883212 : PassiveAbility_1883102 { }
public class PassiveAbility_1883213 : PassiveAbility_1883103 { }

public class PassiveAbility_1883202 : PassiveAbilityBase
{
    private readonly HashSet<BattleUnitModel> _countedDeaths = new HashSet<BattleUnitModel>();
    private readonly List<TaffyInvention> _scheduled = new List<TaffyInvention>();
    private BattleUnitBuf_AceTaffyScheduledInventions _scheduledBuf;
    private BattleUnitBuf_AceTaffyInventionProgress _progress;
    private bool _initialized, _cleaning, _spawning;
    public int Progress { get { return _progress == null ? 0 : _progress.stack; } }
    public int ScheduledCount { get { return _scheduled.Count; } }
    public bool WorkshopDestroyed { get; private set; }
    public override int SpeedDiceBreakedAdder() { return WorkshopDestroyed ? 2 : 0; }
    public int DestroyedCount { get { return _countedDeaths.Count; } }

    public override void OnWaveStart()
    {
        if (_initialized || owner.faction != Faction.Enemy) return;
        _initialized = true;
        TaffyReceptionFormation.Ensure(Singleton<StageController>.Instance.GetCurrentWaveModel());
        _progress = new BattleUnitBuf_AceTaffyInventionProgress { stack = 0 };
        owner.bufListDetail.AddBuf(_progress);
        SyncGuard();
        if (TaffyInventions.Workshop(owner) != null) return;
        int index = TaffyInventions.FreeSlot(owner.faction);
        if (index < 0) throw new InvalidOperationException("No free workshop formation slot");
        var workshop = Singleton<StageController>.Instance.AddNewUnit(owner.faction,
            new LorId("AceTaffy", TaffyInventions.WorkshopEnemyId), index, 170);
        if (workshop == null) throw new InvalidOperationException("Steam workshop spawn failed");
        workshop.OnWaveStart();
        TaffyInventions.RefreshPortraits();
    }
    public override void OnRoundStart()
    {
        if (owner.IsDead() || _cleaning) return;
        // Grow only devices that were already present at the start of the scene.
        foreach (var unit in TaffyInventions.AliveUnits(owner))
        {
            var passive = TaffyInventions.UnitPassive(unit);
            if (passive != null) AddAura(passive.Device.Qualifier, unit, 1);
        }
        // Drain last scene's completed inventions before this scene's free progress.
        // A completion from the free point must wait for the following scene too.
        var arriving = _scheduled.ToArray();
        _scheduled.Clear();
        SyncScheduled();
        if (!WorkshopDestroyed)
            foreach (var device in arriving) SummonInvention(device);
        if (!WorkshopDestroyed && TaffyInventions.Workshop(owner) != null) AddProgress(1);
        RefreshSupport();
    }
    public void AddProgress(int count)
    {
        if (count <= 0 || owner == null || owner.IsDead() || _cleaning || WorkshopDestroyed) return;
        if (_progress == null)
        {
            _progress = new BattleUnitBuf_AceTaffyInventionProgress { stack = 0 };
            owner.bufListDetail.AddBuf(_progress);
        }
        for (int i = 0; i < count; i++)
        {
            if (++_progress.stack < 5) continue;
            _progress.stack = 0;
            CompleteInvention(new TaffyInvention((InventionQualifier)RandomUtil.Range(0, 4),
                (InventionKind)RandomUtil.Range(0, 4)));
        }
    }
    public void CompleteInvention(TaffyInvention device)
    {
        if (device == null || owner.IsDead() || _cleaning || _spawning || WorkshopDestroyed) return;
        // Reserve next-scene slots. Excess production grants support, never a
        // backlog that would auto-refill deaths in later scenes.
        if (TaffyInventions.AliveUnits(owner).Count + _scheduled.Count >= TaffyInventions.MaxActiveUnits)
        {
            var choices = new[] { KeywordBuf.Strength, KeywordBuf.Quickness, KeywordBuf.Protection };
            foreach (var ally in BattleObjectManager.instance.GetAliveList(owner.faction))
                ally.bufListDetail.AddBuf(new BattleUnitBuf_AceTaffyNextSceneSupport {
                    Reward = choices[RandomUtil.Range(0, choices.Length - 1)], Source = owner, stack = 3 });
            return;
        }
        _scheduled.Add(device);
        SyncScheduled();
    }
    private void SyncScheduled()
    {
        if (_scheduled.Count == 0)
        {
            if (_scheduledBuf != null) { _scheduledBuf.stack = 0; _scheduledBuf.Destroy(); _scheduledBuf = null; }
            return;
        }
        if (_scheduledBuf == null)
        {
            _scheduledBuf = new BattleUnitBuf_AceTaffyScheduledInventions();
            owner.bufListDetail.AddBuf(_scheduledBuf);
        }
        _scheduledBuf.stack = _scheduled.Count;
    }
    private void SummonInvention(TaffyInvention device)
    {
        int slot = TaffyInventions.FreeSlot(owner.faction);
        if (slot < 0) throw new InvalidOperationException("No free invention formation slot");
        _spawning = true;
        try
        {
            var unit = Singleton<StageController>.Instance.AddNewUnit(owner.faction,
                new LorId("AceTaffy", device.UnitId), slot, 170);
            if (unit == null) throw new InvalidOperationException("Invention summon failed");
            var passive = TaffyInventions.UnitPassive(unit);
            if (passive == null) throw new InvalidOperationException("Invention unit lacks passive: " + device.UnitId);
            passive.Creator = owner;
            unit.OnWaveStart();
            unit.OnRoundStartOnlyUI();
            unit.RollSpeedDice();
            AddAura(device.Qualifier, unit, 1);
            RefreshSupport();
            TaffyInventions.RefreshPortraits();
            AceTaffyMod.Log("invention unit summoned " + device.Key + " in slot " + slot);
        }
        finally { _spawning = false; }
    }
    private void AddAura(InventionQualifier qualifier, BattleUnitModel source, int amount)
    {
        // Every invention owns its aura instance, so its accumulated layers can
        // be removed in full without touching layers granted by other devices.
        var buf = TaffyInventions.Aura(owner, source);
        if (buf == null)
        {
            if (amount <= 0 || source == null) return;
            buf = new BattleUnitBuf_AceTaffyInventionAura { Qualifier = qualifier, Source = source, stack = 0 };
            owner.bufListDetail.AddBuf(buf);
        }
        buf.stack = Math.Max(0, buf.stack + amount);
        if (buf.stack == 0) buf.Destroy();
    }
    private void RemoveAura(BattleUnitModel source)
    {
        var buf = TaffyInventions.Aura(owner, source);
        if (buf == null) return;
        buf.stack = 0;
        buf.Destroy();
    }
    public void RefreshSupport()
    {
        foreach (var ally in BattleObjectManager.instance.GetAliveList(owner.faction))
        {
            var buf = ally.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyDeviceSupport>()
                .FirstOrDefault(x => !x.IsDestroyed() && x.Creator == owner);
            if (buf == null)
                ally.bufListDetail.AddBuf(new BattleUnitBuf_AceTaffyDeviceSupport { Creator = owner, stack = 1 });
        }
    }
    private void LoseWorkshop()
    {
        if (WorkshopDestroyed) return;
        WorkshopDestroyed = true;
        _scheduled.Clear();
        SyncScheduled();
        if (_progress != null) { _progress.stack = 0; _progress.Destroy(); }
        // Cancel remaining inventions immediately, including pages queued before the workshop died.
        foreach (var page in owner.cardSlotDetail.cardAry)
            if (page != null && TaffyInventions.IsInventCard(page.card)) page.DestroyPlayingCard();
        int broken = 0;
        for (int i = 0; i < owner.speedDiceResult.Count && broken < 2; i++)
        {
            if (owner.speedDiceResult[i].breaked) continue;
            owner.speedDiceResult[i].breaked = true;
            owner.speedDiceResult[i].value = 0;
            if (i < owner.cardSlotDetail.cardAry.Count && owner.cardSlotDetail.cardAry[i] != null)
                owner.cardSlotDetail.cardAry[i].DestroyPlayingCard();
            broken++;
        }
        owner.allyCardDetail.ExhaustAllCards();
        if (owner.view != null && owner.view.speedDiceSetterUI != null)
            owner.view.speedDiceSetterUI.SetSpeedDicesAfterRoll(owner.speedDiceResult);
    }
    public void SyncGuard()
    {
        if (!owner.passiveDetail.HasPassive<PassiveAbility_1883206>()) return;
        int protection = Math.Max(0, 8 - DestroyedCount);
        int vulnerable = Math.Max(0, DestroyedCount - 8);
        var guard = owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyManufactureGuard>().FirstOrDefault(x => !x.IsDestroyed());
        var broken = owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_AceTaffyManufactureVulnerable>().FirstOrDefault(x => !x.IsDestroyed());
        if (protection > 0)
        {
            if (guard == null) { guard = new BattleUnitBuf_AceTaffyManufactureGuard(); owner.bufListDetail.AddBuf(guard); }
            guard.stack = protection;
        }
        else if (guard != null) { guard.stack = 0; guard.Destroy(); }
        if (vulnerable > 0)
        {
            if (broken == null) { broken = new BattleUnitBuf_AceTaffyManufactureVulnerable(); owner.bufListDetail.AddBuf(broken); }
            broken.stack = vulnerable;
        }
    }
    public override void OnDieOtherUnit(BattleUnitModel unit)
    {
        if (_cleaning || owner.IsDead() || unit == null || !unit.IsDeadReal()) return;
        if (unit.faction == owner.faction && TaffyInventions.IsWorkshop(unit)) { LoseWorkshop(); return; }
        var invention = TaffyInventions.UnitPassive(unit);
        if (
            invention == null || invention.Creator != owner || !_countedDeaths.Add(unit)) return;
        SyncGuard();
        RemoveAura(unit);
        // A destroyed device leaves an empty slot until a new invention completes.
    }
    public override void OnEndBattle(BattlePlayingCardDataInUnitModel curCard)
    { if (!WorkshopDestroyed) TaffyInventions.EndImprovise(curCard); }
    public override BattleUnitModel ChangeAttackTarget(BattleDiceCardModel card, int idx)
    { return TaffyInventions.IsInventCard(card) ? TaffyInventions.Workshop(owner) : null; }
    public override void OnDie()
    {
        _cleaning = true;
        _scheduled.Clear();
        SyncScheduled();
        foreach (var invention in TaffyInventions.AliveUnits(owner)) invention.Die();
        var workshop = TaffyInventions.Workshop(owner);
        if (workshop != null) workshop.Die();
    }
}

public class PassiveAbility_1883206 : PassiveAbilityBase
{
    public override void OnWaveStart()
    { var inventor = TaffyInventions.Inventor(owner); if (inventor != null) inventor.SyncGuard(); }
}

public class PassiveAbility_1883203 : PassiveAbilityBase
{
    public override void OnAfterRollSpeedDice()
    {
        if (owner.faction != Faction.Enemy) return;
        owner.allyCardDetail.ExhaustAllCards();
        int slots = Math.Min(9, owner.speedDiceResult.Count(x => !x.breaked));
        int budget = Math.Max(0, owner.cardSlotDetail.PlayPoint);
        int priority = 1000;
        int used = 0;
        if (slots > 0 && TaffyInventions.Workshop(owner) != null)
        {
            var inventionPages = new List<BattleDiceCardModel>();
            for (int i = 0; i < Math.Min(2, slots); i++)
            {
                var page = owner.allyCardDetail.AddTempCard(new LorId("AceTaffy", TaffyInventions.InventCardId));
                if (page == null) throw new InvalidOperationException("Failed to deal reception card " + TaffyInventions.InventCardId);
                inventionPages.Add(page);
            }
            // Two free pages always occupy the two slowest dice so the
            // high-speed dice remain available for the random pages.
            AssignLowestDice(inventionPages);
            used = inventionPages.Count;
        }
        for (; used < slots; used++)
        {
            var candidates = new List<DiceCardXmlInfo>();
            for (int id = 1883001; id <= 1883009; id++)
            {
                var xml = ItemXmlDataList.instance.GetCardItem(new LorId("AceTaffy", id), false);
                if (xml != null && xml.Spec.Cost <= budget) candidates.Add(xml);
            }
            if (candidates.Count == 0) break;
            var chosen = candidates[RandomUtil.Range(0, candidates.Count - 1)];
            Add(chosen.id.id, priority);
            priority -= 100;
            budget -= chosen.Spec.Cost;
        }
    }

    private void AssignLowestDice(List<BattleDiceCardModel> pages)
    {
        if (pages.Count == 0 || owner.cardSlotDetail == null || owner.cardSlotDetail.cardAry == null) return;
        // Native speed dice are sorted high to low and broken dice sit at the
        // front, so the last non-broken indices are the two slowest slots.
        var slots = new List<int>();
        for (int i = owner.speedDiceResult.Count - 1; i >= 0 && slots.Count < pages.Count; i--)
            if (!owner.speedDiceResult[i].breaked) slots.Add(i);
        slots.Reverse();
        for (int i = 0; i < slots.Count; i++)
        {
            var page = pages[i];
            var target = BattleObjectManager.instance.GetTargetByCard(owner, page, slots[i], owner.TeamKill());
            if (target == null) continue;
            int targetSlot = target.speedDiceResult != null && target.speedDiceResult.Count > 0
                ? RandomUtil.Range(0, target.speedDiceResult.Count - 1) : 0;
            targetSlot = owner.ChangeTargetSlot(page, target, slots[i], targetSlot, owner.TeamKill());
            owner.SetCurrentOrder(slots[i]);
            owner.cardSlotDetail.AddCard(page, target, targetSlot, false);
        }
        owner.SetCurrentOrder(-1);
    }

    private void Add(int id, int priority)
    {
        var card = owner.allyCardDetail.AddTempCard(new LorId("AceTaffy", id));
        if (card == null) throw new InvalidOperationException("Failed to deal reception card " + id);
        // Preserve original costs; only the authored zero-cost invention is free.
        card.SetPriorityAdder(priority - card.XmlData.Priority);
    }
}

public class PassiveAbility_1883204 : PassiveAbilityBase
{
    public override bool isActionable { get { return false; } }
    public override bool isStraighten { get { return true; } }
    public override bool isInvincibleBp { get { return true; } }
    public override bool IsImmuneBreakDmg(DamageType type) { return true; }
    public override bool OnBreakGageZero() { return true; }
    public override void OnTakeDamageByAttack(BattleDiceBehavior atkDice, int dmg)
    {
        foreach (var ally in BattleObjectManager.instance.GetAliveList(owner.faction))
        {
            var inventor = TaffyInventions.Inventor(ally);
            if (inventor == null) continue;
            ally.breakDetail.RecoverBreak(10);
            inventor.AddProgress(1);
        }
    }
}
