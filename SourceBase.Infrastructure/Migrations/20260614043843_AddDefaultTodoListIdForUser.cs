using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SourceBase.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultTodoListIdForUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DefaultTodoListId",
                table: "Users",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultTodoListId",
                table: "Users");
        }
    }
}
