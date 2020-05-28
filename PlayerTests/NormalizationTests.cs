using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xunit;

namespace Player.Tests
{
    public class NormalizationTests
    {
        public static IEnumerable<object[]> Data =>
         new List<object[]>
         {
            //// Single number
            new object[] { 1 },
            //// In order
            new object[] { 0, 1, 2, 3 },
            //// Reverse order
            new object[] { 3, 2, 1, 0 },
            //// Prime numbers
            new object[] { 2, 5, 3, 9, 7 },
            //// Semiprime numbers
            new object[] { 4, 6, 9, 10, 14, 15, 21, 22, 25, },
            //// Fibonacci sequence
            new object[] { 1, 2, 3, 5, 8, 13 },
            //// Symmetric numbers, odd count
            new object[] { -1, 0, 1, },
            //// Symmetric numbers, even count
            new object[] { -2, -1, 1, 2, },
            //// Even numbers, odd count
            new object[] { 0, 2, 4, 6, 8, },
            //// Even numbers, even count
            new object[] { 0, 2, 4, 6, 8, 10, },
            //// Negative numbers, odd count
            new object[] { -5, -4, -3, -2, -1, },
            //// Negative numbers, even count
            new object[] { -6, -5, -4, -3, -2, -1, },
            //// Power of 2
            new object[] { 2, 4, 8, 16, 32, 64, 0 },
            //// Smallest at the end, biggest at the beginning
            new object[] { 1000, 0, 5, 10, 15, -1000, },
            //// Amicable numbers
            new object[] 
            { 
                // Only those on same row are amicable with each other :) 
                2620, 2924,
                220, 284,
                1184, 1210 
            },
            //// Perfect numbers
            new object[] { 6, 28, 496 },
            //// Sociable numbers
            new object[] { 12496, 14316, 1264460, 2115324, 2784580, 4938136, }
         };

        [Theory]
        [MemberData(nameof(Data))]
        public void TestIdNormalizationWithIdInAllies(params int[] teamIds)
        {
            // Arrange
            List<int> normalizedIds = new List<int>(teamIds.Length);

            // Act
            for (int i = 0; i < teamIds.Length; ++i)
            {
                int id = teamIds[i];
                normalizedIds.Add(Models.Player.NormalizeId(id, teamIds));
            }

            // Assert
            var distinctNormalizedIds = normalizedIds.Distinct().ToList();
            Assert.True(distinctNormalizedIds.Count == normalizedIds.Count, "Every player should have different id"
                + $"\n expected: {normalizedIds.Count}, actual: {distinctNormalizedIds.Count}");
            foreach (int id in normalizedIds)
            {
                Assert.True(id >= 0 && id < normalizedIds.Count, "Id should be in range [0, n-1], n - number of allies." +
                    $"\n actual: {id}");
            }
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void TestIdNormalizationWithoutIdInAllies(params int[] teamIds)
        {
            // Arrange
            List<int> normalizedIds = new List<int>(teamIds.Length);

            // Act
            for (int i = 0; i < teamIds.Length; ++i)
            {
                int id = teamIds[i];
                var alliesIds = teamIds.Where(playerId => playerId != id).ToArray();
                normalizedIds.Add(Models.Player.NormalizeId(id, alliesIds));
            }

            // Assert
            var distinctNormalizedIds = normalizedIds.Distinct().ToList();
            Assert.True(distinctNormalizedIds.Count == normalizedIds.Count, "Every player should have different id"
                + $"\n expected: {distinctNormalizedIds.Count}, actual: {normalizedIds.Count}");
            foreach (int id in normalizedIds)
            {
                Assert.True(id >= 0 && id < normalizedIds.Count, "Id should be in range [0, n-1], n - number of allies." +
                    $"\n actual: {id}");
            }
        }
    }
}
