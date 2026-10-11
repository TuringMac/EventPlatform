using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class user_scheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                schema: "public",
                table: "bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "users",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Login = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_UserId",
                schema: "public",
                table: "bookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Login",
                schema: "public",
                table: "users",
                column: "Login",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_users_UserId",
                schema: "public",
                table: "bookings",
                column: "UserId",
                principalSchema: "public",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_users_UserId",
                schema: "public",
                table: "bookings");

            migrationBuilder.DropTable(
                name: "users",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_bookings_UserId",
                schema: "public",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "public",
                table: "bookings");
        }
    }
}
