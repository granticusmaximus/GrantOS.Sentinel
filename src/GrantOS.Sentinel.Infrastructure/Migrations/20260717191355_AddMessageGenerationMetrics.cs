using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrantOS.Sentinel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageGenerationMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "EvalDurationNanoseconds",
                table: "ChatMessages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LoadDurationNanoseconds",
                table: "ChatMessages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PromptEvalDurationNanoseconds",
                table: "ChatMessages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromptTokenCount",
                table: "ChatMessages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TotalDurationNanoseconds",
                table: "ChatMessages",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EvalDurationNanoseconds",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "LoadDurationNanoseconds",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "PromptEvalDurationNanoseconds",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "PromptTokenCount",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "TotalDurationNanoseconds",
                table: "ChatMessages");
        }
    }
}
