using Microsoft.EntityFrameworkCore;

using ParentPortal.Domain.Announcements;
using ParentPortal.Domain.Classrooms;
using ParentPortal.Domain.Schools;
using ParentPortal.Infrastructure.Identity;

namespace ParentPortal.Infrastructure.Persistence;

/// <summary>Seeds two schools so tenant isolation is visible in the demo.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        if (await db.Schools.AnyAsync(cancellationToken))
        {
            return;
        }

        db.Schools.AddRange(
            School.Create(DemoUsers.GreenfieldSchoolId, DemoUsers.GreenfieldName, "greenfield"),
            School.Create(DemoUsers.OakridgeSchoolId, DemoUsers.OakridgeName, "oakridge"));

        var grade4A = Classroom.Create(DemoUsers.GreenfieldSchoolId, "Grade 4 - A", 4).Value;
        var grade7B = Classroom.Create(DemoUsers.GreenfieldSchoolId, "Grade 7 - B", 7).Value;
        var oakGrade2 = Classroom.Create(DemoUsers.OakridgeSchoolId, "Grade 2 - Blue", 2).Value;
        db.Classrooms.AddRange(grade4A, grade7B, oakGrade2);

        Announcement Published(Guid schoolId, Guid? classroomId, string title, string body,
            AnnouncementPriority priority, string author, TimeSpan age)
        {
            var a = Announcement.Create(schoolId, classroomId, title, body, priority, author, nowUtc - age).Value;
            a.Publish(nowUtc - age);
            return a;
        }

        var sportsDay = Published(DemoUsers.GreenfieldSchoolId, null, "Annual Sports Day - 20 November",
            "Students should wear house colours and bring a water bottle. Parents are welcome from 8:30 AM.",
            AnnouncementPriority.High, "admin.greenfield", TimeSpan.FromDays(2));
        sportsDay.Acknowledge("parent.greenfield.1", nowUtc - TimeSpan.FromDays(1));

        db.Announcements.AddRange(
            sportsDay,
            Published(DemoUsers.GreenfieldSchoolId, grade4A.Id, "Science project submission",
                "Please help your child finish the plant-growth project by Thursday.",
                AnnouncementPriority.Normal, "teacher.greenfield", TimeSpan.FromHours(20)),
            Published(DemoUsers.GreenfieldSchoolId, grade7B.Id, "Early dismissal tomorrow",
                "School will close at 12:00 PM tomorrow due to staff training. Buses will run early.",
                AnnouncementPriority.Urgent, "admin.greenfield", TimeSpan.FromHours(3)),
            Announcement.Create(DemoUsers.GreenfieldSchoolId, grade4A.Id, "Field trip consent (draft)",
                "Draft: museum visit next month - consent form to follow.",
                AnnouncementPriority.Normal, "teacher.greenfield", nowUtc - TimeSpan.FromHours(1)).Value,
            Published(DemoUsers.OakridgeSchoolId, oakGrade2.Id, "Reading week",
                "Each child should bring one favourite book to share with the class.",
                AnnouncementPriority.Normal, "teacher.oakridge", TimeSpan.FromDays(1)));

        await db.SaveChangesAsync(cancellationToken);
    }
}
