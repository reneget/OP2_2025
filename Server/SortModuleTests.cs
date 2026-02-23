using Xunit;
using Server.Modules.Sorting;

namespace Server.Tests.Modules.Sorting
{
    public class SortModuleTests
    {
        [Fact]
        public void SortWithMetadata_WithAscendingOrder_ReturnsSortedArray()
        {
            // Arrange
            var sortModule = new CombSortModule();
            int[] inputArray = [3, 1, 2];
            bool ascending = true;
            int? gap = null;

            // Act
            var sortResult = sortModule.SortWithMetadata(inputArray, ascending, gap);

            // Assert
            Assert.NotNull(sortResult);
            Assert.Equal(new int[] { 1, 2, 3 }, sortResult.SortedArray);
            Assert.True(sortResult.ExecutionTimeMs >= 0);
            Assert.True(sortResult.InitialGap >= 1);
        }

        [Fact]
        public void SortWithMetadata_WithDescendingOrder_ReturnsSortedArray()
        {
            // Arrange
            var sortModule = new CombSortModule();
            int[] inputArray = [3, 1, 2];
            bool ascending = false;
            int? gap = null;

            // Act
            var sortResult = sortModule.SortWithMetadata(inputArray, ascending, gap);

            // Assert
            Assert.NotNull(sortResult);
            Assert.Equal(new int[] { 3, 2, 1 }, sortResult.SortedArray);
        }

        [Fact]
        public void SortWithMetadata_WithEmptyArray_ReturnsEmptyArray()
        {
            // Arrange
            var sortModule = new CombSortModule();
            int[] inputArray = [];
            bool ascending = true;
            int? gap = null;

            // Act
            var sortResult = sortModule.SortWithMetadata(inputArray, ascending, gap);

            // Assert
            Assert.NotNull(sortResult);
            Assert.Empty(sortResult.SortedArray);
            Assert.Equal(0, sortResult.InitialGap);
        }

        [Fact]
        public void SortWithMetadata_WithSingleElement_ReturnsSameArray()
        {
            // Arrange
            var sortModule = new CombSortModule();
            int[] inputArray = [42];
            bool ascending = true;
            int? gap = null;

            // Act
            var sortResult = sortModule.SortWithMetadata(inputArray, ascending, gap);

            // Assert
            Assert.NotNull(sortResult);
            Assert.Equal(new int[] { 42 }, sortResult.SortedArray);
        }

        [Theory]
        [InlineData(new int[] { 5, 2, 8, 1, 9 }, true, new int[] { 1, 2, 5, 8, 9 })]
        [InlineData(new int[] { 5, 2, 8, 1, 9 }, false, new int[] { 9, 8, 5, 2, 1 })]
        [InlineData(new int[] { 1, 1, 1, 1 }, true, new int[] { 1, 1, 1, 1 })]
        [InlineData(new int[] { -3, -1, -2, 0 }, true, new int[] { -3, -2, -1, 0 })]
        public void SortWithMetadata_VariousInputs_ReturnsCorrectResult(int[] input, bool ascending, int[] expected)
        {
            // Arrange
            var sortModule = new CombSortModule();
            int? gap = null;

            // Act
            var sortResult = sortModule.SortWithMetadata(input, ascending, gap);

            // Assert
            Assert.NotNull(sortResult);
            Assert.Equal(expected, sortResult.SortedArray);
        }
    }
}