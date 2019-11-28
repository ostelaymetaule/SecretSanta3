using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecretSanta.Bot.Repository
{
    public class SecretSantaEntry
    {
        public string UserName { get; set; }
        public string UserFirstName { get; set; }
        public string UserLastName { get; set; }

        public string Message { get; set; }
        public int MessageId { get; set; }
        public DateTime MessageTime { get; set; }
        public long ChatId { get; set; }

        public string WillSendToUserName { get; set; }

    }
}
