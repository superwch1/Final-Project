namespace Backend.Models
{
    public static class PasswordRules
    {
        public const int MinimumLength = 8;

        public const int MaximumLength = 128;

        // Requires at least one digit and one non-alphanumeric character.
        public const string Pattern = @"^(?=.*[0-9])(?=.*[^A-Za-z0-9]).*$";

        public const string Message = "The password must be at least 8 characters and contain a number and a symbol.";
    }
}
