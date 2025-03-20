using System.Globalization;

namespace Scover.Psdc;

static class Format
{
    public static CultureInfo Code => CultureInfo.InvariantCulture;
    public static CultureInfo Msg => CultureInfo.CurrentCulture;
    public static CultureInfo Date => CultureInfo.CurrentCulture;
}
