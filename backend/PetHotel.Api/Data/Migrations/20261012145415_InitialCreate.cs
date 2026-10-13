using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PetHotel.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Owners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Owners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RoomType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Species = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Breed = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CareInstructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pets_Owners_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Owners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PetId = table.Column<int>(type: "integer", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    ScheduledCheckIn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ScheduledCheckOut = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActualCheckIn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActualCheckOut = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stays", x => x.Id);
                    table.CheckConstraint("CK_Stays_ActualDates", "\"ActualCheckOut\" IS NULL OR (\"ActualCheckIn\" IS NOT NULL AND \"ActualCheckOut\" >= \"ActualCheckIn\")");
                    table.CheckConstraint("CK_Stays_ScheduledDates", "\"ScheduledCheckOut\" > \"ScheduledCheckIn\"");
                    table.CheckConstraint("CK_Stays_Status", "\"Status\" IN ('Booked', 'CheckedIn', 'CheckedOut', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_Stays_Pets_PetId",
                        column: x => x.PetId,
                        principalTable: "Pets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Stays_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CareRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StayId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareRecords", x => x.Id);
                    table.CheckConstraint("CK_CareRecords_Completion", "(\"IsCompleted\" AND \"CompletedAt\" IS NOT NULL) OR (NOT \"IsCompleted\" AND \"CompletedAt\" IS NULL)");
                    table.CheckConstraint("CK_CareRecords_Description", "length(btrim(\"Description\")) > 0");
                    table.ForeignKey(
                        name: "FK_CareRecords_Stays_StayId",
                        column: x => x.StayId,
                        principalTable: "Stays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareRecords_ScheduledAt_IsCompleted",
                table: "CareRecords",
                columns: new[] { "ScheduledAt", "IsCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CareRecords_StayId_ScheduledAt",
                table: "CareRecords",
                columns: new[] { "StayId", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Pets_OwnerId",
                table: "Pets",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_RoomNumber",
                table: "Rooms",
                column: "RoomNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stays_PetId_ScheduledCheckIn",
                table: "Stays",
                columns: new[] { "PetId", "ScheduledCheckIn" });

            migrationBuilder.CreateIndex(
                name: "IX_Stays_RoomId_ScheduledCheckIn_ScheduledCheckOut",
                table: "Stays",
                columns: new[] { "RoomId", "ScheduledCheckIn", "ScheduledCheckOut" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareRecords");

            migrationBuilder.DropTable(
                name: "Stays");

            migrationBuilder.DropTable(
                name: "Pets");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "Owners");
        }
    }
}
