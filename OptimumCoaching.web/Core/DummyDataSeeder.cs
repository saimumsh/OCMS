using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OptimumCoaching.core;
using OptimumCoaching.repo;

namespace OptimumCoaching.web.Core
{
    // Seeds a realistic-looking dummy dataset for local analysis / screenshots:
    //   ~3 admins, ~3 CCs, ~15 teachers, ~30 guardians, ~100 students,
    //   ~25 batches, ~50 batch updates, ~20 notices, plus subjects/classes.
    //
    // Runs ONLY in Development. Idempotent — short-circuits if it sees its own
    // marker user (`teacher1@dummy.local`). Drop the database (or those rows)
    // to re-seed.
    //
    // Default password for every seeded login account: Demo@1234
    public static class DummyDataSeeder
    {
        public const string DummyDomain = "dummy.local";
        public const string DummyPassword = "Demo@1234";
        private const string MarkerEmail = "teacher1@dummy.local";

        public static async Task SeedAsync(IServiceProvider services, IHostEnvironment env, ILogger? log = null)
        {
            if (!env.IsDevelopment()) return;

            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

            // Idempotency guard — if our marker exists, the seeder has already run.
            if (await userManager.FindByEmailAsync(MarkerEmail) != null)
            {
                log?.LogInformation("DummyDataSeeder: marker user found, skipping.");
                return;
            }

            log?.LogInformation("DummyDataSeeder: starting medium dataset seed…");
            var rng = new Random(42); // fixed seed for reproducibility

            // ----- Departments / classes / subjects -----
            var departments = await db.Departments.Where(d => !d.IsDeleted).ToListAsync();
            if (!departments.Any())
            {
                log?.LogWarning("DummyDataSeeder: no departments found; the IdentitySeeder must run first.");
                return;
            }

            var classes = SeedClasses(db, departments);
            var subjects = SeedSubjects(db, departments, classes);
            await db.SaveChangesAsync();

            // ----- Admin / CC accounts -----
            for (int i = 1; i <= 3; i++)
                await EnsureUser(userManager, $"admin{i}@{DummyDomain}", $"Admin User {i}", Roles.Admin);

            for (int i = 1; i <= 3; i++)
                await EnsureUser(userManager, $"cc{i}@{DummyDomain}", $"Course Coordinator {i}", Roles.CC);

            // ----- Teachers -----
            var teacherUsers = new List<ApplicationUser>();
            for (int i = 1; i <= 15; i++)
            {
                var u = await EnsureUser(userManager, $"teacher{i}@{DummyDomain}", FakeNames.Teacher(i), Roles.Teacher);
                teacherUsers.Add(u);
            }

            var teachers = teacherUsers.Select((u, i) => new Teacher
            {
                Id = Guid.NewGuid(),
                UserId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = FakePhone(rng),
                Specialization = FakeNames.Specialization(rng),
                Qualification = FakeNames.Qualification(rng),
                ExperienceYears = rng.Next(1, 22),
                HireDate = DateTime.UtcNow.AddDays(-rng.Next(60, 1800)),
                Gender = i % 2 == 0 ? Gender.Female : Gender.Male,
                IsActive = true,
                Created = DateTime.UtcNow
            }).ToList();
            db.Teachers.AddRange(teachers);

            // ----- Guardians -----
            var guardianUsers = new List<ApplicationUser>();
            for (int i = 1; i <= 30; i++)
            {
                var u = await EnsureUser(userManager, $"guardian{i}@{DummyDomain}", FakeNames.Guardian(i), Roles.User);
                guardianUsers.Add(u);
            }

            var guardians = guardianUsers.Select(u => new Guardian
            {
                Id = Guid.NewGuid(),
                UserId = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = FakePhone(rng),
                Relationship = rng.Next(2) == 0 ? "Father" : "Mother",
                Occupation = FakeNames.Occupation(rng),
                IsActive = true,
                Created = DateTime.UtcNow
            }).ToList();
            db.Guardians.AddRange(guardians);

            await db.SaveChangesAsync();

            // ----- Batches -----
            var batches = new List<Batch>();
            for (int i = 1; i <= 25; i++)
            {
                var dept = departments[rng.Next(departments.Count)];
                var subj = subjects.Where(s => s.DepartmentId == dept.Id).ToList();
                var cls = classes.Where(c => c.DepartmentId == dept.Id).ToList();
                var teacher = teachers[rng.Next(teachers.Count)];

                var startOffsetDays = rng.Next(-180, 30);
                var lengthDays = rng.Next(60, 180);
                var batch = new Batch
                {
                    Id = Guid.NewGuid(),
                    Name = $"Batch-{dept.Code ?? dept.Name[..3].ToUpper()}-{i:D2}",
                    Code = $"B{i:D3}",
                    Description = $"{dept.Name} batch #{i}",
                    DepartmentId = dept.Id,
                    SubjectId = subj.Any() ? subj[rng.Next(subj.Count)].Id : null,
                    ClassId = cls.Any() ? cls[rng.Next(cls.Count)].Id : null,
                    TeacherId = teacher.Id,
                    StartDate = DateTime.UtcNow.AddDays(startOffsetDays),
                    EndDate = DateTime.UtcNow.AddDays(startOffsetDays + lengthDays),
                    Capacity = rng.Next(15, 45),
                    IsActive = true,
                    Created = DateTime.UtcNow
                };
                batches.Add(batch);
            }
            db.Batches.AddRange(batches);
            await db.SaveChangesAsync();

            // ----- Students -----
            var students = new List<Student>();
            for (int i = 1; i <= 100; i++)
            {
                var u = await EnsureUser(userManager, $"student{i}@{DummyDomain}", FakeNames.Student(i), Roles.User);
                var dept = departments[rng.Next(departments.Count)];
                var deptBatches = batches.Where(b => b.DepartmentId == dept.Id).ToList();
                var batch = deptBatches.Any() ? deptBatches[rng.Next(deptBatches.Count)] : null;
                var guardian = guardians[rng.Next(guardians.Count)];

                // 80% Approved, 15% Pending, 5% Rejected — realistic distribution
                var roll = rng.Next(100);
                var status = roll < 80 ? StudentApprovalStatus.Approved
                            : roll < 95 ? StudentApprovalStatus.Pending
                            : StudentApprovalStatus.Rejected;

                var enrollDaysAgo = rng.Next(1, 365);
                students.Add(new Student
                {
                    Id = Guid.NewGuid(),
                    UserId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = FakePhone(rng),
                    DateOfBirth = DateTime.UtcNow.AddYears(-rng.Next(15, 25)).AddDays(-rng.Next(0, 365)),
                    Gender = i % 2 == 0 ? Gender.Female : Gender.Male,
                    Address = FakeNames.Address(rng),
                    GuardianId = guardian.Id,
                    GuardianName = guardian.FullName,
                    GuardianPhone = guardian.PhoneNumber,
                    DepartmentId = dept.Id,
                    BatchId = status == StudentApprovalStatus.Approved ? batch?.Id : null,
                    EnrollmentDate = DateTime.UtcNow.AddDays(-enrollDaysAgo),
                    ApprovalStatus = status,
                    ApprovedAt = status == StudentApprovalStatus.Approved
                        ? DateTime.UtcNow.AddDays(-enrollDaysAgo + rng.Next(1, 5))
                        : null,
                    RejectionReason = status == StudentApprovalStatus.Rejected
                        ? "Documents did not meet requirements."
                        : null,
                    IsActive = true,
                    Created = DateTime.UtcNow.AddDays(-enrollDaysAgo)
                });
            }
            db.Students.AddRange(students);
            await db.SaveChangesAsync();

            // ----- Batch updates -----
            var updates = new List<BatchUpdate>();
            foreach (var batch in batches)
            {
                int updateCount = rng.Next(0, 5);
                for (int k = 0; k < updateCount; k++)
                {
                    updates.Add(new BatchUpdate
                    {
                        Id = Guid.NewGuid(),
                        BatchId = batch.Id,
                        Title = FakeNames.UpdateTitle(rng),
                        Body = FakeNames.UpdateBody(rng),
                        PostedAt = DateTime.UtcNow.AddDays(-rng.Next(0, 180)).AddHours(-rng.Next(0, 24)),
                        PostedByUserId = batch.TeacherId.HasValue
                            ? teachers.First(t => t.Id == batch.TeacherId).UserId
                            : null,
                        IsActive = true,
                        Created = DateTime.UtcNow
                    });
                }
            }
            db.BatchUpdates.AddRange(updates);

            // ----- Notices -----
            var adminUser = await userManager.FindByEmailAsync($"admin1@{DummyDomain}");
            var noticeAuthor = adminUser?.Id;
            var notices = new List<Notice>();
            for (int i = 1; i <= 20; i++)
            {
                var aud = (i % 3) switch
                {
                    0 => NoticeAudience.Both,
                    1 => NoticeAudience.Students,
                    _ => NoticeAudience.Teachers
                };
                var postedDaysAgo = rng.Next(0, 90);
                notices.Add(new Notice
                {
                    Id = Guid.NewGuid(),
                    Title = FakeNames.NoticeTitle(rng, i),
                    Body = FakeNames.NoticeBody(rng),
                    Audience = aud,
                    DepartmentId = i % 4 == 0 ? departments[rng.Next(departments.Count)].Id : (Guid?)null,
                    IsPinned = i <= 3,
                    ExpiresAt = i % 5 == 0 ? DateTime.UtcNow.AddDays(rng.Next(7, 60)) : (DateTime?)null,
                    PostedAt = DateTime.UtcNow.AddDays(-postedDaysAgo),
                    PostedByUserId = noticeAuthor,
                    IsActive = true,
                    Created = DateTime.UtcNow.AddDays(-postedDaysAgo)
                });
            }
            db.Notices.AddRange(notices);

            await db.SaveChangesAsync();
            log?.LogInformation(
                "DummyDataSeeder: done. {T} teachers, {G} guardians, {S} students, {B} batches, {U} updates, {N} notices.",
                teachers.Count, guardians.Count, students.Count, batches.Count, updates.Count, notices.Count);
        }

