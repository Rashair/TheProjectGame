﻿using System;
using System.IO;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

using GameMaster.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;

namespace GameMaster.Controllers;

[ApiController]
[Route("/api")]
public class GameController : ControllerBase
{
    private readonly ILogger logger;
    private readonly IConfiguration configuration;
    private readonly GameConfiguration gameConfiguration;
    private readonly GM gameMaster;

    public GameController(IConfiguration configuration, GameConfiguration gameConfiguration, GM gameMaster,
        ILogger log)
    {
        this.logger = log.ForContext<GameController>();
        this.configuration = configuration;
        this.gameConfiguration = gameConfiguration;
        this.gameMaster = gameMaster;
    }

    [HttpGet]
    [Route("[action]")]
    public ActionResult<GameConfiguration> Configuration()
    {
        return gameConfiguration;
    }

    [HttpPost]
    [Route("[action]")]
    [Consumes(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<GameConfiguration>> Configuration(GameConfiguration conf)
    {
        if (conf == null || string.IsNullOrEmpty(conf.CsIP))
        {
            var msg = "Received empty configuration";
            logger.Warning(msg);
            return BadRequest(msg);
        }

        gameConfiguration.Update(conf);

        string gameConfigString = JsonConvert.SerializeObject(gameConfiguration);
        string path = configuration.GetValue<string>("GameConfigPath");
        try
        {
            using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var buffer = Encoding.UTF8.GetBytes(gameConfigString);
                await fileStream.WriteAsync(buffer);
            }
        }
        catch (Exception e)
        {
            logger.Warning($"Error writing to file: {e}");
        }

        return Created("/configuration", gameConfiguration);
    }

    [HttpPost]
    [Route("[action]")]
    public IActionResult InitGame()
    {
        if (gameMaster.WasGameInitialized)
        {
            // Should be BadRequest, but it more handy in development - TODO: change later
            logger.Warning("Game was already initialized");
            return Ok();
        }

        bool result = gameMaster.InitGame();
        if (result)
        {
            return Ok();
        }
        return BadRequest();
    }

    [HttpGet]
    [Route("[action]")]
    public ActionResult<bool> WasGameStarted()
    {
        return gameMaster.WasGameStarted;
    }

    [HttpGet]
    [Route("[action]")]
    public ActionResult<bool> WasGameFinished()
    {
        return gameMaster.WasGameFinished;
    }
}
