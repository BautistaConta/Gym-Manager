namespace GymManager.API.Options;

public class TwilioOptions
{
    public const string SectionName = "Twilio";
    public bool Enabled { get; set; }
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string WhatsAppFromNumber { get; set; } = string.Empty;
    public string ContentSid { get; set; } = string.Empty;
    public string WelcomeContentSid { get; set; } = string.Empty;
    public string PromotionContentSid { get; set; } = string.Empty;
    public string GeneralNoticeContentSid { get; set; } = string.Empty;
}
