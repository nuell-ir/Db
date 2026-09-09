using System.Globalization;

namespace nuell;

public static class LibraryInitializer
{
    public static void Initialize()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
    }
}