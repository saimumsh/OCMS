using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;

namespace OptimumCoaching.repo
{
    public class ApplicationDbContext : IdentityDbContext<
        ApplicationUser, ApplicationRole, Guid,
        IdentityUserClaim<Guid>, ApplicationUserRole, IdentityUserLogin<Guid>,
        IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<StudentAcademicRecord> StudentAcademicRecords => Set<StudentAcademicRecord>();
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<Guardian> Guardians => Set<Guardian>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Class> Classes => Set<Class>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Batch> Batches => Set<Batch>();
        public DbSet<BatchUpdate> BatchUpdates => Set<BatchUpdate>();
        public DbSet<Group> Groups => Set<Group>();
        public DbSet<Notice> Notices => Set<Notice>();
        public DbSet<Conversation> Conversations => Set<Conversation>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<ConversationReadState> ConversationReadStates => Set<ConversationReadState>();
        public DbSet<Exam> Exams => Set<Exam>();
        public DbSet<ExamResult> ExamResults => Set<ExamResult>();
        public DbSet<TeacherReview> TeacherReviews => Set<TeacherReview>();
        public DbSet<TeacherReport> TeacherReports => Set<TeacherReport>();
        public DbSet<Topic> Topics => Set<Topic>();
        public DbSet<BatchTopicAssignment> BatchTopicAssignments => Set<BatchTopicAssignment>();
        public DbSet<BatchTeacher> BatchTeachers => Set<BatchTeacher>();
        public DbSet<StudentFeeAccount> StudentFeeAccounts => Set<StudentFeeAccount>();
        public DbSet<FeePayment> FeePayments => Set<FeePayment>();
        public DbSet<TeacherSalaryPayment> TeacherSalaryPayments => Set<TeacherSalaryPayment>();
        public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
        public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
        public DbSet<PaymentSettings> PaymentSettingsRows => Set<PaymentSettings>();
        public DbSet<ResultDiscountTier> ResultDiscountTiers => Set<ResultDiscountTier>();
        public DbSet<FeePaymentRequest> FeePaymentRequests => Set<FeePaymentRequest>();
        public DbSet<NoticeSettings> NoticeSettingsRows => Set<NoticeSettings>();
        public DbSet<NoticeTemplate> NoticeTemplates => Set<NoticeTemplate>();
        public DbSet<CourseLesson> CourseLessons => Set<CourseLesson>();
        public DbSet<StudentLessonProgress> StudentLessonProgresses => Set<StudentLessonProgress>();
        public DbSet<LessonComment> LessonComments => Set<LessonComment>();
        public DbSet<Assignment> Assignments => Set<Assignment>();
        public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
        public DbSet<ClassMaterial> ClassMaterials => Set<ClassMaterial>();
        public DbSet<ClassRoutineSlot> ClassRoutineSlots => Set<ClassRoutineSlot>();
        public DbSet<ClassSessionOverride> ClassSessionOverrides => Set<ClassSessionOverride>();
        public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUserRole>(userRole =>
            {
                userRole.HasKey(ur => new { ur.UserId, ur.RoleId });

                userRole.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();

                userRole.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();
            });

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FullName).HasMaxLength(200);
            });

            builder.Entity<Permission>(entity =>
            {
                entity.HasIndex(p => p.Name).IsUnique();
            });

            builder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

                entity.HasOne(rp => rp.Role)
                    .WithMany(r => r.RolePermissions)
                    .HasForeignKey(rp => rp.RoleId)
                    .IsRequired();

                entity.HasOne(rp => rp.Permission)
                    .WithMany(p => p.RolePermissions)
                    .HasForeignKey(rp => rp.PermissionId)
                    .IsRequired();
            });

            builder.Entity<Student>(entity =>
            {
                entity.HasIndex(s => s.UserId).IsUnique()
                    .HasFilter("[UserId] IS NOT NULL");

                entity.HasOne(s => s.User)
                    .WithOne()
                    .HasForeignKey<Student>(s => s.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(s => s.Guardian)
                    .WithMany()
                    .HasForeignKey(s => s.GuardianId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(s => s.Department)
                    .WithMany()
                    .HasForeignKey(s => s.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(s => s.Batch)
                    .WithMany()
                    .HasForeignKey(s => s.BatchId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(s => s.ApprovalStatus).HasConversion<int>();
            });

            builder.Entity<StudentAcademicRecord>(entity =>
            {
                entity.HasOne(r => r.Student)
                    .WithMany(s => s.AcademicRecords)
                    .HasForeignKey(r => r.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(r => new { r.StudentId, r.SortOrder });
            });

            builder.Entity<Guardian>(entity =>
            {
                entity.HasIndex(g => g.UserId).IsUnique()
                    .HasFilter("[UserId] IS NOT NULL");

                entity.HasOne(g => g.User)
                    .WithOne()
                    .HasForeignKey<Guardian>(g => g.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Teacher>(entity =>
            {
                entity.HasIndex(t => t.UserId).IsUnique()
                    .HasFilter("[UserId] IS NOT NULL");

                entity.HasOne(t => t.User)
                    .WithOne()
                    .HasForeignKey<Teacher>(t => t.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Department>(e =>
            {
                e.HasIndex(d => d.Code).IsUnique().HasFilter("[Code] IS NOT NULL");
                e.HasIndex(d => new { d.Stream, d.Name }).IsUnique();
                e.Property(d => d.Stream).HasConversion<int>();
            });

            builder.Entity<Class>(e =>
            {
                e.HasIndex(c => c.Code).IsUnique().HasFilter("[Code] IS NOT NULL");

                e.HasOne(c => c.Department)
                    .WithMany()
                    .HasForeignKey(c => c.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Subject>(e =>
            {
                e.HasIndex(s => s.Code).IsUnique().HasFilter("[Code] IS NOT NULL");

                e.HasOne(s => s.Department)
                    .WithMany()
                    .HasForeignKey(s => s.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(s => s.Class)
                    .WithMany()
                    .HasForeignKey(s => s.ClassId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Batch>(e =>
            {
                e.HasIndex(b => b.Code).IsUnique().HasFilter("[Code] IS NOT NULL");

                e.HasOne(b => b.Department)
                    .WithMany()
                    .HasForeignKey(b => b.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(b => b.Class)
                    .WithMany()
                    .HasForeignKey(b => b.ClassId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(b => b.Subject)
                    .WithMany()
                    .HasForeignKey(b => b.SubjectId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(b => b.Teacher)
                    .WithMany()
                    .HasForeignKey(b => b.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<BatchUpdate>(e =>
            {
                e.HasOne(u => u.Batch)
                    .WithMany()
                    .HasForeignKey(u => u.BatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(u => u.PostedByUser)
                    .WithMany()
                    .HasForeignKey(u => u.PostedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(u => new { u.BatchId, u.PostedAt });
            });

            builder.Entity<Group>(e =>
            {
                e.HasIndex(g => g.Code).IsUnique().HasFilter("[Code] IS NOT NULL");

                e.HasOne(g => g.Batch)
                    .WithMany()
                    .HasForeignKey(g => g.BatchId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Notice>(e =>
            {
                e.Property(n => n.Audience).HasConversion<int>();

                e.HasOne(n => n.Department)
                    .WithMany()
                    .HasForeignKey(n => n.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(n => n.Student)
                    .WithMany()
                    .HasForeignKey(n => n.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(n => n.PostedByUser)
                    .WithMany()
                    .HasForeignKey(n => n.PostedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(n => new { n.Audience, n.PostedAt });
                e.HasIndex(n => n.ExpiresAt);
                e.HasIndex(n => new { n.StudentId, n.SystemTag });
            });

            builder.Entity<Conversation>(e =>
            {
                e.Property(c => c.Status).HasConversion<int>();

                e.HasOne(c => c.StartedByUser)
                    .WithMany()
                    .HasForeignKey(c => c.StartedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(c => new { c.TargetRole, c.LastMessageAt });
                e.HasIndex(c => new { c.StartedByUserId, c.LastMessageAt });
            });

            builder.Entity<Message>(e =>
            {
                e.HasOne(m => m.Conversation)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(m => m.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(m => m.SenderUser)
                    .WithMany()
                    .HasForeignKey(m => m.SenderUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(m => new { m.ConversationId, m.SentAt });
            });

            builder.Entity<ConversationReadState>(e =>
            {
                e.HasKey(rs => new { rs.ConversationId, rs.UserId });

                e.HasOne(rs => rs.Conversation)
                    .WithMany()
                    .HasForeignKey(rs => rs.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(rs => rs.User)
                    .WithMany()
                    .HasForeignKey(rs => rs.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Student>(e =>
            {
                e.HasIndex(s => s.StudentCode).IsUnique().HasFilter("[StudentCode] IS NOT NULL");
            });

            builder.Entity<Exam>(e =>
            {
                e.Property(x => x.Type).HasConversion<int>();
                e.Property(x => x.Status).HasConversion<int>();
                e.Property(x => x.TotalMarks).HasColumnType("decimal(8,2)");
                e.Property(x => x.PassMarks).HasColumnType("decimal(8,2)");

                e.HasOne(x => x.Batch)
                    .WithMany()
                    .HasForeignKey(x => x.BatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.BatchId, x.ExamDate });
            });

            builder.Entity<ExamResult>(e =>
            {
                e.Property(x => x.Status).HasConversion<int>();
                e.Property(x => x.MarksObtained).HasColumnType("decimal(8,2)");

                e.HasOne(x => x.Exam)
                    .WithMany(ex => ex.Results)
                    .HasForeignKey(x => x.ExamId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Student)
                    .WithMany()
                    .HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.ExamId, x.StudentId }).IsUnique();
            });

            builder.Entity<TeacherReview>(e =>
            {
                e.HasOne(x => x.Student)
                    .WithMany()
                    .HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Teacher)
                    .WithMany()
                    .HasForeignKey(x => x.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Batch)
                    .WithMany()
                    .HasForeignKey(x => x.BatchId)
                    .OnDelete(DeleteBehavior.SetNull);

                // One latest review per student-teacher pair (we upsert).
                e.HasIndex(x => new { x.StudentId, x.TeacherId }).IsUnique()
                    .HasFilter("[IsDeleted] = 0");
            });

            builder.Entity<Topic>(e =>
            {
                e.HasOne(x => x.Subject)
                    .WithMany()
                    .HasForeignKey(x => x.SubjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.SubjectId, x.Title });
            });

            builder.Entity<BatchTopicAssignment>(e =>
            {
                e.HasOne(x => x.Batch)
                    .WithMany()
                    .HasForeignKey(x => x.BatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Topic)
                    .WithMany()
                    .HasForeignKey(x => x.TopicId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Teacher)
                    .WithMany()
                    .HasForeignKey(x => x.TeacherId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(x => new { x.BatchId, x.TopicId }).IsUnique()
                    .HasFilter("[IsDeleted] = 0");
            });

            builder.Entity<BatchTeacher>(e =>
            {
                e.HasOne(x => x.Batch)
                    .WithMany()
                    .HasForeignKey(x => x.BatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Restrict so a Teacher row can't be deleted while still
                // assigned; admin must remove the assignment first.
                e.HasOne(x => x.Teacher)
                    .WithMany()
                    .HasForeignKey(x => x.TeacherId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(x => new { x.BatchId, x.TeacherId }).IsUnique()
                    .HasFilter("[IsDeleted] = 0");
            });

            builder.Entity<Batch>(e =>
            {
                e.Property(x => x.CourseFee).HasColumnType("decimal(12,2)");
                e.Property(x => x.MinimumEnrollment).HasColumnType("decimal(12,2)");
                e.Property(x => x.FullPaymentDiscountPercent).HasColumnType("decimal(5,2)");
                e.Property(x => x.LateFeeFlat).HasColumnType("decimal(12,2)");
                e.Property(x => x.LateFeePerDay).HasColumnType("decimal(12,2)");
                e.Property(x => x.DeliveryMode).HasConversion<int>();
                e.Property(x => x.OfferedPrice).HasColumnType("decimal(12,2)");
            });

            builder.Entity<CourseLesson>(e =>
            {
                e.HasOne(x => x.Batch)
                    .WithMany()
                    .HasForeignKey(x => x.BatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Topic)
                    .WithMany()
                    .HasForeignKey(x => x.TopicId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(x => new { x.BatchId, x.SortOrder });
            });

            builder.Entity<CourseEnrollment>(e =>
            {
                e.Property(x => x.Status).HasConversion<int>();
                e.Property(x => x.PriceAtEnrollment).HasColumnType("decimal(12,2)");

                e.HasOne(x => x.Batch)
                    .WithMany()
                    .HasForeignKey(x => x.BatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                // NoAction on Student to avoid multiple cascade paths
                // (Batch -> Student is already linked via Student.BatchId).
                e.HasOne(x => x.Student)
                    .WithMany()
                    .HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasIndex(x => new { x.StudentId, x.BatchId }).IsUnique();
                e.HasIndex(x => x.BatchId);
            });

            builder.Entity<StudentLessonProgress>(e =>
            {
                e.HasOne(x => x.Lesson)
                    .WithMany()
                    .HasForeignKey(x => x.LessonId)
                    .OnDelete(DeleteBehavior.Cascade);

                // NoAction on Student to avoid multiple cascade paths
                // (Lesson → Batch → Student).
                e.HasOne(x => x.Student)
                    .WithMany()
                    .HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasIndex(x => new { x.LessonId, x.StudentId }).IsUnique();
            });

            builder.Entity<LessonComment>(e =>
            {
                e.HasOne(x => x.Lesson)
                    .WithMany()
                    .HasForeignKey(x => x.LessonId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.AuthorUser)
                    .WithMany()
                    .HasForeignKey(x => x.AuthorUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Self-reference for one-level threading. NoAction avoids
                // cascade cycles.
                e.HasOne(x => x.ParentComment)
                    .WithMany()
                    .HasForeignKey(x => x.ParentCommentId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasIndex(x => new { x.LessonId, x.PostedAt });
            });

            builder.Entity<Assignment>(e =>
            {
                e.Property(x => x.Status).HasConversion<int>();
                e.Property(x => x.MaxScore).HasColumnType("decimal(8,2)");

                e.HasOne(x => x.Batch)
                    .WithMany()
                    .HasForeignKey(x => x.BatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Topic)
                    .WithMany()
                    .HasForeignKey(x => x.TopicId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(x => new { x.BatchId, x.DueDate });
            });

            builder.Entity<AssignmentSubmission>(e =>
            {
                e.Property(x => x.Status).HasConversion<int>();
                e.Property(x => x.Score).HasColumnType("decimal(8,2)");

                e.HasOne(x => x.Assignment)
                    .WithMany(a => a.Submissions)
                    .HasForeignKey(x => x.AssignmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                // NoAction on Student to avoid the Batch→Assignment→Student
                // multiple cascade path.
                e.HasOne(x => x.Student)
                    .WithMany()
                    .HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.GradedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.GradedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(x => new { x.AssignmentId, x.StudentId }).IsUnique();
            });

            builder.Entity<PaymentSettings>(e =>
            {
                // Single-row table — seeder ensures exactly one row.
            });

            builder.Entity<ResultDiscountTier>(e =>
            {
                e.Property(x => x.MinResultPercent).HasColumnType("decimal(5,2)");
                e.Property(x => x.DiscountPercent).HasColumnType("decimal(5,2)");
                e.HasIndex(x => x.MinResultPercent);
            });

            builder.Entity<NoticeSettings>(e =>
            {
                e.Property(x => x.DefaultAudience).HasConversion<int>();
            });

            builder.Entity<NoticeTemplate>(e =>
            {
                e.Property(x => x.DefaultAudience).HasConversion<int>();
                e.HasIndex(x => x.Name);
            });

            builder.Entity<FeePaymentRequest>(e =>
            {
                e.Property(x => x.Method).HasConversion<int>();
                e.Property(x => x.Status).HasConversion<int>();
                e.Property(x => x.Amount).HasColumnType("decimal(12,2)");

                e.HasOne(x => x.Account)
                    .WithMany()
                    .HasForeignKey(x => x.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.SubmittedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.SubmittedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.ReviewedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.ReviewedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(x => new { x.Status, x.SubmittedAt });
                e.HasIndex(x => new { x.AccountId, x.SubmittedAt });
            });

            builder.Entity<StudentFeeAccount>(e =>
            {
                e.Property(x => x.Status).HasConversion<int>();
                e.Property(x => x.FinalFee).HasColumnType("decimal(12,2)");
                e.Property(x => x.AmountPaid).HasColumnType("decimal(12,2)");
                e.Property(x => x.DiscountAmount).HasColumnType("decimal(12,2)");
                e.Ignore(x => x.Balance); // computed in code, not stored

                // NoAction on the second leg avoids "multiple cascade paths"
                // because Batch and Student would both cascade-delete here.
                e.HasOne(x => x.Student)
                    .WithMany()
                    .HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Batch)
                    .WithMany()
                    .HasForeignKey(x => x.BatchId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasIndex(x => new { x.StudentId, x.BatchId }).IsUnique()
                    .HasFilter("[IsDeleted] = 0");
            });

            builder.Entity<FeePayment>(e =>
            {
                e.Property(x => x.Method).HasConversion<int>();
                e.Property(x => x.Amount).HasColumnType("decimal(12,2)");

                e.HasOne(x => x.Account)
                    .WithMany(a => a.Payments)
                    .HasForeignKey(x => x.AccountId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.RecordedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.RecordedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(x => new { x.AccountId, x.PaidOn });
            });

            builder.Entity<AttendanceSession>(e =>
            {
                e.HasOne(x => x.Batch)
                    .WithMany()
                    .HasForeignKey(x => x.BatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Topic)
                    .WithMany()
                    .HasForeignKey(x => x.TopicId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(x => x.TakenByUser)
                    .WithMany()
                    .HasForeignKey(x => x.TakenByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(x => new { x.BatchId, x.SessionDate }).IsUnique()
                    .HasFilter("[IsDeleted] = 0");
            });

            builder.Entity<AttendanceRecord>(e =>
            {
                e.Property(x => x.Status).HasConversion<int>();

                e.HasOne(x => x.Session)
                    .WithMany(s => s.Records)
                    .HasForeignKey(x => x.SessionId)
                    .OnDelete(DeleteBehavior.Cascade);

                // NoAction on Student side because both Session→Batch→Student
                // and Session→Records→Student would otherwise create multiple
                // cascade paths.
                e.HasOne(x => x.Student)
                    .WithMany()
                    .HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasIndex(x => new { x.SessionId, x.StudentId }).IsUnique();
            });

            builder.Entity<TeacherSalaryPayment>(e =>
            {
                e.Property(x => x.Method).HasConversion<int>();
                e.Property(x => x.Status).HasConversion<int>();
                e.Property(x => x.Amount).HasColumnType("decimal(12,2)");

                e.HasOne(x => x.Teacher)
                    .WithMany()
                    .HasForeignKey(x => x.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.RecordedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.RecordedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(x => new { x.TeacherId, x.PeriodMonth });
            });

            builder.Entity<ClassMaterial>(e =>
            {
                e.Property(x => x.Type).HasConversion<int>();

                e.HasOne(x => x.Batch)
                    .WithMany()
                    .HasForeignKey(x => x.BatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Topic)
                    .WithMany()
                    .HasForeignKey(x => x.TopicId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(x => x.UploadedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.UploadedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(x => new { x.BatchId, x.UploadedAt });
            });

            builder.Entity<ClassRoutineSlot>(e =>
            {
                e.Property(x => x.Day).HasConversion<int>();

                e.HasOne(x => x.Batch)
                    .WithMany()
                    .HasForeignKey(x => x.BatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Topic)
                    .WithMany()
                    .HasForeignKey(x => x.TopicId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(x => x.Teacher)
                    .WithMany()
                    .HasForeignKey(x => x.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(x => new { x.BatchId, x.Day, x.StartTime });
            });

            builder.Entity<ClassSessionOverride>(e =>
            {
                e.HasOne(x => x.Batch)
                    .WithMany()
                    .HasForeignKey(x => x.BatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                // NoAction (not SetNull) avoids "multiple cascade paths"
                // because both ClassRoutineSlots and ClassSessionOverrides
                // already cascade from Batches.
                e.HasOne(x => x.RoutineSlot)
                    .WithMany()
                    .HasForeignKey(x => x.RoutineSlotId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.Topic)
                    .WithMany()
                    .HasForeignKey(x => x.TopicId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(x => x.Teacher)
                    .WithMany()
                    .HasForeignKey(x => x.TeacherId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(x => new { x.BatchId, x.SessionDate });
            });

            builder.Entity<TeacherReport>(e =>
            {
                e.Property(x => x.Category).HasConversion<int>();
                e.Property(x => x.Status).HasConversion<int>();

                e.HasOne(x => x.Student)
                    .WithMany()
                    .HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Teacher)
                    .WithMany()
                    .HasForeignKey(x => x.TeacherId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Batch)
                    .WithMany()
                    .HasForeignKey(x => x.BatchId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(x => x.HandledByUser)
                    .WithMany()
                    .HasForeignKey(x => x.HandledByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(x => new { x.Status, x.Created });
            });
        }
    }
}
