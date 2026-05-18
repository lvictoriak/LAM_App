using LAM_App.Models;

namespace LAM_App.Tests;

public class UnitTests
{
    [Fact]
    public void Attendance_GetCellText_returns_medical_absent_and_lesson_numbers()
    {
        Assert.Equal("нб", InvokeAttendance<string>("GetCellText", new List<AttendanceMark>
        {
            new() { IsAbsent = true, IsMedicalExcused = true }
        }));

        Assert.Equal("н", InvokeAttendance<string>("GetCellText", new List<AttendanceMark>
        {
            new() { IsAbsent = true }
        }));

        Assert.Equal("2,4", InvokeAttendance<string>("GetCellText", new List<AttendanceMark>
        {
            new() { Id = 20, LessonNumber = 4 },
            new() { Id = 10, LessonNumber = 2 },
            new() { Id = 30 }
        }));
    }

    [Fact]
    public void Attendance_GetCurrentMarkType_detects_all_supported_states()
    {
        Assert.Equal("clear", InvokeAttendance<string>("GetCurrentMarkType", new List<AttendanceMark>()));
        Assert.Equal("medical", InvokeAttendance<string>("GetCurrentMarkType", new List<AttendanceMark>
        {
            new() { IsAbsent = true, IsMedicalExcused = true }
        }));
        Assert.Equal("absent", InvokeAttendance<string>("GetCurrentMarkType", new List<AttendanceMark>
        {
            new() { IsAbsent = true }
        }));
        Assert.Equal("present", InvokeAttendance<string>("GetCurrentMarkType", new List<AttendanceMark>
        {
            new() { IsAbsent = false }
        }));
    }

    [Fact]
    public void Attendance_CountPresentChildren_counts_unique_non_absent_clients()
    {
        var count = InvokeAttendance<int>("CountPresentChildren", new List<AttendanceMark>
        {
            new() { ClientId = 1, IsAbsent = false },
            new() { ClientId = 1, IsAbsent = false },
            new() { ClientId = 2, IsAbsent = true },
            new() { ClientId = 3, IsAbsent = false },
            new() { ClientId = 4, IsAbsent = true, IsMedicalExcused = true }
        });

        Assert.Equal(2, count);
    }

    [Fact]
    public void Attendance_GetClientName_joins_non_empty_child_name_parts()
    {
        var client = new Client
        {
            ChildSurname = "Иванов",
            ChildName = "Иван",
            ChildPatronymic = "Иванович"
        };

        Assert.Equal("Иванов Иван Иванович", InvokeAttendance<string>("GetClientName", client));
        Assert.Equal("", InvokeAttendance<string>("GetClientName", (object?)null));
    }

    [Fact]
    public void Trials_StatusHelpers_normalize_find_and_match_status()
    {
        var statuses = new List<TrialStatus>
        {
            new() { Id = 7, Name = "  Пришли " }
        };
        var recordById = new TrialRecord { StatusId = 7 };
        var recordByName = new TrialRecord { Status = new TrialStatus { Name = "пришли" } };

        Assert.Equal("пришли", InvokeTrials<string>("NormalizeStatus", "  Пришли "));
        Assert.Equal(7, InvokeTrials<int?>("FindStatusId", statuses, "пришли"));
        Assert.True(InvokeTrials<bool>("HasStatus", recordById, "Пришли", 7));
        Assert.True(InvokeTrials<bool>("HasStatus", recordByName, "Пришли", 99));
        Assert.False(InvokeTrials<bool>("HasStatus", new TrialRecord { StatusId = 99 }, "Пришли", 7));
    }

    [Fact]
    public void Trials_ToUtcDate_returns_date_only_utc_value()
    {
        var result = InvokeTrials<DateTime>("ToUtcDate", new DateTime(2026, 5, 17, 18, 30, 0));

        Assert.Equal(new DateTime(2026, 5, 17), result);
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    private static T InvokeAttendance<T>(string methodName, params object?[] args)
    {
        return ReflectionHelper.InvokePrivateStatic<T>(typeof(AttendanceWindow), methodName, args);
    }

    private static T InvokeTrials<T>(string methodName, params object?[] args)
    {
        return ReflectionHelper.InvokePrivateStatic<T>(typeof(TrialsWindow), methodName, args);
    }
}
