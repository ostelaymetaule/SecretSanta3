using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using SecretSanta.Bot.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SecretSanta.Bot.Helpers;

namespace SecretSanta.Bot.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Produces("application/json")]
    [ApiController]
    //[Route("api/[controller]")]
    public class SecretSantaController : ControllerBase
    {
        private IConfiguration _configuration;
        private IWebHostEnvironment _env;
        private IFileRepository _rep;
        private BotCaller _botCaller;
        private string _token;

        public SecretSantaController(IConfiguration configuration, IWebHostEnvironment env, IFileRepository rep, BotCaller botCaller)
        {
            _configuration = configuration;
            _env = env;
            _rep = rep;
            _token = Environment.GetEnvironmentVariable("bottoken");
            _botCaller = botCaller;
        }


        // GET: api/SecretSanta
        [HttpGet("api/SecretSanta/allSantas")]
        public List<UserSantaInfos> GetAll()
        {
            return _rep.GetUserInfos();
        }
        // Post: api/SecretSanta

        // GET: api/SecretSanta
        [HttpGet("api/SecretSanta/test")]
        public int GetTest()
        {
            return _rep.GetUserInfos().Count();
        }
        // Post: api/SecretSanta
        [HttpPost("api/SecretSanta")]
        public List<UserSantaInfos> Post([FromBody]List<UserSantaInfos> userInfos)
        {
            _rep.SaveUserInfosToFile(userInfos);
            return _rep.GetUserSataInfosFromFile();
        }
        [HttpPost("api/SecretSanta/updateInfos")]
        public List<UserSantaInfos> Post2([FromBody]List<UserSantaInfos> userInfos)
        {
            _rep.SaveUserInfosToFile(userInfos);
            return userInfos;
        }
        [HttpPost("api/SecretSanta/TriggerMatching")]
        public List<Tuple<string, string>> TriggerSantaMatch([FromBody] string test)
        {
            //SendSecretSantaMatchAsync
            System.Console.WriteLine(test);
            return _botCaller.TriggerMatching();

        }


        [HttpPost("api/SecretSanta/SendInfosAboutTargets")]
        public async Task<bool> SendSecretSantaMatchAsync([FromBody] string test)
        {
            //SendSecretSantaMatchAsync
            System.Console.WriteLine(test);

            return await _botCaller.SendSecretSantaMatchAsync();

        }
        [HttpPost("api/SecretSanta/SendNotification")]
        public async Task<bool> SendNotification([FromBody] string test)
        {
            //SendSecretSantaMatchAsync
            System.Console.WriteLine(test);

            return await _botCaller.SendNotification2Async();

        }


        // GET: api/SecretSanta/5
        [HttpGet("api/SecretSanta/{userName}", Name = "Get")]
        public List<SecretSantaEntry> Get(string userName)
        {
            return _rep.GetMessages().Where(x => x.UserName == userName).ToList();
        }


    }
}
