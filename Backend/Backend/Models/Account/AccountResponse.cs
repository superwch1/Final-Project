namespace Backend.Models
{
    /// <summary>
    /// Account details.
    /// </summary>
    public record AccountResponse
    {
        public required Guid Id { get; init; }

        public required string Email { get; init; }

        public required string Name { get; init; }

        /// <summary>
        /// Project an account onto a response.
        /// </summary>
        public static AccountResponse FromAccount(Account account)
        {
            return new AccountResponse
            {
                Id = account.Id,
                Email = account.Email,
                Name = account.Name
            };
        }
    }
}
