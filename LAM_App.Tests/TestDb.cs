using System.Reflection;
using LAM_App.Data;
using LAM_App.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LAM_App.Tests;

internal sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public AppDbContext Context { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
        SetAppContext(Context);
    }

    public Studio AddStudio(string name = "LAM")
    {
        var studio = new Studio { Name = name };
        Context.studios.Add(studio);
        Context.SaveChanges();
        return studio;
    }

    public Style AddStyle(string name = "дети 3-4", int? teacherId = null)
    {
        var studio = Context.studios.FirstOrDefault() ?? AddStudio();
        var style = new Style
        {
            Name = name,
            StudioId = studio.Id,
            TeacherId = teacherId
        };

        Context.styles.Add(style);
        Context.SaveChanges();
        return style;
    }

    public Client AddClient(int styleId, string surname, string name)
    {
        var client = new Client
        {
            StyleId = styleId,
            ChildSurname = surname,
            ChildName = name,
            ParentName = $"Родитель {surname}",
            ParentPhone = "+70000000000"
        };

        Context.clients.Add(client);
        Context.SaveChanges();
        return client;
    }

    public Teacher AddTeacher(string fullName = "Тренер")
    {
        var studio = Context.studios.FirstOrDefault() ?? AddStudio();
        var teacher = new Teacher
        {
            FullName = fullName,
            StudioId = studio.Id
        };

        Context.teachers.Add(teacher);
        Context.SaveChanges();
        return teacher;
    }

    public static void SetAppContext(AppDbContext context)
    {
        var property = typeof(App).GetProperty(nameof(App.DbContext), BindingFlags.Public | BindingFlags.Static)
            ?? throw new MissingMemberException(typeof(App).FullName, nameof(App.DbContext));
        var setter = property.GetSetMethod(nonPublic: true)
            ?? throw new MissingMethodException(typeof(App).FullName, $"set_{nameof(App.DbContext)}");

        setter.Invoke(null, new object[] { context });
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
