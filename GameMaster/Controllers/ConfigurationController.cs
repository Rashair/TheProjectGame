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
        public void PostConfiguration(GameConfiguration model)
        {
            gameConfiguration.Update(model);
            string gameConfigString = JsonConvert.SerializeObject(model);

            using (StreamWriter outputFile = new StreamWriter(configuration.GetValue<string>("GameConfigPath")))
            {
                outputFile.Write(gameConfigString);
            }
        }
    }
}
