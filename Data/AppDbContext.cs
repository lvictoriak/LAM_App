using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LAM_App.Models;
using Microsoft.EntityFrameworkCore;

namespace LAM_App.Data
{
    public class AppDbContext : DbContext
    {
        //создаем DbSet для каждой таблицы
        public DbSet<Client> clients { get; set; }
        public DbSet<IncomeCategory> incomes { get; set; }
        public DbSet<PaymentLog> paymentLogs { get; set; }
        public DbSet<PaymentType> paymentTypes { get; set; }
        public DbSet<Studio> studios { get; set; }
        public DbSet<Style> styles { get; set; }
        public DbSet<Teacher> teachers { get; set; }
        public DbSet<TrialStatus> trialStatuses { get; set; }
        public DbSet<TrialRecord> trials { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Студия
            modelBuilder.Entity<Studio>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("studio_id");
                entity.Property(e => e.Name).HasColumnName("studio_name").HasMaxLength(100).IsRequired();
                entity.Property(e => e.Address).HasColumnName("address").HasMaxLength(255);
                entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
            });

            //Тренеры
            modelBuilder.Entity<Teacher>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("teacher_id");
                entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(150).IsRequired();
                entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
                entity.Property(e => e.Age).HasColumnName("age");
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(100);
                entity.Property(e => e.DanceExperience).HasColumnName("dance_experience");
                entity.Property(e => e.Comment).HasColumnName("to_comment");
                entity.Property(e => e.BirthDate).HasColumnName("date_birth");

                // Связь: Преподаватель - Студия
                entity.HasOne(e => e.Studio)
                      .WithMany(s => s.Teachers)
                      .HasForeignKey(e => e.StudioId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //Стили танцев
            modelBuilder.Entity<Style>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("style_id");
                entity.Property(e => e.Name).HasColumnName("style_name").HasMaxLength(100).IsRequired();
                entity.Property(e => e.ScheduleOptions).HasColumnName("schedule_options");
                entity.Property(e => e.TeacherId).HasColumnName("teacher");
                entity.Property(e => e.Comment).HasColumnName("to_comment");

                //Связь: Стили - Студия
                entity.HasOne(e => e.Studio)
                      .WithMany(s => s.Styles)
                      .HasForeignKey(e => e.StudioId)
                      .OnDelete(DeleteBehavior.Restrict);

                //Связь: Направление - Преподаватель
                entity.HasOne<Teacher>()
                      .WithMany()
                      .HasForeignKey(e => e.TeacherId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            //Клиенты
            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("client_id");
                entity.Property(e => e.ParentName).HasColumnName("parent_name").HasMaxLength(150).IsRequired();
                entity.Property(e => e.ParentPhone).HasColumnName("parent_phone").HasMaxLength(20).IsRequired();
                entity.Property(e => e.ChildSurname).HasColumnName("child_surname").HasMaxLength(100).IsRequired();
                entity.Property(e => e.ChildName).HasColumnName("child_name").HasMaxLength(100).IsRequired();
                entity.Property(e => e.ChildPatronymic).HasColumnName("child_patronymic").HasMaxLength(100);
                entity.Property(e => e.BirthDate).HasColumnName("birth_date");
                entity.Property(e => e.Age).HasColumnName("age");
                entity.Property(e => e.Shift).HasColumnName("shift").HasMaxLength(50);
                entity.Property(e => e.StyleId).HasColumnName("style_name"); // Да, в БД поле называется так
                entity.Property(e => e.Comment).HasColumnName("to_comment");

                //Связь: Клиент - Направление
                entity.HasOne(e => e.Style)
                      .WithMany(s => s.Clients)
                      .HasForeignKey(e => e.StyleId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            //Статусы пробных занятий
            modelBuilder.Entity<TrialStatus>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("status_id");
                entity.Property(e => e.Name).HasColumnName("status_name").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Comment).HasColumnName("to_comment");
            });

            //Записи на пробные занятия
            modelBuilder.Entity<TrialRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("trial_id");
                entity.Property(e => e.ParentName).HasColumnName("parent_name").HasMaxLength(150).IsRequired();
                entity.Property(e => e.ParentPhone).HasColumnName("parent_phone").HasMaxLength(20).IsRequired();
                entity.Property(e => e.ChildName).HasColumnName("child_name").HasMaxLength(150).IsRequired();
                entity.Property(e => e.ChildAge).HasColumnName("child_age");
                entity.Property(e => e.TrialDate).HasColumnName("trial_date");
                entity.Property(e => e.RecordDate).HasColumnName("record_date");
                entity.Property(e => e.Comment).HasColumnName("to_comment");

                //Связь: Запись - Статус
                entity.HasOne(e => e.Status)
                      .WithMany(s => s.TrialRecords)
                      .HasForeignKey(e => e.StatusId)
                      .OnDelete(DeleteBehavior.Restrict);

                //Связь: Запись - Направление
                entity.HasOne(e => e.Style)
                      .WithMany(s => s.TrialRecords)
                      .HasForeignKey(e => e.StyleId)
                      .OnDelete(DeleteBehavior.Restrict);

                //  Связь: Запись - Преподаватель 
                entity.HasOne(e => e.Instructor)
                      .WithMany(t => t.TrialRecords)
                      .HasForeignKey(e => e.InstructorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            //Виды оплат
            modelBuilder.Entity<PaymentType>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("payment_type_id");
                entity.Property(e => e.Name).HasColumnName("type_name").HasMaxLength(50).IsRequired();
            });

            // Статьи доходов
            modelBuilder.Entity<IncomeCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("category_id");
                entity.Property(e => e.Name).HasColumnName("category_name").HasMaxLength(100).IsRequired();
            });

            // Журнал платежей (Кассовая книга)
            modelBuilder.Entity<PaymentLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("payment_id");
                entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
                entity.Property(e => e.Income).HasColumnName("income").HasColumnType("numeric(10,2)");
                entity.Property(e => e.Expense).HasColumnName("expense").HasColumnType("numeric(10,2)");
                entity.Property(e => e.PaymentTypeId).HasColumnName("payment_type_id");
                entity.Property(e => e.CategoryId).HasColumnName("category_id");
                entity.Property(e => e.StyleId).HasColumnName("style_id");
                entity.Property(e => e.Contractor).HasColumnName("contractor").HasMaxLength(150);
                entity.Property(e => e.Comment).HasColumnName("to_comment");
                entity.Property(e => e.ExtraInfo).HasColumnName("extra_info");

                //Связь: Платёж - Вид оплаты
                entity.HasOne(e => e.PaymentType)
                      .WithMany(p => p.PaymentLogs)
                      .HasForeignKey(e => e.PaymentTypeId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Связь: Платёж - Статье дохода
                entity.HasOne(e => e.IncomeCategory)
                      .WithMany(c => c.PaymentLogs)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.SetNull);

                //Связь: Платёж - Направлению
                entity.HasOne(e => e.Style)
                      .WithMany(s => s.PaymentLogs)
                      .HasForeignKey(e => e.StyleId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            //отключаем каскадное удаление по умолчанию для безопасности
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var foreignKey in entityType.GetForeignKeys())
                {
                    if (foreignKey.DeleteBehavior == DeleteBehavior.Cascade && foreignKey.IsRequired)
                    {
                        foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
                    }
                }
            }
        }

        //метод для инициализации базы данных на всякий случай
        public static void InitializeDatabase(AppDbContext context)
        {
            context.Database.EnsureCreated();
        }
    }
}
