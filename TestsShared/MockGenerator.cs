using Moq;

namespace TestsShared
{
    public class MockGenerator
    {
        public static T Get<T>()
            where T : class
        {
            return new Mock<T>() { DefaultValue = DefaultValue.Mock }.Object;
        }
    }
}
