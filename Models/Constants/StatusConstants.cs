namespace LightenUp.Web.Models.Constants
{
    public static class AssignmentStatus
    {
        public const string Active = "Active";
        public const string PendingAdminApproval = "PendingAdminApproval";
        public const string PendingPsychologistApproval = "PendingPsychologistApproval";
        public const string PendingCancellationByHr = "PendingCancellationByHr";
        public const string PendingCancellationByAdmin = "PendingCancellationByAdmin";
        public const string Cancelled = "Cancelled";
        public const string Rejected = "Rejected";
    }

    public static class PaymentStatus
    {
        public const string Pending = "pending";
        public const string Paid = "paid";
        public const string Failed = "failed";
        public const string Expired = "expired";
    }

    public static class ScheduleStatus
    {
        public const string Scheduled = "Scheduled";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
        public const string NoShow = "NoShow";
    }

    public static class SubscriptionStatus
    {
        public const string Pending = "Pending";
        public const string Active = "Active";
        public const string Expired = "Expired";
    }

    public static class MentalHealthStatus
    {
        public const string Sehat = "Sehat";
        public const string Beresiko = "Beresiko";
        public const string Bahaya = "Bahaya";
    }

    public static class WorksheetStatus
    {
        public const string Assigned = "Assigned";
        public const string InProgress = "InProgress";
        public const string Completed = "Completed";
    }

    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Patient = "Patient";
        public const string Psychologist = "Psychologist";
        public const string HR = "HR";
    }

    public static class Feelings
    {
        public const string Overjoyed = "Overjoyed";
        public const string Happy = "Happy";
        public const string Calm = "Calm";
        public const string Neutral = "Neutral";
        public const string Disappointed = "Disappointed";
        public const string Angry = "Angry";
    }

    public static class ReportDirection
    {
        public const string HrToPsy = "HrToPsy";
        public const string PsyToHr = "PsyToHr";
    }

    public static class ReportStatus
    {
        public const string Draft = "Draft";
        public const string Sent = "Sent";
    }

    public static class RequestType
    {
        public const string Worksheet = "Worksheet";
        public const string Schedule = "Schedule";
    }

    public static class RequestStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
    }

    public static class PayrollAgreementStatus
    {
        public const string None = "None";
        public const string PendingAdmin = "PendingAdmin";
        public const string Approved = "Approved";
    }

    public static class PayrollSettingStatus
    {
        public const string Active = "Active";
        public const string PendingPsyApproval = "PendingPsyApproval";
        public const string RejectedByPsy = "RejectedByPsy";
    }

    public static class Gender
    {
        public const string Male = "Male";
        public const string Female = "Female";
    }

    public static class EmploymentStatus
    {
        public const string Active = "active";
    }

    public static class RequesterRole
    {
        public const string HR = "HR";
        public const string Patient = "Patient";
        public const string Psychologist = "Psychologist";
    }

    public static class PayoutStatus
    {
        public const string Pending = "Pending";
        public const string Paid = "Paid";
    }

    public static class PatientAdminRequestStatus
    {
        public const string Pending = "Pending";
        public const string Assigned = "Assigned";
        public const string Dismissed = "Dismissed";
    }

    public static class RemovalRequestStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
    }
}
