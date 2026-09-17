namespace GymManager.API.Options;

public sealed class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";
    public bool CampaignsEnabled { get; set; }
    public int MaxRecipientsPerCampaign { get; set; } = 200;
    public int CampaignBatchSize { get; set; } = 20;
    public int CampaignDelayMilliseconds { get; set; } = 250;
}
