using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedAuditTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AuditHistories");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "AuditHistories");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "AuditHistories");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "AuditHistories",
                newName: "Author");

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                table: "AuditHistories",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .OldAnnotation("Relational:ColumnOrder", 99);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Author",
                table: "AuditHistories",
                newName: "UpdatedBy");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "AuditHistories",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 99);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AuditHistories",
                type: "nvarchar(max)",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 97);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "AuditHistories",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified))
                .Annotation("Relational:ColumnOrder", 96);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "AuditHistories",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified))
                .Annotation("Relational:ColumnOrder", 98);
        }
    }
}
