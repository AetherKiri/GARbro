using GameRes.Formats;
using Xunit;

namespace GARbro.Core.Tests
{
    public class GameDataCatalogTests
    {
        [Fact]
        public void V2_manifest_validates_every_embedded_dataset ()
        {
            GameDataCatalog.ValidateAllDatasets();
        }
    }
}
