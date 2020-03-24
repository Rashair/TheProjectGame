﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using GameMaster.Models;
using GameMaster.Services;
using GameMaster.Tests.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Messages;
using Xunit;

using static GameMaster.Tests.Helpers.ReflectionHelpers;

namespace GameMaster.Tests
{
    public class GMServiceTests
    {
        [Fact(Timeout = 3000)]
        public async Task TestExecuteAsyncShouldNotBlock()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            var conf = new MockConfiguration();
            services.AddSingleton<Configuration>(conf);
            services.AddSingleton<BufferBlock<PlayerMessage>>();
            AddLogging(services);
            services.AddSingleton<GM>();
            services.AddHostedService<GMService>();

            var serviceProvider = services.BuildServiceProvider();
            var hostedService = (GMService)serviceProvider.GetService<IHostedService>();
            var gameMaster = serviceProvider.GetService<GM>();
            var startGame = GetMethod("StartGame");

            // Act
            int delay = 1000;
            await Task.Run(async () =>
            {
                await hostedService.StartAsync(CancellationToken.None);
                await Task.Delay(delay);

                startGame.Invoke(gameMaster, null);

                int waitForGeneratePieceDelay = hostedService.WaitForStartDelay + conf.GeneratePieceInterval;
                await Task.Delay(waitForGeneratePieceDelay);
                await hostedService.StopAsync(CancellationToken.None);
            });

            // Assert
            int numberOfPieces = GetValue<int>("piecesOnBoard", gameMaster);
            Assert.True(numberOfPieces > 0, "GM Should generate at least one piece");
        }

        [Fact(Timeout = 1000)]
        public async Task TestExecuteAsyncShouldGeneratePieces()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            var conf = new MockConfiguration();
            services.AddSingleton<Configuration>(conf);
            services.AddSingleton<BufferBlock<PlayerMessage>>();
            AddLogging(services);
            services.AddSingleton<GM>();
            services.AddHostedService<GMService>();

            var serviceProvider = services.BuildServiceProvider();
            var hostedService = serviceProvider.GetService<IHostedService>();
            var gameMaster = serviceProvider.GetService<GM>();
            var startGame = GetMethod("StartGame");
            startGame.Invoke(gameMaster, null);

            // Act
            int delay = 500;
            await Task.Run(async () =>
            {
                await hostedService.StartAsync(CancellationToken.None);
                await Task.Delay(delay);
                await hostedService.StopAsync(CancellationToken.None);
            });

            // Assert
            int minimumNumberOfPieces = Math.Min((delay / conf.GeneratePieceInterval) - 1,
               conf.MaximumNumberOfPiecesOnBoard);
            int numberOfPieces = GetValue<int>("piecesOnBoard", gameMaster);
            Assert.InRange(numberOfPieces, minimumNumberOfPieces, conf.MaximumNumberOfPiecesOnBoard);
        }

        [Fact(Timeout = 1500)]
        public async Task TestExecuteAsyncShouldReadMessages()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            var conf = new MockConfiguration();
            services.AddSingleton<Configuration>(conf);
            int messagesNum = 10;
            var queue = new BufferBlock<PlayerMessage>();
            for (int i = 0; i < messagesNum; ++i)
            {
                queue.Post(new PlayerMessage());
            }
            services.AddSingleton(queue);
            AddLogging(services);
            services.AddSingleton<GM>();
            services.AddHostedService<GMService>();

            var serviceProvider = services.BuildServiceProvider();
            var hostedService = serviceProvider.GetService<IHostedService>();
            var gameMaster = serviceProvider.GetService<GM>();
            var startGame = GetMethod("StartGame");
            startGame.Invoke(gameMaster, null);

            // Act
            int delay = 500;
            await Task.Run(async () =>
            {
                await hostedService.StartAsync(CancellationToken.None);
                await Task.Delay(delay);
                await hostedService.StopAsync(CancellationToken.None);
            });

            // Assert
            Assert.Equal(0, queue.Count);
        }

        [Fact(Timeout = 2000)]
        public async Task TestExecuteAsyncShouldGeneratePiecesAndReadMessages()
        {
            // Arrange
            IServiceCollection services = new ServiceCollection();
            var conf = new MockConfiguration();
            services.AddSingleton<Configuration>(conf);
            int messagesNum = 10;
            var queue = new BufferBlock<PlayerMessage>();
            for (int i = 0; i < messagesNum; ++i)
            {
                queue.Post(new PlayerMessage());
            }
            services.AddSingleton(queue);
            AddLogging(services);
            services.AddSingleton<GM>();
            services.AddHostedService<GMService>();

            var serviceProvider = services.BuildServiceProvider();
            var hostedService = serviceProvider.GetService<IHostedService>();
            var gameMaster = serviceProvider.GetService<GM>();
            var startGame = GetMethod("StartGame");
            startGame.Invoke(gameMaster, null);

            // Act
            int delay = 500;
            await Task.Run(async () =>
            {
                await hostedService.StartAsync(CancellationToken.None);
                await Task.Delay(delay);
                await hostedService.StopAsync(CancellationToken.None);
            });

            // Assert
            Assert.Equal(0, queue.Count);
            int minimumNumberOfPieces = Math.Min(delay / (2 * conf.GeneratePieceInterval),
             conf.MaximumNumberOfPiecesOnBoard);
            int numberOfPieces = GetValue<int>("piecesOnBoard", gameMaster);
            Assert.InRange(numberOfPieces, minimumNumberOfPieces, conf.MaximumNumberOfPiecesOnBoard);
        }

        private void AddLogging(IServiceCollection services)
        {
            services.AddSingleton<ILogger, MockLogger>();
        }
    }
}
