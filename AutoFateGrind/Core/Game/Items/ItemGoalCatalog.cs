using AutoFateGrind.Core.Localization;

namespace AutoFateGrind.Core.Game.Items;

internal readonly record struct ItemGoalDrop(uint ItemId, uint[] ZoneIds, int PerWeapon);

internal enum ItemGoalGate : byte
{
    None,
    QuestAccepted,
    ZenithEquipped
}

internal sealed class ItemGoalDefinition
{
    public required string ModeId { get; init; }

    public required string DisplayName { get; init; }

    public required LocString Name { get; init; }

    public required LocString GoalToken { get; init; }

    public required LocString Note { get; init; }

    public required ItemGoalDrop[] Drops { get; init; }

    public ItemGoalGate Gate { get; init; }

    public uint QuestId { get; init; }
}

internal static class ItemGoalCatalog
{
    public const string AtmaModeId = "relicatma";

    public const string LuminousModeId = "relicluminous";

    public const string MemoriesModeId = "relicmemories";

    public const string LawsOrderModeId = "reliclawsorder";

    public const string DemiatmaModeId = "phantomdemiatma";

    public const string PasteModeId = "phantompaste";

    public const int MaxWeaponCount = 20;

    private static readonly uint[] DawntrailZones = new uint[] { 1187u, 1188u, 1189u, 1190u, 1191u, 1192u };

    public static readonly ItemGoalDefinition[] Definitions = new ItemGoalDefinition[]
    {
        new ItemGoalDefinition
        {
            ModeId = AtmaModeId,
            DisplayName = "Atma (Zodiac)",
            Name = L.Grind.ItemGoalAtma,
            GoalToken = L.Grind.GoalAtma,
            Note = L.Grind.NoteAtma,
            Gate = ItemGoalGate.ZenithEquipped,
            QuestId = 66971u,
            Drops = new ItemGoalDrop[]
            {
                new ItemGoalDrop(7851u, new uint[] { 148u }, 1),
                new ItemGoalDrop(7852u, new uint[] { 146u }, 1),
                new ItemGoalDrop(7853u, new uint[] { 139u }, 1),
                new ItemGoalDrop(7854u, new uint[] { 152u }, 1),
                new ItemGoalDrop(7855u, new uint[] { 145u }, 1),
                new ItemGoalDrop(7856u, new uint[] { 134u }, 1),
                new ItemGoalDrop(7857u, new uint[] { 140u }, 1),
                new ItemGoalDrop(7858u, new uint[] { 180u }, 1),
                new ItemGoalDrop(7859u, new uint[] { 135u }, 1),
                new ItemGoalDrop(7860u, new uint[] { 154u }, 1),
                new ItemGoalDrop(7861u, new uint[] { 141u }, 1),
                new ItemGoalDrop(7862u, new uint[] { 138u }, 1)
            }
        },
        new ItemGoalDefinition
        {
            ModeId = LuminousModeId,
            DisplayName = "Luminous Crystals (Anima)",
            Name = L.Grind.ItemGoalLuminous,
            GoalToken = L.Grind.GoalLuminous,
            Note = L.Grind.NoteLuminous,
            Drops = new ItemGoalDrop[]
            {
                new ItemGoalDrop(13569u, new uint[] { 397u }, 1),
                new ItemGoalDrop(13570u, new uint[] { 401u }, 1),
                new ItemGoalDrop(13571u, new uint[] { 402u }, 1),
                new ItemGoalDrop(13572u, new uint[] { 398u }, 1),
                new ItemGoalDrop(13573u, new uint[] { 400u }, 1),
                new ItemGoalDrop(13574u, new uint[] { 399u }, 1)
            }
        },
        new ItemGoalDefinition
        {
            ModeId = MemoriesModeId,
            DisplayName = "Memories of the Dying (Resistance)",
            Name = L.Grind.ItemGoalMemories,
            GoalToken = L.Grind.GoalMemories,
            Note = L.Grind.NoteMemories,
            Drops = new ItemGoalDrop[]
            {
                new ItemGoalDrop(31573u, new uint[] { 397u, 401u }, 20),
                new ItemGoalDrop(31574u, new uint[] { 398u, 400u }, 20),
                new ItemGoalDrop(31575u, new uint[] { 399u, 402u }, 20)
            }
        },
        new ItemGoalDefinition
        {
            ModeId = LawsOrderModeId,
            DisplayName = "Haunting and Vexatious Memories (Resistance)",
            Name = L.Grind.ItemGoalLawsOrder,
            GoalToken = L.Grind.GoalLawsOrder,
            Note = L.Grind.NoteLawsOrder,
            Gate = ItemGoalGate.QuestAccepted,
            QuestId = 69575u,
            Drops = new ItemGoalDrop[]
            {
                new ItemGoalDrop(32957u, new uint[] { 612u, 620u, 621u }, 18),
                new ItemGoalDrop(32958u, new uint[] { 613u, 614u, 622u }, 18)
            }
        },
        new ItemGoalDefinition
        {
            ModeId = DemiatmaModeId,
            DisplayName = "Demiatma (Phantom)",
            Name = L.Grind.ItemGoalDemiatma,
            GoalToken = L.Grind.GoalDemiatma,
            Note = L.Grind.NoteDemiatma,
            Gate = ItemGoalGate.QuestAccepted,
            QuestId = 70855u,
            Drops = new ItemGoalDrop[]
            {
                new ItemGoalDrop(47744u, new uint[] { 1187u }, 3),
                new ItemGoalDrop(47745u, new uint[] { 1188u }, 3),
                new ItemGoalDrop(47746u, new uint[] { 1189u }, 3),
                new ItemGoalDrop(47747u, new uint[] { 1190u }, 3),
                new ItemGoalDrop(47748u, new uint[] { 1191u }, 3),
                new ItemGoalDrop(47749u, new uint[] { 1192u }, 3)
            }
        },
        new ItemGoalDefinition
        {
            ModeId = PasteModeId,
            DisplayName = "Crystal Paste (Phantom)",
            Name = L.Grind.ItemGoalPaste,
            GoalToken = L.Grind.GoalPaste,
            Note = L.Grind.NotePaste,
            Gate = ItemGoalGate.QuestAccepted,
            QuestId = 70991u,
            Drops = new ItemGoalDrop[]
            {
                new ItemGoalDrop(50059u, DawntrailZones, 1200)
            }
        }
    };

    public static ItemGoalDefinition? Find(string? modeId)
    {
        if (modeId == null)
        {
            return null;
        }
        for (var i = 0; i < Definitions.Length; i++)
        {
            if (Definitions[i].ModeId == modeId)
            {
                return Definitions[i];
            }
        }
        return null;
    }

    public static bool IsItemGoal(string? modeId) => (Find(modeId) == null) is false;
}
