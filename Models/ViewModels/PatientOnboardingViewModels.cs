using System.ComponentModel.DataAnnotations;

namespace LightenUp.Web.Models.ViewModels
{
    // Helper: total step count for the progress bar, and which step is current.
    public class OnboardingProgress
    {
        public int Current { get; set; }
        public int Total => 10; // Gender, Birthdate, Relationship, Spiritual, Counseling, Medication, Sleep, AppGoals, ReferralCode, Terms
        public int Percent => (int)((double)Current / Total * 100);
    }

    // Step 1: Gender
    public class OnboardingGenderViewModel
    {
        [Required(ErrorMessage = "Silakan pilih gender.")]
        public string Gender { get; set; } = string.Empty; // "Male" or "Female"
    }

    // Step 2: Birthdate (3 dropdowns)
    public class OnboardingBirthdateViewModel
    {
        [Required(ErrorMessage = "Tanggal wajib dipilih.")]
        [Range(1, 31)]
        public int? Day { get; set; }

        [Required(ErrorMessage = "Bulan wajib dipilih.")]
        [Range(1, 12)]
        public int? Month { get; set; }

        [Required(ErrorMessage = "Tahun wajib dipilih.")]
        public int? Year { get; set; }

        public DateTime? AsDate()
        {
            if (Day is null || Month is null || Year is null) return null;
            try { return new DateTime(Year.Value, Month.Value, Day.Value); }
            catch { return null; }
        }
    }

    // Step 3: Relationship
    public class OnboardingRelationshipViewModel
    {
        [Required(ErrorMessage = "Pilih status hubungan.")]
        public string RelationshipStatus { get; set; } = string.Empty;
        // Values: Single, Dating, Married, Divorced, PreferNotToSay
    }

    // Step 4: Spiritual
    public class OnboardingSpiritualViewModel
    {
        [Required(ErrorMessage = "Pilih jawaban.")]
        public string SpiritualActivity { get; set; } = string.Empty;
        // Values: Active, Rare, Inactive
    }

    // Step 5: Counseling history (conditional follow-up)
    public class OnboardingCounselingViewModel
    {
        [Required(ErrorMessage = "Silakan pilih.")]
        public bool? HasPreviousCounseling { get; set; }

        // Only required if HasPreviousCounseling == true; controller validates.
        public List<string> CounselingMethods { get; set; } = new();
        // Values: CBT, Hypnotherapy, NLP, NarrativeTherapy, Psychoanalysis, Unsure, Other

        public string? CounselingMethodOther { get; set; }
    }

    // Step 6: Medication
    public class OnboardingMedicationViewModel
    {
        [Required(ErrorMessage = "Silakan pilih.")]
        public bool? HasMedicationHistory { get; set; }
    }

    // Step 7: Sleep quality
    public class OnboardingSleepViewModel
    {
        [Required(ErrorMessage = "Pilih salah satu opsi.")]
        public string SleepQuality { get; set; } = string.Empty;
        // Values: VeryGood, Average, Poor, Bad, VeryBad
    }

    // Step 8: App goals (multi-select, ≥1)
    public class OnboardingAppGoalsViewModel
    {
        [Required(ErrorMessage = "Pilih minimal satu tujuan.")]
        [MinLength(1, ErrorMessage = "Pilih minimal satu tujuan.")]
        public List<string> AppGoals { get; set; } = new();
        // Values: MoodTracking, Journaling, PersonalGoals, Unsure
    }

    // Step 9: Referral code (optional)
    public class OnboardingReferralViewModel
    {
        [StringLength(64)]
        public string? ReferralCode { get; set; }
    }

    // Step 10: Terms acceptance — manual validation in controller
    // (Range(typeof(bool)) is flaky on POST; we now check `model.Accepted` directly.)
    public class OnboardingTermsViewModel
    {
        public bool Accepted { get; set; }
    }
}
