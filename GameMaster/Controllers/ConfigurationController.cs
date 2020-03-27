using System.IO;

using GameMaster.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace GameMaster.Controllers
{
    [Route("/Configuration")]
    public class ConfigurationController : Controller
    {
        private readonly GameConfiguration gameConfiguration;
        private readonly IConfiguration configuration;

        public ConfigurationController(IConfiguration configuration, GameConfiguration gameConfiguration)
        {
            this.configuration = configuration;
            this.gameConfiguration = gameConfiguration;
        }

        [HttpGet]
        public GameConfiguration GetDefaultConfiguration()
        {
            return gameConfiguration;
        }

        [HttpPost]
        public void PostConfiguration(GameConfiguration conf)
        {
            gameConfiguration.Update(conf);
            string gameConfigString = JsonConvert.SerializeObject(conf);
            string path = configuration.GetValue<string>("GameConfigPath");

            using (StreamWriter file = new StreamWriter(path))
            {
                file.Write(gameConfigString);
            }
        }
    }
}
