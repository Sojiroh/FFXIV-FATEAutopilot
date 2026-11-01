namespace AutoFateGrind.Core.Localization;

internal static class L
{
    internal static class Common
    {
        public static readonly LocString Close = new LocString("common.close", "Close");

        public static readonly LocString Cancel = new LocString("common.cancel", "Cancel");

        public static readonly LocString Remove = new LocString("common.remove", "Remove");

        public static readonly LocString Add = new LocString("common.add", "Add");

        public static readonly LocString Clear = new LocString("common.clear", "Clear");

        public static readonly LocString SelectAll = new LocString("common.selectAll", "Select all");

        public static readonly LocString MoveUp = new LocString("common.moveUp", "Move up");

        public static readonly LocString MoveDown = new LocString("common.moveDown", "Move down");

        public static readonly LocString Ascending = new LocString("common.ascending", "Ascending");

        public static readonly LocString Descending = new LocString("common.descending", "Descending");

        public static readonly LocString Pause = new LocString("common.pause", "Pause");

        public static readonly LocString Resume = new LocString("common.resume", "Resume");

        public static readonly LocString StopRun = new LocString("common.stopRun", "Stop the run");

        public static readonly LocString Working = new LocString("common.working", "Working…");

        public static readonly LocString PlayerNotLoaded = new LocString("common.playerNotLoaded", "Player not loaded.");

        public static readonly LocString DragAdjustHint = new LocString("common.dragAdjustHint", "Drag to adjust · Ctrl+click to type");

        public static readonly LocString Preview = new LocString("common.preview", "Preview");

        public static readonly LocString Test = new LocString("common.test", "Test");

        public static readonly LocString NoMatches = new LocString("common.noMatches", "Nothing matches “{0}”.");
    }

    internal static class Shell
    {
        public static readonly LocString NavGrind = new LocString("shell.nav.grind", "Grind");

        public static readonly LocString NavSettings = new LocString("shell.nav.settings", "Settings");

        public static readonly LocString NavHistory = new LocString("shell.nav.history", "History");

        public static readonly LocString NavPlugins = new LocString("shell.nav.plugins", "Plugins");

        public static readonly LocString NavLog = new LocString("shell.nav.log", "Console");

        public static readonly LocString NavChangelog = new LocString("shell.nav.changelog", "Changelog");

        public static readonly LocString NavAbout = new LocString("shell.nav.about", "About");

        public static readonly LocString StatusRunning = new LocString("shell.status.running", "Running");

        public static readonly LocString StatusPaused = new LocString("shell.status.paused", "Paused");

        public static readonly LocString StatusReady = new LocString("shell.status.ready", "Ready");

        public static readonly LocString StatusPickZones = new LocString("shell.status.pickZones", "Pick zones");

        public static readonly LocString StatusNothingToFarm = new LocString("shell.status.nothingToFarm", "Nothing to farm");

        public static readonly LocString StatusSetupNeeded = new LocString("shell.status.setupNeeded", "Setup needed");

        public static readonly LocString StatusBlocked = new LocString("shell.status.blocked", "Blocked");

        public static readonly LocString StatusIdle = new LocString("shell.status.idle", "Idle");

        public static readonly LocString Minimize = new LocString("shell.minimize", "Minimize to the title strip");

        public static readonly LocString Restore = new LocString("shell.restore", "Restore the window");

        public static readonly LocString ShowLiveTracker = new LocString("shell.showLiveTracker", "Show the live FATE tracker");

        public static readonly LocString HideLiveTracker = new LocString("shell.hideLiveTracker", "Hide the live FATE tracker");

        public static readonly LocString ResumeBlocked = new LocString("shell.resumeBlocked", "Resumes automatically once you leave the duty");

        public static readonly LocString Wallet = new LocString("shell.wallet", "Bicolor Gemstones: {0} / {1}");

        public static readonly LocString WalletCapped = new LocString("shell.walletCapped", "At the cap. Gemstones from further FATEs are lost until you spend some.");

        public static readonly LocString WalletTradeReady = new LocString("shell.walletTradeReady", "At or above your auto-trade threshold ({0}).");

        public static readonly LocString GreetingMorning = new LocString("shell.greeting.morning", "Good morning");

        public static readonly LocString GreetingAfternoon = new LocString("shell.greeting.afternoon", "Good afternoon");

        public static readonly LocString GreetingEvening = new LocString("shell.greeting.evening", "Good evening");

        public static readonly LocString GreetingNight = new LocString("shell.greeting.night", "Late night");
    }

    internal static class Grind
    {
        public static readonly LocString TitleSetupNeeded = new LocString("grind.title.setupNeeded", "Setup needed");

        public static readonly LocString DetailSetupNeeded = new LocString("grind.detail.setupNeeded", "Install the required plugins before your first run.");

        public static readonly LocString TitlePickZones = new LocString("grind.title.pickZones", "Pick a zone to begin");

        public static readonly LocString DetailPickZones = new LocString("grind.detail.pickZones", "Tick zones in the library and they'll appear in your plan.");

        public static readonly LocString TitleNoYokai = new LocString("grind.title.noYokai", "No yo-kai left to farm");

        public static readonly LocString DetailNoYokai = new LocString("grind.detail.noYokai", "Tick more yo-kai below or raise the medal target.");

        public static readonly LocString TitleNoRankedZones = new LocString("grind.title.noRankedZones", "Pick a Shadowbringers or later zone");

        public static readonly LocString DetailNoRankedZones = new LocString("grind.detail.noRankedZones", "Only zones from Shadowbringers onward have Shared FATE ranks.");

        public static readonly LocString TitleRanksMaxed = new LocString("grind.title.ranksMaxed", "Every selected zone is maxed");

        public static readonly LocString DetailRanksMaxed = new LocString("grind.detail.ranksMaxed", "Tick zones whose Shared FATE rank still has room to grow.");

        public static readonly LocString TitleNeedQuest = new LocString("grind.title.needQuest", "Pick up the quest first");

        public static readonly LocString DetailNeedQuest = new LocString("grind.detail.needQuest", "These items only drop while the quest \"{0}\" is active.");

        public static readonly LocString TitleNeedZenith = new LocString("grind.title.needZenith", "Equip a Zenith weapon first");

        public static readonly LocString DetailNeedZenith = new LocString("grind.detail.needZenith", "Atma only drop with a Zodiac Zenith weapon equipped.");

        public static readonly LocString TitleAllCollected = new LocString("grind.title.allCollected", "Everything is collected");

        public static readonly LocString DetailAllCollected = new LocString("grind.detail.allCollected", "Every item for this goal is in your bags. Raise the number of weapons or pick another goal.");

        public static readonly LocString TitleReady = new LocString("grind.title.ready", "Ready to grind");

        public static readonly LocString DetailReady = new LocString("grind.detail.ready", "Everything's set. Press Start whenever you're ready.");

        public static readonly LocString TitleRunning = new LocString("grind.title.running", "Grinding");

        public static readonly LocString TitlePaused = new LocString("grind.title.paused", "Paused");

        public static readonly LocString DetailPausedInContent = new LocString("grind.detail.pausedInContent", "Resumes once you leave the duty");

        public static readonly LocString DetailPausedManual = new LocString("grind.detail.pausedManual", "Resume whenever you're ready");

        public static readonly LocString OpenPlugins = new LocString("grind.openPlugins", "Open plugins");

        public static readonly LocString PickForMe = new LocString("grind.pickForMe", "Pick for me");

        public static readonly LocString PickForMeHint = new LocString("grind.pickForMeHint", "Selects every unlocked zone of your newest expansion that fits this goal.");

        public static readonly LocString NoRunsYet = new LocString("grind.noRunsYet", "No runs yet");

        public static readonly LocString StatsAppearHere = new LocString("grind.statsAppearHere", "your stats will appear here");

        public static readonly LocString LastRun = new LocString("grind.lastRun", "Last run  ·  {0} FATEs");

        public static readonly LocString LastRunDetail = new LocString("grind.lastRunDetail", "{0}  ·  {1} gems");

        public static readonly LocString GoalTitle = new LocString("grind.goalTitle", "Goal");

        public static readonly LocString CatFarm = new LocString("grind.cat.farm", "Farm");

        public static readonly LocString CatRanks = new LocString("grind.cat.ranks", "Ranks");

        public static readonly LocString CatRelics = new LocString("grind.cat.relics", "Relics");

        public static readonly LocString CatEvents = new LocString("grind.cat.events", "Events");

        public static readonly LocString CardGemstones = new LocString("grind.card.gemstones", "Bicolor Gemstones");

        public static readonly LocString CardPlain = new LocString("grind.card.plain", "Just FATEs");

        public static readonly LocString CardShared = new LocString("grind.card.shared", "Shared FATE ranks");

        public static readonly LocString CardYokai = new LocString("grind.card.yokai", "Yo-kai Watch medals");

        public static readonly LocString StatusGems = new LocString("grind.status.gems", "{0} / {1} gemstones");

        public static readonly LocString StatusPlain = new LocString("grind.status.plain", "no target of its own");

        public static readonly LocString StatusRanks = new LocString("grind.status.ranks", "{0} of {1} zones maxed");

        public static readonly LocString StatusRanksNoZones = new LocString("grind.status.ranksNoZones", "needs Shadowbringers or later zones");

        public static readonly LocString StatusQuestMissing = new LocString("grind.status.questMissing", "quest not active");

        public static readonly LocString StatusZenithMissing = new LocString("grind.status.zenithMissing", "Zenith weapon not equipped");

        public static readonly LocString NotePlain = new LocString("grind.note.plain", "No target of its own. A limit below, or the Stop button, ends the run.");

        public static readonly LocString HowLong = new LocString("grind.howLong", "How long");

        public static readonly LocString UntilGoalDone = new LocString("grind.untilGoalDone", "Until the goal is done");

        public static readonly LocString UntilYouStopIt = new LocString("grind.untilYouStopIt", "Until you stop it");

        public static readonly LocString ANumberOfFates = new LocString("grind.aNumberOfFates", "A number of FATEs");

        public static readonly LocString ALengthOfTime = new LocString("grind.aLengthOfTime", "A length of time");

        public static readonly LocString ANumberOfLevels = new LocString("grind.aNumberOfLevels", "A number of levels");

        public static readonly LocString Afterwards = new LocString("grind.afterwards", "Afterwards");

        public static readonly LocString EachZone = new LocString("grind.eachZone", "Each zone");

        public static readonly LocString ZoneAsManyAsItFinds = new LocString("grind.zoneAsManyAsItFinds", "As many as it finds");

        public static readonly LocString AfterStayShort = new LocString("grind.after.stay.short", "Stay put");

        public static readonly LocString AfterInnShort = new LocString("grind.after.inn.short", "Go to the inn");

        public static readonly LocString AfterLogoutShort = new LocString("grind.after.logout.short", "Log out");

        public static readonly LocString AfterCloseShort = new LocString("grind.after.close.short", "Close the game");

        public static readonly LocString Summary = new LocString("grind.summary", "{0} in {1} until {2}, then {3}.");

        public static readonly LocString SummaryGems = new LocString("grind.summary.gems", "Farm Bicolor Gemstones");

        public static readonly LocString SummaryPlain = new LocString("grind.summary.plain", "Grind FATEs");

        public static readonly LocString SummaryShared = new LocString("grind.summary.shared", "Max Shared FATE ranks");

        public static readonly LocString SummaryCollect = new LocString("grind.summary.collect", "Collect {0}");

        public static readonly LocString SummaryYokai = new LocString("grind.summary.yokai", "Farm Yo-kai medals");

        public static readonly LocString Or = new LocString("grind.or", " or ");

        public static readonly LocString UntilShort = new LocString("grind.untilShort", "until {0}");

        public static readonly LocString SentenceEnd = new LocString("grind.sentence.end", ".");

        public static readonly LocString ZonesYokaiNone = new LocString("grind.zonesYokaiNone", "no yo-kai to farm");

        public static readonly LocPlural ZonesCount = new LocPlural("grind.zonesCount", "{0} zone", "{0} zones");

        public static readonly LocString GoalGemstones = new LocString("grind.goal.gemstones", "{0} gemstones");

        public static readonly LocString GoalFates = new LocString("grind.goal.fates", "{0} FATEs");

        public static readonly LocString GoalMinutes = new LocString("grind.goal.minutes", "{0} minutes");

        public static readonly LocString GoalLevels = new LocString("grind.goal.levels", "{0} levels");

        public static readonly LocString GoalPerZone = new LocString("grind.goal.perZone", "{0} per zone");

        public static readonly LocString GoalYokai = new LocString("grind.goal.yokai", "{0} medals per yo-kai");

        public static readonly LocString GoalSharedFates = new LocString("grind.goal.sharedFates", "every Shared FATE rank is maxed");

        public static readonly LocString GoalAtma = new LocString("grind.goal.atma", "your Atma are collected");

        public static readonly LocString GoalLuminous = new LocString("grind.goal.luminous", "your Luminous Crystals are collected");

        public static readonly LocString GoalMemories = new LocString("grind.goal.memories", "your Memories of the Dying are collected");

        public static readonly LocString GoalLawsOrder = new LocString("grind.goal.lawsOrder", "your Haunting and Vexatious Memories are collected");

        public static readonly LocString GoalDemiatma = new LocString("grind.goal.demiatma", "your Demiatma are collected");

        public static readonly LocString GoalPaste = new LocString("grind.goal.paste", "your Crystal Paste is collected");

        public static readonly LocString GoalEndless = new LocString("grind.goal.endless", "you stop it");

        public static readonly LocString PlanLocked = new LocString("grind.planLocked", "Stop the run to change the plan.");

        public static readonly LocString AfterStayToken = new LocString("grind.after.stay.token", "stay where you are");

