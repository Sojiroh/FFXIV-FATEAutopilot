using System;
using System.Collections.Generic;
using System.Text;
using AutoFateGrind.Core.Zones;
using Dalamud.Hooking;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace AutoFateGrind.Core.Game.SharedFates;

internal static class SharedFateProgress
{
    private struct Slot
    {
        public uint Current;

        public uint Max;

        public long ReceivedAtMs;

        public long RequestedAtMs;

        public long RefreshDueAtMs;

        public int LocalCompletions;
    }

    private const int RequestCooldownMs = 10000;

    private const int RefreshAfterFateMs = 3000;

    private static readonly Slot[] slots = new Slot[SharedFateCatalog.Entries.Length];

    private static readonly long[] tabRequestedAtMs = new long[SharedFateCatalog.TabCount];

    private static Hook<Achievement.Delegates.ReceiveAchievementProgress>? receiveHook;

    public static bool Available => (receiveHook == null) is false;

    public unsafe static void Initialize()
    {
        if (receiveHook != null)
        {
            return;
        }
        try
        {
            receiveHook = Svc.Hook.HookFromAddress<Achievement.Delegates.ReceiveAchievementProgress>(Achievement.Addresses.ReceiveAchievementProgress.Value, ReceiveDetour);
            receiveHook.Enable();
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "Could not hook achievement progress; Shared FATE ranks stay unknown");
            receiveHook = null;
        }
        Svc.ClientState.Logout += OnLogout;
    }

    public static void Shutdown()
    {
        Svc.ClientState.Logout -= OnLogout;
        receiveHook?.Disable();
        receiveHook?.Dispose();
        receiveHook = null;
        Forget();
    }

    public static bool TryGet(uint territoryId, out SharedFateRank rank)
    {
        var slotIndex = SharedFateCatalog.IndexOfTerritory(territoryId);
        if (slotIndex < 0 || slots[slotIndex].ReceivedAtMs == 0L)
        {
            rank = default;
            return false;
        }
        rank = Resolve(slotIndex);
        return true;
    }

    public static bool IsMaxed(uint territoryId) => TryGet(territoryId, out var rank) && rank.IsMaxed;

    public static void Request(uint territoryId, bool force = false)
    {
        var slotIndex = SharedFateCatalog.IndexOfTerritory(territoryId);
        if (slotIndex < 0 || !CanRequest())
        {
            return;
        }
        var tickCount = Environment.TickCount64;
        RequestTab(SharedFateCatalog.Entries[slotIndex].TabIndex, tickCount, force);
        RequestAchievement(slotIndex, tickCount, force);
    }

    public static void Request(IReadOnlyList<ZoneInfo> zones, bool force = false)
    {
        for (var i = 0; i < zones.Count; i++)
        {
            Request(zones[i].TerritoryId, force);
        }
    }

    public static void Request(IReadOnlyList<uint> territoryIds, bool force = false)
    {
        for (var i = 0; i < territoryIds.Count; i++)
        {
            Request(territoryIds[i], force);
        }
    }

    public static void NoteCompletion(uint territoryId)
    {
        var slotIndex = SharedFateCatalog.IndexOfTerritory(territoryId);
        if (slotIndex < 0)
        {
            return;
        }
        ref var slot = ref slots[slotIndex];
        if (slot.ReceivedAtMs != 0L && slot.Current + slot.LocalCompletions < Math.Max(slot.Max, 1u))
        {
            slot.LocalCompletions++;
        }
        slot.RefreshDueAtMs = Environment.TickCount64 + RefreshAfterFateMs;
    }

    public static void Tick()
    {
        var tickCount = Environment.TickCount64;
        for (var i = 0; i < slots.Length; i++)
        {
            ref var slot = ref slots[i];
            if (slot.RefreshDueAtMs != 0L && tickCount >= slot.RefreshDueAtMs)
            {
                slot.RefreshDueAtMs = 0L;
                if (CanRequest())
                {
                    RequestTab(SharedFateCatalog.Entries[i].TabIndex, tickCount, force: true);
                    RequestAchievement(i, tickCount, force: true);
                }
            }
        }
    }

    public static bool AllKnown(IReadOnlyList<ZoneInfo> zones)
    {
        for (var i = 0; i < zones.Count; i++)
        {
            var slotIndex = SharedFateCatalog.IndexOfTerritory(zones[i].TerritoryId);
            if (slotIndex >= 0 && slots[slotIndex].ReceivedAtMs == 0L)
            {
                return false;
            }
        }
        return true;
    }

    public static bool AllMaxed(IReadOnlyList<ZoneInfo> zones)
    {
        var rankedCount = 0;
        for (var i = 0; i < zones.Count; i++)
        {
            if (SharedFateCatalog.HasRanks(zones[i].TerritoryId))
            {
                rankedCount++;
                if (!IsMaxed(zones[i].TerritoryId))
                {
                    return false;
                }
            }
        }
        return rankedCount > 0;
    }

    public static int CountUnmaxed(IReadOnlyList<ZoneInfo> zones)
    {
        var unmaxedCount = 0;
        for (var i = 0; i < zones.Count; i++)
        {
            if (SharedFateCatalog.HasRanks(zones[i].TerritoryId) && !IsMaxed(zones[i].TerritoryId))
            {
                unmaxedCount++;
            }
        }
        return unmaxedCount;
    }

    public static (int Maxed, int Ranked) CountMaxed(IReadOnlyList<uint> territoryIds)
    {
        var maxed = 0;
        var ranked = 0;
        for (var i = 0; i < territoryIds.Count; i++)
        {
            if (SharedFateCatalog.HasRanks(territoryIds[i]))
            {
                ranked++;
                if (IsMaxed(territoryIds[i]))
                {
                    maxed++;
                }
            }
        }
        return (Maxed: maxed, Ranked: ranked);
    }

    public static string Describe(ZoneInfo zone)
    {
        var slotIndex = SharedFateCatalog.IndexOfTerritory(zone.TerritoryId);
        if (slotIndex < 0)
        {
            return zone.Name + ": no ranks";
        }
        if (!TryGet(zone.TerritoryId, out var rank))
        {
            return zone.Name + ": unknown";
        }
        var localCompletions = slots[slotIndex].LocalCompletions;
        var unconfirmedNote = localCompletions > 0 ? $" (+{localCompletions} unconfirmed)" : string.Empty;
        if (!rank.IsMaxed)
        {
            return $"{zone.Name}: rank {rank.Rank}/{rank.MaxRank}, {rank.RankProgress}/{rank.RankSize} to next, {rank.Completed}/{rank.Total} total{unconfirmedNote}";
        }
        return $"{zone.Name}: rank {rank.Rank}/{rank.MaxRank} maxed{unconfirmedNote}";
    }

    public static string Describe(IReadOnlyList<ZoneInfo> zones)
    {
        var builder = new StringBuilder(zones.Count * 48);
        for (var i = 0; i < zones.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(" | ");
            }
            builder.Append(Describe(zones[i]));
        }
        return builder.ToString();
    }

    private static SharedFateRank Resolve(int index)
    {
        ref var slot = ref slots[index];
        return SharedFateCatalog.Resolve(index, (int)slot.Current + slot.LocalCompletions, (int)slot.Max);
    }

    private unsafe static bool CanRequest() => receiveHook != null && Svc.ClientState.IsLoggedIn && UIState.Instance() != null;

    private unsafe static void RequestTab(byte tabIndex, long now, bool force)
    {
        if (!force && now - tabRequestedAtMs[tabIndex] < RequestCooldownMs)
        {
            return;
        }
        tabRequestedAtMs[tabIndex] = now;
        try
        {
            UIState.Instance()->Achievement.RequestFateProgressTab(tabIndex);
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, $"Shared FATE tab {tabIndex} request failed");
        }
    }

    private static void RequestAchievement(int index, long now, bool force)
    {
        ref var slot = ref slots[index];
        if (!force && now - slot.RequestedAtMs < RequestCooldownMs)
        {
            return;
        }
        slot.RequestedAtMs = now;
        try
        {
            GameMain.ExecuteCommand(1000, (int)SharedFateCatalog.Entries[index].AchievementId);
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, $"Shared FATE achievement {SharedFateCatalog.Entries[index].AchievementId} request failed");
        }
    }

    private unsafe static void ReceiveDetour(Achievement* thisPtr, uint id, uint current, uint max)
    {
        receiveHook!.Original(thisPtr, id, current, max);
        try
        {
            var slotIndex = SharedFateCatalog.IndexOfAchievement(id);
            if (slotIndex < 0)
            {
                return;
            }
            ref var slot = ref slots[slotIndex];
            slot.Current = current;
            slot.Max = max;
            slot.ReceivedAtMs = Environment.TickCount64;
            slot.LocalCompletions = 0;
            RunLog.Debug($"Shared FATE progress received for territory {SharedFateCatalog.Entries[slotIndex].TerritoryId}: {current}/{max}");
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "Shared FATE progress receive failed");
        }
    }

    private static void OnLogout(int type, int code)
    {
        Forget();
    }

    private static void Forget()
    {
        Array.Clear(slots);
        Array.Clear(tabRequestedAtMs);
    }
}
