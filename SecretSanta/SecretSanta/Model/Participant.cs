
namespace SecretSanta.Model
{
    public class Participant
    {

        public Guid Id { get; set; }

        public string AccountName { get; set; }
        public long UserId { get; set; }
        public string FullName { get; set; }
        public string Location { get; set; }
        public string LikeToReceive { get; set; }
        public string HateToReceive { get; set; }
        public string PostalAddress { get; set; }
        public string TrackingInformation { get; set; }
        public LocationMarker CanSendTo { get; set; }

        public SantaMatching SantaMatching { get; set; }


    }
    public enum LocationMarker
    {
        World,
        Country
    }
}