        public static readonly LocString AfterStayDetail = new LocString("grind.after.stay.detail", "Just stop. You're left standing wherever the last FATE ended.");

        public static readonly LocString AfterInnToken = new LocString("grind.after.inn.token", "return to the inn");

        public static readonly LocString AfterInnDetail = new LocString("grind.after.inn.detail", "Travel to your Grand Company city and enter the inn room.");

        public static readonly LocString AfterLogoutToken = new LocString("grind.after.logout.token", "log out");

        public static readonly LocString AfterLogoutDetail = new LocString("grind.after.logout.detail", "Log out to the title screen.");

        public static readonly LocString AfterCloseToken = new LocString("grind.after.close.token", "close the game");

        public static readonly LocString AfterCloseDetail = new LocString("grind.after.close.detail", "Close FFXIV entirely (via XIVLauncher's /xlkill).");

        public static readonly LocString StopAt = new LocString("grind.stopAt", "Stop at");

        public static readonly LocString UnitGemstones = new LocString("grind.unit.gemstones", "gemstones");

        public static readonly LocString UnitFates = new LocString("grind.unit.fates", "FATEs");

        public static readonly LocString UnitMinutes = new LocString("grind.unit.minutes", "minutes");

        public static readonly LocString UnitLevels = new LocString("grind.unit.levels", "levels");

        public static readonly LocString UnitYokaiMedals = new LocString("grind.unit.yokaiMedals", "medals per yo-kai");

        public static readonly LocString NoteGemstones = new LocString("grind.note.gemstones", "You have {0} right now.");

        public static readonly LocString NoteYokai = new LocString("grind.note.yokai", "Summons each yo-kai minion you own, grinds its zones, and moves on once it holds this many Legendary Medals. Keep the Yo-kai Watch equipped or in your armoury chest.");

        public static readonly LocString NoteSharedFates = new LocString("grind.note.sharedFates", "Grinds each zone until its Shared FATE rank is maxed, then moves on to the next one in your order. Only Shadowbringers and later zones have ranks; other zones in the plan are skipped.");

        public static readonly LocString NoteSharedFatesSummary = new LocString("grind.note.sharedFatesSummary", "{0} of {1} selected zones already maxed.");

        public static readonly LocString ItemGoalAtma = new LocString("grind.itemGoals.atma", "Atma (Zodiac)");

        public static readonly LocString ItemGoalLuminous = new LocString("grind.itemGoals.luminous", "Luminous Crystals (Anima)");

        public static readonly LocString ItemGoalMemories = new LocString("grind.itemGoals.memories", "Memories of the Dying (Resistance)");

        public static readonly LocString ItemGoalLawsOrder = new LocString("grind.itemGoals.lawsOrder", "Haunting and Vexatious Memories (Resistance)");

        public static readonly LocString ItemGoalDemiatma = new LocString("grind.itemGoals.demiatma", "Demiatma (Phantom)");

        public static readonly LocString ItemGoalPaste = new LocString("grind.itemGoals.paste", "Crystal Paste (Phantom)");

        public static readonly LocString NoteAtma = new LocString("grind.itemGoals.noteAtma", "One of each of the twelve Atma per weapon, from FATEs in the A Realm Reborn zones. They only drop with a Zodiac Zenith weapon equipped and the quest Up in Arms active.");

        public static readonly LocString NoteLuminous = new LocString("grind.itemGoals.noteLuminous", "One of each of the six Luminous Crystals per weapon, from FATEs in the Heavensward zones, for the Anima weapon step.");

        public static readonly LocString NoteMemories = new LocString("grind.itemGoals.noteMemories", "Twenty of each of the three Memories of the Dying per weapon, from FATEs in the Heavensward zones, for the Resistance weapon step.");

        public static readonly LocString NoteLawsOrder = new LocString("grind.itemGoals.noteLawsOrder", "Eighteen Haunting and eighteen Vexatious Memories of the Dying per weapon, from FATEs in the Stormblood zones while The Resistance Remembers is active.");

        public static readonly LocString NoteDemiatma = new LocString("grind.itemGoals.noteDemiatma", "Three of each of the six Demiatma per weapon, from FATEs in the Dawntrail zones while Arcane Artistry is active.");

        public static readonly LocString NotePaste = new LocString("grind.itemGoals.notePaste", "1,200 Crystal Paste per weapon, from FATEs anywhere in Dawntrail while In Pursuit of Perfection is active.");

        public static readonly LocString WeaponsToMake = new LocString("grind.itemGoals.weaponsToMake", "Weapons to make");

        public static readonly LocString UnitWeapons = new LocString("grind.itemGoals.unitWeapons", "weapons");

        public static readonly LocString ItemGoalQuest = new LocString("grind.itemGoals.quest", "Needs the quest \"{0}\" active.");

        public static readonly LocString ItemGoalZenith = new LocString("grind.itemGoals.zenith", "Needs a Zodiac Zenith weapon equipped.");

        public static readonly LocString ItemsSummary = new LocString("grind.itemGoals.summary", "{0} of {1} items");

        public static readonly LocString ItemDropsIn = new LocString("grind.itemGoals.dropsIn", "Drops in: {0}");

        public static readonly LocString ItemGoalPlanZones = new LocString("grind.itemGoals.planZones", "Zones with items still to collect: {0}");

        public static readonly LocString YokaiEventOwned = new LocString("grind.event.yokai.owned", "{0} of {1} yo-kai owned");

        public static readonly LocPlural YokaiEventWeaponsLeft = new LocPlural("grind.event.yokai.weaponsLeft", "{0} weapon left", "{0} weapons left");

        public static readonly LocString YokaiEventWatchEquipped = new LocString("grind.event.yokai.watchEquipped", "Yo-kai Watch equipped");

        public static readonly LocString YokaiEventWatchStored = new LocString("grind.event.yokai.watchStored", "Yo-kai Watch in your bags or armoury chest");

        public static readonly LocString YokaiEventWatchMissing = new LocString("grind.event.yokai.watchMissing", "No Yo-kai Watch on this character");

        public static readonly LocString Zones = new LocString("grind.library.zones", "Zones");

        public static readonly LocString SearchZones = new LocString("grind.library.search", "Search zones");

        public static readonly LocString ClearSearch = new LocString("grind.library.clearSearch", "Clear search");

        public static readonly LocString NoMatches = new LocString("grind.library.noMatches", "No zones match “{0}”.");

        public static readonly LocPlural Matches = new LocPlural("grind.library.matches", "{0} zone matches “{1}”  ·  every expansion", "{0} zones match “{1}”  ·  every expansion");

        public static readonly LocString SelectedSummary = new LocString("grind.library.selected", "{0} of {1} selected");

        public static readonly LocPlural ActiveFates = new LocPlural("grind.library.activeFates", "{0} FATE active here right now.", "{0} FATEs active here right now.");

        public static readonly LocString ZonesLockedRunning = new LocString("grind.library.lockedRunning", "Stop the run to change your zones.");

        public static readonly LocString LockedGateway = new LocString("grind.library.lockedGateway", "Locked: attune {0}; {1} is entered from there over the aethernet.");

        public static readonly LocString LockedAetheryte = new LocString("grind.library.lockedAetheryte", "Locked: attune an aetheryte in this zone first.");

        public static readonly LocString RankBadge = new LocString("grind.library.rank", "Rank {0}  ·  {1}/{2}");

        public static readonly LocString RankMaxed = new LocString("grind.library.rankMaxed", "Maxed");

        public static readonly LocString RankSyncing = new LocString("grind.library.rankSyncing", "Reading rank…");

        public static readonly LocString RankTooltip = new LocString("grind.library.rankTooltip", "Shared FATE rank {0} of {1}  ·  {2} of {3} FATEs toward the next rank  ·  {4} of {5} in total");

        public static readonly LocString RankMaxedTooltip = new LocString("grind.library.rankMaxedTooltip", "Shared FATE rank maxed: all {0} FATEs done.");

        public static readonly LocString NoRanksHere = new LocString("grind.library.noRanks", "No Shared FATE ranks in this zone. Only Shadowbringers and later zones have them.");

        public static readonly LocString OrderHint = new LocString("grind.order.hint", "Your grind order will appear here once you pick a zone.");

        public static readonly LocString DragToReorder = new LocString("grind.order.drag", "Drag to reorder");

        public static readonly LocString RemoveFromOrder = new LocString("grind.order.remove", "Remove from grind order");

        public static readonly LocString OrderSkippedNoRanks = new LocString("grind.order.skippedNoRanks", "Skipped: this zone has no Shared FATE ranks.");

        public static readonly LocString Yokai = new LocString("grind.yokai.title", "Yo-kai");

        public static readonly LocString YokaiSummary = new LocString("grind.yokai.summary", "{0} of {1} Legendary Medals");

        public static readonly LocString YokaiNext = new LocString("grind.yokai.next", "next");

        public static readonly LocString YokaiPlanNext = new LocString("grind.yokai.planNext", "Up next: {0}  ·  {1}");

        public static readonly LocString YokaiWeaponTag = new LocString("grind.yokai.weaponTag", "weapon owned");

        public static readonly LocString YokaiDropsIn = new LocString("grind.yokai.dropsIn", "Legendary Medals drop in: {0}");

        public static readonly LocString YokaiMinionMissing = new LocString("grind.yokai.minionMissing", "You do not own this minion yet.");

        public static readonly LocString YokaiWeaponOwned = new LocString("grind.yokai.weaponOwned", "You already have this weapon.");

        public static readonly LocString YokaiUnreachable = new LocString("grind.yokai.unreachable", "Attune an aetheryte in one of its zones first: {0}");

        public static readonly LocString YokaiLockedRunning = new LocString("grind.yokai.lockedRunning", "Stop the run to change your yo-kai.");

        public static readonly LocString ExpansionArr = new LocString("grind.expansion.arr", "A Realm Reborn");

        public static readonly LocString ExpansionHw = new LocString("grind.expansion.hw", "Heavensward");

        public static readonly LocString ExpansionSb = new LocString("grind.expansion.sb", "Stormblood");

        public static readonly LocString ExpansionShb = new LocString("grind.expansion.shb", "Shadowbringers");

        public static readonly LocString ExpansionEw = new LocString("grind.expansion.ew", "Endwalker");

        public static readonly LocString ExpansionDt = new LocString("grind.expansion.dt", "Dawntrail");

        public static readonly LocString Start = new LocString("grind.start", "START");

        public static readonly LocString Stop = new LocString("grind.stop", "STOP");

        public static readonly LocString PauseCaps = new LocString("grind.pause", "PAUSE");

        public static readonly LocString ResumeCaps = new LocString("grind.resume", "RESUME");

        public static readonly LocString InContent = new LocString("grind.inContent", "in content");

        public static readonly LocString ReasonInstall = new LocString("grind.reason.install", "install the required plugins");

        public static readonly LocString ReasonPickZone = new LocString("grind.reason.pickZone", "pick at least one zone");

        public static readonly LocString ReasonAtLevelCap = new LocString("grind.reason.atLevelCap", "your job is already at the level cap");

        public static readonly LocString ReasonNoYokai = new LocString("grind.reason.noYokai", "no yo-kai left to farm");

        public static readonly LocString ReasonNoWatch = new LocString("grind.reason.noWatch", "get the Yo-kai Watch first");

        public static readonly LocString ReasonNoRankedZones = new LocString("grind.reason.noRankedZones", "pick a Shadowbringers or later zone");

        public static readonly LocString ReasonRanksMaxed = new LocString("grind.reason.ranksMaxed", "every selected zone is already maxed");

        public static readonly LocString ReasonNeedQuest = new LocString("grind.reason.needQuest", "pick up the quest first");

        public static readonly LocString ReasonNeedZenith = new LocString("grind.reason.needZenith", "equip a Zenith weapon");

        public static readonly LocString ReasonAllCollected = new LocString("grind.reason.allCollected", "everything is collected");

        public static readonly LocString StartSub = new LocString("grind.startSub", "{0}  ·  {1}");

        public static readonly LocString StateRunning = new LocString("grind.state.running", "running");

        public static readonly LocString StatePaused = new LocString("grind.state.paused", "paused");

        public static readonly LocString StopSub = new LocString("grind.stopSub", "{0} · {1}");

        public static readonly LocString StopAfterFate = new LocString("grind.stopAfterFate", "stopping after this FATE");

        public static readonly LocString StopSoftHint = new LocString("grind.stopSoftHint", "Ctrl+click to stop after the current FATE instead of right away.");

        public static readonly LocString StopNowHint = new LocString("grind.stopNowHint", "Stops once the current FATE is over. Click to stop right away instead.");
    }

    internal static class Run
    {
        public static readonly LocString PhaseTrading = new LocString("run.phase.trading", "Trading gemstones");

        public static readonly LocString PhaseRepairing = new LocString("run.phase.repairing", "Repairing gear");

        public static readonly LocString PhaseBreak = new LocString("run.phase.break", "Taking a break");

        public static readonly LocString PhaseFinishing = new LocString("run.phase.finishing", "Finishing up");

        public static readonly LocString PhaseGrinding = new LocString("run.phase.grinding", "Grinding FATEs");

        public static readonly LocString PhaseEngaging = new LocString("run.phase.engaging", "Engaging FATE");

        public static readonly LocString PhaseStandingBy = new LocString("run.phase.standingBy", "Standing by");

        public static readonly LocString PhaseReady = new LocString("run.phase.ready", "Ready");

        public static readonly LocString PhasePaused = new LocString("run.phase.paused", "Paused");

        public static readonly LocString PhasePausedInContent = new LocString("run.phase.pausedInContent", "Paused (in content)");

        public static readonly LocString FateProgress = new LocString("run.fateProgress", "{0}%   ·   {1} left");

