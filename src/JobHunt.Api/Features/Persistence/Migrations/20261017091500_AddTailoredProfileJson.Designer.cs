using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace JobHunt.Api.Features.Persistence.Migrations
{
    [DbContext(typeof(JobHuntDbContext))]
    partial class AddTailoredProfileJson
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.12");

            modelBuilder.Entity("JobHunt.Api.Features.Applications.JobApplication", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Company")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("CoverLetter")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("DiscoveredAt")
                        .HasColumnType("TEXT");

                    b.Property<int?>("FitScore")
                        .HasColumnType("INTEGER");

                    b.Property<string>("FollowUpBody")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("FollowUpDraftedAt")
                        .HasColumnType("TEXT");

                    b.Property<int?>("FollowUpSendAfterDays")
                        .HasColumnType("INTEGER");

                    b.Property<string>("FollowUpSubject")
                        .HasColumnType("TEXT");

                    b.Property<string>("Location")
                        .HasColumnType("TEXT");

                    b.Property<string>("MatchSummary")
                        .HasColumnType("TEXT");

                    b.Property<int?>("OverallScore")
                        .HasColumnType("INTEGER");

                    b.Property<DateOnly?>("PostedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("SalaryBand")
                        .HasColumnType("TEXT");

                    b.Property<string>("SalaryCurrency")
                        .HasColumnType("TEXT");

                    b.Property<int?>("SalaryMax")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("SalaryMin")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SalarySource")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int?>("SeniorityScore")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("SimilarityScore")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("SkillsScore")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("SourceId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset?>("TailoredAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("TailoredBullets")
                        .HasColumnType("TEXT");

                    b.Property<string>("TailoredProfileJson")
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Url")
                        .HasColumnType("TEXT");

                    b.Property<string>("WorkModel")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Source", "SourceId")
                        .IsUnique();

                    b.ToTable("Applications");
                });

            modelBuilder.Entity("JobHunt.Api.Features.Outcomes.OutcomeRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("JobApplicationId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Note")
                        .HasColumnType("TEXT");

                    b.Property<string>("Outcome")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("RecordedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("JobApplicationId");

                    b.ToTable("Outcomes");
                });

            modelBuilder.Entity("JobHunt.Api.Features.Pii.PiiMappingRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Placeholder")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Placeholder")
                        .IsUnique();

                    b.HasIndex("Value")
                        .IsUnique();

                    b.ToTable("PiiMappings");
                });

            modelBuilder.Entity("JobHunt.Api.Features.Profile.ProfileRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Json")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Profiles");
                });
        }
    }
}
