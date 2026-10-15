using Microsoft.EntityFrameworkCore;
using PetHotel.Api.Models;

namespace PetHotel.Api.Data;

public class PetHotelDbContext(DbContextOptions<PetHotelDbContext> options) : DbContext(options)
{
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Stay> Stays => Set<Stay>();
    public DbSet<CareRecord> CareRecords => Set<CareRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Owner>(entity =>
        {
            entity.Property(owner => owner.FullName).HasMaxLength(100).IsRequired();
            entity.Property(owner => owner.Phone).HasMaxLength(30).IsRequired();
            entity.Property(owner => owner.Email).HasMaxLength(254);
        });

        modelBuilder.Entity<Pet>(entity =>
        {
            entity.Property(pet => pet.Name).HasMaxLength(100).IsRequired();
            entity.Property(pet => pet.Species).HasMaxLength(50).IsRequired();
            entity.Property(pet => pet.Breed).HasMaxLength(100);
            entity.Property(pet => pet.CareInstructions).HasMaxLength(1000);
            entity.HasOne(pet => pet.Owner).WithMany(owner => owner.Pets)
                .HasForeignKey(pet => pet.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.Property(room => room.RoomNumber).HasMaxLength(20).IsRequired();
            entity.Property(room => room.RoomType).HasMaxLength(50).IsRequired();
            entity.HasIndex(room => room.RoomNumber).IsUnique();
        });

        modelBuilder.Entity<Stay>(entity =>
        {
            entity.Property(stay => stay.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(stay => stay.TotalCharge).HasPrecision(10, 2);
            entity.HasOne(stay => stay.Pet).WithMany(pet => pet.Stays)
                .HasForeignKey(stay => stay.PetId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(stay => stay.Room).WithMany(room => room.Stays)
                .HasForeignKey(stay => stay.RoomId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(stay => new { stay.RoomId, stay.ScheduledCheckIn, stay.ScheduledCheckOut });
            entity.HasIndex(stay => new { stay.PetId, stay.ScheduledCheckIn });
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Stays_ScheduledDates",
                    "\"ScheduledCheckOut\" > \"ScheduledCheckIn\"");
                table.HasCheckConstraint("CK_Stays_ActualDates",
                    "\"ActualCheckOut\" IS NULL OR (\"ActualCheckIn\" IS NOT NULL AND \"ActualCheckOut\" >= \"ActualCheckIn\")");
                table.HasCheckConstraint("CK_Stays_Status",
                    "\"Status\" IN ('Booked', 'CheckedIn', 'CheckedOut', 'Cancelled')");
            });
        });

        modelBuilder.Entity<CareRecord>(entity =>
        {
            entity.Property(record => record.Description).HasMaxLength(500).IsRequired();
            entity.HasOne(record => record.Stay).WithMany(stay => stay.CareRecords)
                .HasForeignKey(record => record.StayId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(record => new { record.StayId, record.ScheduledAt });
            entity.HasIndex(record => new { record.ScheduledAt, record.IsCompleted });
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_CareRecords_Description",
                    "length(btrim(\"Description\")) > 0");
                table.HasCheckConstraint("CK_CareRecords_Completion",
                    "(\"IsCompleted\" AND \"CompletedAt\" IS NOT NULL) OR (NOT \"IsCompleted\" AND \"CompletedAt\" IS NULL)");
            });
        });
    }
}
