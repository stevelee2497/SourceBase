using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Migrations
{
    /// <inheritdoc />
    public partial class AddedAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "TodoItems",
                type: "nvarchar(max)",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 97);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "TodoItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified))
                .Annotation("Relational:ColumnOrder", 96);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "TodoItems",
                type: "datetime2",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 100);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "TodoItems",
                type: "nvarchar(max)",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 99);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "TodoItems",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified))
                .Annotation("Relational:ColumnOrder", 98);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "TodoItems");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "TodoItems");
        }
    }
}
