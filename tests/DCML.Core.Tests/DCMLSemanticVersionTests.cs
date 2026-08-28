using DCML.Core.Runtime;
using Xunit;

namespace DCML.Core.Tests
{
    public sealed class DCMLSemanticVersionTests
    {
        [Theory]
        [InlineData("0.1.0")]
        [InlineData("1.0.0")]
        [InlineData("1.0.0-alpha")]
        [InlineData("1.0.0-alpha.1+build.42")]
        public void IsValid_AcceptsSemanticVersions(
            string value
        )
        {
            Assert.True(
                DCMLSemanticVersion.IsValid(
                    value
                )
            );
        }

        [Theory]
        [InlineData("banana")]
        [InlineData("1.0")]
        [InlineData("1.0.0.0")]
        [InlineData("01.0.0")]
        [InlineData("v1.0.0")]
        [InlineData("1.0.0-alpha.01")]
        public void IsValid_RejectsInvalidVersions(
            string value
        )
        {
            Assert.False(
                DCMLSemanticVersion.IsValid(
                    value
                )
            );
        }
    }
}