        public static readonly LocString UpNext = new LocString("run.upNext", "Up next");

        public static readonly LocString NoOtherFates = new LocString("run.noOtherFates", "No other eligible FATEs in this zone.");

        public static readonly LocPlural Rotation = new LocPlural("run.rotation", "{1}  ·  {0} zone in rotation", "{1}  ·  {0} zones in rotation");

        public static readonly LocString SomewhereElse = new LocString("run.somewhereElse", "Somewhere else");

        public static readonly LocString TileFates = new LocString("run.tile.fates", "FATEs");

        public static readonly LocString TileGems = new LocString("run.tile.gems", "Gems");

        public static readonly LocString TileFatesPerHour = new LocString("run.tile.fatesPerHour", "FATEs/h");

        public static readonly LocString TileElapsed = new LocString("run.tile.elapsed", "Elapsed");

        public static readonly LocString PerHour = new LocString("run.perHour", "{0} /h");

        public static readonly LocString QueueMeta = new LocString("run.queueMeta", "{0}%   ·   {1}   ·   {2}y");

        public static readonly LocString GoalOf = new LocString("run.goal.of", "/ {0}");

        public static readonly LocString GoalOfMinutes = new LocString("run.goal.ofMinutes", "/ {0}m");

        public static readonly LocString GoalMinutes = new LocString("run.goal.minutes", "{0}m");

        public static readonly LocString GemsToGo = new LocString("run.goal.gemsToGo", "{0} gems to go");

        public static readonly LocString TargetReached = new LocString("run.goal.targetReached", "target reached");

        public static readonly LocString FatesLeft = new LocString("run.goal.fatesLeft", "{0} FATEs left");

        public static readonly LocString LevelsLeft = new LocString("run.goal.levelsLeft", "{0} levels left");

        public static readonly LocString YokaiToGo = new LocString("run.goal.yokaiToGo", "{0}  ·  {1} medals to go");

        public static readonly LocString SharedRank = new LocString("run.goal.sharedRank", "Rank {0} of {1}  ·  {2}/{3} to the next rank");

        public static readonly LocString SharedMaxedHere = new LocString("run.goal.sharedMaxedHere", "rank maxed here");

        public static readonly LocString SharedSyncing = new LocString("run.goal.sharedSyncing", "reading Shared FATE ranks…");

        public static readonly LocPlural SharedZonesLeft = new LocPlural("run.goal.sharedZonesLeft", "{0} zone left", "{0} zones left");

        public static readonly LocPlural ItemsToGo = new LocPlural("run.goal.itemsToGo", "{0} item to go", "{0} items to go");

        public static readonly LocString ItemsCollected = new LocString("run.goal.itemsCollected", "all items collected");

        public static readonly LocString HoursLeft = new LocString("run.goal.hoursLeft", "{0}h {1:00}m left");

        public static readonly LocString MinutesLeft = new LocString("run.goal.minutesLeft", "{0}m {1:00}s left");

        public static readonly LocString TimeReached = new LocString("run.goal.timeReached", "time reached");

        public static readonly LocString Done = new LocString("run.goal.done", "done");

        public static readonly LocString UntilYouStop = new LocString("run.goal.untilYouStop", "until you stop");
    }

    internal static class Live
    {
        public static readonly LocString Title = new LocString("live.title", "Live FATEs");

        public static readonly LocString Hide = new LocString("live.hide", "Hide the live tracker");

        public static readonly LocString Engaging = new LocString("live.engaging", "Engaging");

        public static readonly LocString StandingBy = new LocString("live.standingBy", "Standing by");

        public static readonly LocString Ready = new LocString("live.ready", "Ready");

        public static readonly LocString Paused = new LocString("live.paused", "Paused");

        public static readonly LocString NoFate = new LocString("live.noFate", "No FATE engaged.");

        public static readonly LocString NoOtherFates = new LocString("live.noOtherFates", "No other FATEs.");

        public static readonly LocString NoSession = new LocString("live.noSession", "No session.");

        public static readonly LocString Session = new LocString("live.session", "{0} FATEs · {1} gems · {2}");

        public static readonly LocString Exp = new LocString("live.exp", "{0} exp · {1}/h");

        public static readonly LocString Ban = new LocString("live.ban", "Blacklist this FATE for this character (skips it while grinding).");

        public static readonly LocString Blacklisted = new LocString("live.blacklisted", "Blacklisted");

        public static readonly LocString QueueMeta = new LocString("live.queueMeta", "{0}%  {1}");

        public static readonly LocString EligibleTitle = new LocString("live.eligibleTitle", "Will be automated.");

        public static readonly LocString ReasonTitle = new LocString("live.reasonTitle", "Passed over: {0}.");

        public static readonly LocString WalkHint = new LocString("live.walkHint", "Click to walk there. Click again to stop.");

        public static readonly LocString ReasonBlacklisted = new LocString("live.reason.blacklisted", "blacklisted");

        public static readonly LocString ReasonSessionStuck = new LocString("live.reason.sessionStuck", "skipped for this session");

        public static readonly LocString ReasonSkippedRule = new LocString("live.reason.skippedRule", "its FATE type is skipped");

        public static readonly LocString ReasonLevelBand = new LocString("live.reason.levelBand", "outside your level band");

        public static readonly LocString ReasonLevelWindow = new LocString("live.reason.levelWindow", "outside the level window");

        public static readonly LocString ReasonTooLong = new LocString("live.reason.tooLong", "longer than the duration limit");

        public static readonly LocString ReasonNotStarted = new LocString("live.reason.notStarted", "not started yet");

        public static readonly LocString ReasonTooLittleTime = new LocString("live.reason.tooLittleTime", "too little time left");

        public static readonly LocString ReasonTooMuchProgress = new LocString("live.reason.tooMuchProgress", "too far along");

        public static readonly LocString ReasonFinished = new LocString("live.reason.finished", "already finished");

        public static readonly LocString ReasonNotOnMap = new LocString("live.reason.notOnMap", "not on the map yet");
    }

    internal static class History
    {
        public static readonly LocString Title = new LocString("history.title", "History");

        public static readonly LocString Empty = new LocString("history.empty", "Your finished runs will show up here.");

        public static readonly LocPlural Summary = new LocPlural("history.summary", "{0} recorded run  ·  {1} grinding  ·  {2} FATEs/h average", "{0} recorded runs  ·  {1} grinding  ·  {2} FATEs/h average");

        public static readonly LocString TileRuns = new LocString("history.tile.runs", "Runs");

        public static readonly LocString TileFates = new LocString("history.tile.fates", "FATEs");

        public static readonly LocString TileGems = new LocString("history.tile.gems", "Gems");

        public static readonly LocString TileExp = new LocString("history.tile.exp", "Exp");

        public static readonly LocString TileLevels = new LocString("history.tile.levels", "Levels");

        public static readonly LocString NoRuns = new LocString("history.noRuns", "No runs recorded yet. Finish (or stop) a grind and it'll show up here.");

        public static readonly LocString FatesPerRun = new LocString("history.fatesPerRun", "FATEs per run");

        public static readonly LocString RecentRuns = new LocString("history.recentRuns", "Recent runs");

        public static readonly LocPlural ChartRange = new LocPlural("history.chartRange", "last {0} run  ·  oldest to newest", "last {0} runs  ·  oldest to newest");

        public static readonly LocString ChartPeak = new LocString("history.chartPeak", "peak {0}");

        public static readonly LocString ChartTooltip = new LocString("history.chartTooltip", "{0}  ·  {1} FATEs  ·  {2} gems  ·  {3}");

        public static readonly LocString RowDetail = new LocString("history.rowDetail", "{0}  ·  {1}  ·  {2}");

        public static readonly LocString RowDetailNoMode = new LocString("history.rowDetailNoMode", "{0}  ·  {1}");

        public static readonly LocString TooltipMode = new LocString("history.tooltip.mode", "Mode: {0}");

        public static readonly LocString TooltipLevel = new LocString("history.tooltip.level", "Level: {0} → {1}");

        public static readonly LocString TooltipRate = new LocString("history.tooltip.rate", "Rate: {0} FATEs/h  ·  {1} exp/h");

        public static readonly LocString TooltipZones = new LocString("history.tooltip.zones", "Zones: {0}");

        public static readonly LocString JustNow = new LocString("history.time.justNow", "just now");

        public static readonly LocString MinutesAgo = new LocString("history.time.minutesAgo", "{0}m ago");

        public static readonly LocString HoursAgo = new LocString("history.time.hoursAgo", "{0}h ago");

        public static readonly LocString DaysAgo = new LocString("history.time.daysAgo", "{0}d ago");

        public static readonly LocString ClearHistory = new LocString("history.clear", "Clear history");

        public static readonly LocString ClearQuestion = new LocString("history.clearQuestion", "Delete all recorded runs?");

        public static readonly LocString ClearYes = new LocString("history.clearYes", "Yes, clear");
    }

    internal static class Plugins
    {
        public static readonly LocString Title = new LocString("plugins.title", "Plugins");

        public static readonly LocString AllInstalled = new LocString("plugins.allInstalled", "All required plugins are installed and loaded.");

        public static readonly LocPlural Missing = new LocPlural("plugins.missing", "{0} required plugin is missing.", "{0} required plugins are missing.");

        public static readonly LocString Required = new LocString("plugins.required", "Required");

        public static readonly LocString Optional = new LocString("plugins.optional", "Optional");

        public static readonly LocString Installed = new LocString("plugins.installed", "Installed");

        public static readonly LocString Disabled = new LocString("plugins.disabled", "Disabled");

        public static readonly LocString Install = new LocString("plugins.install", "Install");

        public static readonly LocString Installing = new LocString("plugins.installing", "Installing…");

        public static readonly LocString RepoHint = new LocString("plugins.repoHint", "Repo: {0}\nLeft-click to open repo URL · right-click to copy");

        public static readonly LocString TextAdvanceDisabled = new LocString("plugins.textAdvanceDisabled", "Loaded, but TextAdvance's own \"Enable plugin\" toggle is off.\nFTA doesn't need it: FATE turn-ins and trader dialogue are driven directly.\nTurn it on in TextAdvance's settings window (/xlplugins -> TextAdvance)\nas a fallback if a Collect FATE turn-in stalls.");

        public static readonly LocString Footer = new LocString("plugins.footer", "Install adds the plugin's source repository to Dalamud and queues an install. If one-click install fails (URL drift, network), right-click a plugin name to copy its repo URL and add it manually via /xlsettings -> Experimental -> Custom Plugin Repositories.");

        public static readonly LocString PurposeVnavmesh = new LocString("plugins.purpose.vnavmesh", "Pathfinding and movement to FATEs.");

        public static readonly LocString PurposeBossMod = new LocString("plugins.purpose.bossMod", "Auto-rotation, targeting, and dodging during FATE combat.");

        public static readonly LocString PurposeTextAdvance = new LocString("plugins.purpose.textAdvance", "Talk-skip during Collect FATE turn-ins (scoped, only enabled mid-Collect).");
    }

    internal static class Log
    {
        public static readonly LocString Title = new LocString("log.title", "Console");

        public static readonly LocPlural Entries = new LocPlural("log.entries", "{0} line in the buffer", "{0} lines in the buffer");

        public static readonly LocString Empty = new LocString("log.empty", "Nothing logged yet. Start a run and every step the plugin takes shows up here.");

        public static readonly LocString NoMatches = new LocString("log.noMatches", "No lines match the current filters.");

        public static readonly LocString Footer = new LocString("log.footer", "Every line also goes to the Dalamud log (/xllog) with the {0} prefix. When reporting a bug, press Copy log and paste the result into the issue.");

        public static readonly LocString SearchHint = new LocString("log.searchHint", "Search messages and sources");

        public static readonly LocString CopyAll = new LocString("log.copyAll", "Copy log");

        public static readonly LocPlural CopyFiltered = new LocPlural("log.copyFiltered", "Copy {0} line", "Copy {0} lines");

        public static readonly LocString CopyTooltip = new LocString("log.copyTooltip", "Copies the lines shown below with a header naming the plugin version, Dalamud version and zone, ready to paste into a bug report.");

        public static readonly LocString Copied = new LocString("log.copied", "Copied");

        public static readonly LocPlural CopiedLines = new LocPlural("log.copiedLines", "Copied {0} line to the clipboard", "Copied {0} lines to the clipboard");

        public static readonly LocString Clear = new LocString("log.clear", "Clear");

        public static readonly LocString ConfirmClear = new LocString("log.confirmClear", "Click again to clear");

        public static readonly LocString Close = new LocString("log.close", "Close");

        public static readonly LocString LevelVerbose = new LocString("log.level.verbose", "Verbose");

        public static readonly LocString LevelDebug = new LocString("log.level.debug", "Debug");

        public static readonly LocString LevelInfo = new LocString("log.level.info", "Info");

        public static readonly LocString LevelWarning = new LocString("log.level.warning", "Warnings");

        public static readonly LocString LevelError = new LocString("log.level.error", "Errors");

        public static readonly LocString LevelTooltip = new LocString("log.levelTooltip", "Click to show or hide these lines. Shift-click to show only this level.");

        public static readonly LocString SourceChip = new LocString("log.sourceChip", "Source: {0}");

        public static readonly LocString SourceChipTooltip = new LocString("log.sourceChipTooltip", "Click to stop filtering by source.");

        public static readonly LocString JumpLatest = new LocString("log.jumpLatest", "Jump to latest");

        public static readonly LocPlural NewLines = new LocPlural("log.newLines", "{0} new line", "{0} new lines");

        public static readonly LocString Showing = new LocString("log.showing", "Showing {0} of {1}");

        public static readonly LocString ResetFilters = new LocString("log.resetFilters", "Reset filters");

