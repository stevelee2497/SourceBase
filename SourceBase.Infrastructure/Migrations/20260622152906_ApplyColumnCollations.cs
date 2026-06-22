using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SourceBase.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ApplyColumnCollations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Wallets",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Wallets",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "Wallets",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Wallets",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Wallets",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "Users",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Users",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SecurityStamp",
                table: "Users",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "OtpCode",
                table: "Users",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Users",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Users",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AvatarUrl",
                table: "Users",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Transfers",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Transfers",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Transfers",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Transactions",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Transactions",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Transactions",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "TodoLists",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "TodoLists",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "TodoLists",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "TodoItems",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "TodoItems",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "TodoItems",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "TimeSheets",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Project",
                table: "TimeSheets",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "TimeSheets",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Roles",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Roles",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Roles",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Notifications",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Notifications",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "Notifications",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                table: "Notifications",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Notifications",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "Icons",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Icons",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Icons",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Icons",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "GoldPrices",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "GoldPrices",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "To",
                table: "Emails",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "Emails",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Body",
                table: "Emails",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Categories",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "Categories",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Categories",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Original",
                table: "AuditHistories",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "AuditHistories",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                table: "AuditHistories",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Current",
                table: "AuditHistories",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Changes",
                table: "AuditHistories",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                table: "AuditHistories",
                type: "text",
                nullable: true,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditHistories",
                type: "text",
                nullable: false,
                collation: "case_insensitive",
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Wallets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Wallets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "Wallets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Wallets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Wallets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "SecurityStamp",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "OtpCode",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "AvatarUrl",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Transfers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Transfers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Transfers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Transactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Transactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Transactions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "TodoLists",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "TodoLists",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "TodoLists",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "TodoItems",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "TodoItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "TodoItems",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "TimeSheets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Project",
                table: "TimeSheets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "TimeSheets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Roles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Roles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Roles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Notifications",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Notifications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "Notifications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                table: "Notifications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Notifications",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "Icons",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Icons",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Icons",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Icons",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "GoldPrices",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "GoldPrices",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "To",
                table: "Emails",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Subject",
                table: "Emails",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Body",
                table: "Emails",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "Categories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "Categories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Categories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Original",
                table: "AuditHistories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "EntityType",
                table: "AuditHistories",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "EntityId",
                table: "AuditHistories",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Current",
                table: "AuditHistories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Changes",
                table: "AuditHistories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                table: "AuditHistories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldCollation: "case_insensitive");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "AuditHistories",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldCollation: "case_insensitive");
        }
    }
}
