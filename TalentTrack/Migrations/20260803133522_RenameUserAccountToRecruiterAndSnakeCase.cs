using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TalentTrack.Migrations
{
    /// <inheritdoc />
    public partial class RenameUserAccountToRecruiterAndSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateApplications_Candidates_CandidateId",
                table: "CandidateApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_CandidateApplications_Jobs_JobId",
                table: "CandidateApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_CandidateSkills_Candidates_CandidateId",
                table: "CandidateSkills");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewFeedbacks_Candidates_CandidateId",
                table: "InterviewFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewFeedbacks_Interviewers_InterviewerId",
                table: "InterviewFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewFeedbacks_Interviews_InterviewId",
                table: "InterviewFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewParticipants_Interviews_InterviewId",
                table: "InterviewParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewParticipants_UserAccounts_UserAccountId",
                table: "InterviewParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_Interviews_Candidates_CandidateId",
                table: "Interviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Interviews_Interviewers_InterviewerId",
                table: "Interviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Interviews_Jobs_JobId",
                table: "Interviews");

            migrationBuilder.DropForeignKey(
                name: "FK_JobSkills_Jobs_JobId",
                table: "JobSkills");

            migrationBuilder.DropForeignKey(
                name: "FK_Screenings_CandidateApplications_ApplicationId",
                table: "Screenings");

            migrationBuilder.DropForeignKey(
                name: "FK_ScreeningSkillEvaluations_Screenings_ScreeningId",
                table: "ScreeningSkillEvaluations");

            migrationBuilder.DropTable(
                name: "UserAccounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Screenings",
                table: "Screenings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Notifications",
                table: "Notifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Jobs",
                table: "Jobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Interviews",
                table: "Interviews");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Interviewers",
                table: "Interviewers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Candidates",
                table: "Candidates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScreeningSkillEvaluations",
                table: "ScreeningSkillEvaluations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JobSkills",
                table: "JobSkills");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InterviewParticipants",
                table: "InterviewParticipants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InterviewFeedbacks",
                table: "InterviewFeedbacks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CandidateSkills",
                table: "CandidateSkills");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CandidateApplications",
                table: "CandidateApplications");

            migrationBuilder.RenameTable(
                name: "Screenings",
                newName: "screenings");

            migrationBuilder.RenameTable(
                name: "Notifications",
                newName: "notifications");

            migrationBuilder.RenameTable(
                name: "Jobs",
                newName: "jobs");

            migrationBuilder.RenameTable(
                name: "Interviews",
                newName: "interviews");

            migrationBuilder.RenameTable(
                name: "Interviewers",
                newName: "interviewers");

            migrationBuilder.RenameTable(
                name: "Candidates",
                newName: "candidates");

            migrationBuilder.RenameTable(
                name: "ScreeningSkillEvaluations",
                newName: "screening_skill_evaluations");

            migrationBuilder.RenameTable(
                name: "JobSkills",
                newName: "job_skills");

            migrationBuilder.RenameTable(
                name: "InterviewParticipants",
                newName: "interview_participants");

            migrationBuilder.RenameTable(
                name: "InterviewFeedbacks",
                newName: "interview_feedbacks");

            migrationBuilder.RenameTable(
                name: "CandidateSkills",
                newName: "candidate_skills");

            migrationBuilder.RenameTable(
                name: "CandidateApplications",
                newName: "candidate_applications");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "screenings",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "screenings",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "ScreeningDate",
                table: "screenings",
                newName: "screening_date");

            migrationBuilder.RenameColumn(
                name: "ScreenedByUserId",
                table: "screenings",
                newName: "screened_by_user_id");

            migrationBuilder.RenameColumn(
                name: "ApplicationId",
                table: "screenings",
                newName: "application_id");

            migrationBuilder.RenameColumn(
                name: "ScreeningId",
                table: "screenings",
                newName: "screening_id");

            migrationBuilder.RenameIndex(
                name: "IX_Screenings_ApplicationId",
                table: "screenings",
                newName: "ix_screenings_application_id");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "notifications",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Message",
                table: "notifications",
                newName: "message");

            migrationBuilder.RenameColumn(
                name: "TargetUserEmail",
                table: "notifications",
                newName: "target_user_email");

            migrationBuilder.RenameColumn(
                name: "TargetUrl",
                table: "notifications",
                newName: "target_url");

            migrationBuilder.RenameColumn(
                name: "TargetRole",
                table: "notifications",
                newName: "target_role");

            migrationBuilder.RenameColumn(
                name: "IsRead",
                table: "notifications",
                newName: "is_read");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "notifications",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "NotificationId",
                table: "notifications",
                newName: "notification_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "jobs",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Skills",
                table: "jobs",
                newName: "skills");

            migrationBuilder.RenameColumn(
                name: "Location",
                table: "jobs",
                newName: "location");

            migrationBuilder.RenameColumn(
                name: "Experience",
                table: "jobs",
                newName: "experience");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "jobs",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "JobTitle",
                table: "jobs",
                newName: "job_title");

            migrationBuilder.RenameColumn(
                name: "JobId",
                table: "jobs",
                newName: "job_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "interviews",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "interviews",
                newName: "notes");

            migrationBuilder.RenameColumn(
                name: "Mode",
                table: "interviews",
                newName: "mode");

            migrationBuilder.RenameColumn(
                name: "Duration",
                table: "interviews",
                newName: "duration");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "interviews",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "MeetingLink",
                table: "interviews",
                newName: "meeting_link");

            migrationBuilder.RenameColumn(
                name: "JobId",
                table: "interviews",
                newName: "job_id");

            migrationBuilder.RenameColumn(
                name: "InterviewerId",
                table: "interviews",
                newName: "interviewer_id");

            migrationBuilder.RenameColumn(
                name: "InterviewDate",
                table: "interviews",
                newName: "interview_date");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "interviews",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CandidateId",
                table: "interviews",
                newName: "candidate_id");

            migrationBuilder.RenameColumn(
                name: "InterviewId",
                table: "interviews",
                newName: "interview_id");

            migrationBuilder.RenameIndex(
                name: "IX_Interviews_JobId",
                table: "interviews",
                newName: "ix_interviews_job_id");

            migrationBuilder.RenameIndex(
                name: "IX_Interviews_InterviewerId",
                table: "interviews",
                newName: "ix_interviews_interviewer_id");

            migrationBuilder.RenameIndex(
                name: "IX_Interviews_CandidateId",
                table: "interviews",
                newName: "ix_interviews_candidate_id");

            migrationBuilder.RenameColumn(
                name: "Specialization",
                table: "interviewers",
                newName: "specialization");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "interviewers",
                newName: "phone");

            migrationBuilder.RenameColumn(
                name: "Password",
                table: "interviewers",
                newName: "password");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "interviewers",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "interviewers",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Department",
                table: "interviewers",
                newName: "department");

            migrationBuilder.RenameColumn(
                name: "Bio",
                table: "interviewers",
                newName: "bio");

            migrationBuilder.RenameColumn(
                name: "YearsExperience",
                table: "interviewers",
                newName: "years_experience");

            migrationBuilder.RenameColumn(
                name: "LinkedIn",
                table: "interviewers",
                newName: "linked_in");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "interviewers",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "AvatarInitials",
                table: "interviewers",
                newName: "avatar_initials");

            migrationBuilder.RenameColumn(
                name: "InterviewerId",
                table: "interviewers",
                newName: "interviewer_id");

            migrationBuilder.RenameColumn(
                name: "Skills",
                table: "candidates",
                newName: "skills");

            migrationBuilder.RenameColumn(
                name: "Resume",
                table: "candidates",
                newName: "resume");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "candidates",
                newName: "phone");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "candidates",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Experience",
                table: "candidates",
                newName: "experience");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "candidates",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "candidates",
                newName: "last_name");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "candidates",
                newName: "first_name");

            migrationBuilder.RenameColumn(
                name: "CandidateId",
                table: "candidates",
                newName: "candidate_id");

            migrationBuilder.RenameColumn(
                name: "SkillName",
                table: "screening_skill_evaluations",
                newName: "skill_name");

            migrationBuilder.RenameColumn(
                name: "ScreeningId",
                table: "screening_skill_evaluations",
                newName: "screening_id");

            migrationBuilder.RenameColumn(
                name: "HasSkill",
                table: "screening_skill_evaluations",
                newName: "has_skill");

            migrationBuilder.RenameColumn(
                name: "ExperienceYears",
                table: "screening_skill_evaluations",
                newName: "experience_years");

            migrationBuilder.RenameColumn(
                name: "EvaluationId",
                table: "screening_skill_evaluations",
                newName: "evaluation_id");

            migrationBuilder.RenameIndex(
                name: "IX_ScreeningSkillEvaluations_ScreeningId",
                table: "screening_skill_evaluations",
                newName: "ix_screening_skill_evaluations_screening_id");

            migrationBuilder.RenameColumn(
                name: "SkillName",
                table: "job_skills",
                newName: "skill_name");

            migrationBuilder.RenameColumn(
                name: "RequiredExperience",
                table: "job_skills",
                newName: "required_experience");

            migrationBuilder.RenameColumn(
                name: "JobId",
                table: "job_skills",
                newName: "job_id");

            migrationBuilder.RenameColumn(
                name: "JobSkillId",
                table: "job_skills",
                newName: "job_skill_id");

            migrationBuilder.RenameIndex(
                name: "IX_JobSkills_JobId",
                table: "job_skills",
                newName: "ix_job_skills_job_id");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "interview_participants",
                newName: "role");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "interview_participants",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "InterviewId",
                table: "interview_participants",
                newName: "interview_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "interview_participants",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "InterviewParticipantId",
                table: "interview_participants",
                newName: "interview_participant_id");

            migrationBuilder.RenameColumn(
                name: "UserAccountId",
                table: "interview_participants",
                newName: "recruiter_id");

            migrationBuilder.RenameIndex(
                name: "IX_InterviewParticipants_UserAccountId",
                table: "interview_participants",
                newName: "ix_interview_participants_recruiter_id");

            migrationBuilder.RenameIndex(
                name: "IX_InterviewParticipants_InterviewId_UserAccountId",
                table: "interview_participants",
                newName: "ix_interview_participants_interview_id_recruiter_id");

            migrationBuilder.RenameColumn(
                name: "Weaknesses",
                table: "interview_feedbacks",
                newName: "weaknesses");

            migrationBuilder.RenameColumn(
                name: "Strengths",
                table: "interview_feedbacks",
                newName: "strengths");

            migrationBuilder.RenameColumn(
                name: "Recommendation",
                table: "interview_feedbacks",
                newName: "recommendation");

            migrationBuilder.RenameColumn(
                name: "Comments",
                table: "interview_feedbacks",
                newName: "comments");

            migrationBuilder.RenameColumn(
                name: "TechnicalScore",
                table: "interview_feedbacks",
                newName: "technical_score");

            migrationBuilder.RenameColumn(
                name: "ProblemSolvingScore",
                table: "interview_feedbacks",
                newName: "problem_solving_score");

            migrationBuilder.RenameColumn(
                name: "OverallRating",
                table: "interview_feedbacks",
                newName: "overall_rating");

            migrationBuilder.RenameColumn(
                name: "InterviewerId",
                table: "interview_feedbacks",
                newName: "interviewer_id");

            migrationBuilder.RenameColumn(
                name: "InterviewId",
                table: "interview_feedbacks",
                newName: "interview_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "interview_feedbacks",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CommunicationScore",
                table: "interview_feedbacks",
                newName: "communication_score");

            migrationBuilder.RenameColumn(
                name: "CandidateId",
                table: "interview_feedbacks",
                newName: "candidate_id");

            migrationBuilder.RenameColumn(
                name: "FeedbackId",
                table: "interview_feedbacks",
                newName: "feedback_id");

            migrationBuilder.RenameIndex(
                name: "IX_InterviewFeedbacks_InterviewId",
                table: "interview_feedbacks",
                newName: "ix_interview_feedbacks_interview_id");

            migrationBuilder.RenameIndex(
                name: "IX_InterviewFeedbacks_InterviewerId",
                table: "interview_feedbacks",
                newName: "ix_interview_feedbacks_interviewer_id");

            migrationBuilder.RenameIndex(
                name: "IX_InterviewFeedbacks_CandidateId",
                table: "interview_feedbacks",
                newName: "ix_interview_feedbacks_candidate_id");

            migrationBuilder.RenameColumn(
                name: "SkillName",
                table: "candidate_skills",
                newName: "skill_name");

            migrationBuilder.RenameColumn(
                name: "ExperienceYears",
                table: "candidate_skills",
                newName: "experience_years");

            migrationBuilder.RenameColumn(
                name: "CandidateId",
                table: "candidate_skills",
                newName: "candidate_id");

            migrationBuilder.RenameColumn(
                name: "CandidateSkillId",
                table: "candidate_skills",
                newName: "candidate_skill_id");

            migrationBuilder.RenameIndex(
                name: "IX_CandidateSkills_CandidateId",
                table: "candidate_skills",
                newName: "ix_candidate_skills_candidate_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "candidate_applications",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "JobId",
                table: "candidate_applications",
                newName: "job_id");

            migrationBuilder.RenameColumn(
                name: "CandidateId",
                table: "candidate_applications",
                newName: "candidate_id");

            migrationBuilder.RenameColumn(
                name: "AppliedDate",
                table: "candidate_applications",
                newName: "applied_date");

            migrationBuilder.RenameColumn(
                name: "ApplicationId",
                table: "candidate_applications",
                newName: "application_id");

            migrationBuilder.RenameIndex(
                name: "IX_CandidateApplications_JobId",
                table: "candidate_applications",
                newName: "ix_candidate_applications_job_id");

            migrationBuilder.RenameIndex(
                name: "IX_CandidateApplications_CandidateId_JobId",
                table: "candidate_applications",
                newName: "ix_candidate_applications_candidate_id_job_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_screenings",
                table: "screenings",
                column: "screening_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_notifications",
                table: "notifications",
                column: "notification_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_jobs",
                table: "jobs",
                column: "job_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_interviews",
                table: "interviews",
                column: "interview_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_interviewers",
                table: "interviewers",
                column: "interviewer_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_candidates",
                table: "candidates",
                column: "candidate_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_screening_skill_evaluations",
                table: "screening_skill_evaluations",
                column: "evaluation_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_job_skills",
                table: "job_skills",
                column: "job_skill_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_interview_participants",
                table: "interview_participants",
                column: "interview_participant_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_interview_feedbacks",
                table: "interview_feedbacks",
                column: "feedback_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_candidate_skills",
                table: "candidate_skills",
                column: "candidate_skill_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_candidate_applications",
                table: "candidate_applications",
                column: "application_id");

            migrationBuilder.CreateTable(
                name: "recruiters",
                columns: table => new
                {
                    recruiter_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    department = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_approved = table.Column<bool>(type: "bit", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recruiters", x => x.recruiter_id);
                });

            migrationBuilder.InsertData(
                table: "recruiters",
                columns: new[] { "recruiter_id", "created_at", "created_by", "department", "email", "is_approved", "name", "password", "phone", "role", "status", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "System", "Administration", "admin@talenttrack.com", true, "System Admin", "admin123", "+91 99999 88888", "Admin", "Approved", null, null },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "System", "Human Resources", "recruiter@talenttrack.com", true, "Mansi Verma", "recruiter123", "+91 98765 11111", "Recruiter", "Approved", null, null },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "System", "Engineering", "interviewer@talenttrack.com", true, "Priya Sharma", "interviewer123", "+91 98765 43210", "Interviewer", "Approved", null, null }
                });

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_applications_candidates_candidate_id",
                table: "candidate_applications",
                column: "candidate_id",
                principalTable: "candidates",
                principalColumn: "candidate_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_applications_jobs_job_id",
                table: "candidate_applications",
                column: "job_id",
                principalTable: "jobs",
                principalColumn: "job_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_candidate_skills_candidates_candidate_id",
                table: "candidate_skills",
                column: "candidate_id",
                principalTable: "candidates",
                principalColumn: "candidate_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_interview_feedbacks_candidates_candidate_id",
                table: "interview_feedbacks",
                column: "candidate_id",
                principalTable: "candidates",
                principalColumn: "candidate_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_interview_feedbacks_interviewers_interviewer_id",
                table: "interview_feedbacks",
                column: "interviewer_id",
                principalTable: "interviewers",
                principalColumn: "interviewer_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_interview_feedbacks_interviews_interview_id",
                table: "interview_feedbacks",
                column: "interview_id",
                principalTable: "interviews",
                principalColumn: "interview_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_interview_participants_interviews_interview_id",
                table: "interview_participants",
                column: "interview_id",
                principalTable: "interviews",
                principalColumn: "interview_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_interview_participants_recruiters_recruiter_id",
                table: "interview_participants",
                column: "recruiter_id",
                principalTable: "recruiters",
                principalColumn: "recruiter_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_interviews_candidates_candidate_id",
                table: "interviews",
                column: "candidate_id",
                principalTable: "candidates",
                principalColumn: "candidate_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_interviews_interviewers_interviewer_id",
                table: "interviews",
                column: "interviewer_id",
                principalTable: "interviewers",
                principalColumn: "interviewer_id");

            migrationBuilder.AddForeignKey(
                name: "fk_interviews_jobs_job_id",
                table: "interviews",
                column: "job_id",
                principalTable: "jobs",
                principalColumn: "job_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_job_skills_jobs_job_id",
                table: "job_skills",
                column: "job_id",
                principalTable: "jobs",
                principalColumn: "job_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_screening_skill_evaluations_screenings_screening_id",
                table: "screening_skill_evaluations",
                column: "screening_id",
                principalTable: "screenings",
                principalColumn: "screening_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_screenings_candidate_applications_application_id",
                table: "screenings",
                column: "application_id",
                principalTable: "candidate_applications",
                principalColumn: "application_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_candidate_applications_candidates_candidate_id",
                table: "candidate_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_candidate_applications_jobs_job_id",
                table: "candidate_applications");

            migrationBuilder.DropForeignKey(
                name: "fk_candidate_skills_candidates_candidate_id",
                table: "candidate_skills");

            migrationBuilder.DropForeignKey(
                name: "fk_interview_feedbacks_candidates_candidate_id",
                table: "interview_feedbacks");

            migrationBuilder.DropForeignKey(
                name: "fk_interview_feedbacks_interviewers_interviewer_id",
                table: "interview_feedbacks");

            migrationBuilder.DropForeignKey(
                name: "fk_interview_feedbacks_interviews_interview_id",
                table: "interview_feedbacks");

            migrationBuilder.DropForeignKey(
                name: "fk_interview_participants_interviews_interview_id",
                table: "interview_participants");

            migrationBuilder.DropForeignKey(
                name: "fk_interview_participants_recruiters_recruiter_id",
                table: "interview_participants");

            migrationBuilder.DropForeignKey(
                name: "fk_interviews_candidates_candidate_id",
                table: "interviews");

            migrationBuilder.DropForeignKey(
                name: "fk_interviews_interviewers_interviewer_id",
                table: "interviews");

            migrationBuilder.DropForeignKey(
                name: "fk_interviews_jobs_job_id",
                table: "interviews");

            migrationBuilder.DropForeignKey(
                name: "fk_job_skills_jobs_job_id",
                table: "job_skills");

            migrationBuilder.DropForeignKey(
                name: "fk_screening_skill_evaluations_screenings_screening_id",
                table: "screening_skill_evaluations");

            migrationBuilder.DropForeignKey(
                name: "fk_screenings_candidate_applications_application_id",
                table: "screenings");

            migrationBuilder.DropTable(
                name: "recruiters");

            migrationBuilder.DropPrimaryKey(
                name: "pk_screenings",
                table: "screenings");

            migrationBuilder.DropPrimaryKey(
                name: "pk_notifications",
                table: "notifications");

            migrationBuilder.DropPrimaryKey(
                name: "pk_jobs",
                table: "jobs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_interviews",
                table: "interviews");

            migrationBuilder.DropPrimaryKey(
                name: "pk_interviewers",
                table: "interviewers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_candidates",
                table: "candidates");

            migrationBuilder.DropPrimaryKey(
                name: "pk_screening_skill_evaluations",
                table: "screening_skill_evaluations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_job_skills",
                table: "job_skills");

            migrationBuilder.DropPrimaryKey(
                name: "pk_interview_participants",
                table: "interview_participants");

            migrationBuilder.DropPrimaryKey(
                name: "pk_interview_feedbacks",
                table: "interview_feedbacks");

            migrationBuilder.DropPrimaryKey(
                name: "pk_candidate_skills",
                table: "candidate_skills");

            migrationBuilder.DropPrimaryKey(
                name: "pk_candidate_applications",
                table: "candidate_applications");

            migrationBuilder.RenameTable(
                name: "screenings",
                newName: "Screenings");

            migrationBuilder.RenameTable(
                name: "notifications",
                newName: "Notifications");

            migrationBuilder.RenameTable(
                name: "jobs",
                newName: "Jobs");

            migrationBuilder.RenameTable(
                name: "interviews",
                newName: "Interviews");

            migrationBuilder.RenameTable(
                name: "interviewers",
                newName: "Interviewers");

            migrationBuilder.RenameTable(
                name: "candidates",
                newName: "Candidates");

            migrationBuilder.RenameTable(
                name: "screening_skill_evaluations",
                newName: "ScreeningSkillEvaluations");

            migrationBuilder.RenameTable(
                name: "job_skills",
                newName: "JobSkills");

            migrationBuilder.RenameTable(
                name: "interview_participants",
                newName: "InterviewParticipants");

            migrationBuilder.RenameTable(
                name: "interview_feedbacks",
                newName: "InterviewFeedbacks");

            migrationBuilder.RenameTable(
                name: "candidate_skills",
                newName: "CandidateSkills");

            migrationBuilder.RenameTable(
                name: "candidate_applications",
                newName: "CandidateApplications");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Screenings",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "Screenings",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "screening_date",
                table: "Screenings",
                newName: "ScreeningDate");

            migrationBuilder.RenameColumn(
                name: "screened_by_user_id",
                table: "Screenings",
                newName: "ScreenedByUserId");

            migrationBuilder.RenameColumn(
                name: "application_id",
                table: "Screenings",
                newName: "ApplicationId");

            migrationBuilder.RenameColumn(
                name: "screening_id",
                table: "Screenings",
                newName: "ScreeningId");

            migrationBuilder.RenameIndex(
                name: "ix_screenings_application_id",
                table: "Screenings",
                newName: "IX_Screenings_ApplicationId");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "Notifications",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "message",
                table: "Notifications",
                newName: "Message");

            migrationBuilder.RenameColumn(
                name: "target_user_email",
                table: "Notifications",
                newName: "TargetUserEmail");

            migrationBuilder.RenameColumn(
                name: "target_url",
                table: "Notifications",
                newName: "TargetUrl");

            migrationBuilder.RenameColumn(
                name: "target_role",
                table: "Notifications",
                newName: "TargetRole");

            migrationBuilder.RenameColumn(
                name: "is_read",
                table: "Notifications",
                newName: "IsRead");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Notifications",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "notification_id",
                table: "Notifications",
                newName: "NotificationId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Jobs",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "skills",
                table: "Jobs",
                newName: "Skills");

            migrationBuilder.RenameColumn(
                name: "location",
                table: "Jobs",
                newName: "Location");

            migrationBuilder.RenameColumn(
                name: "experience",
                table: "Jobs",
                newName: "Experience");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "Jobs",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "job_title",
                table: "Jobs",
                newName: "JobTitle");

            migrationBuilder.RenameColumn(
                name: "job_id",
                table: "Jobs",
                newName: "JobId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Interviews",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "Interviews",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "mode",
                table: "Interviews",
                newName: "Mode");

            migrationBuilder.RenameColumn(
                name: "duration",
                table: "Interviews",
                newName: "Duration");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Interviews",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "meeting_link",
                table: "Interviews",
                newName: "MeetingLink");

            migrationBuilder.RenameColumn(
                name: "job_id",
                table: "Interviews",
                newName: "JobId");

            migrationBuilder.RenameColumn(
                name: "interviewer_id",
                table: "Interviews",
                newName: "InterviewerId");

            migrationBuilder.RenameColumn(
                name: "interview_date",
                table: "Interviews",
                newName: "InterviewDate");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Interviews",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "candidate_id",
                table: "Interviews",
                newName: "CandidateId");

            migrationBuilder.RenameColumn(
                name: "interview_id",
                table: "Interviews",
                newName: "InterviewId");

            migrationBuilder.RenameIndex(
                name: "ix_interviews_job_id",
                table: "Interviews",
                newName: "IX_Interviews_JobId");

            migrationBuilder.RenameIndex(
                name: "ix_interviews_interviewer_id",
                table: "Interviews",
                newName: "IX_Interviews_InterviewerId");

            migrationBuilder.RenameIndex(
                name: "ix_interviews_candidate_id",
                table: "Interviews",
                newName: "IX_Interviews_CandidateId");

            migrationBuilder.RenameColumn(
                name: "specialization",
                table: "Interviewers",
                newName: "Specialization");

            migrationBuilder.RenameColumn(
                name: "phone",
                table: "Interviewers",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "password",
                table: "Interviewers",
                newName: "Password");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Interviewers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Interviewers",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "department",
                table: "Interviewers",
                newName: "Department");

            migrationBuilder.RenameColumn(
                name: "bio",
                table: "Interviewers",
                newName: "Bio");

            migrationBuilder.RenameColumn(
                name: "years_experience",
                table: "Interviewers",
                newName: "YearsExperience");

            migrationBuilder.RenameColumn(
                name: "linked_in",
                table: "Interviewers",
                newName: "LinkedIn");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Interviewers",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "avatar_initials",
                table: "Interviewers",
                newName: "AvatarInitials");

            migrationBuilder.RenameColumn(
                name: "interviewer_id",
                table: "Interviewers",
                newName: "InterviewerId");

            migrationBuilder.RenameColumn(
                name: "skills",
                table: "Candidates",
                newName: "Skills");

            migrationBuilder.RenameColumn(
                name: "resume",
                table: "Candidates",
                newName: "Resume");

            migrationBuilder.RenameColumn(
                name: "phone",
                table: "Candidates",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Candidates",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "experience",
                table: "Candidates",
                newName: "Experience");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Candidates",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "last_name",
                table: "Candidates",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "first_name",
                table: "Candidates",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "candidate_id",
                table: "Candidates",
                newName: "CandidateId");

            migrationBuilder.RenameColumn(
                name: "skill_name",
                table: "ScreeningSkillEvaluations",
                newName: "SkillName");

            migrationBuilder.RenameColumn(
                name: "screening_id",
                table: "ScreeningSkillEvaluations",
                newName: "ScreeningId");

            migrationBuilder.RenameColumn(
                name: "has_skill",
                table: "ScreeningSkillEvaluations",
                newName: "HasSkill");

            migrationBuilder.RenameColumn(
                name: "experience_years",
                table: "ScreeningSkillEvaluations",
                newName: "ExperienceYears");

            migrationBuilder.RenameColumn(
                name: "evaluation_id",
                table: "ScreeningSkillEvaluations",
                newName: "EvaluationId");

            migrationBuilder.RenameIndex(
                name: "ix_screening_skill_evaluations_screening_id",
                table: "ScreeningSkillEvaluations",
                newName: "IX_ScreeningSkillEvaluations_ScreeningId");

            migrationBuilder.RenameColumn(
                name: "skill_name",
                table: "JobSkills",
                newName: "SkillName");

            migrationBuilder.RenameColumn(
                name: "required_experience",
                table: "JobSkills",
                newName: "RequiredExperience");

            migrationBuilder.RenameColumn(
                name: "job_id",
                table: "JobSkills",
                newName: "JobId");

            migrationBuilder.RenameColumn(
                name: "job_skill_id",
                table: "JobSkills",
                newName: "JobSkillId");

            migrationBuilder.RenameIndex(
                name: "ix_job_skills_job_id",
                table: "JobSkills",
                newName: "IX_JobSkills_JobId");

            migrationBuilder.RenameColumn(
                name: "role",
                table: "InterviewParticipants",
                newName: "Role");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "InterviewParticipants",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "interview_id",
                table: "InterviewParticipants",
                newName: "InterviewId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "InterviewParticipants",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "interview_participant_id",
                table: "InterviewParticipants",
                newName: "InterviewParticipantId");

            migrationBuilder.RenameColumn(
                name: "recruiter_id",
                table: "InterviewParticipants",
                newName: "UserAccountId");

            migrationBuilder.RenameIndex(
                name: "ix_interview_participants_recruiter_id",
                table: "InterviewParticipants",
                newName: "IX_InterviewParticipants_UserAccountId");

            migrationBuilder.RenameIndex(
                name: "ix_interview_participants_interview_id_recruiter_id",
                table: "InterviewParticipants",
                newName: "IX_InterviewParticipants_InterviewId_UserAccountId");

            migrationBuilder.RenameColumn(
                name: "weaknesses",
                table: "InterviewFeedbacks",
                newName: "Weaknesses");

            migrationBuilder.RenameColumn(
                name: "strengths",
                table: "InterviewFeedbacks",
                newName: "Strengths");

            migrationBuilder.RenameColumn(
                name: "recommendation",
                table: "InterviewFeedbacks",
                newName: "Recommendation");

            migrationBuilder.RenameColumn(
                name: "comments",
                table: "InterviewFeedbacks",
                newName: "Comments");

            migrationBuilder.RenameColumn(
                name: "technical_score",
                table: "InterviewFeedbacks",
                newName: "TechnicalScore");

            migrationBuilder.RenameColumn(
                name: "problem_solving_score",
                table: "InterviewFeedbacks",
                newName: "ProblemSolvingScore");

            migrationBuilder.RenameColumn(
                name: "overall_rating",
                table: "InterviewFeedbacks",
                newName: "OverallRating");

            migrationBuilder.RenameColumn(
                name: "interviewer_id",
                table: "InterviewFeedbacks",
                newName: "InterviewerId");

            migrationBuilder.RenameColumn(
                name: "interview_id",
                table: "InterviewFeedbacks",
                newName: "InterviewId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "InterviewFeedbacks",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "communication_score",
                table: "InterviewFeedbacks",
                newName: "CommunicationScore");

            migrationBuilder.RenameColumn(
                name: "candidate_id",
                table: "InterviewFeedbacks",
                newName: "CandidateId");

            migrationBuilder.RenameColumn(
                name: "feedback_id",
                table: "InterviewFeedbacks",
                newName: "FeedbackId");

            migrationBuilder.RenameIndex(
                name: "ix_interview_feedbacks_interviewer_id",
                table: "InterviewFeedbacks",
                newName: "IX_InterviewFeedbacks_InterviewerId");

            migrationBuilder.RenameIndex(
                name: "ix_interview_feedbacks_interview_id",
                table: "InterviewFeedbacks",
                newName: "IX_InterviewFeedbacks_InterviewId");

            migrationBuilder.RenameIndex(
                name: "ix_interview_feedbacks_candidate_id",
                table: "InterviewFeedbacks",
                newName: "IX_InterviewFeedbacks_CandidateId");

            migrationBuilder.RenameColumn(
                name: "skill_name",
                table: "CandidateSkills",
                newName: "SkillName");

            migrationBuilder.RenameColumn(
                name: "experience_years",
                table: "CandidateSkills",
                newName: "ExperienceYears");

            migrationBuilder.RenameColumn(
                name: "candidate_id",
                table: "CandidateSkills",
                newName: "CandidateId");

            migrationBuilder.RenameColumn(
                name: "candidate_skill_id",
                table: "CandidateSkills",
                newName: "CandidateSkillId");

            migrationBuilder.RenameIndex(
                name: "ix_candidate_skills_candidate_id",
                table: "CandidateSkills",
                newName: "IX_CandidateSkills_CandidateId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "CandidateApplications",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "job_id",
                table: "CandidateApplications",
                newName: "JobId");

            migrationBuilder.RenameColumn(
                name: "candidate_id",
                table: "CandidateApplications",
                newName: "CandidateId");

            migrationBuilder.RenameColumn(
                name: "applied_date",
                table: "CandidateApplications",
                newName: "AppliedDate");

            migrationBuilder.RenameColumn(
                name: "application_id",
                table: "CandidateApplications",
                newName: "ApplicationId");

            migrationBuilder.RenameIndex(
                name: "ix_candidate_applications_job_id",
                table: "CandidateApplications",
                newName: "IX_CandidateApplications_JobId");

            migrationBuilder.RenameIndex(
                name: "ix_candidate_applications_candidate_id_job_id",
                table: "CandidateApplications",
                newName: "IX_CandidateApplications_CandidateId_JobId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Screenings",
                table: "Screenings",
                column: "ScreeningId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Notifications",
                table: "Notifications",
                column: "NotificationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Jobs",
                table: "Jobs",
                column: "JobId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Interviews",
                table: "Interviews",
                column: "InterviewId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Interviewers",
                table: "Interviewers",
                column: "InterviewerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Candidates",
                table: "Candidates",
                column: "CandidateId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScreeningSkillEvaluations",
                table: "ScreeningSkillEvaluations",
                column: "EvaluationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JobSkills",
                table: "JobSkills",
                column: "JobSkillId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InterviewParticipants",
                table: "InterviewParticipants",
                column: "InterviewParticipantId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InterviewFeedbacks",
                table: "InterviewFeedbacks",
                column: "FeedbackId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CandidateSkills",
                table: "CandidateSkills",
                column: "CandidateSkillId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CandidateApplications",
                table: "CandidateApplications",
                column: "ApplicationId");

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    UserAccountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.UserAccountId);
                });

            migrationBuilder.InsertData(
                table: "UserAccounts",
                columns: new[] { "UserAccountId", "CreatedAt", "Department", "Email", "IsApproved", "Name", "Password", "Phone", "Role", "Status" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Administration", "admin@talenttrack.com", true, "System Admin", "admin123", "+91 99999 88888", "Admin", "Approved" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Human Resources", "recruiter@talenttrack.com", true, "Mansi Verma", "recruiter123", "+91 98765 11111", "Recruiter", "Approved" },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Engineering", "interviewer@talenttrack.com", true, "Priya Sharma", "interviewer123", "+91 98765 43210", "Interviewer", "Approved" }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateApplications_Candidates_CandidateId",
                table: "CandidateApplications",
                column: "CandidateId",
                principalTable: "Candidates",
                principalColumn: "CandidateId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateApplications_Jobs_JobId",
                table: "CandidateApplications",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "JobId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateSkills_Candidates_CandidateId",
                table: "CandidateSkills",
                column: "CandidateId",
                principalTable: "Candidates",
                principalColumn: "CandidateId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewFeedbacks_Candidates_CandidateId",
                table: "InterviewFeedbacks",
                column: "CandidateId",
                principalTable: "Candidates",
                principalColumn: "CandidateId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewFeedbacks_Interviewers_InterviewerId",
                table: "InterviewFeedbacks",
                column: "InterviewerId",
                principalTable: "Interviewers",
                principalColumn: "InterviewerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewFeedbacks_Interviews_InterviewId",
                table: "InterviewFeedbacks",
                column: "InterviewId",
                principalTable: "Interviews",
                principalColumn: "InterviewId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewParticipants_Interviews_InterviewId",
                table: "InterviewParticipants",
                column: "InterviewId",
                principalTable: "Interviews",
                principalColumn: "InterviewId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewParticipants_UserAccounts_UserAccountId",
                table: "InterviewParticipants",
                column: "UserAccountId",
                principalTable: "UserAccounts",
                principalColumn: "UserAccountId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Interviews_Candidates_CandidateId",
                table: "Interviews",
                column: "CandidateId",
                principalTable: "Candidates",
                principalColumn: "CandidateId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Interviews_Interviewers_InterviewerId",
                table: "Interviews",
                column: "InterviewerId",
                principalTable: "Interviewers",
                principalColumn: "InterviewerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Interviews_Jobs_JobId",
                table: "Interviews",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "JobId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JobSkills_Jobs_JobId",
                table: "JobSkills",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "JobId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Screenings_CandidateApplications_ApplicationId",
                table: "Screenings",
                column: "ApplicationId",
                principalTable: "CandidateApplications",
                principalColumn: "ApplicationId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ScreeningSkillEvaluations_Screenings_ScreeningId",
                table: "ScreeningSkillEvaluations",
                column: "ScreeningId",
                principalTable: "Screenings",
                principalColumn: "ScreeningId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