        // ---------------- Helpers ----------------

        private static List<Class> SeedClasses(ApplicationDbContext db, IList<Department> departments)
        {
            var existing = db.Classes.Where(c => !c.IsDeleted).ToList();
            if (existing.Count > 0) return existing;

            var classes = new List<Class>();
            foreach (var d in departments)
            {
                // Two "classes" per department — e.g. SSC/HSC for academic, Sem-1/Sem-3 for diploma
                var classNames = d.Stream == EducationStream.Diploma
                    ? new[] { "Semester 1", "Semester 3" }
                    : new[] { "SSC", "HSC" };
                foreach (var name in classNames)
                {
                    // Preserve trailing digits so "Semester 1" and "Semester 3"
                    // don't both collapse to "SEM" and collide on the unique index.
                    var compact = name.Replace(" ", "").ToUpper();
                    classes.Add(new Class
                    {
                        Id = Guid.NewGuid(),
                        Name = $"{d.Name} – {name}",
                        Code = $"{d.Code ?? d.Name[..3].ToUpper()}-{compact}",
                        DepartmentId = d.Id,
                        IsActive = true,
                        Created = DateTime.UtcNow
                    });
                }
            }
            db.Classes.AddRange(classes);
            return classes;
        }

        private static List<Subject> SeedSubjects(ApplicationDbContext db, IList<Department> departments, IList<Class> classes)
        {
            var existing = db.Subjects.Where(s => !s.IsDeleted).ToList();
            if (existing.Count > 0) return existing;

            var subjects = new List<Subject>();
            var perDept = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Science"]      = new[] { "Physics", "Chemistry", "Biology", "Mathematics" },
                ["Arts"]         = new[] { "History", "Civics", "Economics", "Bangla" },
                ["Commerce"]     = new[] { "Accounting", "Finance", "Business Studies" },
                ["Electrical"]   = new[] { "Circuit Analysis", "Power Systems", "Electrical Machines" },
                ["Electronics"]  = new[] { "Digital Electronics", "Microcontrollers", "Communication" },
                ["Computer"]     = new[] { "Programming", "Databases", "Networking", "Algorithms" },
                ["Civil"]        = new[] { "Surveying", "Structural Analysis", "Concrete Tech" },
                ["Mechanical"]   = new[] { "Thermodynamics", "Fluid Mechanics", "Machine Design" }
            };

