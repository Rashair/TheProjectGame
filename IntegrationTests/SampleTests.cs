using Xunit;

namespace IntegrationTests
{
    public class SampleTests
    {
        [Fact]
        public void AlwaysFailTest()
        {
            Assert.True(false);
        }
    }
}
