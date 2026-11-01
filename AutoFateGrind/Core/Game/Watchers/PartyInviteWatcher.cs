using System;
using System.Text;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

namespace AutoFateGrind.Core.Game.Watchers;

internal sealed class PartyInviteWatcher : IDisposable
{
    private enum Stage : byte
    {
        Idle,
        DeclinePending,
        ConfirmPending
    }

    private readonly struct PromptTemplate
    {
        public static readonly PromptTemplate Invalid = new PromptTemplate("", "");

        private readonly string prefix;

        private readonly string suffix;

        private PromptTemplate(string prefix, string suffix)
        {
            this.prefix = prefix;
            this.suffix = suffix;
        }

        public bool IsValid => prefix.Length + suffix.Length > 0;

        public bool Matches(string prompt) => IsValid && prompt.Length > prefix.Length + suffix.Length && prompt.StartsWith(prefix, StringComparison.Ordinal) && prompt.EndsWith(suffix, StringComparison.Ordinal);

        public override string ToString() => prefix + "<name>" + suffix;

        public static PromptTemplate FromAddonRow(uint rowId)
        {
            var addonRow = Svc.Data.GetExcelSheet<Addon>().GetRowOrDefault(rowId);
            if (!addonRow.HasValue)
            {
                return Invalid;
            }
            var prefixBuilder = new StringBuilder();
            var suffixBuilder = new StringBuilder();
            var seenPlaceholder = false;
            foreach (var segment in addonRow.Value.Text)
            {
                var payload = segment;
                if (payload.Type == ReadOnlySePayloadType.Text)
                {
                    (seenPlaceholder ? suffixBuilder : prefixBuilder).Append(Encoding.UTF8.GetString(payload.Body.Span));
                }
                else if (!IsLayoutMacro(in payload))
                {
                    seenPlaceholder = true;
                    suffixBuilder.Clear();
                }
            }
            if (!seenPlaceholder)
            {
                return Invalid;
            }
            return new PromptTemplate(Normalize(prefixBuilder.ToString()), Normalize(suffixBuilder.ToString()));
        }

        public static string Normalize(string text)
        {
            var builder = new StringBuilder(text.Length);
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c is not ('\n' or '\r' or '\u00a0' or '\u00ad'))
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }

