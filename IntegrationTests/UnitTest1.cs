using Shared;
using System;
using System.IO;
using Xunit;

namespace IntegrationTests
{
    public class UnitTest1
    {
        [Fact]
        public void TestCreateLogfile()
        {
            // Arrange
            var logger = new Logger();
            string str = "Hello!";
            string path = "C:/Log/";
            string fileName = DateTime.Now.Day.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Year.ToString() + "_Logs.txt";

            // Act
            logger.Log(str);
            bool created = File.Exists(path + fileName);
            // Assert 
            Assert.True(created);
        }
    }
}