            foreach (var d in departments)
            {
                if (!perDept.TryGetValue(d.Name, out var subjNames)) continue;
                foreach (var name in subjNames)
                {
                    subjects.Add(new Subject
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        Code = $"{(d.Code ?? d.Name[..3].ToUpper())}-{name[..3].ToUpper()}",
                        DepartmentId = d.Id,
                        IsActive = true,
                        Created = DateTime.UtcNow
                    });
                }
            }
            db.Subjects.AddRange(subjects);
            return subjects;
        }

        private static async Task<ApplicationUser> EnsureUser(
            UserManager<ApplicationUser> userManager, string email, string fullName, string roleName)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = fullName,
                    Status = ApplicationUserStatus.Active,
                    IsActive = true,
                    Created = DateTime.UtcNow
                };
                var r = await userManager.CreateAsync(user, DummyPassword);
                if (!r.Succeeded)
                    throw new InvalidOperationException(
                        $"Failed to create dummy user {email}: " +
                        string.Join(", ", r.Errors.Select(e => e.Description)));
            }
            if (!await userManager.IsInRoleAsync(user, roleName))
                await userManager.AddToRoleAsync(user, roleName);
            return user;
        }

        private static string FakePhone(Random rng) =>
            $"+8801{rng.Next(3, 10)}{rng.Next(10000000, 99999999)}";

        // Inline name pools — kept here so the seeder is self-contained.
        private static class FakeNames
        {
            private static readonly string[] Male = {
                "Rakib", "Sajid", "Tanvir", "Mehedi", "Imran", "Sabbir", "Faisal", "Anik",
                "Shihab", "Riyad", "Mahbub", "Nayeem", "Tareq", "Junayed", "Arif"
            };
            private static readonly string[] Female = {
                "Tasnia", "Maliha", "Nusrat", "Sadia", "Farzana", "Nabila", "Tahmina", "Mim",
                "Ishrat", "Sumaiya", "Anika", "Lamia", "Tania", "Rifa", "Sania"
            };
            private static readonly string[] LastNames = {
                "Hossain", "Rahman", "Khan", "Ahmed", "Islam", "Chowdhury", "Karim",
                "Akter", "Mahmud", "Bhuiyan", "Sarker", "Sheikh", "Talukder"
            };
            private static readonly string[] Specs = {
                "Mathematics", "Physics", "Chemistry", "Biology", "Accounting",
                "Programming", "Electronics", "Civil Engineering", "Mechanical Eng.", "English"
            };
            private static readonly string[] Quals = {
                "B.Sc., M.Sc. (DU)", "M.Sc. Engineering (BUET)", "M.A. (RU)", "B.B.A., M.B.A.",
                "B.Sc. Engineering (KUET)", "M.Sc. (JU)", "B.Ed., M.Ed."
            };
            private static readonly string[] Occupations = {
                "Service holder", "Businessman", "Banker", "Teacher", "Farmer",
                "Doctor", "Engineer", "Government officer"
            };
            private static readonly string[] Areas = {
                "Dhanmondi, Dhaka", "Mirpur, Dhaka", "Uttara, Dhaka",
                "Chawkbazar, Chittagong", "Khulshi, Chittagong",
                "Boyra, Khulna", "Sonadanga, Khulna",
                "Sadar, Rajshahi", "Sadar, Sylhet", "Sadar, Barisal"
            };
            private static readonly string[] UpdateTitles = {
                "Class rescheduled", "Test next week", "Make-up class on Friday",
                "Submit assignment by Monday", "Topic change for this week",
                "Materials uploaded", "Group activity tomorrow"
            };
            private static readonly string[] UpdateBodies = {
                "Please be on time and bring your books.",
                "Topics will cover the last two chapters; revise thoroughly.",
                "Submit handwritten copies before class starts.",
                "Recording will be shared after the session ends.",
                "Bring calculators; phones not allowed during the test."
            };
            private static readonly string[] NoticeTitles = {
                "Mid-term examination schedule",
                "Eid holidays announcement",
                "Tuition fee due reminder",
                "New batch admission open",
                "Library timing update",
                "Annual sports day",
                "Department head meeting",
                "Scholarship application deadline",
                "Faculty development workshop",
                "Parent-teacher meeting"
            };
            private static readonly string[] NoticeBodies = {
                "Please refer to the routine posted on the notice board for full details.",
                "All affected parties are requested to acknowledge by replying to this notice.",
                "For further queries, contact the front desk during office hours.",
                "Failure to comply may result in administrative action; act accordingly.",
                "Detailed guidelines will be circulated to department heads shortly."
            };

            public static string Teacher(int i)
            {
                var first = i % 3 == 0 ? Female[i % Female.Length] : Male[i % Male.Length];
                var last = LastNames[i % LastNames.Length];
                return $"{first} {last}";
            }

            public static string Guardian(int i)
            {
                var first = i % 2 == 0 ? Male[(i + 4) % Male.Length] : Female[(i + 4) % Female.Length];
                return $"{first} {LastNames[(i + 7) % LastNames.Length]}";
            }

            public static string Student(int i)
            {
                var first = i % 2 == 0 ? Male[(i + 1) % Male.Length] : Female[(i + 1) % Female.Length];
                return $"{first} {LastNames[(i + 3) % LastNames.Length]}";
            }

            public static string Specialization(Random r) => Specs[r.Next(Specs.Length)];
            public static string Qualification(Random r) => Quals[r.Next(Quals.Length)];
            public static string Occupation(Random r) => Occupations[r.Next(Occupations.Length)];
            public static string Address(Random r) => Areas[r.Next(Areas.Length)];
            public static string UpdateTitle(Random r) => UpdateTitles[r.Next(UpdateTitles.Length)];
            public static string UpdateBody(Random r) => UpdateBodies[r.Next(UpdateBodies.Length)];
            public static string NoticeTitle(Random r, int i) => NoticeTitles[i % NoticeTitles.Length];
            public static string NoticeBody(Random r) => NoticeBodies[r.Next(NoticeBodies.Length)];
        }
    }
}
