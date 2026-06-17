using System.ComponentModel.DataAnnotations;

namespace LightenUp.Web.Models.ViewModels
{
    // Shared across all mood tracker steps. Hidden fields carry data forward.
    public class MoodTrackerSessionViewModel
    {
        [Required(ErrorMessage = "Pilih perasaan kamu.")]
        public string Feeling { get; set; } = string.Empty;

        public List<string> Triggers { get; set; } = new();

        [StringLength(500)]
        public string? Note { get; set; }

        // Questionnaire scores (1-5). Accumulated via hidden fields.
        [Range(0, 5)] public int FocusScore    { get; set; }
        [Range(0, 5)] public int AnxietyScore  { get; set; }
        [Range(0, 5)] public int SleepScore    { get; set; }
        [Range(0, 5)] public int MindLoadScore { get; set; }
        [Range(0, 5)] public int EmotionScore  { get; set; }

        // Which question step we are on (1-5) during the questionnaire phase.
        public int QuestionStep { get; set; }

        public int CurrentQuestionScore() => QuestionStep switch
        {
            1 => FocusScore,
            2 => AnxietyScore,
            3 => SleepScore,
            4 => MindLoadScore,
            5 => EmotionScore,
            _ => 0
        };
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

        /// <summary>Parse stored trigger CSV; normalize labels to keys and trim.</summary>
        public static IEnumerable<string> ParseStoredTriggers(string? triggersCsv)
        {
            if (string.IsNullOrWhiteSpace(triggersCsv)) yield break;

            foreach (var raw in triggersCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var key = NormalizeTriggerKey(raw);
                if (!string.IsNullOrEmpty(key))
                    yield return key;
            }
        }

        public static string NormalizeTriggerKey(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            foreach (var t in Triggers)
            {
                if (string.Equals(t.Value, raw, StringComparison.OrdinalIgnoreCase))
                    return t.Value;
                if (string.Equals(t.Label, raw, StringComparison.OrdinalIgnoreCase))
                    return t.Value;
            }

            return raw.Trim();
        }

        public static List<string> SanitizeTriggerList(IEnumerable<string>? triggers, string? customTrigger = null)
        {
            var raw = new List<string>();
            if (triggers != null)
            {
                foreach (var t in triggers)
                {
                    if (!string.IsNullOrWhiteSpace(t))
                        raw.Add(t.Trim());
                }
            }
            if (!string.IsNullOrWhiteSpace(customTrigger))
                raw.Add(customTrigger.Trim());

            return ParseStoredTriggers(string.Join(",", raw))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string SerializeTriggers(IEnumerable<string>? triggers, string? customTrigger = null)
            => string.Join(",", SanitizeTriggerList(triggers, customTrigger));
    }

    public static class MoodQuestions
    {
        public static readonly (string Field, string Question, string Hint)[] All = new[]
        {
            ("FocusScore",    "Seberapa mampu kamu fokus hari ini?",              "1 = Sangat sulit fokus, 5 = Sangat fokus"),
            ("AnxietyScore",  "Seberapa tenang perasaanmu saat ini?",             "1 = Sangat cemas, 5 = Sangat tenang"),
            ("SleepScore",    "Bagaimana kualitas tidurmu semalam?",              "1 = Sangat buruk, 5 = Sangat baik"),
            ("MindLoadScore", "Seberapa ringan beban pikiran yang kamu rasakan?", "1 = Sangat berat, 5 = Sangat ringan"),
            ("EmotionScore",  "Seberapa baik kamu mengelola emosimu hari ini?",  "1 = Sangat buruk, 5 = Sangat baik"),
        };

        public static (string Field, string Question, string Hint) For(int step) => All[step - 1];
    }
}
