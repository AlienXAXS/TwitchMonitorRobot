namespace TMRAgent.Twitch.Models
{
    internal class Authorisation
    {
        public string Code { get; }

        public Authorisation(string code)
        {
            Code = code;
        }
    }
}