        public static readonly LocString Shortcuts = new LocString("log.shortcuts", "Ctrl+F search · Ctrl+C copy · Shift-click selects a range · Double-click copies a line");

        public static readonly LocPlural Selected = new LocPlural("log.selected", "{0} line selected", "{0} lines selected");

        public static readonly LocString CopySelection = new LocString("log.copySelection", "Copy selection");

        public static readonly LocString ClearSelection = new LocString("log.clearSelection", "Clear selection");

        public static readonly LocString CopyLine = new LocString("log.copyLine", "Copy line");

        public static readonly LocString CopyToEnd = new LocString("log.copyToEnd", "Copy from here to the end");

        public static readonly LocString OnlySource = new LocString("log.onlySource", "Show only {0}");

        public static readonly LocString Repeated = new LocString("log.repeated", "Repeated {0} times in a row");

        public static readonly LocString HasDetails = new LocString("log.hasDetails", "Has a stack trace. Select the line to read it.");
    }

    internal static class Changelog
    {
        public static readonly LocString Title = new LocString("changelog.title", "What's new");

        public static readonly LocString Subtitle = new LocString("changelog.subtitle", "Every update, newest first.");

        public static readonly LocString Version = new LocString("changelog.version", "Version {0}");

        public static readonly LocString Latest = new LocString("changelog.latest", "Latest");

        public static readonly LocString New = new LocString("changelog.new", "New");

        public static readonly LocPlural Changes = new LocPlural("changelog.changes", "{0} change", "{0} changes");

        public static readonly LocString[] Release22200 = new LocString[3]
        {
            new LocString("changelog.r22200.1", "Added Chat commands to the settings: list your own commands, one per line (such as /li home or /ays m), and they run when a run finishes on its own, before the after-run action, never after Stop or a failure"),
            new LocString("changelog.r22200.2", "Fixed the character standing idle beside FATE enemies that spawn outside the FATE area, as in Mascot Murder: when nothing has a target, the nearest FATE enemy is now targeted"),
            new LocString("changelog.r22200.3", "The main window now moves only when you drag its top bar, so a drag inside it no longer moves it by accident; turn on Drag from anywhere in the settings for the old behaviour")
        };

        public static readonly LocString[] Release22130 = new LocString[2]
        {
            new LocString("changelog.r22130.1", "Fixed long sessions ending in a crash or a Start button that stops working; moving around no longer sets up new game hooks each time"),
            new LocString("changelog.r22130.2", "Fixed the gemstone shopping list stopping after the first trader: a trade now buys down the whole list, travelling on to the next trader when an item is only sold elsewhere, within your spend settings")
        };

        public static readonly LocString[] Release22120 = new LocString[1]
        {
            new LocString("changelog.r22120.1", "Fixed long trips to a FATE in the same zone, such as from the surface of Yak T'el down to Mamook, being taken for stuck after 60 seconds and ending in a teleport to an aetheryte even with Economy mode on; the time limit now starts over while the character keeps getting closer")
        };

        public static readonly LocString[] Release22110 = new LocString[1]
        {
            new LocString("changelog.r22110.1", "Fixed auto-trade failing to buy items that do not stack, such as furnishings: the trader was asked for several in one exchange, which the game refuses, so they are now bought one at a time")
        };

        public static readonly LocString[] Release22100 = new LocString[3]
        {
            new LocString("changelog.r22100.1", "Added Teleport out when stuck under Settings > General: when the run makes no progress for a set time, such as after landing on water where the yo-kai minion cannot be summoned, the character teleports to the nearest aetheryte and carries on. Contributed by blackappleD"),
            new LocString("changelog.r22100.2", "Added Pull as many mobs as possible under Settings > Travel: a tank tags every idle mob of the FATE in reach with its ranged attack, then lets BossMod AoE the pack down, with a reach, a mob cap and HP points to stop and resume pulling. Contributed by blackappleD"),
            new LocString("changelog.r22100.3", "Fixed allied NPCs that fight alongside you in a FATE being counted as FATE mobs, which could walk the character to an ally or keep it searching for mobs that were already gone. Contributed by blackappleD")
        };

        public static readonly LocString[] Release22000 = new LocString[4]
        {
            new LocString("changelog.r22000.1", "Added Each zone on the Grind page: a number of FATEs for every selected zone, after which the grind moves on and the run ends once each zone has had its share"),
            new LocString("changelog.r22000.2", "Fixed the trade, repair and break hand-offs starting while the character was still in combat, which left it standing in a follow-up FATE with the rotation off; the grind now plays that FATE first and hands off after it"),
            new LocString("changelog.r22000.3", "Fixed a trade whose teleport could not be cast skipping its items for the rest of the run; the next FATE retries it once"),
            new LocString("changelog.r22000.4", "Fixed melee jobs parking on the edge of the FATE ring in escort FATEs such as Mascot March, where the mobs walk out of the ring")
        };

        public static readonly LocString[] Release21900 = new LocString[1]
        {
            new LocString("changelog.r21900.1", "Fixed the run stopping when gemstones reach the auto-trade threshold instead of going to the trader")
        };

        public static readonly LocString[] Release21800 = new LocString[13]
        {
            new LocString("changelog.r21800.1", "Added a Mount option under Settings > Travel to pick which flying mount to ride instead of the mount roulette"),
            new LocString("changelog.r21800.2", "Added Vary dodging in combat under Settings > Humanizer, giving each FATE its own dodge timing from ranges you set. Contributed by blackappleD"),
            new LocString("changelog.r21800.3", "Added A number of levels to How long on the Grind page: the run ends once your job gains that many levels. Contributed by mmkhatib"),
            new LocString("changelog.r21800.4", "Added a shopping list to auto-trade under Settings > Gemstones: items are bought from the top down, and collectibles you own are skipped"),
            new LocString("changelog.r21800.5", "Added Idle at a spot as a break activity under Settings > Humanizer: breaks stand still at a place you saved instead of wandering a city"),
            new LocString("changelog.r21800.6", "Moved the mount, economy mode, zone swap, Twist of Fate and Collect hand-in options into the new Settings > Travel tab"),
            new LocString("changelog.r21800.7", "Fixed flights to FATEs whose center lies below the ground, like those at The Imperious, diving into the floor"),
            new LocString("changelog.r21800.8", "Fixed getting stuck on walls in FATEs while picking up Collect items or attacking from range"),
            new LocString("changelog.r21800.9", "Fixed gemstone auto-trade stalling at the trader's dialogue when TextAdvance is switched off"),
            new LocString("changelog.r21800.10", "Fixed auto-trade waiting for the first FATE when a run starts with gemstones already over the threshold"),
            new LocString("changelog.r21800.11", "Fixed auto-trade going back to the trader after every FATE when a purchase fails"),
            new LocString("changelog.r21800.12", "Fixed the live tracker getting wider every time it was dragged to the left"),
            new LocString("changelog.r21800.13", "Fixed NPC repair at the Maelstrom mender and returning to the Limsa Lominsa inn giving up before the teleport")
        };

        public static readonly LocString[] Release21700 = new LocString[3]
        {
            new LocString("changelog.r21700.1", "Added Mount while waiting for FATEs under Settings > General, which mounts up after a short delay between FATEs. Contributed by lizvik"),
            new LocString("changelog.r21700.2", "Added Economy mode under Settings > General: flies to the next FATE in the same zone instead of teleporting, to save gil"),
            new LocString("changelog.r21700.3", "Fixed trips to a FATE staying on the ground when flying became available after the route was planned. Contributed by lizvik")
        };

        public static readonly LocString[] Release21600 = new LocString[2]
        {
            new LocString("changelog.r21600.1", "Added your Bicolor Gemstone count to the title strip and live tracker, colored at your trade threshold and at the cap"),
            new LocString("changelog.r21600.2", "Added the Summon chocobo toggle under Settings > Consumables to keep BossMod from using Gysahl Greens. Contributed by lizvik")
        };

        public static readonly LocString[] Release21500 = new LocString[2]
        {
            new LocString("changelog.r21500.1", "Fixed the Relic goals asking for the Yo-kai Watch before they would start"),
            new LocString("changelog.r21500.2", "Fixed a Relic run being able to switch to the yo-kai medal zones partway through on a character that owns a yo-kai minion")
        };

        public static readonly LocString[] Release21400 = new LocString[10]
        {
            new LocString("changelog.r21400.1", "Added the Shared FATE ranks goal: grinds each zone until its rank is maxed, then moves on, with the rank shown on every Shadowbringers and later zone card"),
            new LocString("changelog.r21400.2", "Added six relic and item goals: Atma, Luminous Crystals, Memories of the Dying, Haunting and Vexatious Memories, Demiatma and Crystal Paste"),
            new LocString("changelog.r21400.3", "Added run limits that work with any goal: stop after a number of FATEs or a length of time"),
            new LocString("changelog.r21400.4", "Added /fta start, stop, stop soft and run <count>, and Ctrl+click on Stop to finish the current FATE first"),
            new LocString("changelog.r21400.5", "Added filters for long FATEs and a fixed level window, and a Keep Twist of Fate option that holds zone swaps while the bonus is up"),
            new LocString("changelog.r21400.6", "Added a setting for how long to wait in an empty zone before swapping, which the pacing roll now respects"),
            new LocString("changelog.r21400.7", "Overhauled the Grind page: a Goal section with Farm, Ranks, Relics and Events tabs, a live status on every goal card, and two plain questions for how long to run and what to do afterwards"),
            new LocString("changelog.r21400.8", "Improved the live tracker: passed-over FATEs are listed with the reason, a click walks you to a FATE while idle, and the FATE name format is configurable"),
            new LocString("changelog.r21400.9", "Fixed the level range and skipped FATE types not applying to a FATE you were already standing in or died in"),
            new LocString("changelog.r21400.10", "Fixed FATEs skipped for routing trouble staying skipped for the whole session; they are retried after the next completion")
        };

        public static readonly LocString[] Release21300 = new LocString[2]
        {
            new LocString("changelog.r21300.1", "Added the Console page to view, filter and copy the plugin's logs"),
            new LocString("changelog.r21300.2", "Added this changelog, with a badge whenever an update brings something new")
        };
    }

    internal static class About
    {
        public static readonly LocString LinkHint = new LocString("about.linkHint", "Click to open · right-click to copy");

        public static readonly LocString MadeBy = new LocString("about.madeBy", "Made by {0}");

        public static readonly LocString Version = new LocString("about.version", "v {0}");

        public static readonly LocString Community = new LocString("about.community", "Community");

        public static readonly LocString GitHubTitle = new LocString("about.githubTitle", "View on GitHub");

        public static readonly LocString GitHubBody = new LocString("about.githubBody", "Browse the source code and every release.");

        public static readonly LocString ReminderTitle = new LocString("about.reminder.title", "A little reminder");

        public static readonly LocString FactsTitle = new LocString("about.facts.title", "Did you know?");

        public static readonly LocString QuotesTitle = new LocString("about.quotes.title", "Words to live by");

        public static readonly LocString JokesTitle = new LocString("about.jokes.title", "Just for fun");

        public static readonly LocString[] Reminders = new LocString[12]
        {
            new LocString("about.reminder.1", "Been at it a while? Roll your shoulders and take one slow breath."),
            new LocString("about.reminder.2", "Hydration check. When did you last drink some water?"),
            new LocString("about.reminder.3", "Blink a few times and let your eyes rest for a moment."),
            new LocString("about.reminder.4", "Stand up, stretch, and shake out your hands. Future you says thanks."),
            new LocString("about.reminder.5", "Sit up and settle in comfortably. Your back will thank you later."),
            new LocString("about.reminder.6", "Remember to eat something today. You matter more than any score."),
            new LocString("about.reminder.7", "Eyes feel tired? Look at something far away for twenty seconds."),
            new LocString("about.reminder.8", "Whatever you're chasing, you're allowed to take a break whenever."),
            new LocString("about.reminder.9", "You're doing great. Be a little kinder to yourself today."),
            new LocString("about.reminder.10", "A glass of water and a quick stretch can reset a long session."),
            new LocString("about.reminder.11", "Unclench your jaw and drop your shoulders. There you go."),
            new LocString("about.reminder.12", "Rest is part of the journey too. Step away whenever you need to.")
        };

        public static readonly LocString[] Facts = new LocString[12]
        {
            new LocString("about.facts.1", "Honey never spoils. Jars over 3,000 years old have been found still edible."),
            new LocString("about.facts.2", "Octopuses have three hearts and blue blood."),
            new LocString("about.facts.3", "A day on Venus is longer than a whole year on Venus."),
            new LocString("about.facts.4", "Bananas are berries, but strawberries aren't."),
            new LocString("about.facts.5", "There are more possible chess games than atoms in the observable universe."),
            new LocString("about.facts.6", "Sharks have been around longer than trees have."),
            new LocString("about.facts.7", "A group of flamingos is called a flamboyance."),
            new LocString("about.facts.8", "Honeybees can recognize individual human faces."),
            new LocString("about.facts.9", "Wombat droppings are cube shaped."),
            new LocString("about.facts.10", "The Eiffel Tower can grow over 15 cm taller on a hot day."),
            new LocString("about.facts.11", "Hot water can sometimes freeze faster than cold water."),
            new LocString("about.facts.12", "A bolt of lightning is roughly five times hotter than the surface of the Sun.")
        };

