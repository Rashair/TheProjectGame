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
    [Route("[controller]")]
    public class ConfigurationController : ControllerBase
    {
        private readonly GameConfiguration gameConfiguration;
        private readonly IConfiguration configuration;

        public ConfigurationController(IConfiguration configuration, GameConfiguration gameConfiguration)
        {
            this.configuration = configuration;
            this.gameConfiguration = gameConfiguration;
        }

        [HttpGet]
        public ActionResult<GameConfiguration> GetDefaultConfiguration()
        {
            return Ok(gameConfiguration);
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<GameConfiguration>> PostConfiguration(GameConfiguration conf)
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
    }
}
