using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecretSanta.Model
{
    public class ChatGroup
    {
        public long ChatId { get; set; }
        public string GroupName { get; set; }
        public List<Participant> Participants { get; set; }
        public Participant Admin { get; set;  }
        public Status Status{  get; set; }

    }
    public enum Status
    {
        init,
        roomCreated,
        registrationClosed,
        drawn,
        instructionsSent,
        additionalParticipants,
        closed,
        error
    }
}