        public static readonly LocString[] Quotes = new LocString[10]
        {
            new LocString("about.quotes.1", "Done is better than perfect. You can always polish later."),
            new LocString("about.quotes.2", "Small steps every day add up to surprising distances."),
            new LocString("about.quotes.3", "Comparison is the thief of joy. Run your own race."),
            new LocString("about.quotes.4", "Progress, not perfection."),
            new LocString("about.quotes.5", "You don't have to be great to start, but you have to start to be great."),
            new LocString("about.quotes.6", "Be patient with yourself. Growth takes time."),
            new LocString("about.quotes.7", "The best time to begin was yesterday. The second best is right now."),
            new LocString("about.quotes.8", "Celebrate the small wins. They count too."),
            new LocString("about.quotes.9", "Slow progress is still progress."),
            new LocString("about.quotes.10", "Your only real competition is who you were yesterday.")
        };

        public static readonly LocString[] Jokes = new LocString[10]
        {
            new LocString("about.jokes.1", "Why don't scientists trust atoms? Because they make up everything."),
            new LocString("about.jokes.2", "I would tell you a chemistry joke, but I know I wouldn't get a reaction."),
            new LocString("about.jokes.3", "Why did the scarecrow win an award? He was outstanding in his field."),
            new LocString("about.jokes.4", "I'm reading a book about anti-gravity. It's impossible to put down."),
            new LocString("about.jokes.5", "Why don't skeletons fight each other? They don't have the guts."),
            new LocString("about.jokes.6", "What do you call fake spaghetti? An impasta."),
            new LocString("about.jokes.7", "Why did the bicycle fall over? It was two tired."),
            new LocString("about.jokes.8", "What do you call cheese that isn't yours? Nacho cheese."),
            new LocString("about.jokes.9", "I'm on a seafood diet. I see food, and I eat it."),
            new LocString("about.jokes.10", "I only know 25 letters of the alphabet. I don't know y.")
        };
    }

    internal static class Settings
    {
        public static readonly LocString Title = new LocString("settings.title", "Settings");

        public static readonly LocString Language = new LocString("settings.language", "Language");

        public static readonly LocString LanguageHelp = new LocString("settings.languageHelp", "The language of this plugin's windows. FATE and zone names always follow the game client.");

        public static readonly LocString CatGeneral = new LocString("settings.cat.general", "General");

        public static readonly LocString CatGeneralSub = new LocString("settings.cat.generalSub", "Language, window and run preferences.");

        public static readonly LocString CatTravel = new LocString("settings.cat.travel", "Travel");

        public static readonly LocString CatTravelSub = new LocString("settings.cat.travelSub", "Mounts, teleports, zone swaps and how FATEs are played.");

        public static readonly LocString CatFilters = new LocString("settings.cat.filters", "FATE filters");

        public static readonly LocString CatFiltersSub = new LocString("settings.cat.filtersSub", "Keeps the plugin off dying or late FATEs.");

        public static readonly LocString CatClasses = new LocString("settings.cat.classes", "Class queue");

        public static readonly LocString CatClassesSub = new LocString("settings.cat.classesSub", "Switch gearsets on start, and advance to the next class when one hits its level cap.");

        public static readonly LocString CatGemstones = new LocString("settings.cat.gemstones", "Gemstones");

        public static readonly LocString CatGemstonesSub = new LocString("settings.cat.gemstonesSub", "Auto-spend Bicolor Gemstones once the wallet hits your threshold.");

        public static readonly LocString CatRepair = new LocString("settings.cat.repair", "Repair");

        public static readonly LocString CatRepairSub = new LocString("settings.cat.repairSub", "Auto-repair gear when equipped item condition drops below the threshold.");

        public static readonly LocString CatConsumables = new LocString("settings.cat.consumables", "Consumables");

        public static readonly LocString CatConsumablesSub = new LocString("settings.cat.consumablesSub", "Keep food and medicine buffs up while grinding: Well Fed is a free +3% EXP.");

        public static readonly LocString CatHumanizer = new LocString("settings.cat.humanizer", "Humanizer");

        public static readonly LocString CatHumanizerSub = new LocString("settings.cat.humanizerSub", "Act less like a bot: vary your timing between FATEs and take periodic city breaks in a random hub.");

        public static readonly LocString CatPartyInvites = new LocString("settings.cat.partyInvites", "Party invites");

        public static readonly LocString CatPartyInvitesSub = new LocString("settings.cat.partyInvitesSub", "Auto-decline incoming party invites during a run, after a human-like delay, with an optional reply.");

        public static readonly LocString CatGmAlert = new LocString("settings.cat.gmAlert", "GM alert");

        public static readonly LocString CatGmAlertSub = new LocString("settings.cat.gmAlertSub", "Detects nearby Game Masters and reacts: stop the bot, ping you, or take more drastic action.");

        public static readonly LocString GeneralWindow = new LocString("settings.general.window", "Window");

        public static readonly LocString OpenOnLogin = new LocString("settings.general.openOnLogin", "Open on login");

        public static readonly LocString OpenOnLoginHelp = new LocString("settings.general.openOnLoginHelp", "Pop the main window automatically the next time you log in.");

        public static readonly LocString DragAnywhere = new LocString("settings.general.dragAnywhere", "Drag from anywhere");

        public static readonly LocString DragAnywhereHelp = new LocString("settings.general.dragAnywhereHelp", "Move the window by dragging anywhere inside it. When off, only the top bar moves it.");

        public static readonly LocString LivePopout = new LocString("settings.general.livePopout", "Live FATE tracker popout");

        public static readonly LocString LivePopoutHelp = new LocString("settings.general.livePopoutHelp", "Show the live FATE tracker as a small overlay window so you can keep it visible while the main window is closed.");

        public static readonly LocString NameFormat = new LocString("settings.general.nameFormat", "FATE name format");

        public static readonly LocString NameFormatHelp = new LocString("settings.general.nameFormatHelp", "How FATE names read in the running panel and the live tracker. Tokens: {Level}, {Name}, {Id}, {Progress}, {TimeRemaining}, {Distance}, {State}.");

        public static readonly LocString GeneralRun = new LocString("settings.general.run", "Run");

        public static readonly LocString TravelMount = new LocString("settings.travel.mountGroup", "Mount and teleports");

        public static readonly LocString TravelZoneSwap = new LocString("settings.travel.zoneSwap", "Zone swaps");

        public static readonly LocString SwapZones = new LocString("settings.general.swapZones", "Swap zones when empty");

        public static readonly LocString SwapZonesHelp = new LocString("settings.general.swapZonesHelp", "When the current zone runs out of eligible FATEs, jump to the next zone in your priority order.");

        public static readonly LocString SwapWait = new LocString("settings.general.swapWait", "Wait before swapping");

        public static readonly LocString SwapWaitHelp = new LocString("settings.general.swapWaitHelp", "How long to sit in an empty zone before jumping to the next one. With pacing on, the real wait is rolled around this value so a zone full of FTA users does not all leave at once.");

        public static readonly LocString KeepTwist = new LocString("settings.general.keepTwist", "Keep Twist of Fate");

        public static readonly LocString KeepTwistHelp = new LocString("settings.general.keepTwistHelp", "While the Twist of Fate bonus is up, stay in the zone: no zone swap, no teleport shortcut, and no rotating past a finished zone until it wears off.");

        public static readonly LocString MountWhileWaiting = new LocString("settings.general.mountWhileWaiting", "Mount while waiting for FATEs");

        public static readonly LocString MountWhileWaitingHelp = new LocString("settings.general.mountWhileWaitingHelp", "Mount after a short delay while idle and waiting for FATEs. Disabled for Yo-kai medal runs.");

        public static readonly LocString Mount = new LocString("settings.general.mount", "Mount");

        public static readonly LocString MountHelp = new LocString("settings.general.mountHelp", "The mount FTA summons when it travels or mounts up while waiting. Random uses the game's mount roulette. Only flying mounts this character owns are listed.");

        public static readonly LocString MountRandom = new LocString("settings.general.mountRandom", "Random (roulette)");

        public static readonly LocString MountNotOwned = new LocString("settings.general.mountNotOwned", "This character does not own {0}; FTA uses the mount roulette instead.");

        public static readonly LocString EconomyTravel = new LocString("settings.general.economyTravel", "Economy mode");

        public static readonly LocString EconomyTravelHelp = new LocString("settings.general.economyTravelHelp", "Save gil by never teleporting to an aetheryte closer to the next FATE in the same zone; FTA mounts up and flies there instead. Teleports to other zones, and the teleport used to recover from being stuck, still happen.");

        public static readonly LocString AutoPause = new LocString("settings.general.autoPause", "Auto-pause in content");

        public static readonly LocString AutoPauseHelp = new LocString("settings.general.autoPauseHelp", "Pause the run while you are inside a duty, trial, raid, or any other instanced content, then resume it once you are back outside. Your zones, goal, and session stats are kept, and paused time does not count toward a time-based goal.");

        public static readonly LocString AutoResume = new LocString("settings.general.autoResume", "Auto-resume on fault");

        public static readonly LocString AutoResumeHelp = new LocString("settings.general.autoResumeHelp", "If the grind hits an unrecoverable error and stops, automatically restart it (up to 3 times in a row) instead of ending the run. Leave off if you want faults to surface.");

        public static readonly LocString StuckRescue = new LocString("settings.general.stuckRescue", "Teleport out when stuck");

        public static readonly LocString StuckRescueHelp = new LocString("settings.general.stuckRescueHelp", "If the run makes no progress for this long (no FATE finished, no zone change, no combat, and the character stays within 15 m of one spot), teleport to the nearest aetheryte in the zone and carry on from there. Catches cases like landing on water, where the yo-kai minion cannot be summoned. After 3 teleports with no FATE finished in between, the run is treated as faulted instead (see Auto-resume on fault).");

        public static readonly LocString StuckRescueAfter = new LocString("settings.general.stuckRescueAfter", "Stuck after");

        public static readonly LocString StuckRescueAfterHelp = new LocString("settings.general.stuckRescueAfterHelp", "How long the run must make no progress before FTA teleports out.");

        public static readonly LocString PostRunGroup = new LocString("settings.postRun.group", "Chat commands");

        public static readonly LocString PostRunLabel = new LocString("settings.postRun.label", "Run when the run finishes");

        public static readonly LocString PostRunHelp = new LocString("settings.postRun.help", "One command per line, each starting with a slash. They run when a run ends on its own, before the after-run action, and never after Stop or a failure.");

        public static readonly LocString PostRunCaption = new LocString("settings.postRun.caption", "Lines that do not start with a slash are skipped, so nothing is ever said in chat. Try /li home to head home or /ays m to hand off to AutoRetainer.");

        public static readonly LocString TravelFatePlay = new LocString("settings.travel.fatePlay", "FATE play");

        public static readonly LocString PullMany = new LocString("settings.travel.pullMany", "Pull as many mobs as possible");

        public static readonly LocString PullManyHelp = new LocString("settings.travel.pullManyHelp", "Tanks only. Instead of fighting whatever walks up, FTA tags every idle mob of the FATE in reach with your ranged attack, then lets BossMod AoE the pack down.");

        public static readonly LocString PullReach = new LocString("settings.travel.pullReach", "Pull reach");

        public static readonly LocString PullReachHelp = new LocString("settings.travel.pullReachHelp", "Only idle mobs this close to you, and inside the FATE ring, are pulled. Tank ranged attacks reach 20 m; farther mobs are walked to first.");

        public static readonly LocString PullMaxMobs = new LocString("settings.travel.pullMaxMobs", "Max mobs on you");

        public static readonly LocString PullMaxMobsHelp = new LocString("settings.travel.pullMaxMobsHelp", "Pulling stops once this many mobs have you on their enmity list, and picks up again as they die.");

        public static readonly LocString PullMaxMobsFormat = new LocString("settings.travel.pullMaxMobsFormat", "%d mobs");

        public static readonly LocString PullStopHp = new LocString("settings.travel.pullStopHp", "Stop pulling below HP");

        public static readonly LocString PullStopHpHelp = new LocString("settings.travel.pullStopHpHelp", "Below this HP the current pull ends and BossMod fights what is already on you.");

        public static readonly LocString PullResumeHp = new LocString("settings.travel.pullResumeHp", "Resume pulling at HP");

        public static readonly LocString PullResumeHpHelp = new LocString("settings.travel.pullResumeHpHelp", "After a low-HP stop, pulling waits until HP is back up to this. Never set below the stop point.");

        public static readonly LocString CollectHandIn = new LocString("settings.general.collectHandIn", "Hand in items yourself");

        public static readonly LocString CollectHandInHelp = new LocString("settings.general.collectHandInHelp", "FTA walks to the hand-in NPC as soon as you hold a batch, and turns in any leftovers once the FATE hits 100%. Off leaves turn-ins to BossMod's FATE helper, which only hands in at 10 items.");

        public static readonly LocString CollectHandInBatch = new LocString("settings.general.collectHandInBatch", "Hand in every");

        public static readonly LocString CollectHandInBatchHelp = new LocString("settings.general.collectHandInBatchHelp", "How many items to collect before each trip to the NPC. Smaller batches waste fewer items when other players finish the FATE first; larger ones mean fewer trips.");

        public static readonly LocString CollectHandInBatchFormat = new LocString("settings.general.collectHandInBatchFormat", "%d items");

        public static readonly LocString FiltersEligibility = new LocString("settings.filters.eligibility", "Eligibility");

        public static readonly LocString MinTime = new LocString("settings.filters.minTime", "Minimum time remaining");

        public static readonly LocString MinTimeHelp = new LocString("settings.filters.minTimeHelp", "Skip FATEs that have less than this many seconds left. Keeps you off corpse-FATEs other players are finishing.");

        public static readonly LocString MinTimeFormat = new LocString("settings.filters.minTimeFormat", "%d s");

        public static readonly LocString MaxProgress = new LocString("settings.filters.maxProgress", "Maximum progress");

        public static readonly LocString MaxProgressHelp = new LocString("settings.filters.maxProgressHelp", "Skip FATEs already past this percent. Keeps you off near-finished FATEs others are clearing.");

