using System.ComponentModel.DataAnnotations;

namespace LightenUp.Web.Models.ViewModels
{
    // ─── Daily 6-question Check-in ───
    // Accumulated across 6 steps via hidden fields, saved at the end.
    // #Class JournalCheckInSessionViewModel#
    public class JournalCheckInSessionViewModel
    {
        [Range(0, 5)] public int FocusScore { get; set; }      // Q1
        [Range(0, 5)] public int AnxietyScore { get; set; }    // Q2  (5 = least anxious)
        [Range(0, 5)] public int SleepScore { get; set; }      // Q3
        [Range(0, 5)] public int MindLoadScore { get; set; }   // Q4  (5 = least burdened)
        [Range(0, 5)] public int EmotionScore { get; set; }    // Q5
        [Range(0, 5)] public int OverallScore { get; set; }    // Q6

        // 1..6 — the step currently being asked.
        public int Step { get; set; }

        // For convenience inside Question.cshtml
        public int CurrentScore()
        {
            return Step switch
            {
                1 => FocusScore,
                2 => AnxietyScore,
                3 => SleepScore,
                4 => MindLoadScore,
                5 => EmotionScore,
                6 => OverallScore,
                _ => 0
            };
        }
    }

    // #Class CheckInQuestions#
    public static class CheckInQuestions
    {
        public static readonly (string Field, string Question)[] All = new[]
        {
            ("FocusScore",    "Seberapa mampu kamu fokus hari ini?"),
            ("AnxietyScore",  "Apakah kamu merasa cemas atau gelisah?"),
            ("SleepScore",    "Bagaimana kualitas tidurmu semalam?"),
            ("MindLoadScore", "Seberapa berat beban pikiran yang kamu rasakan?"),
            ("EmotionScore",  "Apakah kamu merasa mampu mengelola emosimu hari ini?"),
            ("OverallScore",  "Bagaimana perasaanmu hari ini secara keseluruhan?")
        };

        public static (string Field, string Question) For(int step) => All[step - 1];
    }

    // ─── Free-write journal entry ───
    // #Class JournalWriteViewModel#
    public class JournalWriteViewModel
    {
        public int? JournalId { get; set; }   // null = create new, else update

        [StringLength(200)]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Tuliskan isi jurnal kamu.")]
        public string Content { get; set; } = string.Empty;
    }
}
