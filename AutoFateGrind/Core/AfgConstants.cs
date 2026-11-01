namespace AutoFateGrind.Core;

internal static class AfgConstants
{
    internal static class ThrottleKeys
    {
        public const string Save = "FateAutopilot.Save";

        public const string RepairTrigger = "FTA.Repair.Trigger";

        public const string RepairAll = "FTA.Repair.RepairAll";

        public const string RepairYesno = "FTA.Repair.Yesno";

        public const string RepairIconString = "FTA.Repair.IconString";

        public const string ShopClickIconString = "FTA.ClickSelectIconString";

        public const string ShopClickYesno = "FTA.ClickSelectYesno";

        public const string ShopBuyCurrency = "FTA.BuyFromCurrencyShop";

        public const string FoodUse = "FTA.Food.Use";

        public const string NpcTalk = "FTA.Npc.Talk";

        public const string NpcSelectYesno = "FTA.Npc.SelectYesno";

        public const string NpcSelectString = "FTA.Npc.SelectString";

        public const string RequestHandOver = "FTA.Request.HandOver";

        public const string RequestFillOpen = "FTA.Request.FillOpen";

        public const string RequestFillPick = "FTA.Request.FillPick";
    }

    internal static class AddonNames
    {
        public const string SelectYesno = "SelectYesno";

        public const string SelectIconString = "SelectIconString";

        public const string SelectString = "SelectString";

        public const string Talk = "Talk";

        public const string Shop = "Shop";

        public const string ShopExchangeCurrency = "ShopExchangeCurrency";

        public const string Repair = "Repair";

        public const string Request = "Request";

        public const string ContextIconMenu = "ContextIconMenu";

        public const string TelepotTown = "TelepotTown";
    }

    public const string PrimaryCommand = "/fta";

    public const string AliasCommand = "/fateautopilot";

    public const string BundledCombatPresetName = "FATE Autopilot";

    public const string TextAdvanceCallerName = "FateAutopilot";

    public const string LogPrefix = "[FTA]";

    public const int BicolorCap = 1500;

    public const int FollowUpWaitMs = 15000;

    public const int PostRunCommandSpacingMs = 500;

    public const int AddonInteractThrottleMs = 500;

    public const int SaveThrottleMs = 500;
}
