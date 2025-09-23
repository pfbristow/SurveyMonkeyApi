namespace SurveyMonkey.RequestSettings;

public class GetSurveyCategoryListSettings : IPagingSettings
{
    public int? Page { get; set; }
    public int? PerPage { get; set; }
    public string Language { get; set; } // Seems to use standard ISO 639 language codes
}