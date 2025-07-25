﻿using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace fitPass.Models;

public partial class GymManagementContext : DbContext
{
    public GymManagementContext()
    {
    }

    public GymManagementContext(DbContextOptions<GymManagementContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Attach> Attaches { get; set; }

    public virtual DbSet<CheckInRecord> CheckInRecords { get; set; }

    public virtual DbSet<Coach> Coaches { get; set; }

    public virtual DbSet<CoachTime> CoachTimes { get; set; }

    public virtual DbSet<CourseSchedule> CourseSchedules { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<FeedbackComment> FeedbackComments { get; set; }

    public virtual DbSet<Inbody> Inbodies { get; set; }

    public virtual DbSet<News> News { get; set; }

    public virtual DbSet<PointLog> PointLogs { get; set; }

    public virtual DbSet<PrivateSession> PrivateSessions { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<SubscriptionLog> SubscriptionLogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=.\\sqlexpress;Database=GymManagement;Integrated Security=True;Encrypt=False;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.MemberId).HasName("PK__Account__0CF04B1855C0EEE1");

            entity.ToTable("Account");

            entity.HasIndex(e => e.Email, "UQ__Account__A9D105345CB1B271").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.JoinDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(256);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Type).HasDefaultValue(1);
        });

        modelBuilder.Entity<Attach>(entity =>
        {
            entity.ToTable("Attach");

            entity.Property(e => e.AttachId).HasColumnName("AttachID");
            entity.Property(e => e.FeedbackId).HasColumnName("FeedbackID");
            entity.Property(e => e.Photo).HasColumnName("photo");

            entity.HasOne(d => d.Feedback).WithMany(p => p.Attaches)
                .HasForeignKey(d => d.FeedbackId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attach_Feedback");
        });

        modelBuilder.Entity<CheckInRecord>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__CheckInR__FBDF78E936F59788");

            entity.Property(e => e.Device).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Member).WithMany(p => p.CheckInRecords)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CheckInRecord__Member");
        });

        modelBuilder.Entity<Coach>(entity =>
        {
            entity.HasKey(e => e.CoachId).HasName("PK__Coaches__F411D9413A5C7AC8");

            entity.HasIndex(e => e.AccountId, "UQ__Coaches__349DA5A7437000D9").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.Specialty).HasMaxLength(100);

            entity.HasOne(d => d.Account).WithOne(p => p.Coach)
                .HasForeignKey<Coach>(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Coaches_Account");
        });

        modelBuilder.Entity<CoachTime>(entity =>
        {
            entity.HasKey(e => e.TimeId);

            entity.ToTable("CoachTime");

            entity.Property(e => e.TimeId).HasColumnName("TimeID");
            entity.Property(e => e.CoachId).HasColumnName("CoachID");

            entity.HasOne(d => d.Coach).WithMany(p => p.CoachTimes)
                .HasForeignKey(d => d.CoachId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CoachTime_Coaches");
        });

        modelBuilder.Entity<CourseSchedule>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__CourseSc__C92D71A7DA15C32C");

            entity.ToTable("CourseSchedule");

            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.IsCanceled).HasDefaultValue(false);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(100);

            entity.HasOne(d => d.Coach).WithMany(p => p.CourseSchedules)
                .HasForeignKey(d => d.CoachId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CourseSchedule__Coaches");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("Feedback");

            entity.Property(e => e.FeedbackId).HasColumnName("FeedbackID");
            entity.Property(e => e.CreatedAt).HasColumnName("Created_at");
            entity.Property(e => e.Subject).HasMaxLength(50);

            entity.HasOne(d => d.Member).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Feedback_Account");
        });

        modelBuilder.Entity<FeedbackComment>(entity =>
        {
            entity.HasKey(e => e.CommentId);

            entity.ToTable("Feedback_Comments");

            entity.Property(e => e.CommentId).HasColumnName("CommentID");
            entity.Property(e => e.CommentText).HasColumnName("Comment_text");
            entity.Property(e => e.CreatedAt).HasColumnName("Created_at");
            entity.Property(e => e.FeedbackId).HasColumnName("FeedbackID");

            entity.HasOne(d => d.Feedback).WithMany(p => p.FeedbackComments)
                .HasForeignKey(d => d.FeedbackId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Feedback_Comments_Feedback");
        });

        modelBuilder.Entity<Inbody>(entity =>
        {
            entity.HasKey(e => e.InBodyId).HasName("PK__Inbody__24B7C1B4B008D0EF");

            entity.ToTable("Inbody");

            entity.Property(e => e.InBodyId).HasColumnName("InBodyID");
            entity.Property(e => e.Bmr).HasColumnName("BMR");
            entity.Property(e => e.GoalNote).HasMaxLength(300);
            entity.Property(e => e.Note).HasMaxLength(300);

            entity.HasOne(d => d.Member).WithMany(p => p.Inbodies)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inbody__MemberId");
        });

        modelBuilder.Entity<News>(entity =>
        {
            entity.HasKey(e => e.NewsId).HasName("PK__News__954EBDF3EC43CB14");

            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.IsVisible).HasDefaultValue(true);
            entity.Property(e => e.PublishTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Showtime).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<PointLog>(entity =>
        {
            entity.Property(e => e.PointLogId).HasColumnName("PointLogID");
            entity.Property(e => e.Detail).HasMaxLength(50);
            entity.Property(e => e.MemberId).HasColumnName("MemberID");

            entity.HasOne(d => d.Member).WithMany(p => p.PointLogs)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PointLogs_Account");
        });

        modelBuilder.Entity<PrivateSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__PrivateS__C9F492909AC351C2");

            entity.ToTable("PrivateSession");

            entity.Property(e => e.CreateTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.Note).HasMaxLength(300);
            entity.Property(e => e.Status).HasDefaultValue(1);
            entity.Property(e => e.TimeId).HasColumnName("TimeID");

            entity.HasOne(d => d.Time).WithMany(p => p.PrivateSessions)
                .HasForeignKey(d => d.TimeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PrivateSession_CoachTime");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.ReservationId).HasName("PK__Reservat__B7EE5F24D77550C5");

            entity.Property(e => e.CreateTime).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Note).HasMaxLength(300);

            entity.HasOne(d => d.Course).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reservati__Cours__5BE2A6F2");

            entity.HasOne(d => d.Member).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reservation__Member");
        });

        modelBuilder.Entity<SubscriptionLog>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId);

            entity.Property(e => e.SubscriptionId).HasColumnName("SubscriptionID");
            entity.Property(e => e.MemberId).HasColumnName("MemberID");
            entity.Property(e => e.SubscribedTime).HasColumnName("subscribedTime");
            entity.Property(e => e.SubscriptionType).HasMaxLength(50);

            entity.HasOne(d => d.Member).WithMany(p => p.SubscriptionLogs)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SubscriptionLogs_Account");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
