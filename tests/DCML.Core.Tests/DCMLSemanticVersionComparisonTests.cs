using DCML.Core.Runtime;
using Xunit;

namespace DCML.Core.Tests
{
    public sealed class DCMLSemanticVersionComparisonTests
    {
        [Theory]
        [InlineData("1.0.0", "1.0.0", 0)]
        [InlineData("1.0.1", "1.0.0", 1)]
        [InlineData("1.0.0", "1.0.1", -1)]
        [InlineData("2.0.0", "10.0.0", -1)]
        [InlineData("1.0.0", "1.0.0-alpha", 1)]
        [InlineData("1.0.0-alpha", "1.0.0-alpha.1", -1)]
        [InlineData("1.0.0-alpha.1", "1.0.0-alpha.beta", -1)]
        [InlineData("1.0.0+build.1", "1.0.0+build.99", 0)]
        public void TryCompare_UsesSemanticVersionPrecedence(
            string left,
            string right,
            int expected
        )
        {
            bool success =
                DCMLSemanticVersion.TryCompare(
                    left,
                    right,
                    out int comparison
                );

            Assert.True(success);
            Assert.Equal(
                expected,
                comparison
            );
        }
    }
}
