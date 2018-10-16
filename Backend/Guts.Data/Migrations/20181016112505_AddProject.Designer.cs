﻿// <auto-generated />
using System;
using Guts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Guts.Data.Migrations
{
    [DbContext(typeof(GutsContext))]
    [Migration("20181016112505_AddProject")]
    partial class AddProject
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.4-rtm-31024")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Guts.Domain.Assignment", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Discriminator")
                        .IsRequired();

                    b.HasKey("Id");

                    b.ToTable("Assignments");

                    b.HasDiscriminator<string>("Discriminator").HasValue("Assignment");
                });

            modelBuilder.Entity("Guts.Domain.Chapter", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("CourseId");

                    b.Property<int>("Number");

                    b.Property<int>("PeriodId");

                    b.HasKey("Id");

                    b.HasIndex("CourseId");

                    b.HasIndex("PeriodId");

                    b.ToTable("Chapters");
                });

            modelBuilder.Entity("Guts.Domain.Course", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(20);

                    b.Property<string>("Name")
                        .IsRequired();

                    b.HasKey("Id");

                    b.ToTable("Courses");
                });

            modelBuilder.Entity("Guts.Domain.Period", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Description")
                        .IsRequired();

                    b.Property<DateTime>("From");

                    b.Property<DateTime>("Until");

                    b.HasKey("Id");

                    b.ToTable("Periods");
                });

            modelBuilder.Entity("Guts.Domain.ProjectTeam", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("ProjectId");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.ToTable("ProjectTeam");
                });

            modelBuilder.Entity("Guts.Domain.ProjectTeamUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("ProjectTeamId");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("ProjectTeamId");

                    b.HasIndex("UserId");

                    b.ToTable("ProjectTeamUser");
                });

            modelBuilder.Entity("Guts.Domain.Role", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("Name")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasName("RoleNameIndex");

                    b.ToTable("Roles");
                });

            modelBuilder.Entity("Guts.Domain.Test", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AssignmentId");

                    b.Property<string>("TestName")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("AssignmentId");

                    b.ToTable("Tests");
                });

            modelBuilder.Entity("Guts.Domain.TestResult", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreateDateTime");

                    b.Property<string>("Message");

                    b.Property<bool>("Passed");

                    b.Property<int>("TestId");

                    b.Property<int>("TestRunId");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("TestId");

                    b.HasIndex("TestRunId");

                    b.HasIndex("UserId");

                    b.ToTable("TestResults");
                });

            modelBuilder.Entity("Guts.Domain.TestRun", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AssignmentId");

                    b.Property<DateTime>("CreateDateTime");

                    b.Property<string>("SourceCode");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("AssignmentId");

                    b.HasIndex("UserId");

                    b.ToTable("TestRuns");
                });

            modelBuilder.Entity("Guts.Domain.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AccessFailedCount");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("Email")
                        .HasMaxLength(256);

                    b.Property<bool>("EmailConfirmed");

                    b.Property<string>("FirstName")
                        .IsRequired();

                    b.Property<string>("LastName")
                        .IsRequired();

                    b.Property<bool>("LockoutEnabled");

                    b.Property<DateTimeOffset?>("LockoutEnd");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256);

                    b.Property<string>("PasswordHash");

                    b.Property<string>("PhoneNumber");

                    b.Property<bool>("PhoneNumberConfirmed");

                    b.Property<string>("SecurityStamp");

                    b.Property<bool>("TwoFactorEnabled");

                    b.Property<string>("UserName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasName("UserNameIndex");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<int>("RoleId");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("RoleClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
                {
                    b.Property<string>("LoginProvider");

                    b.Property<string>("ProviderKey");

                    b.Property<string>("ProviderDisplayName");

                    b.Property<int>("UserId");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("UserLogins");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<int>", b =>
                {
                    b.Property<int>("UserId");

                    b.Property<int>("RoleId");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("UserRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
                {
                    b.Property<int>("UserId");

                    b.Property<string>("LoginProvider");

                    b.Property<string>("Name");

                    b.Property<string>("Value");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("UserTokens");
                });

            modelBuilder.Entity("Guts.Domain.Exercise", b =>
                {
                    b.HasBaseType("Guts.Domain.Assignment");

                    b.Property<int>("ChapterId");

                    b.Property<int>("Number");

                    b.HasIndex("ChapterId");

                    b.ToTable("Exercise");

                    b.HasDiscriminator().HasValue("Exercise");
                });

            modelBuilder.Entity("Guts.Domain.Project", b =>
                {
                    b.HasBaseType("Guts.Domain.Assignment");

                    b.Property<int>("CourseId");

                    b.Property<string>("Name");

                    b.Property<int>("PeriodId");

                    b.HasIndex("CourseId");

                    b.HasIndex("PeriodId");

                    b.ToTable("Project");

                    b.HasDiscriminator().HasValue("Project");
                });

            modelBuilder.Entity("Guts.Domain.Chapter", b =>
                {
                    b.HasOne("Guts.Domain.Course", "Course")
                        .WithMany()
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Guts.Domain.Period", "Period")
                        .WithMany()
                        .HasForeignKey("PeriodId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Guts.Domain.ProjectTeam", b =>
                {
                    b.HasOne("Guts.Domain.Project", "Project")
                        .WithMany("Teams")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Guts.Domain.ProjectTeamUser", b =>
                {
                    b.HasOne("Guts.Domain.ProjectTeam", "ProjectTeam")
                        .WithMany("TeamUsers")
                        .HasForeignKey("ProjectTeamId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Guts.Domain.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Guts.Domain.Test", b =>
                {
                    b.HasOne("Guts.Domain.Assignment", "Assignment")
                        .WithMany("Tests")
                        .HasForeignKey("AssignmentId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Guts.Domain.TestResult", b =>
                {
                    b.HasOne("Guts.Domain.Test", "Test")
                        .WithMany("Results")
                        .HasForeignKey("TestId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Guts.Domain.TestRun", "TestRun")
                        .WithMany("TestResults")
                        .HasForeignKey("TestRunId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Guts.Domain.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Guts.Domain.TestRun", b =>
                {
                    b.HasOne("Guts.Domain.Assignment", "Assignment")
                        .WithMany("TestRuns")
                        .HasForeignKey("AssignmentId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Guts.Domain.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
                {
                    b.HasOne("Guts.Domain.Role")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
                {
                    b.HasOne("Guts.Domain.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
                {
                    b.HasOne("Guts.Domain.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<int>", b =>
                {
                    b.HasOne("Guts.Domain.Role")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Guts.Domain.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
                {
                    b.HasOne("Guts.Domain.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Guts.Domain.Exercise", b =>
                {
                    b.HasOne("Guts.Domain.Chapter", "Chapter")
                        .WithMany("Exercises")
                        .HasForeignKey("ChapterId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Guts.Domain.Project", b =>
                {
                    b.HasOne("Guts.Domain.Course", "Course")
                        .WithMany()
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Guts.Domain.Period", "Period")
                        .WithMany()
                        .HasForeignKey("PeriodId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
