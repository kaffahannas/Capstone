namespace LightenUp.Web.Models.ViewModels
{
    public class HrPsychologistCard
    {
        public int PsychologistId { get; set; }
        public string FullName { get; set; } = "";
        public string? Specialization { get; set; }
        public string? ProfilePicture { get; set; }
        public int? ExperienceYears { get; set; }
        public string? Email { get; set; }
        public string? Bio { get; set; }
        public string? University { get; set; }
        public string? LastDegree { get; set; }
        public string? PracticeLocation { get; set; }
        public bool AlreadyPartnered { get; set; }
    }

    public class HrPsychologistDirectoryViewModel
    {
        public List<HrPsychologistCard> Psychologists { get; set; } = new();
    }

    public class HrPsychologistProfileViewModel
    {
        public int PsychologistId { get; set; }
        public string FullName { get; set; } = "";
        public string? Specialization { get; set; }
        public string? Bio { get; set; }
        public string? University { get; set; }
        public string? LastDegree { get; set; }
        public int? ExperienceYears { get; set; }
        public string? PracticeLocation { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Email { get; set; }
        public bool AlreadyPartnered { get; set; }
    }
}
