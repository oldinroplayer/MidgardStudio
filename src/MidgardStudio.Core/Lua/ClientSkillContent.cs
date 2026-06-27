namespace MidgardStudio.Core.Lua;

/// <summary>
/// The exact text each client-skill file (skillinfolist / skilldescript / skilldelaylist) would receive for
/// a skill, or <c>null</c> when there is nothing to write. This is the SINGLE definition shared by the app's
/// dirty tracking: the load baseline, the per-skill dirty diff, and the save all go through it, so they can
/// never disagree.
///
/// The subtle case is an <b>empty descript block</b> (<see cref="ClientSkill.HasDescript"/> is true but
/// <see cref="ClientSkill.Description"/> has no lines): it writes nothing, so its content must read
/// <c>null</c> on every side. When the baseline captured a formatted empty block but the diff treated empty
/// as null, such a skill stayed permanently dirty — latching the Save button on after an unrelated quick-fix
/// was applied and then fully undone.
/// </summary>
public static class ClientSkillContent
{
    public static string? Info(ClientSkill s) => s.HasInfo ? ClientSkillWriter.FormatInfo(s) : null;

    public static string? Descript(ClientSkill s) =>
        s.HasDescript && s.Description.Count > 0 ? ClientSkillWriter.FormatDescript(s) : null;

    public static string? Delay(ClientSkill s) => s.HasDelay ? ClientSkillWriter.FormatDelay(s) : null;
}
