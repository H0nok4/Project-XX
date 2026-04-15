namespace ProjectXX.Domain.Combat
{
    public enum ProjectXXFaction
    {
        Player = 0,
        FriendlyNpc = 1,
        NeutralNpc = 2,
        Enemy = 3
    }

    public enum ProjectXXFactionDisposition
    {
        Allied = 0,
        Neutral = 1,
        Hostile = 2
    }

    public enum ProjectXXFactionRetaliationMode
    {
        None = 0,
        DamageSourceFaction = 1
    }
}
