using System.Globalization;

namespace nuel;

public static class LibraryInitializer
{
    public static void Initialize()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
    }
}