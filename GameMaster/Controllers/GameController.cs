using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;

using GameMaster.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace GameMaster.Controllers
{
    [ApiController]
    [Route("/api")]
    public class GameController : ControllerBase
    {
        private readonly GameConfiguration gameConfiguration;
        private readonly IConfiguration configuration;
        private readonly GM gameMaster;

        public GameController(IConfiguration configuration, GameConfiguration gameConfiguration, GM gameMaster)
        {
            this.configuration = configuration;
            this.gameConfiguration = gameConfiguration;
            this.gameMaster = gameMaster;
        }

        [HttpGet]
        [Route("[action]")]
        public ActionResult<GameConfiguration> Configuration()
        {
            return Ok(gameConfiguration);
        }

        [HttpPost]
        [Route("[action]")]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<GameConfiguration>> Configuration(GameConfiguration conf)
        {
            if (conf == null || string.IsNullOrEmpty(conf.CsIP))
            {
                return BadRequest("Received empty configuration");
            }

            gameConfiguration.Update(conf);
            string gameConfigString = JsonConvert.SerializeObject(conf);
            string path = configuration.GetValue<string>("GameConfigPath");

            using (StreamWriter file = new StreamWriter(path))
            {
                await file.WriteAsync(gameConfigString);
            }

            return Created("/configuration", conf);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> StartGame()
        {
            gameMaster.StartGame();
            return Ok();
        }
    }
}
