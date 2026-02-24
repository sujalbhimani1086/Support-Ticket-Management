using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BackendApiExam.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$svb1Bd2HVCU4EMD4w/fI/eFzWQCww4NcnpknSheFNJcnkNDjVjtAy");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$rG0hVX5hE0/8uY0scmhhGOWH4Ugt3jXiD4ytYDLXFzCJYUvbEodJG");
        }
    }
}
