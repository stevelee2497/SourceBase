using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SourceBase.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpCodeExpiration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OtpCodeExpiresOn",
                table: "Users",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OtpCodeExpiresOn",
                table: "Users");
        }
    }
}
