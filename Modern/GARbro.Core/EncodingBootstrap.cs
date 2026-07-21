using System.Runtime.CompilerServices;
using System.Text;

namespace GameRes
{
    internal static class EncodingBootstrap
    {
        [ModuleInitializer]
        internal static void Initialize ()
        {
            Encoding.RegisterProvider (CodePagesEncodingProvider.Instance);
        }
    }
}
