using System;
using System.IO;
using GameMaster.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace GameMaster.Controllers
{
    public class ConfigurationController : Controller
    {
        [HttpPost]
        [Route("/Configuration")]
        public void PostConfiguration(ConfigurationToFile model)
        {
            string gameConfigString = JsonConvert.SerializeObject(model);
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            using (StreamWriter outputFile = new StreamWriter("gameConfig.json"))
            {
                outputFile.Write(gameConfigString);
            }
        }
    }
}