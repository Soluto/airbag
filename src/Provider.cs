namespace Airbag
{
    public class Provider
    {
        public string Name { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string Authority { get; set; }

        public bool IsEmpty() => string.IsNullOrEmpty(Issuer) && string.IsNullOrEmpty(Audience) && string.IsNullOrEmpty(Authority);
        public bool IsInvalid() => string.IsNullOrEmpty(Issuer) || string.IsNullOrEmpty(Audience) || string.IsNullOrEmpty(Authority);
    }
}