        public static readonly LocString MaxDuration = new LocString("settings.filters.maxDuration", "Skip long FATEs");

        public static readonly LocString MaxDurationHelp = new LocString("settings.filters.maxDurationHelp", "Skip FATEs whose full timer is longer than a limit, such as 30-minute boss FATEs that rarely finish.");

        public static readonly LocString MaxDurationLimit = new LocString("settings.filters.maxDurationLimit", "Longest timer");

        public static readonly LocString MaxDurationLimitHelp = new LocString("settings.filters.maxDurationLimitHelp", "FATEs with a longer full duration than this are skipped, whatever time is left on them.");

        public static readonly LocString MaxDurationOff = new LocString("settings.filters.maxDurationOff", "FATEs of any length are eligible.");

        public static readonly LocString LevelWindow = new LocString("settings.filters.levelWindow", "Level window");

        public static readonly LocString LevelWindowHelp = new LocString("settings.filters.levelWindowHelp", "Only target FATEs between two fixed levels, whatever your own level is. Handy for leveling a specific bracket or staying out of a zone's high-level corner.");

        public static readonly LocString LevelWindowOff = new LocString("settings.filters.levelWindowOff", "No fixed level window; only the band around your level applies, if it is on.");

        public static readonly LocString LevelLowest = new LocString("settings.filters.levelLowest", "Lowest level");

        public static readonly LocString LevelLowestHelp = new LocString("settings.filters.levelLowestHelp", "Skip FATEs below this level.");

        public static readonly LocString LevelHighest = new LocString("settings.filters.levelHighest", "Highest level");

        public static readonly LocString LevelHighestHelp = new LocString("settings.filters.levelHighestHelp", "Skip FATEs above this level.");

        public static readonly LocString LevelRange = new LocString("settings.filters.levelRange", "Restrict by level");

        public static readonly LocString LevelRangeHelp = new LocString("settings.filters.levelRangeHelp", "Only target FATEs within a level range of your current level. Useful in zones that mix low- and high-level FATEs (e.g. Lv 10-15 and Lv 40-45 in the same area) so the grinder doesn't drag you into one that can kill you.");

        public static readonly LocString LevelRangeOff = new LocString("settings.filters.levelRangeOff", "All FATE levels in your selected zones are eligible.");

        public static readonly LocString LevelBelow = new LocString("settings.filters.levelBelow", "Levels below you");

        public static readonly LocString LevelBelowHelp = new LocString("settings.filters.levelBelowHelp", "Skip FATEs more than this many levels below your current level.");

        public static readonly LocString LevelAbove = new LocString("settings.filters.levelAbove", "Levels above you");

        public static readonly LocString LevelAboveHelp = new LocString("settings.filters.levelAboveHelp", "Skip FATEs more than this many levels above your current level. Set to 0 to never fight FATEs above your own level.");

        public static readonly LocString LevelFormat = new LocString("settings.filters.levelFormat", "%d lvl");

        public static readonly LocString FiltersTypes = new LocString("settings.filters.types", "FATE types");

        public static readonly LocString SkipTypes = new LocString("settings.filters.skipTypes", "Skip FATE types");

        public static readonly LocString SkipTypesHelp = new LocString("settings.filters.skipTypesHelp", "Toggle on a type to skip every FATE of that kind. Useful if you don't enjoy escorts or collect hand-ins.");

        public static readonly LocString RuleNormal = new LocString("settings.filters.rule.normal", "Slay enemies");

        public static readonly LocString RuleNormalHelp = new LocString("settings.filters.rule.normalHelp", "Kill the target mobs in the FATE ring.");

        public static readonly LocString RuleCollect = new LocString("settings.filters.rule.collect", "Collect / hand-in");

        public static readonly LocString RuleCollectHelp = new LocString("settings.filters.rule.collectHelp", "Gather items off mobs or nodes and turn them in.");

        public static readonly LocString RuleEscort = new LocString("settings.filters.rule.escort", "Escort");

        public static readonly LocString RuleEscortHelp = new LocString("settings.filters.rule.escortHelp", "Protect an NPC that walks a fixed path.");

        public static readonly LocString RuleDefend = new LocString("settings.filters.rule.defend", "Defend");

        public static readonly LocString RuleDefendHelp = new LocString("settings.filters.rule.defendHelp", "Hold a point or NPC against waves.");

        public static readonly LocString RuleEvent = new LocString("settings.filters.rule.event", "Talk to NPC");

        public static readonly LocString RuleEventHelp = new LocString("settings.filters.rule.eventHelp", "Dialogue-style FATE that starts by interacting with an NPC.");

        public static readonly LocString RuleChase = new LocString("settings.filters.rule.chase", "Chase");

        public static readonly LocString RuleChaseHelp = new LocString("settings.filters.rule.chaseHelp", "Pursue a moving enemy across the zone.");

        public static readonly LocString RuleBoss = new LocString("settings.filters.rule.boss", "Boss");

        public static readonly LocString RuleBossHelp = new LocString("settings.filters.rule.bossHelp", "Single-boss encounter (notorious monster style).");

        public static readonly LocString RuleFete = new LocString("settings.filters.rule.fete", "Fete");

        public static readonly LocString RuleFeteHelp = new LocString("settings.filters.rule.feteHelp", "Special seasonal / community FATE.");

        public static readonly LocString CollectTextAdvanceNote = new LocString("settings.filters.collectTextAdvanceNote", "TextAdvance is installed but disabled. Collect hand-ins usually work anyway (FTA drives the turn-in directly), but enabling TextAdvance's \"Enable plugin\" toggle is the safe fallback if a hand-in stalls.");

        public static readonly LocString FiltersPriority = new LocString("settings.filters.priority", "Priority");

        public static readonly LocString FatePriority = new LocString("settings.filters.fatePriority", "FATE priority");

        public static readonly LocString FatePriorityHelp = new LocString("settings.filters.fatePriorityHelp", "Order the rules used to pick the next FATE. Top rule wins; ties fall through to the next. Reset restores the recommended order.");

        public static readonly LocString SortBonusTwist = new LocString("settings.filters.sort.bonusTwist", "Bonus FATE (skip while Twist active)");

        public static readonly LocString SortProgress = new LocString("settings.filters.sort.progress", "Progress %");

        public static readonly LocString SortBonus = new LocString("settings.filters.sort.bonus", "Bonus FATE");

        public static readonly LocString SortUrgent = new LocString("settings.filters.sort.urgent", "About to expire");

        public static readonly LocString SortDistance = new LocString("settings.filters.sort.distance", "Closest to me");

        public static readonly LocString SortTimeRemaining = new LocString("settings.filters.sort.timeRemaining", "Time remaining");

        public static readonly LocString SortLevel = new LocString("settings.filters.sort.level", "Level");

        public static readonly LocString SortName = new LocString("settings.filters.sort.name", "Name");

        public static readonly LocString ResetRecommended = new LocString("settings.filters.resetRecommended", "Reset to recommended");

        public static readonly LocString FiltersBlacklist = new LocString("settings.filters.blacklist", "Blacklist");

        public static readonly LocString AutoBlacklistDeaths = new LocString("settings.filters.autoBlacklistDeaths", "Blacklist after repeated deaths");

        public static readonly LocString AutoBlacklistDeathsHelp = new LocString("settings.filters.autoBlacklistDeathsHelp", "Once you die in the same FATE this many times in one run, it is added to the list below and the grind moves on. Remove it there to try it again.");

        public static readonly LocString AutoBlacklistDeathsOff = new LocString("settings.filters.autoBlacklistDeathsOff", "A FATE you keep dying in is retried after every revive.");

        public static readonly LocString DeathsBeforeBlacklist = new LocString("settings.filters.deathsBeforeBlacklist", "Deaths before blacklisting");

        public static readonly LocString DeathsBeforeBlacklistHelp = new LocString("settings.filters.deathsBeforeBlacklistHelp", "Deaths inside the same FATE during one run. A death anywhere else does not count.");

        public static readonly LocString DeathsFormat = new LocString("settings.filters.deathsFormat", "%d deaths");

        public static readonly LocString AddByName = new LocString("settings.filters.addByName", "Add a FATE by name");

        public static readonly LocString AddByNameHelp = new LocString("settings.filters.addByNameHelp", "Pulled live from game data, sorted A-Z. Type to search. Use this for FATEs you never want to trigger, like a world boss everyone is waiting on, without needing it to be up.");

        public static readonly LocString Blacklisted = new LocString("settings.filters.blacklisted", "Blacklisted FATEs");

        public static readonly LocString BlacklistedHelp = new LocString("settings.filters.blacklistedHelp", "Blacklisted FATEs are skipped while grinding. The ban button in the Live FATEs window adds to this list too. Remove one here to grind it again.");

        public static readonly LocString NoFatesInData = new LocString("settings.filters.noFatesInData", "No FATEs found in game data.");

        public static readonly LocString NoBlacklisted = new LocString("settings.filters.noBlacklisted", "No FATEs blacklisted.");

        public static readonly LocString RemoveFromBlacklist = new LocString("settings.filters.removeFromBlacklist", "Remove from blacklist.");

        public static readonly LocString SearchHint = new LocString("settings.searchHint", "Search...");

        public static readonly LocString ClassesSwitching = new LocString("settings.classes.switching", "Class switching");

        public static readonly LocString SwitchOnStart = new LocString("settings.classes.switchOnStart", "Switch class when run starts");

        public static readonly LocString SwitchOnStartHelp = new LocString("settings.classes.switchOnStartHelp", "Equip the first eligible gearset below when you press Start. Disable to leave the run on whatever class you're currently on.");

        public static readonly LocString SwitchingOff = new LocString("settings.classes.switchingOff", "Class switching is off. Enable it to configure the queue.");

        public static readonly LocString ClassesDone = new LocString("settings.classes.done", "When the queue is done");

        public static readonly LocString AllCapped = new LocString("settings.classes.allCapped", "All classes capped");

        public static readonly LocString AllCappedHelp = new LocString("settings.classes.allCappedHelp", "What to do after every queued class has hit its level cap.");

        public static readonly LocString DoneKeepName = new LocString("settings.classes.doneKeep.name", "Keep grinding on the last class");

        public static readonly LocString DoneKeepDetail = new LocString("settings.classes.doneKeep.detail", "When every queued class is capped, keep going on the last one.");

        public static readonly LocString DoneStopName = new LocString("settings.classes.doneStop.name", "Stop the run");

        public static readonly LocString DoneStopDetail = new LocString("settings.classes.doneStop.detail", "When every queued class is capped, end the run.");

        public static readonly LocString ClassesQueue = new LocString("settings.classes.queue", "Queue");

        public static readonly LocString AddGearset = new LocString("settings.classes.addGearset", "Add a gearset");

        public static readonly LocString AddGearsetHelp = new LocString("settings.classes.addGearsetHelp", "Use the gear-set number shown in your in-game Gear Set list (1-100). Class is resolved automatically.");

        public static readonly LocString QueueOrder = new LocString("settings.classes.queueOrder", "Queue order");

        public static readonly LocString QueueOrderHelp = new LocString("settings.classes.queueOrderHelp", "Order matters: top entry runs first, then advances when its level cap is hit.");

        public static readonly LocString NoGearsets = new LocString("settings.classes.noGearsets", "No gearsets found. Save one in-game (Character -> Gear Set List) first.");

        public static readonly LocString Queued = new LocString("settings.classes.queued", "(queued)");

        public static readonly LocString AlreadyQueued = new LocString("settings.classes.alreadyQueued", "Already in the queue.");

        public static readonly LocString NoClassesQueued = new LocString("settings.classes.noClassesQueued", "No classes queued. Automation will use whatever class you're on.");

        public static readonly LocString QueueEntry = new LocString("settings.classes.queueEntry", "{0} - gearset {1}");

        public static readonly LocString EntryLevel = new LocString("settings.classes.entryLevel", "(lvl {0})");

        public static readonly LocString NoCap = new LocString("settings.classes.noCap", "no cap");

        public static readonly LocString StopAtLevel = new LocString("settings.classes.stopAtLevel", "Stop at %d Level");

        public static readonly LocString GemsTrigger = new LocString("settings.gems.trigger", "Trade trigger");

        public static readonly LocString AutoTrade = new LocString("settings.gems.autoTrade", "Auto-trade at threshold");

        public static readonly LocString AutoTradeHelp = new LocString("settings.gems.autoTradeHelp", "When your Bicolor Gemstone inventory reaches the threshold below, the plugin teleports to a trader and buys from your shopping list.");

        public static readonly LocString AutoTradeOff = new LocString("settings.gems.autoTradeOff", "Auto-trade is off. Enable it to configure the trade.");

        public static readonly LocString Threshold = new LocString("settings.gems.threshold", "Trade threshold");

        public static readonly LocString ThresholdHelp = new LocString("settings.gems.thresholdHelp", "Gem count that triggers the trade. Game cap is 1500. Lower values trade more often so fewer FATEs are wasted near cap.");

        public static readonly LocString GemsFormat = new LocString("settings.gems.gemsFormat", "%d gems");

        public static readonly LocString GemsItem = new LocString("settings.gems.item", "What to buy");

        public static readonly LocString AddTradeItem = new LocString("settings.gems.addItem", "Add an item");

        public static readonly LocString ItemToBuyHelp = new LocString("settings.gems.itemToBuyHelp", "Pulled live from game data, sorted A-Z. Type to search. Cost shown in gems per one.");

        public static readonly LocString NoShopItems = new LocString("settings.gems.noShopItems", "No gem-shop items found.");

        public static readonly LocString TraderLocked = new LocString("settings.gems.traderLocked", "No trader you can reach sells {0}. It is only sold in {1}. Attune an aetheryte there, or pick another item.");

