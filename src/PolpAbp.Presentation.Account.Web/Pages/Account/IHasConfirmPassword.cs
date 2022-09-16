namespace PolpAbp.Presentation.Account.Web.Pages.Account
{
    public interface IHasConfirmPassword
    {
        public string? Password { get; set; }

        public string? ConfirmPassword { get; set; }
    }
}
