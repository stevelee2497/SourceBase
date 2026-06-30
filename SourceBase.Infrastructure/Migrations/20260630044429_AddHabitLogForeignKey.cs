using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SourceBase.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHabitLogForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""HabitLogs"" ALTER COLUMN ""HabitId"" TYPE uuid USING ""HabitId""::uuid;");

            migrationBuilder.CreateIndex(
                name: "IX_HabitLogs_HabitId",
                table: "HabitLogs",
                column: "HabitId");

            migrationBuilder.AddForeignKey(
                name: "FK_HabitLogs_Habits_HabitId",
                table: "HabitLogs",
                column: "HabitId",
                principalTable: "Habits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HabitLogs_Habits_HabitId",
                table: "HabitLogs");

            migrationBuilder.DropIndex(
                name: "IX_HabitLogs_HabitId",
                table: "HabitLogs");

            migrationBuilder.AlterColumn<string>(
                name: "HabitId",
                table: "HabitLogs",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
