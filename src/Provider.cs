namespace Airbag
{
    public class Provider
    {
        public Provider()
        {
            ValidateAudience = true;
        }

        public string Name { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public bool ValidateAudience { get; set; }
        public string Authority { get; set; }

        public bool IsEmpty() => string.IsNullOrEmpty(Issuer) && string.IsNullOrEmpty(Audience) && string.IsNullOrEmpty(Authority);
        public bool IsInvalid() => string.IsNullOrEmpty(Issuer) || (ValidateAudience && string.IsNullOrEmpty(Audience)) || string.IsNullOrEmpty(Authority);
    }
}