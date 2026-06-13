using SiapControl.Common;
using Xunit;

namespace SiapControl.Tests.Common
{
    public class AfipVersionComparerTests
    {
        [Fact]
        public void RemoveVersionText_RemovesVersionAndRelease()
        {
            string value = AfipVersionComparer.RemoveVersionText("Seguridad Social Version 47.0 Release 6");

            Assert.Equal("Seguridad Social", value);
        }

        [Fact]
        public void TryCompare_OrdersReleaseNumbers()
        {
            Assert.True(AfipVersionComparer.TryCompare("47.0 Release 6", "47.0 Release 5", out int comparison));
            Assert.True(comparison > 0);
        }
    }
}
