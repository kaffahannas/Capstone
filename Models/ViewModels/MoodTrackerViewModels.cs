using System.ComponentModel.DataAnnotations;

namespace LightenUp.Web.Models.ViewModels
{
    // Shared across all 4 mood tracker steps. Hidden fields carry data forward.
    public class MoodTrackerSessionViewModel
    {
        [Required(ErrorMessage = "Pilih perasaan kamu.")]
        public string Feeling { get; set; } = string.Empty;
        // Overjoyed, Happy, Calm, Neutral, Disappointed, Angry

        public List<string> Triggers { get; set; } = new();
        // Self, Family, School, Work, Friends, Partner, Hobby, Activity, SocialMedia, Entertainment

        [StringLength(500)]
        public string? Note { get; set; }
    }

    public static class MoodOptions
    {
        public static readonly (string Value, string Label, string Emoji)[] Feelings = new[]
        {
            ("Overjoyed", "Overjoyed", "😆"),
            ("Happy", "Happy", "😊"),
            ("Calm", "Calm", "😌"),
            ("Neutral", "Neutral", "😐"),
            ("Disappointed", "Disappointed", "🙁"),
            ("Angry", "Angry", "😠")
        };

        public static readonly (string Value, string Label)[] Triggers = new[]
        {
            ("Self", "Diri sendiri"),
            ("Family", "Keluarga"),
            ("School", "Sekolah"),
            ("Work", "Pekerjaan"),
            ("Friends", "Teman"),
            ("Partner", "Pacar"),
            ("Hobby", "Hobi"),
            ("Activity", "Aktivitas indoor/outdoor"),
            ("SocialMedia", "Media sosial"),
            ("Entertainment", "Film/Musik/TV")
        };

        public static string TriggerLabel(string value)
        {
            foreach (var t in Triggers)
                if (t.Value == value) return t.Label;
            return value;
        }
    }
}
