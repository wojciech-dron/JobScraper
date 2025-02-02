﻿// <auto-generated />
using System;
using JobScraper.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace JobScraper.Migrations
{
    [DbContext(typeof(JobsDbContext))]
    partial class JobsDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.1");

            modelBuilder.Entity("JobScraper.Models.Application", b =>
                {
                    b.Property<string>("OfferUrl")
                        .HasMaxLength(500)
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("AppliedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Comments")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("TEXT");

                    b.Property<int?>("ExpectedMonthSalary")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("RespondedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("SentCv")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.HasKey("OfferUrl");

                    b.HasIndex("AppliedAt");

                    b.HasIndex("ExpectedMonthSalary");

                    b.ToTable("Applications", (string)null);
                });

            modelBuilder.Entity("JobScraper.Models.Company", b =>
                {
                    b.Property<string>("Name")
                        .HasMaxLength(255)
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasMaxLength(30000)
                        .HasColumnType("TEXT");

                    b.Property<string>("IndeedUrl")
                        .HasMaxLength(1023)
                        .HasColumnType("TEXT");

                    b.Property<string>("JjitUrl")
                        .HasMaxLength(1023)
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("ScrapedAt")
                        .HasColumnType("TEXT");

                    b.HasKey("Name");

                    b.ToTable("Companies", (string)null);
                });

            modelBuilder.Entity("JobScraper.Models.JobOffer", b =>
                {
                    b.Property<string>("OfferUrl")
                        .HasMaxLength(500)
                        .HasColumnType("TEXT");

                    b.Property<string>("AgeInfo")
                        .HasMaxLength(32)
                        .HasColumnType("TEXT");

                    b.Property<string>("ApplyUrl")
                        .HasMaxLength(2048)
                        .HasColumnType("TEXT");

                    b.Property<string>("Comments")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("TEXT");

                    b.Property<string>("CompanyName")
                        .HasMaxLength(255)
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasMaxLength(5000)
                        .HasColumnType("TEXT");

                    b.Property<string>("DetailsScrapeStatus")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(24)
                        .HasColumnType("TEXT")
                        .HasDefaultValue("ToScrape");

                    b.Property<string>("HtmlPath")
                        .HasMaxLength(1024)
                        .HasColumnType("TEXT");

                    b.Property<string>("Location")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.PrimitiveCollection<string>("MyKeywords")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.PrimitiveCollection<string>("OfferKeywords")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Origin")
                        .IsRequired()
                        .HasMaxLength(24)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("PublishedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Salary")
                        .HasMaxLength(128)
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ScrapedAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("ScreenShotPath")
                        .HasMaxLength(1024)
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("TEXT");

                    b.HasKey("OfferUrl");

                    b.HasIndex("AgeInfo");

                    b.HasIndex("CompanyName");

                    b.HasIndex("DetailsScrapeStatus");

                    b.HasIndex("Location");

                    b.HasIndex("Salary");

                    b.HasIndex("ScrapedAt");

                    b.ToTable("JobOffers", (string)null);
                });

            modelBuilder.Entity("JobScraper.Models.Application", b =>
                {
                    b.HasOne("JobScraper.Models.JobOffer", "JobOffer")
                        .WithOne("Application")
                        .HasForeignKey("JobScraper.Models.Application", "OfferUrl")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("JobOffer");
                });

            modelBuilder.Entity("JobScraper.Models.JobOffer", b =>
                {
                    b.HasOne("JobScraper.Models.Company", "Company")
                        .WithMany("JobOffers")
                        .HasForeignKey("CompanyName");

                    b.Navigation("Company");
                });

            modelBuilder.Entity("JobScraper.Models.Company", b =>
                {
                    b.Navigation("JobOffers");
                });

            modelBuilder.Entity("JobScraper.Models.JobOffer", b =>
                {
                    b.Navigation("Application");
                });
#pragma warning restore 612, 618
        }
    }
}
