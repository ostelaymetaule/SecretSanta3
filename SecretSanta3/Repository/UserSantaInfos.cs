using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecretSanta.Bot.Repository
{
    public class UserSantaInfos
    {
        public long ChatId { get; set; }
        public string UserName { get; set; }
        public string PostAddress { get; set; }
        public String CanSendPresentTo { get; set; }
        public string LoveToReceive { get; set; }
        public string DontWantToReceive { get; set; }


        public bool NotificationSent { get; set; }
        public string IAmSecretSantaForUser { get; set; }
        public string MySecretSanta { get; set; }

        public bool IAmInMoscow { get; set; }
        public bool IAmInRussia { get; set; }


    }
}
