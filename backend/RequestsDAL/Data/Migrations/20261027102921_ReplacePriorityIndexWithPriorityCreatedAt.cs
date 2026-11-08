using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequestsDAL.Data.Migrations
{
    public partial class ReplacePriorityIndexWithPriorityCreatedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requests_Priority",
                table: "Requests");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_Priority_CreatedAt",
                table: "Requests",
                columns: new[] { "Priority", "CreatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requests_Priority_CreatedAt",
                table: "Requests");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_Priority",
                table: "Requests",
                column: "Priority");
        }
    }
}
