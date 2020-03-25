using System.IO;

using GameMaster.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace GameMaster.Controllers
{
    public class ConfigurationController : Controller
    {
        private Configuration configuration;

        public ConfigurationController(Configuration configuration)
        {
            this.configuration = configuration;
        }

        [HttpPost]
        [Route("/Configuration")]
        public void PostConfiguration(Configuration model)
        {
            configuration = model;

            string gameConfigString = JsonConvert.SerializeObject(model);

            using (StreamWriter outputFile = new StreamWriter("gameConfig.json"))
            {
                outputFile.Write(gameConfigString);
            }
        }
    }
}
