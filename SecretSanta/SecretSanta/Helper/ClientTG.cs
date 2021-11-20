using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SecretSanta.Data;
using Telegram.Bot;

namespace SecretSanta.Helper
{
    internal class ClientTG
    {
        private readonly Assigner _assigner;
        private readonly Repository _repository;
        private readonly TelegramBotClient botClient;

        public ClientTG(Assigner assigner, Repository repository, TelegramBotClient botClient)
        {
            this._assigner = assigner;
            this._repository = repository;
            this.botClient = botClient;
        }

        //TODO: admin create a room and get an id  (random set of nouns?)
        //TODO: admin seeing if someone not filled the information
        //TODO: admin triggering the assignment

        //TODO: participant: join an existing group by id
        //TODO: greetings and 
        //TODO: anonym communication via bot 


    }
}
