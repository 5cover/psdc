namespace Scover.Psdc;

static class SysExit
{
    public const int Ok = 0;
    public const int Usage = 64;
    public const int DataErr = 65;
    public const int NoInput = 66;
    public const int NoUser = 67;
    public const int NoHost = 68;
    public const int Unavailable = 69;
    public const int Software = 70;
    public const int OsErr = 71;
    public const int OsFile = 72;
    public const int CantCreat = 73;
    public const int IoErr = 74;
    public const int TempFail = 75;
    public const int Protocol = 76;
    public const int NoPerm = 77;
    public const int Config = 78;
}

static class AppExit
{
    public const int FailedWithErrors = 1;
    public const int FailedWithWarnings = 2;
    public const int FailedWithHints = 3;
}

