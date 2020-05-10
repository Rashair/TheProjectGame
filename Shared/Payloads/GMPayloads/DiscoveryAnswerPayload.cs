namespace Shared.Payloads.GMPayloads
{
    public class DiscoveryAnswerPayload : Payload
    {
        public int? DistanceFromCurrent { get; set; }

        public int? DistanceN { get; set; }

        public int? DistanceNE { get; set; }

        public int? DistanceE { get; set; }

        public int? DistanceSE { get; set; }

        public int? DistanceS { get; set; }

        public int? DistanceSW { get; set; }

        public int? DistanceW { get; set; }

        public int? DistanceNW { get; set; }
    }
}
