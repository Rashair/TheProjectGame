using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Player.Services;
using Shared;
using Xunit;

namespace IntegrationTests
{
    public class StartTests
    {
        [Fact(Timeout = 1500)]
        public async void PlayerStarts()
        {
            // Arrange
            var source = new CancellationTokenSource();
            string[] args = new string[] { "TeamID=red" };
            var webhost = Player.Program.CreateWebHostBuilder(args).Build();

            // Act
            await webhost.StartAsync(source.Token);
            await Task.Delay(500);
            source.Cancel();

            // Assert
            var player = webhost.Services.GetService<Player.Models.Player>();
            Assert.False(player == null, "Player should not be null");

            // TODO: get hosted services and assert those are not null after move to dotnet-core 3.0
        }
    }
}
