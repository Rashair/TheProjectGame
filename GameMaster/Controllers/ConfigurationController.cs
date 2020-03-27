using System.IO;

using GameMaster.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace GameMaster.Controllers
{
    [Route("/Configuration")]
    public class ConfigurationController : Controller
    {
        private readonly Configuration configuration;

        public ConfigurationController(Configuration configuration)
        {
            this.configuration = configuration;
        }

        [HttpGet]
        public Configuration GetDefaultConfiguration()
        {
            return configuration;
        }

        [HttpPost]
        public void PostConfiguration(Configuration model)
        {
            configuration.Update(model);
            string gameConfigString = JsonConvert.SerializeObject(model);

            using (StreamWriter outputFile = new StreamWriter(Configuration.DefaultConfPath))
            {
                outputFile.Write(gameConfigString);
            }
        }
    }
}
