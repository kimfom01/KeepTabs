﻿// <auto-generated />
using System;
using KeepTabs.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KeepTabs.Database.Migrations;

[DbContext(typeof(KeepTabsDbContext))]
[Migration("20250513021437_InitialMigrations")]
partial class InitialMigrations
{
    /// <inheritdoc />
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "9.0.4")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("KeepTabs.Entities.JobTracking", b =>
        {
            b.Property<string>("Id")
                .HasColumnType("text");

            b.Property<string>("JobTitle")
                .IsRequired()
                .HasColumnType("text");

            b.Property<string>("JobUrl")
                .IsRequired()
                .HasColumnType("text");

            b.Property<int>("RequestInterval")
                .HasColumnType("integer");

            b.Property<Guid>("ResponseStatusId")
                .HasColumnType("uuid");

            b.HasKey("Id");

            b.HasIndex("ResponseStatusId");

            b.ToTable("JobTrackings");
        });

        modelBuilder.Entity("KeepTabs.Entities.ResponseStatus", b =>
        {
            b.Property<Guid>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("uuid");

            b.Property<double>("ResponseLatency")
                .HasColumnType("double precision");

            b.Property<int>("RunningState")
                .HasColumnType("integer");

            b.Property<string>("RunningStateName")
                .IsRequired()
                .HasColumnType("text");

            b.Property<int?>("StatusCode")
                .HasColumnType("integer");

            b.Property<string>("StatusMessage")
                .HasColumnType("text");

            b.HasKey("Id");

            b.ToTable("ResponseStatus");
        });

        modelBuilder.Entity("KeepTabs.Entities.JobTracking", b =>
        {
            b.HasOne("KeepTabs.Entities.ResponseStatus", "ResponseStatus")
                .WithMany()
                .HasForeignKey("ResponseStatusId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.Navigation("ResponseStatus");
        });
#pragma warning restore 612, 618
    }
}