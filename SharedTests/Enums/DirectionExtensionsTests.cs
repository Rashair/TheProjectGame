using Shared.Enums;
using Xunit;

namespace SharedTests
{
    public class DirectionExtensionsTests
    {
        [Fact]
        public void TestGetCoordinatesAroundCenterMiddleField()
        {
            // Arrange
            (int y, int x) center = (5, 4);

            // Act
            var result = DirectionExtensions.GetCoordinatesAroundCenter(center);

            // Assert
            Assert.Equal(9, result.Length);
            Assert.Contains((Direction.NW, center.y - 1, center.x - 1), result);
            Assert.Contains((Direction.N, center.y - 1, center.x), result);
            Assert.Contains((Direction.NE, center.y - 1, center.x + 1), result);
            Assert.Contains((Direction.W, center.y, center.x - 1), result);
            Assert.Contains((Direction.FromCurrent, center.y, center.x), result);
            Assert.Contains((Direction.E, center.y, center.x + 1), result);
            Assert.Contains((Direction.SW, center.y + 1, center.x - 1), result);
            Assert.Contains((Direction.S, center.y + 1, center.x), result);
            Assert.Contains((Direction.SE, center.y + 1, center.x + 1), result);
        }
    }
}
