namespace AnoMech.Multiplayer;

// The server multiplayer uses unless the player picks their own in Settings: the Night Parade
// droplet. It has no TLS, so it's dialed as ws:// outright rather than trying wss:// first.
public static class DefaultRelay
{
    public const string Address = "167.71.158.159:7890";
    public const string Url = "ws://" + Address;
    public const string Location = "US West (San Francisco)";
}
