namespace LightenUp.Web.Models.ViewModels;

public class SubscriptionPlanViewModel
{
    public string PlanId { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string Description { get; set; } = "";
    public int DurationMonths { get; set; } = 1;
    public int EmployeeLimit { get; set; }
}

public class PatientSubscriptionIndexViewModel
{
    public List<SubscriptionPlanViewModel> Plans { get; set; } = new();
    public string? ActivePlanName { get; set; }
    public DateTime? ActiveUntil { get; set; }
    public bool HasActiveSubscription { get; set; }
    public bool IsB2B { get; set; }
    public bool CompanySponsors { get; set; }
    public string? CompanyName { get; set; }
}

    public class HrSubscriptionIndexViewModel
    {
        public List<SubscriptionPlanViewModel> Plans { get; set; } = new();
        public string? ActivePlanName { get; set; }
        public DateTime? ActiveUntil { get; set; }
        public bool HasActiveSubscription { get; set; }
        
        public List<CompanyDivisionViewModel> Divisions { get; set; } = new();
        
        public string CompanyName { get; set; } = "";
        public bool SubscriptionRequired { get; set; }
    }

    public class CompanyDivisionViewModel
    {
        public int DivisionId { get; set; }
        public string Name { get; set; } = "";
        public string? ReferralCode { get; set; }
        public int EmployeeCount { get; set; }
    }
