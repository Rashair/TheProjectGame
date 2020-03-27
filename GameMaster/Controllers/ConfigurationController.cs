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
        private readonly Configuration gameConfiguration;
        private readonly IConfiguration configuration;

        public ConfigurationController(IConfiguration configuration, Configuration gameConfiguration)
        {
            this.configuration = configuration;
            this.gameConfiguration = gameConfiguration;
        }

        [HttpGet]
        public Configuration GetDefaultConfiguration()
        {
            return gameConfiguration;
        }

        [HttpPost]
        public void PostConfiguration(Configuration model)
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
