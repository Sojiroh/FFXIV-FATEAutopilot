using clib.Extensions;

namespace AutoFateGrind.Core.Ipc;

internal static class DefaultCombatPreset
{
    public const string Name = "FATE Autopilot";

    public const int Revision = 1;

    private const string Base64Brotli = "G9AhAJwHdiwwM2QEj29odjqXskvjX/jSCK57ViO8ATaISFffBGlQKBadq0szHnqiEMUCFpK/20/rlMcAJUBfN7VQK2IRCyCV0m+JXQHYSbV6EDPRFZdPTyI6jaVG9w2npseNOEIhFFtJJ8QTw3Xd43oM4oIb9kZyaVyYWh2gHOJPMv59zqYQqES0kVhK7AVkSiQX/uMQtrL2gxaBgpVaIAKKJ0kGWc3DpMCVzFsTPmeI8FkCcllz+wVvCKUOgPSiPMl+EBwy0I+8Oha+LT1ZNdCA82EvBiAQ6WabseIBZH5nLB43I8t85zMA9WIjTGgfAvjON11XfBfNTIKAZ+Y4fYQNhDyOdIjhkB5ujakVxXO0ZGC3jwFXRCj8zlgU9SLOkDNZr2UhrxAi8b00zt1h1JhMIOmtIgXOBZIN+JYfoaRsxfBZ9GBRnlxnKwKc6FAABjNK/GB5m/bnBwFVDn2U5NHCwG2+aPEU/8nM9WKTV3GPb5oUJkAHz40ZN8ch5cEHCwgTkbEIMCJOxTM3FQLsJ6cnW/Hcmn7NrFf3mCFzYQJP1q1Yap3HCx9UztAAapUBr8jMUoDfGUpC0PVAoZ+T4cFLNjQs+nhaiq1FC4cTPDD9y2bMS5Lx664D";

    private static string? cached;

    public static string GetSerialized() => cached ?? (cached = Base64Brotli.FromBase64());
}
