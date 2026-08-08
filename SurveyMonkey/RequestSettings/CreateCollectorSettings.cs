using System;
using SurveyMonkey.Containers;

namespace SurveyMonkey.RequestSettings;

public class CreateCollectorSettings
{
    public Collector.CollectorType? Type { get; set; }
    public string Name { get; set; }
    public string ThankYouMessage { get; set; } // Deprecated in favor of ThankYouPage
    public string DisqualificationMessage { get; set; }
    public string DisqualificationUrl { get; set; }
    //The API's datetime formatting treats this property differently to everything else, so omitting until SM's support team respond
    public DateTime? CloseDate { get; set; }
    public string ClosedPageMessage { get; set; }
    public string RedirectUrl { get; set; }
    public bool? DisplaySurveyResults { get; set; }
    public Collector.EditResponseOption? EditResponseType { get; set; }
    public Collector.AnonymousOption? AnonymousType { get; set; }
    public bool? AllowMultipleResponses { get; set; }
    public string Password { get; set; }
    public string SenderEmail { get; set; }
    public int? ResponseLimit { get; set; }
    public Collector.RedirectOption? RedirectType { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string BorderColor { get; set; }
    public bool? IsBrandingEnabled { get; set; }
    public string Headline { get; set; }
    public string Message { get; set; }
    public int? SampleRate { get; set; }
    // TODO: public object PrimaryButton { get; set; }
    // TODO: public object SecondaryButton { get; set; }
    public bool? RespondentAuthentication { get; set; }
    public long? FromCollectorId { get; set; }
}