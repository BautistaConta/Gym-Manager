namespace GymManager.API.Options;

public class TwilioOptions
{
    public const string SectionName = "Twilio";
    public bool Enabled { get; set; }
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string WhatsAppFromNumber { get; set; } = string.Empty;
    public string PorVencerContentSid { get; set; } = string.Empty;
    public bool WorkerEnabled { get; set; }
    public bool SmokeTestEnabled { get; set; }
    public string AuthorizedTestNumber { get; set; } = string.Empty;
    public bool PaidAccountConfirmed { get; set; }
    public bool TemplatesApprovedConfirmed { get; set; }
    public bool AdditionalTypesEnabled { get; set; }
    public string WelcomeContentSid { get; set; } = string.Empty;
    public string PromotionContentSid { get; set; } = string.Empty;
    public string GeneralNoticeContentSid { get; set; } = string.Empty;
}
