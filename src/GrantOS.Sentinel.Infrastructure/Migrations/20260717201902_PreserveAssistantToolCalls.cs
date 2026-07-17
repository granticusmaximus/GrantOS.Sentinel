using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrantOS.Sentinel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PreserveAssistantToolCalls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ToolCallsJson",
                table: "ChatMessages",
                type: "TEXT",
                nullable: true);
            migrationBuilder.Sql(Persistence.KnowledgeFtsSchema.CreateSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Persistence.KnowledgeFtsSchema.DropSql);
            migrationBuilder.DropColumn(
                name: "ToolCallsJson",
                table: "ChatMessages");
        }
    }
}
