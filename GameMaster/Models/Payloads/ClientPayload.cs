namespace GameMaster.Models.Payloads
{
    public class ClientPayload
    {
        public override bool Equals(object obj)
        {
            return this.GetType() == obj.GetType() && this.AreAllPropertiesTheSame(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