        public static readonly LocString TraderMissing = new LocString("settings.gems.traderMissing", "No known Bicolor trader sells {0}. Pick another item.");

        public static readonly LocString AlreadyListed = new LocString("settings.gems.alreadyListed", "Already on the list.");

        public static readonly LocString ShoppingList = new LocString("settings.gems.list", "Shopping list");

        public static readonly LocString ShoppingListHelp = new LocString("settings.gems.listHelp", "Bought from the top down. Each trade goes to a trader for the first item still needed and also buys the items below it that the same trader sells, then does the same at the trader for the next item still needed until nothing more on the list can be bought. Collectibles such as minions and orchestrion rolls are bought once and skipped after you own or learn them.");

        public static readonly LocString ShoppingListEmpty = new LocString("settings.gems.listEmpty", "The list is empty, so auto-trade has nothing to buy. Add an item above.");

        public static readonly LocString UnknownTradeItem = new LocString("settings.gems.unknownItem", "Unknown item ({0})");

        public static readonly LocString StatusLearned = new LocString("settings.gems.statusLearned", "Learned");

        public static readonly LocString StatusHeld = new LocString("settings.gems.statusHeld", "In your inventory");

        public static readonly LocString StatusHave = new LocString("settings.gems.statusHave", "You have {0}");

        public static readonly LocString StatusHaveOf = new LocString("settings.gems.statusHaveOf", "You have {0} of {1}");

        public static readonly LocString StatusNoTrader = new LocString("settings.gems.statusNoTrader", "No trader you can reach");

        public static readonly LocString StatusSkipped = new LocString("settings.gems.statusSkipped", "Skipped for this run");

        public static readonly LocString BuyOnce = new LocString("settings.gems.buyOnce", "Buy once");

        public static readonly LocString KeepBuying = new LocString("settings.gems.keepBuying", "Keep buying");

        public static readonly LocString StopAtCount = new LocString("settings.gems.stopAtCount", "Stop at %d");

        public static readonly LocString GemsSpend = new LocString("settings.gems.spend", "How much to spend");

        public static readonly LocString SpendStrategy = new LocString("settings.gems.spendStrategy", "Spend strategy");

        public static readonly LocString SpendStrategyHelp = new LocString("settings.gems.spendStrategyHelp", "How much each trade spends when it fires.");

        public static readonly LocString SpendAllName = new LocString("settings.gems.spendAll.name", "Spend all gemstones");

        public static readonly LocString SpendAllDetail = new LocString("settings.gems.spendAll.detail", "Spend everything above the reserve on each trade.");

        public static readonly LocString SpendUpToName = new LocString("settings.gems.spendUpTo.name", "Spend up to a set amount");

        public static readonly LocString SpendUpToDetail = new LocString("settings.gems.spendUpTo.detail", "Cap how many gems each trade is allowed to spend.");

        public static readonly LocString BuyFixedName = new LocString("settings.gems.buyFixed.name", "Buy a fixed number");

        public static readonly LocString BuyFixedDetail = new LocString("settings.gems.buyFixed.detail", "Buy a set quantity of each item on each trade.");

        public static readonly LocString SpendUpTo = new LocString("settings.gems.spendUpToRow", "Spend up to");

        public static readonly LocString SpendUpToHelp = new LocString("settings.gems.spendUpToRowHelp", "Maximum gems spent per trade.");

        public static readonly LocString BuyQuantity = new LocString("settings.gems.buyQuantity", "Buy quantity");

        public static readonly LocString BuyQuantityHelp = new LocString("settings.gems.buyQuantityHelp", "How many of each item to buy per trade.");

        public static readonly LocString BuyQuantityFormat = new LocString("settings.gems.buyQuantityFormat", "%d of each");

        public static readonly LocString Reserve = new LocString("settings.gems.reserve", "Keep in reserve");

        public static readonly LocString ReserveHelp = new LocString("settings.gems.reserveHelp", "Gems left untouched on every trade. Use this when you want to save toward a pricier item without turning auto-trade off.");

        public static readonly LocString GemsAfter = new LocString("settings.gems.after", "After the trade");

        public static readonly LocString WhenDone = new LocString("settings.gems.whenDone", "When done");

        public static readonly LocString WhenDoneHelp = new LocString("settings.gems.whenDoneHelp", "What to do once the buy succeeds.");

        public static readonly LocString AfterResumeName = new LocString("settings.gems.afterResume.name", "Resume the grind");

        public static readonly LocString AfterResumeDetail = new LocString("settings.gems.afterResume.detail", "Keep grinding FATEs in the same zone after the buy.");

        public static readonly LocString AfterStopName = new LocString("settings.gems.afterStop.name", "Stop the run");

        public static readonly LocString AfterStopDetail = new LocString("settings.gems.afterStop.detail", "End the run once the buy succeeds.");

        public static readonly LocString PreviewPlan = new LocString("settings.gems.previewPlan", "At threshold {0}g (keeping {1}g), the next trade buys {2} for {3}g.");

        public static readonly LocString PreviewItem = new LocString("settings.gems.previewItem", "{0} x {1}");

        public static readonly LocString PreviewNothing = new LocString("settings.gems.previewNothing", "At threshold {0}g (keeping {1}g), nothing on the list can be bought.");

        public static readonly LocString ItemCostLabel = new LocString("settings.gems.itemCostLabel", "{0}  ({1}g)");

        public static readonly LocString RepairTrigger = new LocString("settings.repair.trigger", "Repair trigger");

        public static readonly LocString AutoRepair = new LocString("settings.repair.autoRepair", "Auto-repair gear");

        public static readonly LocString AutoRepairHelp = new LocString("settings.repair.autoRepairHelp", "Between FATEs, when the lowest equipped item drops to or below the threshold, the plugin runs a repair. At 0% the gear stops working, so keep some margin.");

        public static readonly LocString AutoRepairOff = new LocString("settings.repair.autoRepairOff", "Auto-repair is off. Enable it to configure repair.");

        public static readonly LocString RepairThreshold = new LocString("settings.repair.threshold", "Repair threshold");

        public static readonly LocString RepairThresholdHelp = new LocString("settings.repair.thresholdHelp", "Trips when the worst equipped slot reaches this condition percentage. 20% leaves comfortable margin before the 0% breakdown.");

        public static readonly LocString RepairSource = new LocString("settings.repair.source", "Repair source");

        public static readonly LocString RepairSourceHelp = new LocString("settings.repair.sourceHelp", "How the repair is performed. Self-repair uses Dark Matter from your bag (no travel). NPC repair travels to your Grand Company mender.");

        public static readonly LocString RepairSelfThenNpcName = new LocString("settings.repair.selfThenNpc.name", "Self, then NPC");

        public static readonly LocString RepairSelfThenNpcDetail = new LocString("settings.repair.selfThenNpc.detail", "Use Dark Matter from your bag first; fall back to the Grand Company mender when you run out.");

        public static readonly LocString RepairSelfOnlyName = new LocString("settings.repair.selfOnly.name", "Self only");

        public static readonly LocString RepairSelfOnlyDetail = new LocString("settings.repair.selfOnly.detail", "Repair with Dark Matter from your bag. No travel.");

        public static readonly LocString RepairNpcOnlyName = new LocString("settings.repair.npcOnly.name", "NPC only");

        public static readonly LocString RepairNpcOnlyDetail = new LocString("settings.repair.npcOnly.detail", "Travel to your Grand Company mender (or a custom NPC) and pay in seals.");

        public static readonly LocString CustomNpc = new LocString("settings.repair.customNpc", "Custom repair NPC");

        public static readonly LocString CustomNpcHelp = new LocString("settings.repair.customNpcHelp", "Optional. Travel to any repair NPC instead of the Grand Company mender. Target the NPC in-game, then click \"Set from target\". Clear to fall back to the GC mender.");

        public static readonly LocString NpcNote = new LocString("settings.repair.npcNote", "NPC repair uses your custom repair NPC if set, otherwise your Grand Company mender (teleports there and pays in company seals). A custom NPC removes the Grand Company requirement.");

        public static readonly LocString NpcSet = new LocString("settings.repair.npcSet", "{0}  (territory {1})");

        public static readonly LocString NpcNone = new LocString("settings.repair.npcNone", "None - using Grand Company mender.");

        public static readonly LocString SetFromTarget = new LocString("settings.repair.setFromTarget", "Set from target");

        public static readonly LocString NoTargetChat = new LocString("settings.repair.noTargetChat", "[FTA] No target - target a repair NPC first, then click again.");

        public static readonly LocString NpcSetChat = new LocString("settings.repair.npcSetChat", "[FTA] Custom repair NPC set: {0} (territory {1}).");

        public static readonly LocString ConsumablesGroup = new LocString("settings.consumables.group", "Consumables");

        public static readonly LocString AutoSummonChocobo = new LocString("settings.consumables.autoSummonChocobo", "Summon chocobo");

        public static readonly LocString AutoSummonChocoboHelp = new LocString("settings.consumables.autoSummonChocoboHelp", "Let BossMod's FATE helper resummon your chocobo with Gysahl Greens when its timer drops below 60 seconds while synced to a FATE. Turn off to stop that during runs; your BossMod preset itself is never changed.");

        public static readonly LocString AutoConsume = new LocString("settings.consumables.autoConsume", "Auto-consume food & medicine");

        public static readonly LocString AutoConsumeHelp = new LocString("settings.consumables.autoConsumeHelp", "Use food and medicine between FATEs to keep their buffs up; Well Fed alone is a free +3% EXP. Items are consumed only when out of combat, and refreshed before the buff runs out.");

        public static readonly LocString AutoConsumeOff = new LocString("settings.consumables.autoConsumeOff", "Auto-consume is off. Enable it to pick items.");

        public static readonly LocString RefreshUnder = new LocString("settings.consumables.refreshUnder", "Refresh when under");

        public static readonly LocString RefreshUnderHelp = new LocString("settings.consumables.refreshUnderHelp", "Re-consume once the buff has fewer than this many minutes left. 0 only re-applies after it fully wears off. Food and medicine last 30 minutes.");

        public static readonly LocString RefreshWornOff = new LocString("settings.consumables.refreshWornOff", "only when worn off");

        public static readonly LocString RefreshFormat = new LocString("settings.consumables.refreshFormat", "%d min left");

        public static readonly LocString ConsumablesItems = new LocString("settings.consumables.items", "Items");

        public static readonly LocString AddItem = new LocString("settings.consumables.addItem", "Add an item");

        public static readonly LocString AddItemHelp = new LocString("settings.consumables.addItemHelp", "Pick from the food and medicine in your bag. HQ is used automatically when you have it.");

        public static readonly LocString ActiveItems = new LocString("settings.consumables.activeItems", "Active items");

        public static readonly LocString ActiveItemsHelp = new LocString("settings.consumables.activeItemsHelp", "Each is kept active in order; the next available one is consumed if the first runs out.");

        public static readonly LocString NoneInBag = new LocString("settings.consumables.noneInBag", "No food or medicine in your bag. Stock some, then add it here.");

        public static readonly LocString KindFood = new LocString("settings.consumables.kindFood", "Food");

        public static readonly LocString KindMedicine = new LocString("settings.consumables.kindMedicine", "Medicine");

        public static readonly LocString Added = new LocString("settings.consumables.added", "(added)");

        public static readonly LocString ItemLabel = new LocString("settings.consumables.itemLabel", "{0}  [{1}]{2}");

        public static readonly LocString AlreadyAdded = new LocString("settings.consumables.alreadyAdded", "Already added.");

        public static readonly LocString NoItemsAdded = new LocString("settings.consumables.noItemsAdded", "No items added - nothing will be consumed.");

        public static readonly LocString WellFed = new LocString("settings.consumables.wellFed", "Well Fed");

        public static readonly LocString Medicated = new LocString("settings.consumables.medicated", "Medicated");

        public static readonly LocString NoneInBagShort = new LocString("settings.consumables.noneInBagShort", "{0}, none in bag");

        public static readonly LocString HumanizerBreaks = new LocString("settings.humanizer.breaks", "Breaks");

        public static readonly LocString HumanizerEnable = new LocString("settings.humanizer.enable", "Take periodic city breaks");

        public static readonly LocString HumanizerEnableHelp = new LocString("settings.humanizer.enableHelp", "Every N FATEs, take a few minutes off: wander around a random city or stand at one of your saved spots, then resume. Helps you avoid player reports by acting a little more human, useful when you leave the PC running for long sessions and don't want others noticing you grinding FATEs non-stop.");

        public static readonly LocString HumanizerOff = new LocString("settings.humanizer.off", "Humanizer is off. Enable it to configure breaks.");

        public static readonly LocString FatesBetween = new LocString("settings.humanizer.fatesBetween", "FATEs between breaks");

        public static readonly LocString FatesBetweenHelp = new LocString("settings.humanizer.fatesBetweenHelp", "Take a break after this many completed FATEs. The counter resets after each break.");

        public static readonly LocString FatesFormat = new LocString("settings.humanizer.fatesFormat", "%d FATEs");

        public static readonly LocString BreakLength = new LocString("settings.humanizer.breakLength", "Break length");

        public static readonly LocString BreakLengthHelp = new LocString("settings.humanizer.breakLengthHelp", "A random duration between these two values is rolled for each break.");

        public static readonly LocString MinutesFormat = new LocString("settings.humanizer.minutesFormat", "%d min");

        public static readonly LocString BreakActivity = new LocString("settings.humanizer.breakActivity", "Break activity");

        public static readonly LocString BreakActivityHelp = new LocString("settings.humanizer.breakActivityHelp", "What your character does during a break.");

        public static readonly LocString BreakActivityWanderName = new LocString("settings.humanizer.activityWanderName", "Wander a city");