        private static bool IsLayoutMacro(in ReadOnlySePayload payload) => payload.Type == ReadOnlySePayloadType.Macro && payload.MacroCode is MacroCode.NewLine or MacroCode.SoftHyphen or MacroCode.NonBreakingSpace;
    }

    private const string SelectYesnoAddon = "SelectYesno";

    private const uint JoinPartyPromptRow = 120u;

    private const uint DeclineInvitePromptRow = 121u;

    private const int ConfirmDelayMinMs = 400;

    private const int ConfirmDelayMaxMs = 900;

    private const int ConfirmWaitMs = 4000;

    private const int NotReadyAbandonMs = 15000;

    private static readonly Random rng = new Random();

    private PromptTemplate joinPrompt = PromptTemplate.Invalid;

    private PromptTemplate declinePrompt = PromptTemplate.Invalid;

    private bool templatesLoaded;

    private Stage stage;

    private long actAtTick;

    private long confirmDeadlineTick;

    private ushort inviteAddonId;

    private ushort confirmAddonId;

    private string inviterName = "";

    private string inviterWorld = "";

    public PartyInviteWatcher()
    {
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, SelectYesnoAddon, OnSelectYesnoSetup);
        Svc.Framework.Update += OnUpdate;
    }

    public void Dispose()
    {
        Svc.Framework.Update -= OnUpdate;
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, SelectYesnoAddon, OnSelectYesnoSetup);
    }

    private unsafe void OnSelectYesnoSetup(AddonEvent type, AddonArgs args)
    {
        var address = (AtkUnitBase*)args.Addon.Address;
        if (address == null)
        {
            return;
        }
        if (stage == Stage.DeclinePending)
        {
            return;
        }
        if (stage == Stage.Idle && !DeclineArmed())
        {
            return;
        }
        EnsureTemplates();
        var prompt = ReadPrompt(address);
        if (joinPrompt.Matches(prompt))
        {
            ArmDecline(address);
            return;
        }
        if (declinePrompt.Matches(prompt))
        {
            ArmConfirm(address);
            return;
        }
        if (stage == Stage.ConfirmPending || MentionsPendingInviter(prompt))
        {
            RunLog.Warning($"Party invite: SelectYesno {address->Id} looks invite-related but matched neither prompt template: \"{prompt}\" (join='{joinPrompt}', decline='{declinePrompt}').");
        }
        else
        {
            RunLog.Debug($"SelectYesno {address->Id} is not a party invite: \"{prompt}\"");
        }
    }

    private static bool DeclineArmed() => Plugin.Cfg.DeclinePartyInvites && Plugin.Instance.Controller.Running && !Plugin.Instance.Controller.Paused;

    private unsafe void ArmDecline(AtkUnitBase* addon)
    {
        CaptureInviter();
        var cfg = Plugin.Cfg;
        var minSec = Math.Max(0, cfg.DeclineInviteDelayMinSec);
        var maxSec = Math.Max(minSec, cfg.DeclineInviteDelayMaxSec);
        var delayMs = (minSec == maxSec ? minSec : rng.Next(minSec, maxSec + 1)) * 1000;
        stage = Stage.DeclinePending;
        inviteAddonId = addon->Id;
        confirmAddonId = 0;
        actAtTick = Environment.TickCount64 + delayMs;
        RunLog.Info($"Party invite from {DisplayName()} detected; declining in ~{delayMs / 1000}s.");
    }

    private unsafe void ArmConfirm(AtkUnitBase* addon)
    {
        if (stage == Stage.Idle)
        {
            CaptureInviter();
        }
        stage = Stage.ConfirmPending;
        confirmAddonId = addon->Id;
        actAtTick = Environment.TickCount64 + rng.Next(ConfirmDelayMinMs, ConfirmDelayMaxMs + 1);
        RunLog.Debug($"Party invite: decline confirmation {addon->Id} opened for {DisplayName()}; confirming shortly.");
    }

    private void OnUpdate(IFramework _)
    {
        switch (stage)
        {
            case Stage.DeclinePending:
                UpdateDecline();
                break;
            case Stage.ConfirmPending:
                UpdateConfirm();
                break;
        }
    }

    private unsafe void UpdateDecline()
    {
        var inviteAddon = FindSelectYesno(inviteAddonId);
        if (inviteAddon == null)
        {
            RunLog.Debug("Party invite: prompt closed before our decline; standing down.");
            stage = Stage.Idle;
            return;
        }
        var tickCount = Environment.TickCount64;
        if (tickCount < actAtTick)
        {
            return;
        }
        if (!WaitUntilReady(inviteAddon, tickCount, "invite prompt"))
        {
            return;
        }
        stage = Stage.ConfirmPending;
        confirmAddonId = 0;
        confirmDeadlineTick = tickCount + ConfirmWaitMs;
        if (!Click(inviteAddon, yes: false, "decline click"))
        {
            stage = Stage.Idle;
        }
    }

    private unsafe void UpdateConfirm()
    {
        var tickCount = Environment.TickCount64;
        if (confirmAddonId == 0)
        {
            if (tickCount < confirmDeadlineTick)
            {
                return;
            }
            RunLog.Info("Declined party invite from " + DisplayName() + " (no confirmation prompt appeared).");
            FinishDecline();
            return;
        }
        var confirmAddon = FindSelectYesno(confirmAddonId);
        if (confirmAddon == null)
        {
            RunLog.Debug("Party invite: confirmation prompt closed before our click; standing down.");
            stage = Stage.Idle;
            return;
        }
        if (tickCount < actAtTick)
        {
            return;
        }
        if (!WaitUntilReady(confirmAddon, tickCount, "decline confirmation"))
        {
            return;
        }
        if (!Click(confirmAddon, yes: true, "confirm click"))
        {
            stage = Stage.Idle;
            return;
        }
        RunLog.Info("Declined party invite from " + DisplayName() + ".");
        FinishDecline();
    }

    private unsafe bool WaitUntilReady(AtkUnitBase* addon, long now, string what)
    {
        if (GenericHelpers.IsAddonReady(addon))
        {
            return true;
        }
        if (now < actAtTick + NotReadyAbandonMs)
        {
            return false;
        }
        RunLog.Warning($"Party invite: {what} {addon->Id} never became ready; standing down.");
        stage = Stage.Idle;
        return false;
    }

    private void FinishDecline()
    {
        stage = Stage.Idle;
        if (Plugin.Cfg.DeclineInviteReply)
        {
            SendReply();
        }
    }

    private unsafe static AtkUnitBase* FindSelectYesno(ushort addonId)
    {
        if (addonId == 0)
        {
            return null;
        }
        var addonById = RaptureAtkUnitManager.Instance()->GetAddonById(addonId);
        if (addonById == null)
        {
            return null;
        }
        if (!(addonById->NameString == SelectYesnoAddon))
        {
            return null;
        }
        return addonById;
    }

    private unsafe static bool Click(AtkUnitBase* addon, bool yes, string what)
    {
        try
        {
            var selectYesno = new AddonMaster.SelectYesno((nint)addon);
            if (yes)
            {
                selectYesno.Yes();
            }
            else
            {
                selectYesno.No();
            }
            return true;
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "Party invite: " + what + " threw.");
            return false;
        }
    }

    private unsafe static string ReadPrompt(AtkUnitBase* addon)
    {
        try
        {
            var selectYesno = new AddonMaster.SelectYesno((nint)addon);
            return PromptTemplate.Normalize((selectYesno.Addon->PromptText != null) ? selectYesno.Text : selectYesno.TextLegacy).Trim();
        }
        catch (Exception ex)
        {
            RunLog.Debug("Party invite: failed to read SelectYesno prompt: " + ex.Message);
            return "";
        }
    }

    private void EnsureTemplates()
    {
        if (templatesLoaded)
        {
            return;
        }
        templatesLoaded = true;
        try
        {
            joinPrompt = PromptTemplate.FromAddonRow(JoinPartyPromptRow);
            declinePrompt = PromptTemplate.FromAddonRow(DeclineInvitePromptRow);
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "Party invite: failed to read the Addon sheet prompt templates.");
        }
        if (!joinPrompt.IsValid || !declinePrompt.IsValid)
        {
            RunLog.Warning("Party invite: prompt templates unavailable; auto-decline cannot identify invites.");
            return;
        }
        RunLog.Debug($"Party invite templates: join='{joinPrompt}', decline='{declinePrompt}'.");
    }

    private unsafe static bool MentionsPendingInviter(string prompt)
    {
        try
        {
            var partyInvite = InfoProxyPartyInvite.Instance();
            if (partyInvite == null)
            {
                return false;
            }
            var inviterText = partyInvite->InviterName.ToString();
            return inviterText.Length > 0 && prompt.Contains(inviterText, StringComparison.Ordinal);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void SendReply()
    {
        var message = (Plugin.Cfg.DeclineInviteReplyMessage ?? "").Trim();
        if (message.Length == 0)
        {
            return;
        }
        message = message.Replace("{name}", inviterName).Replace("{world}", inviterWorld);
        var line = (message.StartsWith('/') ? message : (Plugin.Cfg.DeclineInviteReplyChannel switch
        {
            PartyInviteReplyChannel.Tell => BuildTell(message),
            PartyInviteReplyChannel.Yell => "/yell " + message,
            _ => "/say " + message,
        }));
        var outgoing = line;
        try
        {
            Chat.SendMessage(outgoing);
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "Party invite: reply send threw for '" + outgoing + "'.");
        }
    }

    private string BuildTell(string body)
    {
        if (string.IsNullOrEmpty(inviterName))
        {
            return "/say " + body;
        }
        var target = (string.IsNullOrEmpty(inviterWorld) ? inviterName : (inviterName + "@" + inviterWorld));
        return "/tell " + target + " " + body;
    }

    private unsafe void CaptureInviter()
    {
        inviterName = "";
        inviterWorld = "";
        try
        {
            var partyInvite = InfoProxyPartyInvite.Instance();
            if (partyInvite == null)
            {
                return;
            }
            inviterName = partyInvite->InviterName.ToString();
            var world = Svc.Data.GetExcelSheet<World>().GetRowOrDefault(partyInvite->InviterWorldId);
            inviterWorld = world?.Name.ExtractText() ?? "";
        }
        catch (Exception ex)
        {
            RunLog.Debug("Party invite: failed to read inviter info: " + ex.Message);
        }
    }

    private string DisplayName()
    {
        if (!string.IsNullOrEmpty(inviterName))
        {
            if (!string.IsNullOrEmpty(inviterWorld))
            {
                return inviterName + "@" + inviterWorld;
            }
            return inviterName;
        }
        return "a player";
    }
}
