using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LightenUp.Web.Migrations
{
    /// <inheritdoc />
    public partial class YourMigrationName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoleType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfilePicture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsApprovedByAdmin = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    CompanyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferralCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.CompanyId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Psychologists",
                columns: table => new
                {
                    PsychologistId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Specialization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LicenseNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExperienceYears = table.Column<int>(type: "int", nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastDegree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    University = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcademicDocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SiapNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StrDocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PracticeLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcceptsB2B = table.Column<bool>(type: "bit", nullable: false),
                    OnboardingCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AvailabilityText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    OfficeAddress = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Psychologists", x => x.PsychologistId);
                    table.ForeignKey(
                        name: "FK_Psychologists_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyDivisions",
                columns: table => new
                {
                    DivisionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReferralCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyDivisions", x => x.DivisionId);
                    table.ForeignKey(
                        name: "FK_CompanyDivisions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanySubscriptions",
                columns: table => new
                {
                    CompanySubscriptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmployeeLimit = table.Column<int>(type: "int", nullable: false),
                    MaxSessionsPerMonth = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySubscriptions", x => x.CompanySubscriptionId);
                    table.ForeignKey(
                        name: "FK_CompanySubscriptions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HrStaffs",
                columns: table => new
                {
                    HrId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmployeeId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastDegree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    University = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcademicDocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupportDocumentUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OnboardingCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrStaffs", x => x.HrId);
                    table.ForeignKey(
                        name: "FK_HrStaffs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HrStaffs_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId");
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    PatientId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmploymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MentalHealthStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Symptoms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RelationshipStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpiritualActivity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasPreviousCounseling = table.Column<bool>(type: "bit", nullable: true),
                    CounselingMethods = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CounselingMethodOther = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasMedicationHistory = table.Column<bool>(type: "bit", nullable: true),
                    SleepQuality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppGoals = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TermsAcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OnboardingCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmergencyContactEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmergencyContactRelation = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.PatientId);
                    table.ForeignKey(
                        name: "FK_Patients_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Patients_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId");
                });

            migrationBuilder.CreateTable(
                name: "CompanyPsychologist",
                columns: table => new
                {
                    PartneredCompaniesCompanyId = table.Column<int>(type: "int", nullable: false),
                    PartneredPsychologistsPsychologistId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyPsychologist", x => new { x.PartneredCompaniesCompanyId, x.PartneredPsychologistsPsychologistId });
                    table.ForeignKey(
                        name: "FK_CompanyPsychologist_Companies_PartneredCompaniesCompanyId",
                        column: x => x.PartneredCompaniesCompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyPsychologist_Psychologists_PartneredPsychologistsPsychologistId",
                        column: x => x.PartneredPsychologistsPsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyPayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PsychologistId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProofOfTransferFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyPayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyPayouts_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PsychologistId = table.Column<int>(type: "int", nullable: false),
                    SessionRate = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    PsychologistPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProposedPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    AdminReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedByAdminUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AgreementStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    BankDetailsPdfPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollSettings_AspNetUsers_UpdatedByAdminUserId",
                        column: x => x.UpdatedByAdminUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PayrollSettings_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PsyNotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PsychologistId = table.Column<int>(type: "int", nullable: false),
                    RemindNewReports = table.Column<bool>(type: "bit", nullable: false),
                    RemindFollowUp = table.Column<bool>(type: "bit", nullable: false),
                    AllowHrPatientNotif = table.Column<bool>(type: "bit", nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsyNotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PsyNotificationPreferences_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HrNotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HrId = table.Column<int>(type: "int", nullable: false),
                    RemindEmployeeCheck = table.Column<bool>(type: "bit", nullable: false),
                    RemindCounselingSession = table.Column<bool>(type: "bit", nullable: false),
                    AllowEmployeePsyNotif = table.Column<bool>(type: "bit", nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrNotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HrNotificationPreferences_HrStaffs_HrId",
                        column: x => x.HrId,
                        principalTable: "HrStaffs",
                        principalColumn: "HrId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    PsychologistId = table.Column<int>(type: "int", nullable: false),
                    AssignedByHrUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RequestedByRole = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CancellationRequestedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CancellationRequestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DecisionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SlotValue = table.Column<decimal>(type: "decimal(14,2)", nullable: true),
                    MaxSessionsPerMonth = table.Column<int>(type: "int", nullable: true),
                    PsychologistRevenuePercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_Assignments_AspNetUsers_AssignedByHrUserId",
                        column: x => x.AssignedByHrUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Assignments_AspNetUsers_CancellationRequestedByUserId",
                        column: x => x.CancellationRequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_AspNetUsers_DecisionByUserId",
                        column: x => x.DecisionByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HrEmployeeRemovalRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    RequestedByHrUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecisionByAdminUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DecisionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrEmployeeRemovalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HrEmployeeRemovalRequests_AspNetUsers_DecisionByAdminUserId",
                        column: x => x.DecisionByAdminUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HrEmployeeRemovalRequests_AspNetUsers_RequestedByHrUserId",
                        column: x => x.RequestedByHrUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_HrEmployeeRemovalRequests_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JournalCheckIns",
                columns: table => new
                {
                    CheckInId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    FocusScore = table.Column<int>(type: "int", nullable: false),
                    AnxietyScore = table.Column<int>(type: "int", nullable: false),
                    SleepScore = table.Column<int>(type: "int", nullable: false),
                    MindLoadScore = table.Column<int>(type: "int", nullable: false),
                    EmotionScore = table.Column<int>(type: "int", nullable: false),
                    OverallScore = table.Column<int>(type: "int", nullable: false),
                    CheckInDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalCheckIns", x => x.CheckInId);
                    table.ForeignKey(
                        name: "FK_JournalCheckIns_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Journals",
                columns: table => new
                {
                    JournalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JournalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Journals", x => x.JournalId);
                    table.ForeignKey(
                        name: "FK_Journals_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MoodTrackers",
                columns: table => new
                {
                    MoodId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    Feeling = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Triggers = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FocusScore = table.Column<int>(type: "int", nullable: true),
                    AnxietyScore = table.Column<int>(type: "int", nullable: true),
                    SleepScore = table.Column<int>(type: "int", nullable: true),
                    MindLoadScore = table.Column<int>(type: "int", nullable: true),
                    EmotionScore = table.Column<int>(type: "int", nullable: true),
                    MoodDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoodTrackers", x => x.MoodId);
                    table.ForeignKey(
                        name: "FK_MoodTrackers_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientAdminAssignmentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    PreferredPsychologistId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AssignedPsychologistId = table.Column<int>(type: "int", nullable: true),
                    AssignedByAdminUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecisionAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAdminAssignmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientAdminAssignmentRequests_AspNetUsers_AssignedByAdminUserId",
                        column: x => x.AssignedByAdminUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientAdminAssignmentRequests_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientAdminAssignmentRequests_Psychologists_AssignedPsychologistId",
                        column: x => x.AssignedPsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientAdminAssignmentRequests_Psychologists_PreferredPsychologistId",
                        column: x => x.PreferredPsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientNotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    RemindMoodCheck = table.Column<bool>(type: "bit", nullable: false),
                    RemindCounselingSession = table.Column<bool>(type: "bit", nullable: false),
                    AllowHrPsychologistNotif = table.Column<bool>(type: "bit", nullable: false),
                    ReminderTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientNotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientNotificationPreferences_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingEmployees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    EmployeeId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClaimedByPatientId = table.Column<int>(type: "int", nullable: true),
                    ClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingEmployees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingEmployees_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PendingEmployees_Patients_ClaimedByPatientId",
                        column: x => x.ClaimedByPatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PsychologistRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestedByHrUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RequestedByPatientUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RequesterRole = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    PsychologistId = table.Column<int>(type: "int", nullable: true),
                    RequestType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProposedTaskName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProposedDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProposedSessionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RespondedNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PsychologistRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PsychologistRequests_AspNetUsers_RequestedByHrUserId",
                        column: x => x.RequestedByHrUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PsychologistRequests_AspNetUsers_RequestedByPatientUserId",
                        column: x => x.RequestedByPatientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PsychologistRequests_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PsychologistRequests_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Direction = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ReportedByHrUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReportedByPsyUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    HrRecipientUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    PsychologistId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmailSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmailSubject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EmailBody = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_AspNetUsers_HrRecipientUserId",
                        column: x => x.HrRecipientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reports_AspNetUsers_ReportedByHrUserId",
                        column: x => x.ReportedByHrUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reports_AspNetUsers_ReportedByPsyUserId",
                        column: x => x.ReportedByPsyUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reports_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reports_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    ScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PsychologistId = table.Column<int>(type: "int", nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    SessionStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MeetingLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppliedPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    SlotValue = table.Column<decimal>(type: "decimal(14,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.ScheduleId);
                    table.ForeignKey(
                        name: "FK_Schedules_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Schedules_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    SubscriptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxSessionsPerMonth = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Worksheets",
                columns: table => new
                {
                    WorksheetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PsychologistId = table.Column<int>(type: "int", nullable: false),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    TaskName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProofImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PsychologistFeedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HrNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Worksheets", x => x.WorksheetId);
                    table.ForeignKey(
                        name: "FK_Worksheets_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Worksheets_Psychologists_PsychologistId",
                        column: x => x.PsychologistId,
                        principalTable: "Psychologists",
                        principalColumn: "PsychologistId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    PaymentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: true),
                    SubscriptionId = table.Column<int>(type: "int", nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    CompanySubscriptionId = table.Column<int>(type: "int", nullable: true),
                    MerchantOrderId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DuitkuReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(14,2)", nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PaymentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ResultCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ResultMessage = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CallbackPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_CompanySubscriptions_CompanySubscriptionId",
                        column: x => x.CompanySubscriptionId,
                        principalTable: "CompanySubscriptions",
                        principalColumn: "CompanySubscriptionId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "SubscriptionId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_AssignedByHrUserId",
                table: "Assignments",
                column: "AssignedByHrUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_CancellationRequestedByUserId",
                table: "Assignments",
                column: "CancellationRequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_DecisionByUserId",
                table: "Assignments",
                column: "DecisionByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_PatientId",
                table: "Assignments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_PsychologistId",
                table: "Assignments",
                column: "PsychologistId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_RequestedByUserId",
                table: "Assignments",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_ReferralCode",
                table: "Companies",
                column: "ReferralCode",
                unique: true,
                filter: "[ReferralCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDivisions_CompanyId",
                table: "CompanyDivisions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyDivisions_ReferralCode",
                table: "CompanyDivisions",
                column: "ReferralCode",
                unique: true,
                filter: "[ReferralCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyPsychologist_PartneredPsychologistsPsychologistId",
                table: "CompanyPsychologist",
                column: "PartneredPsychologistsPsychologistId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySubscriptions_CompanyId",
                table: "CompanySubscriptions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployeeRemovalRequests_DecisionByAdminUserId",
                table: "HrEmployeeRemovalRequests",
                column: "DecisionByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployeeRemovalRequests_PatientId",
                table: "HrEmployeeRemovalRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployeeRemovalRequests_RequestedByHrUserId",
                table: "HrEmployeeRemovalRequests",
                column: "RequestedByHrUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HrNotificationPreferences_HrId",
                table: "HrNotificationPreferences",
                column: "HrId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HrStaffs_CompanyId",
                table: "HrStaffs",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_HrStaffs_UserId",
                table: "HrStaffs",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalCheckIns_PatientId_CheckInDate",
                table: "JournalCheckIns",
                columns: new[] { "PatientId", "CheckInDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Journals_PatientId_JournalDate",
                table: "Journals",
                columns: new[] { "PatientId", "JournalDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPayouts_PsychologistId",
                table: "MonthlyPayouts",
                column: "PsychologistId");

            migrationBuilder.CreateIndex(
                name: "IX_MoodTrackers_PatientId_MoodDate",
                table: "MoodTrackers",
                columns: new[] { "PatientId", "MoodDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientAdminAssignmentRequests_AssignedByAdminUserId",
                table: "PatientAdminAssignmentRequests",
                column: "AssignedByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAdminAssignmentRequests_AssignedPsychologistId",
                table: "PatientAdminAssignmentRequests",
                column: "AssignedPsychologistId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAdminAssignmentRequests_PatientId",
                table: "PatientAdminAssignmentRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAdminAssignmentRequests_PreferredPsychologistId",
                table: "PatientAdminAssignmentRequests",
                column: "PreferredPsychologistId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientNotificationPreferences_PatientId",
                table: "PatientNotificationPreferences",
                column: "PatientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_CompanyId_EmployeeId",
                table: "Patients",
                columns: new[] { "CompanyId", "EmployeeId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [EmployeeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_UserId",
                table: "Patients",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_CompanyId",
                table: "PaymentTransactions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_CompanySubscriptionId",
                table: "PaymentTransactions",
                column: "CompanySubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_MerchantOrderId",
                table: "PaymentTransactions",
                column: "MerchantOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PatientId",
                table: "PaymentTransactions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_SubscriptionId",
                table: "PaymentTransactions",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettings_PsychologistId",
                table: "PayrollSettings",
                column: "PsychologistId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSettings_UpdatedByAdminUserId",
                table: "PayrollSettings",
                column: "UpdatedByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingEmployees_ClaimedByPatientId",
                table: "PendingEmployees",
                column: "ClaimedByPatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingEmployees_CompanyId_Email",
                table: "PendingEmployees",
                columns: new[] { "CompanyId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PsychologistRequests_PatientId",
                table: "PsychologistRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PsychologistRequests_PsychologistId",
                table: "PsychologistRequests",
                column: "PsychologistId");

            migrationBuilder.CreateIndex(
                name: "IX_PsychologistRequests_RequestedByHrUserId",
                table: "PsychologistRequests",
                column: "RequestedByHrUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PsychologistRequests_RequestedByPatientUserId",
                table: "PsychologistRequests",
                column: "RequestedByPatientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Psychologists_UserId",
                table: "Psychologists",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PsyNotificationPreferences_PsychologistId",
                table: "PsyNotificationPreferences",
                column: "PsychologistId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_HrRecipientUserId",
                table: "Reports",
                column: "HrRecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_PatientId",
                table: "Reports",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_PsychologistId",
                table: "Reports",
                column: "PsychologistId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportedByHrUserId",
                table: "Reports",
                column: "ReportedByHrUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportedByPsyUserId",
                table: "Reports",
                column: "ReportedByPsyUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_PatientId",
                table: "Schedules",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_PsychologistId",
                table: "Schedules",
                column: "PsychologistId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PatientId",
                table: "Subscriptions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Worksheets_PatientId",
                table: "Worksheets",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Worksheets_PsychologistId",
                table: "Worksheets",
                column: "PsychologistId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropTable(
                name: "CompanyDivisions");

            migrationBuilder.DropTable(
                name: "CompanyPsychologist");

            migrationBuilder.DropTable(
                name: "HrEmployeeRemovalRequests");

            migrationBuilder.DropTable(
                name: "HrNotificationPreferences");

            migrationBuilder.DropTable(
                name: "JournalCheckIns");

            migrationBuilder.DropTable(
                name: "Journals");

            migrationBuilder.DropTable(
                name: "MonthlyPayouts");

            migrationBuilder.DropTable(
                name: "MoodTrackers");

            migrationBuilder.DropTable(
                name: "PatientAdminAssignmentRequests");

            migrationBuilder.DropTable(
                name: "PatientNotificationPreferences");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "PayrollSettings");

            migrationBuilder.DropTable(
                name: "PendingEmployees");

            migrationBuilder.DropTable(
                name: "PsychologistRequests");

            migrationBuilder.DropTable(
                name: "PsyNotificationPreferences");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "Schedules");

            migrationBuilder.DropTable(
                name: "Worksheets");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "HrStaffs");

            migrationBuilder.DropTable(
                name: "CompanySubscriptions");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "Psychologists");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