        public static readonly LocString BreakActivityWanderDetail = new LocString("settings.humanizer.activityWanderDetail", "Teleport to one of your cities and walk between random points until the break ends.");

        public static readonly LocString BreakActivityIdleName = new LocString("settings.humanizer.activityIdleName", "Idle at a spot");

        public static readonly LocString BreakActivityIdleDetail = new LocString("settings.humanizer.activityIdleDetail", "Go to one of your saved spots, such as a summoning bell, a vendor or an aetheryte, and stand there until the break ends.");

        public static readonly LocString HumanizerWandering = new LocString("settings.humanizer.wandering", "Wandering");

        public static readonly LocString PauseBetween = new LocString("settings.humanizer.pauseBetween", "Pause between walks");

        public static readonly LocString PauseBetweenHelp = new LocString("settings.humanizer.pauseBetweenHelp", "After arriving at each random point, stand still for a random duration in this range before walking somewhere else.");

        public static readonly LocString SecondsFormat = new LocString("settings.humanizer.secondsFormat", "%d s");

        public static readonly LocString WalkDistance = new LocString("settings.humanizer.walkDistance", "Walk distance");

        public static readonly LocString WalkDistanceHelp = new LocString("settings.humanizer.walkDistanceHelp", "Each random destination is rolled this many meters away from your current position. Larger ranges cover more of the city; smaller ranges keep you near the aetheryte.");

        public static readonly LocString MetersFormat = new LocString("settings.humanizer.metersFormat", "%d m");

        public static readonly LocString HumanizerCities = new LocString("settings.humanizer.cities", "Cities");

        public static readonly LocString AllowedCities = new LocString("settings.humanizer.allowedCities", "Allowed cities");

        public static readonly LocString AllowedCitiesHelp = new LocString("settings.humanizer.allowedCitiesHelp", "Tick the cities the plugin is allowed to teleport to. One is picked at random each break. Untick cities you haven't unlocked or don't want visited.");

        public static readonly LocString NoCities = new LocString("settings.humanizer.noCities", "No cities selected - Humanizer will skip the break and keep grinding.");

        public static readonly LocString IdleSpots = new LocString("settings.humanizer.idleSpots", "Idle spots");

        public static readonly LocString SavedSpots = new LocString("settings.humanizer.savedSpots", "Saved spots");

        public static readonly LocString SavedSpotsHelp = new LocString("settings.humanizer.savedSpotsHelp", "Stand where you want to idle and press Save this spot. Target the bell, vendor or aetheryte first and the spot takes its name. With more than one saved, each break picks one at random and never the same one twice in a row.");

        public static readonly LocString NoSpots = new LocString("settings.humanizer.noSpots", "No spots saved yet. Until you save one, breaks idle next to the aetheryte of one of the cities below.");

        public static readonly LocString SaveSpot = new LocString("settings.humanizer.saveSpot", "Save this spot");

        public static readonly LocString SpotSavedChat = new LocString("settings.humanizer.spotSavedChat", "[FTA] Idle spot saved in {0}.");

        public static readonly LocString SpotAirborneChat = new LocString("settings.humanizer.spotAirborneChat", "[FTA] Land first: an idle spot has to be on the ground.");

        public static readonly LocString SpotNoAetheryteChat = new LocString("settings.humanizer.spotNoAetheryteChat", "[FTA] FTA can't teleport here, so a break could never come back to this spot. Save one in a city or a zone with an aetheryte.");

        public static readonly LocString Pacing = new LocString("settings.humanizer.pacing", "Pacing");

        public static readonly LocString PacingEnable = new LocString("settings.humanizer.pacingEnable", "Vary timing between FATEs");

        public static readonly LocString PacingEnableHelp = new LocString("settings.humanizer.pacingEnableHelp", "Every player running FTA sees a FATE end at the same moment, so without this they all move on, teleport and swap zones in perfect sync. When on, each step waits a short random delay, and the teleport shortcut distance, zone swap wait, follow-up watch, release after a KO and FATEs between breaks are rolled fresh every time.");

        public static readonly LocString PacingOff = new LocString("settings.humanizer.pacingOff", "Pacing is off. Every step starts the moment it can, in sync with other FTA users.");

        public static readonly LocString ReactionDelay = new LocString("settings.humanizer.reactionDelay", "Reaction delay");

        public static readonly LocString ReactionDelayHelp = new LocString("settings.humanizer.reactionDelayHelp", "How long to wait after a FATE ends or a new one appears before moving on. Rolls lean toward the short end, and now and then a longer pause is added.");

        public static readonly LocString PickVariety = new LocString("settings.humanizer.pickVariety", "Vary FATE choice");

        public static readonly LocString PickVarietyHelp = new LocString("settings.humanizer.pickVarietyHelp", "When a few FATEs rank about the same, head to a random one of them instead of always the top pick. FTA users nearby then spread out, at the cost of slightly longer trips.");

        public static readonly LocString CombatMovement = new LocString("settings.humanizer.combatMovement", "Combat movement");

        public static readonly LocString CombatMovementEnable = new LocString("settings.humanizer.combatMovementEnable", "Vary dodging in combat");

        public static readonly LocString CombatMovementEnableHelp = new LocString("settings.humanizer.combatMovementEnableHelp", "BossMod steps out of an AoE the moment it is telegraphed and stops right at its edge, so every FTA user in a FATE dodges on the same frame to the same spot. When on, each FATE rolls a dodge delay, a movement delay and a preference for extra room from the ranges below. Your combat preset itself is never changed.");

        public static readonly LocString CombatMovementOff = new LocString("settings.humanizer.combatMovementOff", "Combat movement variation is off. BossMod dodges the way your combat preset is set up.");

        public static readonly LocString CombatMovementNoModule = new LocString("settings.humanizer.combatMovementNoModule", "The combat preset '{0}' has no Automatic movement (AI) module, so these settings do nothing. Add that module to the preset in BossMod, or switch back to the bundled FATE Autopilot preset.");

        public static readonly LocString DodgeDelay = new LocString("settings.humanizer.dodgeDelay", "Dodge delay");

        public static readonly LocString DodgeDelayHelp = new LocString("settings.humanizer.dodgeDelayHelp", "How long BossMod waits before stepping out of a telegraphed AoE. One value is rolled per FATE from this range. Longer delays mean getting hit more often, more deaths, and more FATEs auto-blacklisted for deaths. Casters may also see more hard casts cancelled.");

        public static readonly LocString MoveDelay = new LocString("settings.humanizer.moveDelay", "Movement delay");

        public static readonly LocString MoveDelayHelp = new LocString("settings.humanizer.moveDelayHelp", "How long BossMod waits before regular repositioning, such as closing in on a target. It only applies when starting to move from standing still. Most users can leave this at None.");

        public static readonly LocString DangerRoom = new LocString("settings.humanizer.dangerRoom", "Prefer extra room from danger");

        public static readonly LocString DangerRoomHelp = new LocString("settings.humanizer.dangerRoomHelp", "Asks BossMod to prefer spots further inside safe ground rather than the very edge of an AoE. This is a soft preference, not a guaranteed distance: it depends on BossMod's map resolution, and the lower levels may change nothing in large FATEs.");

        public static readonly LocString MovementLevelNone = new LocString("settings.humanizer.levelNone", "None");

        public static readonly LocString DelayShort = new LocString("settings.humanizer.delayShort", "0.5 s");

        public static readonly LocString DelayLong = new LocString("settings.humanizer.delayLong", "1 s");

        public static readonly LocString RoomSmall = new LocString("settings.humanizer.roomSmall", "Small");

        public static readonly LocString RoomMedium = new LocString("settings.humanizer.roomMedium", "Medium");

        public static readonly LocString RoomLarge = new LocString("settings.humanizer.roomLarge", "Large");

        public static readonly LocString InvitesDecline = new LocString("settings.invites.decline", "Decline");

        public static readonly LocString AutoDecline = new LocString("settings.invites.autoDecline", "Auto-decline party invites");

        public static readonly LocString AutoDeclineHelp = new LocString("settings.invites.autoDeclineHelp", "While a grind run is active, automatically decline incoming party invites after a short random delay. Invites that arrive while idle or playing manually are left alone for you to handle.");

        public static readonly LocString AutoDeclineOff = new LocString("settings.invites.autoDeclineOff", "Auto-decline is off. Enable it to configure it.");

        public static readonly LocString DeclineDelay = new LocString("settings.invites.delay", "Decline delay");

        public static readonly LocString DeclineDelayHelp = new LocString("settings.invites.delayHelp", "Wait a random time in this range before declining, so it looks like you noticed the popup and dismissed it yourself.");

        public static readonly LocString InvitesReply = new LocString("settings.invites.reply", "Reply");

        public static readonly LocString SendReply = new LocString("settings.invites.sendReply", "Send a reply");

        public static readonly LocString SendReplyHelp = new LocString("settings.invites.sendReplyHelp", "After declining, send a chat message so it reads like a polite human brush-off rather than an instant silent decline.");

        public static readonly LocString ReplyChannel = new LocString("settings.invites.channel", "Reply channel");

        public static readonly LocString ReplyChannelHelp = new LocString("settings.invites.channelHelp", "Where the message goes. \"Tell inviter\" whispers the person who invited you. Ignored when your message starts with a slash command.");

        public static readonly LocString ChannelTellName = new LocString("settings.invites.channelTell.name", "Tell inviter");

        public static readonly LocString ChannelTellDetail = new LocString("settings.invites.channelTell.detail", "Whisper the person who invited you.");

        public static readonly LocString ChannelSayName = new LocString("settings.invites.channelSay.name", "Say");

        public static readonly LocString ChannelSayDetail = new LocString("settings.invites.channelSay.detail", "Local /say, heard by players near you.");

        public static readonly LocString ChannelYellName = new LocString("settings.invites.channelYell.name", "Yell");

        public static readonly LocString ChannelYellDetail = new LocString("settings.invites.channelYell.detail", "Zone-wide /yell.");

        public static readonly LocString ReplyMessage = new LocString("settings.invites.message", "Reply message");

        public static readonly LocString ReplyMessageHelp = new LocString("settings.invites.messageHelp", "Use {name} for the inviter's character name and {world} for their home world. If the message begins with \"/\", it's sent verbatim as a command (e.g. /tell {name}@{world} busy right now!).");

        public static readonly LocString ReplyMessageHint = new LocString("settings.invites.messageHint", "Sorry {name}, I'm busy right now!");

        public static readonly LocString GmAlerts = new LocString("settings.gm.alerts", "Alerts");

        public static readonly LocString GmStopRun = new LocString("settings.gm.stopRun", "Stop the run");

        public static readonly LocString GmStopRunHelp = new LocString("settings.gm.stopRunHelp", "Halt automation immediately when a GM appears in your zone. Strongly recommended; the rest of the alerts are useless if the bot keeps grinding.");

        public static readonly LocString GmToast = new LocString("settings.gm.toast", "Toast notification");

        public static readonly LocString GmToastHelp = new LocString("settings.gm.toastHelp", "Pop a Dalamud toast: \"GM <name> is nearby!\"");

        public static readonly LocString GmChat = new LocString("settings.gm.chat", "Chat alert");

        public static readonly LocString GmChatHelp = new LocString("settings.gm.chatHelp", "Print a red chat warning into your local log.");

        public static readonly LocString GmSound = new LocString("settings.gm.sound", "Sound beeps");

        public static readonly LocString GmSoundHelp = new LocString("settings.gm.soundHelp", "Plays a series of system beeps through your speakers. Loud enough to grab your attention if you're tabbed away.");

        public static readonly LocString BeepCount = new LocString("settings.gm.beepCount", "Beep count");

        public static readonly LocString BeepCountHelp = new LocString("settings.gm.beepCountHelp", "How many beeps to play in the burst.");

        public static readonly LocString BeepCountFormat = new LocString("settings.gm.beepCountFormat", "%d beeps");

        public static readonly LocString BeepLength = new LocString("settings.gm.beepLength", "Beep length");

        public static readonly LocString BeepLengthHelp = new LocString("settings.gm.beepLengthHelp", "How long each beep lasts.");

        public static readonly LocString BeepLengthFormat = new LocString("settings.gm.beepLengthFormat", "%d ms each");

        public static readonly LocString BeepPitch = new LocString("settings.gm.beepPitch", "Beep pitch");

        public static readonly LocString BeepPitchHelp = new LocString("settings.gm.beepPitchHelp", "Tone frequency of each beep.");

        public static readonly LocString BeepPitchFormat = new LocString("settings.gm.beepPitchFormat", "%d Hz");

        public static readonly LocString GmActions = new LocString("settings.gm.actions", "Actions");

        public static readonly LocString GmCommands = new LocString("settings.gm.commands", "Custom commands");

        public static readonly LocString GmCommandsHelp = new LocString("settings.gm.commandsHelp", "Chat commands to run when a GM is spotted. Useful for things like /logout, /sh stay calm, or a macro.");

        public static readonly LocString GmKill = new LocString("settings.gm.kill", "Kill the game");

        public static readonly LocString GmKillHelp = new LocString("settings.gm.killHelp", "Hard-terminate the game process via /xlkill. The last-resort option; no goodbyes, no cutscene, no logout. You'll get a disconnect.");

        public static readonly LocString NoCommands = new LocString("settings.gm.noCommands", "No commands queued.");
    }

    internal static class Plugin
    {
        public static readonly LocString CommandHelp = new LocString("plugin.commandHelp", "Toggle the FATE Autopilot window. /fta start | stop | stop soft (finish the current FATE first) | run <count> (stop after that many FATEs) | pause | config | stats | deps | log | changelog | about | target.");

        public static readonly LocString CommandHelpAlias = new LocString("plugin.commandHelpAlias", "Alias for /fta.");
    }
}
